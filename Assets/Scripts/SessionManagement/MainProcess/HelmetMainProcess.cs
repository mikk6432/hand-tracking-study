using System;
using System.Collections.Generic;
using UnityEngine;

public class HelmetMainProcess: ExperimentNetworkClient
{
    [SerializeField] private StandingTrainingManager standingTrainingManager;

    private int participantId;
    private bool leftHanded;

    private ExperimentManager.RunConfig[] _runConfigs;
    private int currentRunIndex;
    private RunStage currentRunStage;
    
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
    
    protected override void Start()
    {
        base.Start();
    }

    protected override void Receive(MessageToHelmet message)
    {
        
    }
}