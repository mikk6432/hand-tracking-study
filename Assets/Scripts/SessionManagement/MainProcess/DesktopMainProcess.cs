using UnityEngine;

public class DesktopMainProcess: ExperimentNetworkServer
{
    protected override void Receive(MessageFromHelmet message)
    {
        Debug.Log(message);
        // if (message.code == MessageFromHelmet.Code.SelectionDone)
        // {
        
        // }
        // else
        // {
        //     Debug.Log($"Accepted another message: {message.ToString()}");
        // }
    }
}