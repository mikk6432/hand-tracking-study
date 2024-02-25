using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class ParticipantPrefs
{
    public int participantId;
    public bool leftHanded;
    public long doneBitmap;

    private static string IdToPath(int participantId)
    {
        return Path.Combine(UnityEngine.Application.persistentDataPath, $"{participantId}_prefs");
    }
    
    public static ParticipantPrefs ForParticipant(int participantId)
    {
        var path = IdToPath(participantId);

        ParticipantPrefs result;
        if (File.Exists(path))
        {
            var bytes = File.ReadAllBytes(path);
            MemoryStream ms = new MemoryStream(bytes);
            ms.Seek(0, 0);
            BinaryFormatter bf = new BinaryFormatter();
            result = (ParticipantPrefs)bf.Deserialize(ms);
        }
        else
        {
            result = new ParticipantPrefs();
            result.participantId = participantId;
            result.leftHanded = false;
            result.doneBitmap = 0;
        }

        return result;
    }

    public void Save()
    {
        var path = IdToPath(participantId);
        
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream ms = new MemoryStream();
        formatter.Serialize(ms, this);
        byte[] bytes = ms.ToArray();
        File.WriteAllBytes(path, bytes);
    }
}
