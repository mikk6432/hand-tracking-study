using System;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.Networking;

public class ExperimentNetwork: MonoBehaviour
{
    [Serializable]
    protected class MessageFromHelmet
    {
        [Serializable]
        public enum Code
        {
            ExperimentSummary,
            InvalidOperation, // for example, server said to toggle leftHanded, while trial or training was running
            UnexpectedError, // to be deleted. HelmetProcess can surround dangerous code-blocks with rty catch and sent to server info about error which occured unexpectedly
            RequestTrialValidation,
        }

        public readonly Code code;

        public MessageFromHelmet(Code _code)
        {
            code = _code;
        }
        
        public override string ToString()
        {
            return $"FromHelmet: code={Enum.GetName(typeof(Code), code)}";
        }

        [Serializable]
        public class Summary: MessageFromHelmet
        {
            public int id; // by it we can calc runConfigs sequence either on client or server
            public bool left; // whether user il left handed
            public int index; // index of current runConfig. Means that those who have smaller index were fulfilled
            public int stage; // stage of the current runConfig. If it is preparing, running or idle

            public Summary(): base(Code.ExperimentSummary) { }
            
            public override string ToString()
            {
                return base.ToString() + $", participantId={id}," + (left ? "left" : "right") + $"Handed, index={index}, stage={stage}";
            }
        }
        
        [Serializable]
        public class InvalidOperation: MessageFromHelmet
        {
            public readonly string reason;

            public InvalidOperation(string reason) : base(Code.InvalidOperation)
            {
                this.reason = reason;
            }

            public override string ToString()
            {
                return base.ToString() + $", reason: {reason}";
            }
        }
        
        [Serializable]
        public class UnexpectedError: MessageFromHelmet
        {
            public readonly string errorMessage;

            public UnexpectedError(string errorMessage) : base(Code.UnexpectedError)
            {
                this.errorMessage = errorMessage;
            }

            public override string ToString()
            {
                return base.ToString() + $", error: {errorMessage}";
            }
        }
    }
    
    [Serializable]
    protected class MessageToHelmet
    {
        [Serializable]
        public enum Code
        {
            SetParticipantID,
            SetLeftHanded,
            RefreshExperimentSummary,
            PrepareNextRun,
            StartNextRun,
            FinishTraining,
            
            // these two are not supposed to happen, only if experiment was interrupted, to continue from where finished
            SkipNSteps,
            PlaceTrackAndLight,
            
            // walking context trials. User has selected all targets with such size, but was he really walking with metronome?
            ValidateTrial,
            InvalidateTrial
        }

        public readonly Code code;

        public MessageToHelmet(Code _code)
        {
            code = _code;
        }
        
        public override string ToString()
        {
            return $"ToHelmet: code={Enum.GetName(typeof(Code), code)}";
        }
        
        [Serializable]
        public class SetParticipantID: MessageToHelmet
        {
            public readonly int participantID;

            public SetParticipantID(int _participantID): base(Code.SetParticipantID)
            {
                participantID = _participantID;
            }
            
            public override string ToString()
            {
                return base.ToString() + $", participantID={participantID}";
            }
        }

        [Serializable]
        public class SetLeftHanded : MessageToHelmet
        {
            public readonly bool leftHanded;

            public SetLeftHanded(bool _leftHanded): base(Code.SetLeftHanded)
            {
                leftHanded = _leftHanded;
            }
            
            public override string ToString()
            {
                return base.ToString() + $", leftHanded={leftHanded}";
            }
        }
        
        [Serializable]
        public class SkipNSteps: MessageToHelmet
        {
            public readonly int stepsToSkip;

            public SkipNSteps(int stepsToSkip) : base(Code.SkipNSteps)
            {
                this.stepsToSkip = stepsToSkip;
            }
        }
    }

#pragma warning disable CS0618 // Type or member is obsolete
    [SerializeField, Tooltip("The port number has to be the same for the server and the receiver")]
    protected int port = 54300;
    
    protected int channelID;
    protected int hostID;
    protected int сonnectionID = INVALID_CONNECTION;

    protected HostTopology topology;

    protected readonly byte[] messageBuffer = new byte[512];
    protected BinaryFormatter formatter = new BinaryFormatter();

    protected static readonly int MAX_NUMBER_OF_CONNECTIONS = 1;
    protected static readonly int INVALID_CONNECTION = -1;

    public static readonly int BROADCAST_CREDENTIALS_KEY = 13;
    public static readonly int BROADCAST_CREDENTIALS_VERSION = 1;
    public static readonly int BROADCAST_CREDENTIALS_SUBVERSION = 0;
    
    protected virtual void Awake()
    {
        // Initializing the Transport Layer with default settings
        NetworkTransport.Init();

        // We will need only one channel between this server and a client, so we're creating
        // one such that only the most recent message in the receive buffer will be delivered
        ConnectionConfig config = new ConnectionConfig();
        channelID = config.AddChannel(QosType.ReliableStateUpdate);
        config.PacketSize = 1470; // This value is recommended by Unity documentation: https://docs.unity3d.com/ScriptReference/Networking.ConnectionConfig.PacketSize.html

        // The thing is we don't need more than one connection
        topology = new HostTopology(config, MAX_NUMBER_OF_CONNECTIONS);
    }
    
    protected virtual void Start()
    {
        // Opening a socket on a specifeid port
        hostID = NetworkTransport.AddHost(topology, port);
    }

    protected virtual void OnDestroy()
    {
        NetworkTransport.Shutdown();
    }
}