using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DesktopMainProcess: ExperimentNetworkServer
{
    // ui stuff
    [SerializeField] private TextMeshProUGUI connectionIndicator;
    
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
    [SerializeField] private Button finishTrainingButton;
    [SerializeField] private Button validateButton;
    [SerializeField] private Button invalidateButton;
    
    [SerializeField] private Button placeLightAndTrack;
    [SerializeField] private TMP_InputField skipNStepsInput;
    [SerializeField] private Button skipNStepsButton;
    
    private bool connected = false;
    
    private int summaryIndex = 0;
    private MessageFromHelmet.Summary summary;

    private string error;

    private bool awaitingValidation;
    
    protected override void Start()
    {
        base.Start();
        
        connectionEstablished.AddListener(() => { connected = true; Render(); });
        connectionLost.AddListener(() => { connected = false; Render(); });

        refreshButton.onClick.AddListener(() => Send(new MessageToHelmet(MessageToHelmet.Code.RefreshExperimentSummary)));
        
        setLeftHanded.onClick.AddListener(() => Send(new MessageToHelmet.SetLeftHanded(true)));
        setRightHanded.onClick.AddListener(() => Send(new MessageToHelmet.SetLeftHanded(false)));
        
        prepareButton.onClick.AddListener(() => Send(new MessageToHelmet(MessageToHelmet.Code.PrepareNextRun)));
        startButton.onClick.AddListener(() => Send(new MessageToHelmet(MessageToHelmet.Code.StartNextRun)));
        finishTrainingButton.onClick.AddListener(() => Send(new MessageToHelmet(MessageToHelmet.Code.FinishTraining)));
        
        placeLightAndTrack.onClick.AddListener(() => Send(new MessageToHelmet(MessageToHelmet.Code.PlaceTrackAndLight)));
        skipNStepsButton.onClick.AddListener(() =>
        {
            if (!Int32.TryParse(skipNStepsInput.text, out int stepsToSkip))
            {
                Debug.LogError("Cannot set skip non-integer stepsToSkip");
                return;
            }

            Send(new MessageToHelmet.SkipNSteps(stepsToSkip));
        });
        
        validateButton.onClick.AddListener(() =>
        {
            Send(new MessageToHelmet(MessageToHelmet.Code.ValidateTrial));
            awaitingValidation = false;
            Render();
        });
        invalidateButton.onClick.AddListener(() =>
        {
            Send(new MessageToHelmet(MessageToHelmet.Code.InvalidateTrial));
            awaitingValidation = false;
            Render();
        });

        setParticipantIdButton.onClick.AddListener(() =>
        {
            if (!Int32.TryParse(participantIDTextField.text, out int participantId))
            {
                Debug.LogError("Cannot set not integer participantId");
                return;
            }

            Send(new MessageToHelmet.SetParticipantID(participantId));
        });
        
        Render();
    }

    protected override void Receive(MessageFromHelmet message)
    {
        switch (message.code)
        {
            case MessageFromHelmet.Code.ExperimentSummary:
                Debug.Log(message);
                summaryIndex++;
                summary = (message as MessageFromHelmet.Summary);
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
            case MessageFromHelmet.Code.RequestTrialValidation:
                awaitingValidation = true;
                Render();
                break;
            default:
                throw new ArgumentException($"It seems you have implemented a new message from helmet but forget to handle in {nameof(Receive)} method");
        }
    }

    private void Render()
    {
        connectionIndicator.gameObject.SetActive(true);
        
        if (!connected || summaryIndex == 0) // if not connected or no summary receive yet
        {
            participantIDTextField.gameObject.SetActive(false);
            setParticipantIdButton.gameObject.SetActive(false);
            setLeftHanded.gameObject.SetActive(false);
            setRightHanded.gameObject.SetActive(false);
            experimentStepsTable.gameObject.SetActive(false);
            errorMessageDisplay.gameObject.SetActive(false);
            prepareButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(false);
            finishTrainingButton.gameObject.SetActive(false);
            validateButton.gameObject.SetActive(false);
            invalidateButton.gameObject.SetActive(false);
            skipNStepsButton.gameObject.SetActive(false);
            skipNStepsInput.gameObject.SetActive(false);
            placeLightAndTrack.gameObject.SetActive(false);
            
            if (!connected)
            {
                connectionIndicator.text = "<color=\"red\">NOT CONNECTED</color>";
                refreshButton.gameObject.SetActive(false);
                summaryIndexIndicator.gameObject.SetActive(false);
                return;
            }

            if (summaryIndex == 0)
            {
                connectionIndicator.text = "<color=\"green\">CONNECTED</color>";
                refreshButton.gameObject.SetActive(true);
                summaryIndexIndicator.gameObject.SetActive(true);
                summaryIndexIndicator.text = "No summary received yet";
                return;
            }
        }
        
        connectionIndicator.text = "<color=\"green\">CONNECTED</color>";

        // summary refresh stuff
        refreshButton.gameObject.SetActive(true);
        summaryIndexIndicator.gameObject.SetActive(true);
        summaryIndexIndicator.text = $"Showing ExpSummary №{summaryIndex}";
        
        // validation walking trial stuff
        validateButton.gameObject.SetActive(awaitingValidation);
        invalidateButton.gameObject.SetActive(awaitingValidation);
        
        // participant id controller stuff
        participantIDTextField.gameObject.SetActive(true);
        participantIDTextField.text = summary.id.ToString();
        var isIdle = summary.stage == (int)HelmetMainProcess.RunStage.Idle;
        participantIDTextField.readOnly = !isIdle;
        setParticipantIdButton.gameObject.SetActive(isIdle);

        // hands controller stuff
        setLeftHanded.gameObject.SetActive(!summary.left && isIdle);
        setRightHanded.gameObject.SetActive(summary.left && isIdle);
        
        // error stuff
        errorMessageDisplay.gameObject.SetActive(error != null);
        errorMessageDisplay.text = error ?? "";

        // run configs table stuff
        // var text = "Experiment Steps\n\nTYPE            Context         ReferenceFrame";
        var text = "Experiment Steps\n\nTYPE\t\t\tContext\t\t\tReferenceFrame";
        var runConfigs = HelmetMainProcess.GenerateRunConfigs(summary.id, summary.left);
        for (int i = 0; i < runConfigs.Length; i++)
        {
            var line = "";
            if (runConfigs[i].isPlacingComfortYAndZ)
            {
                line = "Placing UI where participant feel comfort";
            }
            else if (runConfigs[i].isMetronomeTraining)
            {
                line = "Training to go with metronome";
            }
            else
            {
                var type = runConfigs[i].isTraining ? "Training" : "Trial\t";
                var context = runConfigs[i].context == ExperimentManager.Context.Standing ? "Standing" : "Walking";
                var refFrame = Enum.GetName(typeof(ExperimentManager.ReferenceFrame), runConfigs[i].referenceFrame);
                line = $"{type}\t\t{context}\t\t\t{refFrame}";
            }

            if (i < summary.index) // means that run was fulfilled. Color it in blue
            {
                line = $"<color=\"blue\">{line}</color>";
            }
            else if (i == summary.index)
            {
                if (summary.stage == (int)HelmetMainProcess.RunStage.Idle) // make it Bold to indicate it will be next
                {
                    line = $"<b>{line}</b>";
                }
                else
                {
                    // yellow – preparing, green – running
                    var color = summary.stage == (int)HelmetMainProcess.RunStage.Running ? "green" : "yellow";
                    line = $"<color=\"{color}\">{line}</color>";
                }
            }

            text += "\n" + line;
        }
        experimentStepsTable.text = text;
        experimentStepsTable.gameObject.SetActive(true);
        
        // prepare/start/finishTraining stuff
        bool allRun = summary.index >= runConfigs.Length;
        if (allRun)
        {
            prepareButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(false);
            finishTrainingButton.gameObject.SetActive(false);
            return;
        }
        prepareButton.gameObject.SetActive(summary.stage != (int)HelmetMainProcess.RunStage.Running);
        startButton.gameObject.SetActive(summary.stage == (int)HelmetMainProcess.RunStage.Preparing);
        bool isCurrentTraining = runConfigs[summary.index].isTraining || runConfigs[summary.index].isMetronomeTraining;
        finishTrainingButton.gameObject.SetActive(isCurrentTraining && summary.stage == (int)HelmetMainProcess.RunStage.Running);
        
        placeLightAndTrack.gameObject.SetActive(true);
        skipNStepsButton.gameObject.SetActive(isIdle);
        skipNStepsInput.gameObject.SetActive(isIdle);
    }
}