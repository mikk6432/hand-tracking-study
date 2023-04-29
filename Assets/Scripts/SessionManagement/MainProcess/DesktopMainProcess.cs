using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DesktopMainProcess: ExperimentNetworkServer
{
    // ui stuff
    [SerializeField] private Button refreshButton; 
    [SerializeField] private TextMeshProUGUI summaryIndexIndicator;
    
    [SerializeField] private TMP_InputField participantIDTextField;
    [SerializeField] private Button setParticipantIdButton;
    
    [SerializeField] private Button setLeftHanded;
    [SerializeField] private Button setRightHanded;
    
    [SerializeField] private TextMeshProUGUI experimentStepsTable;
    [SerializeField] private TextMeshProUGUI errorMessageDisplay;
    
    [SerializeField] private Button prepareButton;
    [SerializeField] private Button startButton;
    
    private int summaryIndex = 0;
    private MessageFromHelmet.RefreshExperimentSummary.ExperimentSummary summary;

    private string error;
    
    protected override void Start()
    {
        base.Start();

        refreshButton.onClick.AddListener(() => Send(new MessageToHelmet(MessageToHelmet.Code.RefreshExperimentSummary)));
        
        setLeftHanded.onClick.AddListener(() => Send(new MessageToHelmet.SetLeftHanded(true)));
        setRightHanded.onClick.AddListener(() => Send(new MessageToHelmet.SetLeftHanded(false)));
        
        setParticipantIdButton.onClick.AddListener(() =>
        {
            bool isInt = Int32.TryParse(participantIDTextField.text, out int participantId);
            if (!isInt)
            {
                Debug.LogError("Cannot set not integer participantId");
                return;
            }

            Send(new MessageToHelmet.SetParticipantID(participantId));
        });
        
        
    }

    protected override void Receive(MessageFromHelmet message)
    {
        switch (message.code)
        {
            case MessageFromHelmet.Code.RefreshExperimentSummary:
                Debug.Log(message);
                summaryIndex++;
                summary = (message as MessageFromHelmet.RefreshExperimentSummary)?.summary;
                error = null;
                Render();
                break;
            case MessageFromHelmet.Code.InvalidOperation:
                Debug.LogError(message);
                error = (message as MessageFromHelmet.InvalidOperation)?.reason;
                Render();
                break;
            case MessageFromHelmet.Code.UnexpectedError:
                Debug.LogError(message);
                error = (message as MessageFromHelmet.UnexpectedError)?.errorMessage;
                Render();
                break;
            default:
                throw new ArgumentException($"It seems you have implemented a new message from helmet but forget to handle in {nameof(Receive)} method");
        }
    }

    private void Render()
    {
        if (summaryIndex == 0)
        {
            refreshButton.gameObject.SetActive(false);
            summaryIndexIndicator.gameObject.SetActive(false);
            participantIDTextField.gameObject.SetActive(false);
            setParticipantIdButton.gameObject.SetActive(false);
            setLeftHanded.gameObject.SetActive(false);
            setRightHanded.gameObject.SetActive(false);
            experimentStepsTable.gameObject.SetActive(false);
            errorMessageDisplay.gameObject.SetActive(false);
            prepareButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(false);
            return;
        }
        
        // summary refresh stuff
        refreshButton.gameObject.SetActive(true);
        summaryIndexIndicator.gameObject.SetActive(true);
        summaryIndexIndicator.text = $"Showing ExpSummary №{summaryIndex}";
        
        // participant id controller stuff
        participantIDTextField.gameObject.SetActive(true);
        participantIDTextField.text = summary.participantID.ToString();
        setParticipantIdButton.gameObject.SetActive(summary.currentRunStage == HelmetMainProcess.RunStage.Idle);

        // hands controller stuff
        setLeftHanded.gameObject.SetActive(!summary.leftHanded && summary.currentRunStage == HelmetMainProcess.RunStage.Idle);
        setRightHanded.gameObject.SetActive(summary.leftHanded && summary.currentRunStage == HelmetMainProcess.RunStage.Idle);
        
        // error stuff
        errorMessageDisplay.gameObject.SetActive(error != null);
        errorMessageDisplay.text = error ?? "";

        // run configs table stuff
        var text = "Experiment Steps\n\nTYPE\t\tContext\t\tReferenceFrame";
        var runConfigs = HelmetMainProcess.GenerateRunConfigs(summary.participantID, summary.leftHanded);

        for (int i = 0; i < runConfigs.Length; i++)
        {
            var line = "";
            if (runConfigs[i].isMetronomeTraining)
            {
                line = "Training to go with metronome";
            }
            else
            {
                var type = runConfigs[i].isTraining ? "Training" : "Trial";
                var context = runConfigs[i].context == ExperimentManager.Context.Standing ? "Standing" : "Walking";
                var refFrame = Enum.GetName(typeof(ExperimentManager.ReferenceFrame), runConfigs[i].referenceFrame);
                line = $"{type}\t\t{context}\t\t{refFrame}";
            }
            
            if (i == summary.currentRunConfigIndex)
            {
                var color = summary.currentRunStage == HelmetMainProcess.RunStage.Running ? "green" :
                    summary.currentRunStage == HelmetMainProcess.RunStage.Preparing ? "yellow" :
                    "grey";
                line = $"<color=\"{color}\">{line}</color>";
            }

            text += "\n" + line;
        }

        experimentStepsTable.text = text;
    }
}