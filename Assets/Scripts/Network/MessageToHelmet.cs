using System;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using Unity.Netcode;

[Serializable]
public abstract class MessageToHelmet : INetworkSerializable
{
    [Serializable]
    public enum Code
    {
        SetParticipantID,
        SetLeftHanded,
        SetStepIsDone,
        SavePrefs,
        RefreshExperimentSummary,

        PrepareNextRun,
        StartNextRun,
        FinishTraining,

        // trials stiff
        ValidateTrial,
        InvalidateTrial
    }

    public Code code;

    public MessageToHelmet()
    {
    }

    public override string ToString()
    {
        return $"ToHelmet: code={Enum.GetName(typeof(Code), code)}";
    }
    public class SavePrefs : MessageToHelmet
    {
        public SavePrefs()
        {
            code = Code.SavePrefs;
        }
    }
    public class RefreshExperimentSummary : MessageToHelmet
    {
        public RefreshExperimentSummary()
        {
            code = Code.RefreshExperimentSummary;
        }
    }
    public class ValidateTrial : MessageToHelmet
    {
        public ValidateTrial()
        {
            code = Code.ValidateTrial;
        }
    }
    public class InvalidateTrial : MessageToHelmet
    {
        public InvalidateTrial()
        {
            code = Code.InvalidateTrial;
        }
    }

    [Serializable]
    public class SetParticipantID : MessageToHelmet
    {
        public int participantID;

        public SetParticipantID()
        {
            code = Code.SetParticipantID;
        }

        public SetParticipantID(int _participantID)
        {
            code = Code.SetParticipantID;
            participantID = _participantID;
        }

        public override string ToString()
        {
            return base.ToString() + $", participantID={participantID}";
        }

        public new void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref participantID);
        }
    }

    [Serializable]
    public class PrepareNextStep : MessageToHelmet
    {
        public int index;

        public PrepareNextStep()
        {
            code = Code.PrepareNextRun;
        }

        public PrepareNextStep(int index)
        {
            code = Code.PrepareNextRun;
            this.index = index;
        }

        public override string ToString()
        {
            return base.ToString() + $", prepare={index}";
        }

        public new void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref index);
        }
    }

    [Serializable]
    public class StartNextStep : MessageToHelmet
    {
        public int index;

        public StartNextStep()
        {
            code = Code.StartNextRun;
        }

        public StartNextStep(int index)
        {
            code = Code.StartNextRun;
            this.index = index;
        }

        public override string ToString()
        {
            return base.ToString() + $", start={index}";
        }

        public new void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref index);
        }
    }

    [Serializable]
    public class FinishTrainingStep : MessageToHelmet
    {
        public int index;

        public FinishTrainingStep()
        {
            code = Code.FinishTraining;
        }

        public FinishTrainingStep(int index)
        {
            code = Code.FinishTraining;
            this.index = index;
        }

        public override string ToString()
        {
            return base.ToString() + $", index={index}";
        }

        public new void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref index);
        }
    }

    [Serializable]
    public class SetLeftHanded : MessageToHelmet
    {
        public bool leftHanded;

        public SetLeftHanded()
        {
            code = Code.SetLeftHanded;
        }

        public SetLeftHanded(bool _leftHanded)
        {
            code = Code.SetLeftHanded;
            leftHanded = _leftHanded;
        }

        public override string ToString()
        {
            return base.ToString() + $", leftHanded={leftHanded}";
        }

        public new void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref leftHanded);
        }
    }

    [Serializable]
    public class SetStepIsDone : MessageToHelmet
    {
        public int stepIndex;
        public bool done;

        public SetStepIsDone()
        {
            code = Code.SetStepIsDone;
        }

        public SetStepIsDone(int stepIndex, bool done)
        {
            code = Code.SetStepIsDone;
            this.stepIndex = stepIndex;
            this.done = done;
        }

        public new void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref code);
            serializer.SerializeValue(ref stepIndex);
            serializer.SerializeValue(ref done);
        }
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref code);
    }
}