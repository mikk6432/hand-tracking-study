using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public abstract class ExperimentNetworkClient: ExperimentNetwork
{
    [SerializeField] protected GameObject errorIndicator;
    [Header("Events")]
    public UnityEvent connectionEstablished = new();
    public UnityEvent connectionLost = new();

#pragma warning disable CS0618
    protected override void Start()
    {
        base.Start();

        // To be able to receive broadcast messages we have to specify broadcast credentials
        NetworkTransport.SetBroadcastCredentials(hostID, BROADCAST_CREDENTIALS_KEY, BROADCAST_CREDENTIALS_VERSION,
            BROADCAST_CREDENTIALS_SUBVERSION, out byte error);
        if ((NetworkError)error != NetworkError.Ok)
        {
            Debug.LogError($"ExperimentNetworkClient: Couldn't set broadcast credentials because of {(NetworkError)error}. Disabling the script");
            enabled = false;
            return;
        }
    }
    
    private void Update()
    {
        try
        {
            var eventType = NetworkTransport.Receive(out int outHostID, out int outConnectionID, out int outChannelID,
                messageBuffer, messageBuffer.Length, out int actualMessageLength, out byte error);
            switch (eventType)
            {
                case NetworkEventType.Nothing:
                    // Nothing has happend. That's a good thing :-)
                    break;
                case NetworkEventType.BroadcastEvent:
                    string address = GetIPAddress(outHostID);
                    if (address != null)
                        ConnectToTheServer(address);
                    break;
                case NetworkEventType.ConnectEvent:
                    connectionEstablished.Invoke();
                    break;
                case NetworkEventType.DataEvent:
                    using (var stream = new MemoryStream(messageBuffer))
                    {
                        var message = (MessageToHelmet)formatter.Deserialize(stream);
                        Receive(message);
                    }
                    
                    break;
                case NetworkEventType.DisconnectEvent:
                    connectionLost.Invoke();
                    break;
            }
        }
        catch (NullReferenceException) { } // This happens when nobody listens to the connection events
    }
    
    private int ConnectToTheServer(string address)
    {
        сonnectionID = NetworkTransport.Connect(hostID, address, port, 0, out byte error);
        if ((NetworkError)error != NetworkError.Ok)
        {
            Debug.LogError($"SyncPoseReceiver: Couldn't connect to the server because of {(NetworkError)error}");
            return INVALID_CONNECTION;
        }

        return сonnectionID;
    }

    private static string GetIPAddress(int hostID)
    {
        NetworkTransport.GetBroadcastConnectionInfo(hostID, out string address, out int port, out byte error);
        if ((NetworkError)error != NetworkError.Ok)
        {
            Debug.LogError($"SyncPoseReceiver: Couldn't get an IP address because of {(NetworkError)error}");
            return null;
        }

        return address;
    }

    protected bool Send(MessageFromHelmet message)
    {
        int bufferSize = 0;
        
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

    protected abstract void Receive(MessageToHelmet message);
#pragma warning restore CS0618
}