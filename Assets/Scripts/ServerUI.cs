using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ServerUI : MonoBehaviour
{
    [SerializeField] private Button startExperimentButton;
    [SerializeField] private Button stopExperimentButton;
    [SerializeField] private Button recalculatePathButton;
    [SerializeField] private TMPro.TMP_InputField personIdInputField;
    [SerializeField] private Toggle leftHandedToggle;
    [SerializeField] private Toggle standingToggle;
    [SerializeField] private TMPro.TextMeshProUGUI distancesText;
    [SerializeField] private TMPro.TextMeshProUGUI connectionStatusText;

    private void StartExperiment()
    {
        int personId = int.Parse(personIdInputField.text);
        bool leftHanded = leftHandedToggle.isOn;
        bool standing = standingToggle.isOn;
        NetworkManager.Singleton.ConnectedClientsList[0].PlayerObject.GetComponent<HeadsetBehaviour>();
    }

    private void StopExperiment()
    {
        NetworkManager.Singleton.ConnectedClientsList[0].PlayerObject.GetComponent<HeadsetBehaviour>();
    }

    private void RecalculatePath()
    {
        NetworkManager.Singleton.ConnectedClientsList[0].PlayerObject.GetComponent<HeadsetBehaviour>();
    }

    // Start is called before the first frame update
    void Start()
    {
        startExperimentButton.onClick.AddListener(StartExperiment);
        stopExperimentButton.onClick.AddListener(StopExperiment);
        recalculatePathButton.onClick.AddListener(RecalculatePath);
    }

    // Update is called once per frame
    void Update()
    {
        if (NetworkManager.Singleton.IsServer && NetworkManager.Singleton.ConnectedClientsList.Count > 0)
        {
            connectionStatusText.text = "Connected";
            connectionStatusText.color = Color.green;
        }
        else
        {
            connectionStatusText.text = "Connecting";
            connectionStatusText.color = Color.yellow;
        }
    }
}
