using System;
using System.Collections;
using System.Collections.Generic;
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
                ((rf, ctx) => new ExperimentManager.RunConfig(participantId, leftHanded, false, false, ctx, rf));

        var result = new List<ExperimentManager.RunConfig>(15); // 7 (rf*context) + 7 trainings + 1 metronome training

        var latinSquaredNotTrainings = Utils.Math.balancedLatinSquare(new ExperimentManager.RunConfig[]
        {
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PalmReferenced, ExperimentManager.Context.Standing),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.HandReferenced, ExperimentManager.Context.Standing),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PathReferenced, ExperimentManager.Context.Standing),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PalmReferenced, ExperimentManager.Context.Walking),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.HandReferenced, ExperimentManager.Context.Walking),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PathReferenced, ExperimentManager.Context.Walking),
            generateNotTrainingRunConfig(ExperimentManager.ReferenceFrame.PathReferencedNeck, ExperimentManager.Context.Walking),
        }, participantId);

        var turnIntoTraining = new Func<ExperimentManager.RunConfig, ExperimentManager.RunConfig>
            (config => new ExperimentManager.RunConfig(config.participantID, config.leftHanded, false, true,
                config.context, config.referenceFrame));

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
            ExperimentManager.ReferenceFrame.PalmReferenced
            ));

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
        experimentManager.trialFinished.AddListener(() =>
        {
            currentRunConfigIndex++;
            currentRunStage = RunStage.Idle;
            SendSummary();
        });
    }

    protected override void Receive(MessageToHelmet message)
    {
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
            default:
                throw new ArgumentException($"It seems you have implemented a new message from helmet but forget to handle in {nameof(Receive)} method");
        }
    }
}