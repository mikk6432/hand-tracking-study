using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HelmetMainProcess: ExperimentNetworkClient
{
    private int participantId = 1;
    private bool leftHanded = false;

    private ExperimentManager.RunConfig[] _runConfigs;
    private int currentRunConfigIndex; // for example 4, means that previous 4 runs (0,1,2,3) were fulfilled already
    private RunStage currentRunStage = RunStage.Idle; // is this run already in progress or not

    [SerializeField] private ExperimentManager experimentManager;
    
    [Serializable]
    public enum RunStage
    {
        Idle, // "prepare" command was not given by server yet. ExperimentManager is in Idle state, actually
        Preparing, // yellow flag. "prepare" was given. Track(and light) were positioned, targets were shown, but not selectable yet
        Running // ExperimentManager is running training/trial. If training, then stop will be called by server command. Otherwise, when participant will finish selecting all targets
    }

    public static ExperimentManager.RunConfig[] GenerateRunConfigs(int participantId, bool leftHanded)
    {
        var generateNotTrainingRunConfig =
            new Func<ExperimentManager.ReferenceFrame, ExperimentManager.Context, ExperimentManager.RunConfig>
                ((rf, ctx) => new ExperimentManager.RunConfig(participantId, leftHanded, false, false, ctx, rf, false));

        var result = new List<ExperimentManager.RunConfig>(17); // 4refFrame * (standing/walking) * (training/trial) + 1metronomeTraining (rf*context)

        var latinSquaredNotTrainings = Utils.Math.balancedLatinSquare(new []
        {
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PalmReferenced, ExperimentManager.Context.Standing),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.HandReferenced, ExperimentManager.Context.Standing),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PathReferenced, ExperimentManager.Context.Standing),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PathReferencedNeck, ExperimentManager.Context.Standing),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PalmReferenced, ExperimentManager.Context.Walking),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.HandReferenced, ExperimentManager.Context.Walking),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PathReferenced, ExperimentManager.Context.Walking),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PathReferencedNeck, ExperimentManager.Context.Walking),
        }, participantId);

        var turnIntoTraining = new Func<ExperimentManager.RunConfig, ExperimentManager.RunConfig>
            (config => new ExperimentManager.RunConfig(config.participantID, config.leftHanded, false, true,
                config.context, config.referenceFrame, false));

        // adding training and not trainings with reference frames
        int added = 0;
        foreach (var notTrainingConfig in latinSquaredNotTrainings)
        {
            result.Insert(added, turnIntoTraining(notTrainingConfig));
            result.Insert(added + 1, notTrainingConfig);
            added += 2;
        }
        
        // adding training to go with metronome
        int index = 0;
        foreach (var runConfig in result)
        {
            if (runConfig.context == ExperimentManager.Context.Walking)
                break;
            index++;
        }
        result.Insert(index, new ExperimentManager.RunConfig(participantId, leftHanded, 
            true, // indicates this is training with metronome
            // now below parameters don't matter
            true,
            ExperimentManager.Context.Walking,
            ExperimentManager.ReferenceFrame.PalmReferenced,
            false
            ));

        index = 0;
        foreach (var runConfig in result)
        {
            if (runConfig.referenceFrame is 
                ExperimentManager.ReferenceFrame.PathReferenced or 
                ExperimentManager.ReferenceFrame.PathReferencedNeck) 
                break;
            index++;
        }
        // now insert run config, which consists of just one step for the participant – please, place UI where it will be comfortable for you
        result.Insert(index, new ExperimentManager.RunConfig(participantId, leftHanded, false, false, ExperimentManager.Context.Standing, ExperimentManager.ReferenceFrame.PathReferenced, true));
        
        return result.ToArray();
    }

    private void UpdateRunConfigs()
    {
        _runConfigs = GenerateRunConfigs(participantId, leftHanded);
        currentRunConfigIndex = 0;
        currentRunStage = RunStage.Idle;
    }

    private void SendSummary()
    {
        var summary = new MessageFromHelmet.Summary();

        summary.id = participantId;
        summary.left = leftHanded;
        summary.index = currentRunConfigIndex;
        summary.stage = (int)currentRunStage;

        Send(summary);
    }
    
    protected override void Start()
    {
        base.Start();
        UpdateRunConfigs();
        connectionEstablished.AddListener(SendSummary);
        experimentManager.unexpectedErrorOccured.AddListener((error) =>
        {
            Send(new MessageFromHelmet.UnexpectedError(error));
        });
        experimentManager.trialsFinished.AddListener(() =>
        {
            currentRunConfigIndex++;
            currentRunStage = RunStage.Idle;
            SendSummary();
        });
        experimentManager.requestTrialValidation.AddListener(() =>
        {
            Send(new MessageFromHelmet(MessageFromHelmet.Code.RequestTrialValidation));
        });
    }

    protected override void Receive(MessageToHelmet message)
    {
        Debug.Log(message.ToString());
        switch (message.code)
        {
            case MessageToHelmet.Code.RefreshExperimentSummary:
                SendSummary();
                break;
            case MessageToHelmet.Code.SetLeftHanded:
                if (currentRunStage != RunStage.Idle)
                {
                    Send(
                        new MessageFromHelmet.InvalidOperation(
                            "Cannot change hands when experiment running/preparing"));
                }
                else
                {
                    leftHanded = (message as MessageToHelmet.SetLeftHanded).leftHanded;
                    UpdateRunConfigs();
                    SendSummary();
                }
                break;
            case MessageToHelmet.Code.SetParticipantID:
                if (currentRunStage != RunStage.Idle)
                {
                    Send(
                        new MessageFromHelmet.InvalidOperation(
                            "Cannot change participant id when experiment running/preparing"));
                }
                else
                {
                    participantId = (message as MessageToHelmet.SetParticipantID).participantID;
                    UpdateRunConfigs();
                    SendSummary();
                }
                break;
            case MessageToHelmet.Code.PrepareNextRun:
                if (currentRunStage == RunStage.Running)
                {
                    Send(new MessageFromHelmet.InvalidOperation(
                        "Cannot prepare when is running"));
                    break;
                }
                // otherwise, if idle (or prepare again), that's ok
                currentRunStage = RunStage.Preparing;
                SendSummary();
                experimentManager.OnServerSaidPrepare(_runConfigs[currentRunConfigIndex]);
                break;
            case MessageToHelmet.Code.StartNextRun:
                if (currentRunStage != RunStage.Preparing)
                {
                    Send(new MessageFromHelmet.InvalidOperation(
                        "Cannot start when stage is not 'preparing'"));
                    break;
                }
                currentRunStage = RunStage.Running;
                SendSummary();
                experimentManager.OnServerSaidStart();
                break;
            case MessageToHelmet.Code.FinishTraining:
                if (currentRunStage == RunStage.Running && (_runConfigs[currentRunConfigIndex].isTraining ||
                                                            _runConfigs[currentRunConfigIndex].isMetronomeTraining))
                {
                    currentRunConfigIndex++;
                    currentRunStage = RunStage.Idle;
                    experimentManager.OnServerSaidFinishTraining();
                    SendSummary();
                }
                else
                {
                    Send(new MessageFromHelmet.InvalidOperation(
                        "Invalid stop command. Can stop only if training is running"));
                }
                break;
            case MessageToHelmet.Code.ValidateTrial:
            case MessageToHelmet.Code.InvalidateTrial:
                if (currentRunStage == RunStage.Running &&
                    !_runConfigs[currentRunConfigIndex].isTraining
                    )
                {
                    if (message.code == MessageToHelmet.Code.ValidateTrial)
                        experimentManager.OnServerValidatedTrial();
                    else experimentManager.OnServerInvalidatedTrial();
                }
                else
                {
                    Send(new MessageFromHelmet.InvalidOperation(
                        "Validate/invalidate can only be called during trials run"));
                }
                break;
            case MessageToHelmet.Code.PlaceTrackAndLight:
                if (_runConfigs[currentRunConfigIndex].context == ExperimentManager.Context.Standing)
                {
                    experimentManager.PlaceLightWhereHeadset();                    
                }
                else
                {
                    experimentManager.PlaceTrackForwardFromHeadset();
                    experimentManager.PlaceLightWhereTrack();
                }
                break;
            case MessageToHelmet.Code.SkipNSteps:
                if (currentRunStage != RunStage.Idle)
                {
                    Send(new MessageFromHelmet.InvalidOperation(
                        "Cannot skip when run is preparing/running"));
                }
                else
                {
                    var skipMsg = (message as MessageToHelmet.SkipNSteps);
                    currentRunConfigIndex += skipMsg.stepsToSkip;
                    SendSummary();
                }
                break;
            default:
                throw new ArgumentException($"It seems you have implemented a new message from helmet but forget to handle in {nameof(Receive)} method");
        }
    }
}