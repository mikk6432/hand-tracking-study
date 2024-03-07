using System;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Unity.Netcode;

public abstract class MessageFromHelmet : INetworkSerializable
{
    [Serializable]
    public enum Code
    {
        ExperimentSummary,
        InvalidOperation, // for example, server said to toggle leftHanded, while trial or training was running
        UnexpectedError, // to be deleted. HelmetProcess can surround dangerous code-blocks with rty catch and sent to server info about error which occured unexpectedly
        RequestTrialValidation,
    }

    public Code code;

    public override string ToString()
    {
        return $"FromHelmet: code={Enum.GetName(typeof(Code), code)}";
    }

    public class RequestTrialValidation : MessageFromHelmet
    {
        public RequestTrialValidation()
        {
            code = Code.RequestTrialValidation;
        }
    }

    [Serializable]
    public class Summary : MessageFromHelmet
    {
        public int id; // by it we can calc runConfigs sequence either on client or server
        public bool left; // whether user il left handed
        public long doneBitmap;

        public int index; // index of current runConfig. Means that those who have smaller index were fulfilled
        public int stage; // stage of the current runConfig. If it is preparing, running or idle

        public Summary()
        {
            code = Code.ExperimentSummary;
        }

        public override string ToString()
        {
            return base.ToString() + $", participantId={id}," + (left ? "left" : "right") + $"Handed, index={index}, stage={stage}, doneBitmap={doneBitmap}";
        }

        public new void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref id);
            serializer.SerializeValue(ref left);
            serializer.SerializeValue(ref doneBitmap);
            serializer.SerializeValue(ref index);
            serializer.SerializeValue(ref stage);
        }
    }

    [Serializable]
    public class InvalidOperation : MessageFromHelmet
    {
        public string reason;

        public InvalidOperation()
        {
            code = Code.InvalidOperation;
        }

        public InvalidOperation(string reason)
        {
            code = Code.InvalidOperation;
            this.reason = reason;
        }

        public override string ToString()
        {
            return base.ToString() + $", reason: {reason}";
        }

        public new void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref reason);
        }
    }

    [Serializable]
    public class UnexpectedError : MessageFromHelmet
    {
        public string errorMessage;

        public UnexpectedError()
        {
            code = Code.UnexpectedError;
        }

        public UnexpectedError(string errorMessage)
        {
            code = Code.UnexpectedError;
            this.errorMessage = errorMessage;
        }

        public override string ToString()
        {
            return base.ToString() + $", error: {errorMessage}";
        }

        public new void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref errorMessage);
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref code);
    }
}