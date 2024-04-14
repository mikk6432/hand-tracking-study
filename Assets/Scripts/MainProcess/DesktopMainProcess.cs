using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DesktopMainProcess : ExperimentNetworkServer
{
    // ui stuff
    [SerializeField] private TextMeshProUGUI connectionIndicator;

    [SerializeField] private Button saveButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private TextMeshProUGUI summaryIndexIndicator;

    [SerializeField] private TMP_InputField participantIDTextField;
    [SerializeField] private Button setParticipantIdButton;

    [SerializeField] private Button setLeftHanded;
    [SerializeField] private Button setRightHanded;

    [SerializeField] private TextMeshProUGUI pointerText;
    [SerializeField] private Button incrementPointerButton;
    [SerializeField] private Button decrementPointerButton;

    [SerializeField] private TextMeshProUGUI experimentStepsTable;
    [SerializeField] private TextMeshProUGUI errorMessageDisplay;

    [SerializeField] private Button undoneButton;
    [SerializeField] private Button doneButton;
    [SerializeField] private Button prepareButton;
    [SerializeField] private Button startButton;
    [SerializeField] private Button finishTrainingButton;
    [SerializeField] private Button validateButton;
    [SerializeField] private Button invalidateButton;
    [SerializeField] private Button placeLightAndTrack;
    [SerializeField] private Toggle showHeadsetAdjustmentText;

    private bool connected = false;

    private int summaryIndex = 0;
    private MessageFromHelmet.Summary summary;

    private string error;

    private bool awaitingValidation;

    private int listLength = 0;

    private int pointer; // points to step, to which send the command (shows bold in table)

    protected override void Start()
    {
        base.Start();

        connectionEstablished.AddListener(() => { connected = true; Render(); });
        connectionLost.AddListener(() => { connected = false; Render(); });

        refreshButton.onClick.AddListener(() => Send(new MessageToHelmet(MessageToHelmet.Code.RefreshExperimentSummary)));

        setLeftHanded.onClick.AddListener(() => Send(new MessageToHelmet.SetLeftHanded(true)));
        setRightHanded.onClick.AddListener(() => Send(new MessageToHelmet.SetLeftHanded(false)));

        undoneButton.onClick.AddListener(() => Send(new MessageToHelmet.SetStepIsDone(pointer, false)));
        doneButton.onClick.AddListener(() => Send(new MessageToHelmet.SetStepIsDone(pointer, true)));
        prepareButton.onClick.AddListener(() => Send(new MessageToHelmet.PrepareNextStep(pointer)));
        startButton.onClick.AddListener(() => Send(new MessageToHelmet.StartNextStep(pointer)));
        finishTrainingButton.onClick.AddListener(() => Send(new MessageToHelmet.FinishTrainingStep(pointer)));

        placeLightAndTrack.onClick.AddListener(() => Send(new MessageToHelmet(MessageToHelmet.Code.PlaceTrackAndLight)));

        showHeadsetAdjustmentText.onValueChanged.AddListener((value) =>
        {
            Send(new MessageToHelmet.ToggleShowHeadsetAdjustmentText(value));
        });

        decrementPointerButton.onClick.AddListener(() =>
        {
            if (pointer < listLength - 1)
                pointer++;
            Render();
        });
        incrementPointerButton.onClick.AddListener(() =>
        {
            if (pointer > 0)
                pointer--;
            Render();
        });

        saveButton.onClick.AddListener(() => Send(new MessageToHelmet(MessageToHelmet.Code.SavePrefs)));

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
                if (summaryIndex == 1)
                    pointer = summary.index;
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
            case MessageFromHelmet.Code.UserError:
                Debug.LogWarning(message);
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
            undoneButton.gameObject.SetActive(false);
            doneButton.gameObject.SetActive(false);
            prepareButton.gameObject.SetActive(false);
            startButton.gameObject.SetActive(false);
            finishTrainingButton.gameObject.SetActive(false);
            validateButton.gameObject.SetActive(false);
            invalidateButton.gameObject.SetActive(false);
            pointerText.gameObject.SetActive(false);
            incrementPointerButton.gameObject.SetActive(false);
            decrementPointerButton.gameObject.SetActive(false);
            saveButton.gameObject.SetActive(false);
            placeLightAndTrack.gameObject.SetActive(false);
            showHeadsetAdjustmentText.gameObject.SetActive(false);

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

        saveButton.gameObject.SetActive(true);

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

        pointerText.text = pointer.ToString();
        pointerText.gameObject.SetActive(isIdle);
        incrementPointerButton.gameObject.SetActive(isIdle);
        decrementPointerButton.gameObject.SetActive(isIdle);

        // run configs table stuff
        var text = "Experiment Steps\n\nTYPE\t\t\tContext\t\t\tReferenceFrame";
        var runConfigs = HelmetMainProcess.GenerateRunConfigs(summary.id, summary.left);
        listLength = runConfigs.Length;
        for (int i = 0; i < runConfigs.Length; i++)
        {
            var line = "";
            if (runConfigs[i].isInitialStandingTraining)
            {
                var refFrame = Enum.GetName(typeof(ExperimentManager.ExperimentReferenceFrame), runConfigs[i].referenceFrame);
                line = $"Initial target selection training ({refFrame})";
            }
            else if (runConfigs[i].isMetronomeTraining)
            {
                line = "Training to go with metronome";
            }
            else
            {
                var type = runConfigs[i].isTraining ? "Training" : "Trial\t";
                var context = Enum.GetName(typeof(ExperimentManager.Context), runConfigs[i].context);
                var refFrame = Enum.GetName(typeof(ExperimentManager.ExperimentReferenceFrame), runConfigs[i].referenceFrame);
                line = $"{type}\t\t{context}\t\t\t{refFrame}";
            }

            if (i == summary.index)
            {
                var color = summary.stage == (int)HelmetMainProcess.RunStage.Running ? "green" : "yellow";
                line = $"<color=\"{color}\">{line}</color>";
            }
            else
            {
                var color = Bitmap.GetBool(summary.doneBitmap, i) ? "grey" : "white";
                line = $"<color=\"{color}\">{line}</color>";
            }

            if (i == pointer)
                line = $"<b>{line}</b>";

            text += "\n" + line;
        }
        experimentStepsTable.text = text;
        experimentStepsTable.gameObject.SetActive(true);

        // start, prepare, done, undone, finish stuff
        if (isIdle)
        {
            bool done = Bitmap.GetBool(summary.doneBitmap, pointer);
            prepareButton.gameObject.SetActive(!done && (pointer >= 0));
            doneButton.gameObject.SetActive(!done);
            undoneButton.gameObject.SetActive(done);

            startButton.gameObject.SetActive(false);
            finishTrainingButton.gameObject.SetActive(false);
        }
        else
        {
            doneButton.gameObject.SetActive(false);
            undoneButton.gameObject.SetActive(false);

            bool pointsToRunning = pointer == summary.index;
            if (!pointsToRunning)
            {
                prepareButton.gameObject.SetActive(false);
                startButton.gameObject.SetActive(false);
                finishTrainingButton.gameObject.SetActive(false);
            }
            else
            {
                prepareButton.gameObject.SetActive(summary.stage != (int)HelmetMainProcess.RunStage.Running);
                startButton.gameObject.SetActive(summary.stage == (int)HelmetMainProcess.RunStage.Preparing);
                bool isCurrentTraining = runConfigs[summary.index].isTraining || runConfigs[summary.index].isMetronomeTraining;
                finishTrainingButton.gameObject.SetActive(isCurrentTraining && summary.stage == (int)HelmetMainProcess.RunStage.Running);
            }
        }

        placeLightAndTrack.gameObject.SetActive(true);
        showHeadsetAdjustmentText.gameObject.SetActive(true);
    }
}