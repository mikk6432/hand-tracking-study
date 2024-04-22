using System;
using System.IO;
using NetworkDiscoveryUnity;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

public abstract class ExperimentNetworkClient : ExperimentNetwork
{
    public NetworkDriver m_Driver;
    public NetworkConnection m_Connection;
    public bool Done;

    [SerializeField] protected GameObject errorIndicator;
    [Header("Events")]
    public UnityEvent connectionEstablished = new();
    public UnityEvent connectionLost = new();

    private NetworkEndPoint endpoint;
    private bool broadcastFound = false;

    protected virtual void Start()
    {
        GetComponent<NetworkDiscovery>().onReceivedServerResponse.AddListener((NetworkDiscovery.DiscoveryInfo info) =>
        {
            if (broadcastFound) return;
            broadcastFound = true;
            if (m_Connection.IsCreated)
            {
                return;
            }
            endpoint = NetworkEndPoint.Parse(info.EndPoint.Address.ToString(), (ushort)GetComponent<NetworkDiscovery>().gameServerPortNumber);
            ConnectToTheServer();
        });
        GetComponent<NetworkDiscovery>().SendBroadcast();
    }

    protected virtual void OnDestroy()
    {
        m_Driver.Dispose();
    }

    private void Update()
    {
        if (!broadcastFound)
        {
            GetComponent<NetworkDiscovery>().SendBroadcast();
            return;
        }
        m_Driver.ScheduleUpdate().Complete();

        if (!m_Connection.IsCreated)
        {
            ConnectToTheServer();
            return;
        }
        DataStreamReader stream;
        NetworkEvent.Type cmd;
        while ((cmd = m_Connection.PopEvent(m_Driver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log("We are now connected to the server");
                connectionEstablished.Invoke();
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                NativeArray<byte> arr = new NativeArray<byte>(stream.Length, Allocator.Temp);
                stream.ReadBytes(arr);
                using (var data = new MemoryStream(arr.ToArray()))
                {
                    var message = (MessageToHelmet)formatter.Deserialize(data);
                    Receive(message);
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Debug.Log("Client got disconnected from server");
                m_Connection = default(NetworkConnection);
                connectionLost.Invoke();
            }
        }
    }

    private void ConnectToTheServer()
    {
        m_Driver = NetworkDriver.Create();
        m_Connection = default(NetworkConnection);
        Debug.Log("Connecting to the server");
        Debug.Log($"Endpoint: {endpoint}");
        m_Connection = m_Driver.Connect(endpoint);
    }

    protected void Send(MessageFromHelmet message)
    {
        m_Driver.BeginSend(m_Connection, out var writer);
        using (var stream = new MemoryStream(messageBuffer))
        {
            formatter.Serialize(stream, message);
            NativeArray<byte> arr = new NativeArray<byte>(stream.ToArray(), Allocator.Temp);
            writer.WriteBytes(arr);
        }
        m_Driver.EndSend(writer);
    }

    protected abstract void Receive(MessageToHelmet message);
}