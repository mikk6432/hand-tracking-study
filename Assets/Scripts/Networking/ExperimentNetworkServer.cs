using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Assertions;
using Unity.Collections;
using Unity.Networking.Transport;
using NetworkDiscoveryUnity;

public abstract class ExperimentNetworkServer : ExperimentNetwork
{
    public NetworkDriver m_Driver;
    private NativeList<NetworkConnection> m_Connections;

    protected UnityEvent connectionEstablished = new();
    protected UnityEvent connectionLost = new();

    protected virtual void Start()
    {
        m_Driver = NetworkDriver.Create();
        var endpoint = NetworkEndPoint.AnyIpv4;
        endpoint.Port = (ushort)GetComponent<NetworkDiscovery>().gameServerPortNumber;
        GetComponent<NetworkDiscovery>().EnsureServerIsInitialized();
        if (m_Driver.Bind(endpoint) != 0)
            Debug.Log("Failed to bind to port 9000");
        else
            m_Driver.Listen();

        m_Connections = new NativeList<NetworkConnection>(16, Allocator.Persistent);
    }

    protected void OnDestroy()
    {
        if (m_Driver.IsCreated)
        {
            m_Driver.Dispose();
            m_Connections.Dispose();
        }
    }

    public void Update()
    {
        m_Driver.ScheduleUpdate().Complete();

        // Clean up connections
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
            {
                m_Connections.RemoveAtSwapBack(i);
                --i;
            }
        }

        // Accept new connections
        NetworkConnection c;
        while ((c = m_Driver.Accept()) != default(NetworkConnection))
        {
            m_Connections.Add(c);
            Debug.Log("Accepted a connection");
            connectionEstablished.Invoke();
        }

        DataStreamReader stream;
        for (int i = 0; i < m_Connections.Length; i++)
        {
            if (!m_Connections[i].IsCreated)
                continue;
            NetworkEvent.Type cmd;
            while ((cmd = m_Driver.PopEventForConnection(m_Connections[i], out stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    NativeArray<byte> arr = new NativeArray<byte>(stream.Length, Allocator.Temp);
                    stream.ReadBytes(arr);
                    using (var data = new MemoryStream(arr.ToArray()))
                    {
                        var message = (MessageFromHelmet)formatter.Deserialize(data);
                        Receive(message);
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    Debug.Log("Client disconnected from server");
                    m_Connections[i] = default(NetworkConnection);
                    connectionLost.Invoke();
                }
            }
        }
    }

    protected void Send(MessageToHelmet message)
    {
        m_Driver.BeginSend(m_Connections[0], out var writer);
        using (var stream = new MemoryStream(messageBuffer))
        {
            formatter.Serialize(stream, message);
            NativeArray<byte> arr = new NativeArray<byte>(stream.ToArray(), Allocator.Temp);
            writer.WriteBytes(arr);
        }
        m_Driver.EndSend(writer);
    }

    protected abstract void Receive(MessageFromHelmet message);
}