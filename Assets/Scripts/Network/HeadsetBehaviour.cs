using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using Unity.VisualScripting;

public class HeadsetBehaviour : NetworkBehaviour
{
    // Here comes the code for distinguishing between the server and the client
    // and loading the scene on the client
#if UNITY_EDITOR
    public UnityEditor.SceneAsset SceneAsset;
    private void OnValidate()
    {
        if (SceneAsset != null)
        {
            m_SceneName = SceneAsset.name;
        }
    }
#endif
    [SerializeField]
    private string m_SceneName;
    public override void OnNetworkSpawn()
    {
        Debug.Log("OnNetworkSpawn device loaded");
        if (IsServer && NetworkManager.Singleton.ConnectedClientsList.Count > 1)
        {
            Debug.Log("Only one client is allowed");
            DisconnectClientRpc();
        }
        if (!IsServer && !string.IsNullOrEmpty(m_SceneName) && IsOwner)
        {
            SceneManager.LoadScene(m_SceneName, LoadSceneMode.Additive);
        }
    }
    [ClientRpc]
    private void DisconnectClientRpc()
    {
        NetworkManager.Singleton.Shutdown();
        Destroy(NetworkManager.Singleton.gameObject);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.RefreshExperimentSummary message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.SavePrefs message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.ValidateTrial message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.InvalidateTrial message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.SetParticipantID message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.PrepareNextStep message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.StartNextStep message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.FinishTrainingStep message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.SetLeftHanded message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }

    [ClientRpc]
    public void SendClientRpc(MessageToHelmet.SetStepIsDone message)
    {
        Debug.Log("SendClientRpc received: " + message.ToString());
        FindAnyObjectByType<HelmetMainProcess>().Receive(message);
    }


    [ServerRpc]
    public void SendServerRpc(MessageFromHelmet.RequestTrialValidation message)
    {
        Debug.Log("SendServerRpc received: " + message.ToString());
        FindAnyObjectByType<DesktopMainProcess>().Receive(message);
    }

    [ServerRpc]
    public void SendServerRpc(MessageFromHelmet.Summary message)
    {
        Debug.Log("SendServerRpc received: " + message.ToString());
        FindAnyObjectByType<DesktopMainProcess>().Receive(message);
    }

    [ServerRpc]
    public void SendServerRpc(MessageFromHelmet.InvalidOperation message)
    {
        Debug.Log("SendServerRpc received: " + message.ToString());
        FindAnyObjectByType<DesktopMainProcess>().Receive(message);
    }

    [ServerRpc]
    public void SendServerRpc(MessageFromHelmet.UnexpectedError message)
    {
        Debug.Log("SendServerRpc received: " + message.ToString());
        FindAnyObjectByType<DesktopMainProcess>().Receive(message);
    }

}
