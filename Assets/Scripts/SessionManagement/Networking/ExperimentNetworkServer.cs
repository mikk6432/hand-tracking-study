using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public abstract class ExperimentNetworkServer : ExperimentNetwork
{
    private static readonly int BROADCAST_INTERVAL = 1000;
    
    protected UnityEvent connectionEstablished = new();
    protected UnityEvent connectionLost = new();

#pragma warning disable CS0618
    protected override void Start()
    {
        base.Start();

        var error = StartBroadcasting(hostID, port);
        if (error != NetworkError.Ok)
        {
            Debug.LogError($"SyncPoseServer: Couldn't start broadcasting because of {error}. Disabling the script");
            enabled = false;
            return;
        }
        Debug.Log("SyncPoseServer: Starting broadcasting");
    }
    
    private void LateUpdate()
    {
        // Checking whether something has happened with the connection since the last frame
        if (CheckConnectionChanges() == INVALID_CONNECTION) return;
    }
    
    private int CheckConnectionChanges()
    {
        var eventType = NetworkTransport.Receive(out int outHostID, out int outConnectionID, out int outChannelID,
            messageBuffer, messageBuffer.Length, out int actualMessageLength, out byte error);
        switch (eventType)
        {
            case NetworkEventType.Nothing:
                // Nothing has happend. That's a good thing :-)
                break;
            case NetworkEventType.ConnectEvent:
                сonnectionID = outConnectionID;
                Debug.Log("connect event");
                StopBroadcasting();
                connectionEstablished.Invoke();
                break;
            case NetworkEventType.DisconnectEvent:
                сonnectionID = INVALID_CONNECTION;
                connectionLost.Invoke();
                // Restarting broadcasting
                StartBroadcasting(hostID, port);
                Debug.Log("SyncPoseServer: Connection lost. Restarting broadcasting");
                break;
            case NetworkEventType.DataEvent: 
                using (var stream = new MemoryStream(messageBuffer))
                {
                    var message = (MessageFromHelmet)
                        formatter.Deserialize(stream);
                    Receive(message);
                }
                    
                break;
            default:
                Debug.LogError($"SyncPoseServer: Unknown network message type received, namely {eventType}");
                break;
        }

        return сonnectionID;
    }
    
    private static NetworkError StartBroadcasting(int hostID, int port)
    {
        NetworkTransport.StartBroadcastDiscovery(hostID, port, BROADCAST_CREDENTIALS_KEY,
            BROADCAST_CREDENTIALS_VERSION, BROADCAST_CREDENTIALS_SUBVERSION, null, 0, BROADCAST_INTERVAL, out byte error);

        return (NetworkError)error;
    }

    private static bool StopBroadcasting()
    {
        if (!NetworkTransport.IsBroadcastDiscoveryRunning())
            return false;

        NetworkTransport.StopBroadcastDiscovery();
        return true;
    }

    protected bool Send(MessageToHelmet message)
    {
        int bufferSize;
        
        using (var stream = new MemoryStream(messageBuffer))
        {
            formatter.Serialize(stream, message);
            bufferSize = (int)stream.Position;
        }

        NetworkTransport.Send(hostID, сonnectionID, channelID, messageBuffer, bufferSize, out byte error);
        if ((NetworkError)error != NetworkError.Ok)
        {
            Debug.LogError($"SyncPoseServer: Couldn't send data over the network because of {(NetworkError)error}");
            return false;
        }
        return true;
    }
    
    protected abstract void Receive(MessageFromHelmet message);
#pragma warning restore CS0618
}