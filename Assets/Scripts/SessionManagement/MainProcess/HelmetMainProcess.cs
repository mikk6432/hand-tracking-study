using UnityEngine;

public class HelmetMainProcess: ExperimentNetworkClient
{
    [SerializeField] private StandingTrainingManager standingTrainingManager;

    protected sealed override void Start()
    {
        base.Start();
        
        standingTrainingManager.selectionDone.AddListener((success, targetIndex) =>
        {
            Send(new MessageFromHelmet.SelectionDone(success, targetIndex));
        });
    }

    protected override void Receive(MessageToHelmet message)
    {
        
    }
}