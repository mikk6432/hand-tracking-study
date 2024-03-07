using NetworkDiscoveryUnity;
using Unity.Netcode;
using UnityEngine;
using Unity.Netcode.Transports.UTP;

public class StartServer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
#if UNITY_EDITOR
        Debug.Log("Running in Unity Editor");
        NetworkManager.Singleton.StartServer();
        GetComponent<NetworkDiscovery>().EnsureServerIsInitialized();
        GetComponent<UnityTransport>().ConnectionData.Address = "localhost";
#elif UNITY_ANDROID
        GetComponent<NetworkDiscovery>().onReceivedServerResponse.AddListener((NetworkDiscovery.DiscoveryInfo info) =>
        {
            if (NetworkManager.Singleton.IsClient)
            {
                return;
            }
            GetComponent<UnityTransport>().ConnectionData.Address = info.EndPoint.Address.ToString();
            NetworkManager.Singleton.StartClient();
        });
        string model = SystemInfo.deviceModel;
        if (model.Contains("Quest"))
        {
            Debug.Log("Running on Oculus Quest");
        }
        else
        {
            Debug.Log("Running on another Android device");
        }
#else
        Debug.Log("Running on an unsupported platform");
#endif
    }
    private void Update()
    {
#if UNITY_EDITOR
        return;
#elif UNITY_ANDROID
        if (NetworkManager.Singleton.IsClient)
        {
            return;
        }
        Debug.Log("Sending broadcast");
        GetComponent<NetworkDiscovery>().SendBroadcast();
#endif
    }
}
