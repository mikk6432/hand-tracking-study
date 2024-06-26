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
        Running, // ExperimentManager is running training/trial. If training, then stop will be called by server command. Otherwise, when participant will finish selecting all targets
        Validation
    }

    public static ExperimentManager.RunConfig[] GenerateRunConfigs(int participantId, bool leftHanded)
    {
        var generateNotTrainingRunConfig =
            new Func<ExperimentManager.ExperimentReferenceFrame, ExperimentManager.Context, ExperimentManager.RunConfig>
                ((rf, ctx) => new ExperimentManager.RunConfig(participantId, leftHanded, false, false, ctx, rf, false));

        var totalNumberOfTraining = 2;
        var numberOfRefs = Enum.GetNames(typeof(ExperimentManager.ExperimentReferenceFrame)).Length;
        var numberOfContexts = Enum.GetNames(typeof(ExperimentManager.Context)).Length;
        var result = new List<ExperimentManager.RunConfig>(numberOfRefs * numberOfContexts * 2 + totalNumberOfTraining + 2); // times 2 for training/trial and plus 2 for training steps + 1 for break

        var refFrames = Enum.GetValues(typeof(ExperimentManager.ExperimentReferenceFrame));
        var contexts = Enum.GetValues(typeof(ExperimentManager.Context));
        var counterbalancedExperiments = new List<ExperimentManager.RunConfig>(numberOfRefs);
        foreach (ExperimentManager.ExperimentReferenceFrame rf in refFrames)
        {
            counterbalancedExperiments.Add(generateNotTrainingRunConfig(rf, ExperimentManager.Context.Standing));
        }
        var latinSquaredNotTrainings = Math.balancedLatinSquare(counterbalancedExperiments.ToArray(), participantId);
        var allExperiments = new List<ExperimentManager.RunConfig>(numberOfRefs * numberOfContexts);
        for (int i = 0; i < refFrames.Length; i++)
        {
            var rf = latinSquaredNotTrainings[i].referenceFrame;
            var seed = participantId * 10 + (int)rf;
            var random = new System.Random(seed);
            var randomized = contexts.Cast<ExperimentManager.Context>().OrderBy(x => random.Next()).ToArray();
            foreach (var ctx in randomized)
            {
                allExperiments.Add(generateNotTrainingRunConfig(rf, ctx));
            }
        }

        var turnIntoTraining = new Func<ExperimentManager.RunConfig, ExperimentManager.RunConfig>
            (config => new ExperimentManager.RunConfig(config.participantID, config.leftHanded, false, true,
                config.context, config.referenceFrame, false));

        // adding training and not trainings with reference frames
        int added = 0;
        foreach (var notTrainingConfig in allExperiments)
        {
            result.Insert(added, turnIntoTraining(notTrainingConfig));
            result.Insert(added + 1, notTrainingConfig);
            added += 2;
        }

        // adding training to go with metronome
        var firstWalkingIndex = FirstIndexOfSpecificContext(ref result, ExperimentManager.Context.Walking);
        result.Insert(firstWalkingIndex, new ExperimentManager.RunConfig(participantId, leftHanded,
            true, // indicates this is training with metronome
                  // now below parameters don't matter
            true,
            ExperimentManager.Context.Walking,
            ExperimentManager.ExperimentReferenceFrame.PalmReferenced,
            false
            ));

        // adding training to go with metronome
        var secondWalkingIndex = FirstIndexOfSpecificContext(ref result, ExperimentManager.Context.Circle);
        result.Insert(secondWalkingIndex, new ExperimentManager.RunConfig(participantId, leftHanded,
            true, // indicates this is training with metronome
                  // now below parameters don't matter
            true,
            ExperimentManager.Context.Circle,
            ExperimentManager.ExperimentReferenceFrame.PalmReferenced,
            false
            ));

        var breakIndex = numberOfContexts * 2 + totalNumberOfTraining;
        result.Insert(breakIndex, new ExperimentManager.RunConfig(participantId, leftHanded,
            false,
            false,
            ExperimentManager.Context.Circle,
            ExperimentManager.ExperimentReferenceFrame.PalmReferenced,
            false,
            true // indicates this is training with metronome. The above arguments does not matter
            ));

        // var firstJoggingIndex = FirstIndexOfSpecificContext(ref result, ExperimentManager.Context.Jogging);
        // result.Insert(firstJoggingIndex, new ExperimentManager.RunConfig(participantId, leftHanded,
        //     true, // indicates this is training with metronome
        //           // now below parameters don't matter
        //     true,
        //     ExperimentManager.Context.Jogging,
        //     ExperimentManager.ExperimentReferenceFrame.PalmReferenced,
        //     false
        //     ));

        // Insert standing training run config, using whatever reference frame is first. 
        // This is the initial training for the user to get familiar with the selection task.
        if (allExperiments[0].context != ExperimentManager.Context.Standing)
            result.Insert(0, new ExperimentManager.RunConfig(participantId, leftHanded, false, true, ExperimentManager.Context.Standing, allExperiments[0].referenceFrame, true));


        // Find index of first path reference frame
        int index = 0;
        foreach (var runConfig in result)
        {
            if (runConfig.referenceFrame is
                ExperimentManager.ExperimentReferenceFrame.PathReferenced)
                break;
            index++;
        }
        // now insert run config, which consists of just one step for the participant – please, place UI where it will be comfortable for you
        result.Insert(index, new ExperimentManager.RunConfig(participantId, leftHanded, false, false, ExperimentManager.Context.Standing, ExperimentManager.ExperimentReferenceFrame.PathReferenced, false, false, true));


        return result.ToArray();
    }

    private static int FirstIndexOfSpecificContext(ref List<ExperimentManager.RunConfig> result, ExperimentManager.Context context)
    {
        int index = 0;
        foreach (var runConfig in result)
        {
            if (runConfig.context == context)
                break;
            index++;
        }
        return index;
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
        int indexOfInitialTraining = _runConfigs.ToList().FindIndex(config => config.isInitialStandingTraining);
        long bitmap = participantPrefs.doneBitmap;
        bitmap = Bitmap.SetFalse(bitmap, indexOfMetronomeTraining);
        bitmap = Bitmap.SetFalse(bitmap, indexOfInitialTraining);
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
        experimentManager.userMistake.AddListener((error) =>
        {
            Send(new MessageFromHelmet.UserError(error));
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
            currentRunStage = RunStage.Validation;
            SendSummary();
        });
        experimentManager.sendTargetSizeToServer.AddListener((targetSize) =>
        {
            Send(new MessageFromHelmet.TargetSize(targetSize));
        });
    }

    protected override void Receive(MessageToHelmet message)
    {
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
                if (currentRunStage == RunStage.Running || currentRunStage == RunStage.Validation)
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
                if (currentRunStage == RunStage.Validation &&
                    !_runConfigs[currentRunningStepIndex].isTraining
                    )
                {
                    currentRunStage = RunStage.Running;
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
            case MessageToHelmet.Code.SetPathRefHeight:
                experimentManager.OnServerSetPathRefHeight();
                break;
            case MessageToHelmet.Code.PlaceTrackAndLight:
                FindObjectOfType<PlaceTrack>().PlaceTrackAndLightsForwardFromHeadset();
                break;
            case MessageToHelmet.Code.showHeadsetAdjustmentText:
                bool shouldShow = (message as MessageToHelmet.ToggleShowHeadsetAdjustmentText).shouldShow;
                experimentManager.OnToggleHeadsetAdjustmentText(shouldShow);
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