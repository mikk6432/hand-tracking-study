using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;

public class HelmetMainProcess : ExperimentNetworkClient
{
    private ParticipantPrefs participantPrefs;

    private ExperimentManager.RunConfig[] _runConfigs;
    private int currentRunningStepIndex; // for example 4, means that previous 4 runs (0,1,2,3) were fulfilled already
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
            new Func<ExperimentManager.ExperimentReferenceFrame, ExperimentManager.Context, ExperimentManager.RunConfig>
                ((rf, ctx) => new ExperimentManager.RunConfig(participantId, leftHanded, false, false, ctx, rf, false));

        var result = new List<ExperimentManager.RunConfig>(17); // 4refFrame * (standing/walking) * (training/trial) + 1metronomeTraining (rf*context)

        var refFrames = Enum.GetValues(typeof(ExperimentManager.ExperimentReferenceFrame));
        var latinSquaredNotTrainings = Math.balancedLatinSquare(
            refFrames.Cast<ExperimentManager.ExperimentReferenceFrame>().Select(rf => generateNotTrainingRunConfig(rf, ExperimentManager.Context.Standing)).ToArray().Concat(
                refFrames.Cast<ExperimentManager.ExperimentReferenceFrame>().Select(rf => generateNotTrainingRunConfig(rf, ExperimentManager.Context.Walking)).ToArray()
            ).ToArray(),
            participantId);

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
            ExperimentManager.ExperimentReferenceFrame.PalmReferenced,
            false
            ));

        index = 0;
        foreach (var runConfig in result)
        {
            if (runConfig.referenceFrame is
                ExperimentManager.ExperimentReferenceFrame.PathReferenced or
                ExperimentManager.ExperimentReferenceFrame.PathReferencedNeck or
                ExperimentManager.ExperimentReferenceFrame.ChestReferenced)
                break;
            index++;
        }
        // now insert run config, which consists of just one step for the participant – please, place UI where it will be comfortable for you
        result.Insert(index, new ExperimentManager.RunConfig(participantId, leftHanded, false, false, ExperimentManager.Context.Standing, ExperimentManager.ExperimentReferenceFrame.PathReferenced, true));

        return result.ToArray();
    }

    private void UpdateRunConfigs()
    {
        _runConfigs = GenerateRunConfigs(participantPrefs.participantId, participantPrefs.leftHanded);
        currentRunningStepIndex = -1;
        currentRunStage = RunStage.Idle;
    }

    private void SendSummary()
    {
        var summary = new MessageFromHelmet.Summary();

        summary.id = participantPrefs.participantId;
        summary.left = participantPrefs.leftHanded;
        summary.doneBitmap = participantPrefs.doneBitmap;
        summary.index = currentRunningStepIndex;
        summary.stage = (int)currentRunStage;

        Send(summary);
    }

    private void PrefsFromFileOrDefault(int participantId)
    {
        participantPrefs = ParticipantPrefs.ForParticipant(participantId);

        int indexOfMetronomeTraining = _runConfigs.ToList().FindIndex(config => config.isMetronomeTraining);
        int indexOfComfortUIPlacement = _runConfigs.ToList().FindIndex(config => config.isPlacingComfortYAndZ);
        long bitmap = participantPrefs.doneBitmap;
        bitmap = Bitmap.SetFalse(bitmap, indexOfMetronomeTraining);
        bitmap = Bitmap.SetFalse(bitmap, indexOfComfortUIPlacement);
        participantPrefs.doneBitmap = bitmap;
    }

    protected override void Start()
    {
        base.Start();
        _runConfigs = new List<ExperimentManager.RunConfig>().ToArray();
        int startParticipantId = 1;
        PrefsFromFileOrDefault(startParticipantId);
        UpdateRunConfigs();

        connectionEstablished.AddListener(SendSummary);
        experimentManager.unexpectedErrorOccured.AddListener((error) =>
        {
            Send(new MessageFromHelmet.UnexpectedError(error));
        });
        experimentManager.trialsFinished.AddListener(() =>
        {
            long bitmap = participantPrefs.doneBitmap;
            bitmap = Bitmap.SetTrue(bitmap, currentRunningStepIndex);
            participantPrefs.doneBitmap = bitmap;
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
            case MessageToHelmet.Code.SavePrefs:
                participantPrefs.Save();
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
                    participantPrefs.leftHanded = (message as MessageToHelmet.SetLeftHanded).leftHanded;
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
                    var participantId = (message as MessageToHelmet.SetParticipantID).participantID;
                    PrefsFromFileOrDefault(participantId);
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
                var msg = message as MessageToHelmet.PrepareNextStep;

                if (currentRunStage == RunStage.Preparing)
                {
                    if (msg.index != currentRunningStepIndex)
                    {
                        Send(new MessageFromHelmet.InvalidOperation(
                            "Cannot prepare when other step was prepared"));
                        break;
                    }
                }

                currentRunningStepIndex = msg.index;

                currentRunStage = RunStage.Preparing;
                SendSummary();
                experimentManager.OnServerSaidPrepare(_runConfigs[currentRunningStepIndex]);
                break;
            case MessageToHelmet.Code.StartNextRun:
                if (currentRunStage != RunStage.Preparing)
                {
                    Send(new MessageFromHelmet.InvalidOperation(
                        "Cannot start when stage is not 'preparing'"));
                    break;
                }
                var startMsg = message as MessageToHelmet.StartNextStep;
                if (startMsg.index != currentRunningStepIndex)
                {
                    Send(new MessageFromHelmet.InvalidOperation(
                        "Cannot start step which has not been prepared yet"));
                    break;
                }
                currentRunStage = RunStage.Running;
                SendSummary();
                experimentManager.OnServerSaidStart();
                break;
            case MessageToHelmet.Code.FinishTraining:
                if (currentRunStage == RunStage.Running && (_runConfigs[currentRunningStepIndex].isTraining ||
                                                            _runConfigs[currentRunningStepIndex].isMetronomeTraining))
                {
                    var finishMsg = message as MessageToHelmet.FinishTrainingStep;
                    if (finishMsg.index != currentRunningStepIndex)
                    {
                        Send(new MessageFromHelmet.InvalidOperation(
                            "Invalid stop command. Can stop only training which is running"));
                        break;
                    }

                    participantPrefs.doneBitmap = Bitmap.SetTrue(participantPrefs.doneBitmap, currentRunningStepIndex);
                    currentRunningStepIndex++;
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
                    !_runConfigs[currentRunningStepIndex].isTraining
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
            case MessageToHelmet.Code.SetStepIsDone:
                if (currentRunStage != RunStage.Idle)
                {
                    Send(new MessageFromHelmet.InvalidOperation(
                        "Can change step is done only in Idle stage"));
                    break;
                }

                var stepMsg = message as MessageToHelmet.SetStepIsDone;
                long bitmap = participantPrefs.doneBitmap;
                if (stepMsg.done)
                {
                    bitmap = Bitmap.SetTrue(bitmap, stepMsg.stepIndex);
                }
                else bitmap = Bitmap.SetFalse(bitmap, stepMsg.stepIndex);

                participantPrefs.doneBitmap = bitmap;
                SendSummary();
                break;
            case MessageToHelmet.Code.PlaceTrackAndLight:
                FindObjectOfType<PlaceTrack>().PlaceTrackAndLightsForwardFromHeadset();
                break;
            default:
                throw new ArgumentException($"It seems you have implemented a new message from helmet but forget to handle in {nameof(Receive)} method");
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        participantPrefs.Save();
    }
}