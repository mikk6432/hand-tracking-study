using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Logging;
using UnityEngine;

public class StandingTrialsManager : MonoBehaviour
{
    private static int targetsCount = 7;
    private static float[] targetSizes = new[] { 0.15f, 0.025f, 0.035f };
    private static readonly string[] selectionsLogColumns = new string[]
    {
        "ParticipantID",
        "SessionID", // not supposed to change for one participant, only if errors force rerun and writing to new file
        "SelectionID",
        "SystemClockTimestampMs",
        
        // CONDITIONS
        "WalkingMode",
        "ReferenceFrame",
        "TargetSize",

        // SELECTION
        "ActiveTargetIndex",
        "SelectionSuccess",
        "SelectionDuration",
        "Selection_ActiveTargetLocalX",
        "Selection_ActiveTargetLocalY",
    };
    private static readonly string[] highFrequencyLogColumns = new string[]
    {
        "ParticipantID",
        "SessionID", // not supposed to change for one participant, only if errors force rerun and writing to new file
        "MeasurementID",
        
        // CONDITIONS
        "WalkingMode",
        "ReferenceFrame",
        "TargetSize",
        
        // TIME
        "HumanReadableTimestampUTC",
        "SystemClockTimestampMs",
        
        // RENDER TIME DATA
        "HeadPositionX",
        "HeadPositionY",
        "HeadPositionZ",
        "HeadOrientationForwardX",
        "HeadOrientationForwardY",
        "HeadOrientationForwardZ",
        "HeadOrientationUpX",
        "HeadOrientationUpY",
        "HeadOrientationUpZ",
        
        "PalmPositionX",
        "PalmPositionY",
        "PalmPositionZ",
        "PalmOrientationForwardX",
        "PalmOrientationForwardY",
        "PalmOrientationForwardZ",
        "PalmOrientationUpX",
        "PalmOrientationUpY",
        "PalmOrientationUpZ",
        
        "SelectorPositionX",
        "SelectorPositionY",
        "SelectorPositionZ",
        "SelectorOrientationForwardX",
        "SelectorOrientationForwardY",
        "SelectorOrientationForwardZ",
        "SelectorOrientationUpX",
        "SelectorOrientationUpY",
        "SelectorOrientationUpZ",
        
        "AllTargetsPositionWorldX",
        "AllTargetsPositionWorldY",
        "AllTargetsPositionWorldZ",
        "AllTargetsOrientationForwardWorldX",
        "AllTargetsOrientationForwardWorldY",
        "AllTargetsOrientationForwardWorldZ",
        "AllTargetsOrientationUpWorldX",
        "AllTargetsOrientationUpWorldY",
        "AllTargetsOrientationUpWorldZ",
        
        "ActiveTargetPositionWorldX",
        "ActiveTargetPositionWorldY",
        "ActiveTargetPositionWorldZ",
        "ActiveTargetOrientationForwardWorldX",
        "ActiveTargetOrientationForwardWorldY",
        "ActiveTargetOrientationForwardWorldZ",
        "ActiveTargetOrientationUpWorldX",
        "ActiveTargetOrientationUpWorldY",
        "ActiveTargetOrientationUpWorldZ",
    };

    private AsyncHighFrequencyCSVLogger selectionsLogger;
    private AsyncHighFrequencyCSVLogger highFrequencyCsvLogger;

    private int participantID;
    private int selectionsDone;    
    private ManagerState _state = ManagerState.Idle;
    
    [SerializeField] private TargetManager targetManager;
    [Space]
    [SerializeField] private GameObject PalmReferencedAnchor;
    [SerializeField] private GameObject PalmReferencedPositionOnlyAnchor;
    [SerializeField] private GameObject PathReferencedAnchor;
    private DateTime _firstSystemClockTimestamp;
    private ReferenceFrameStanding[] referenceFramesSequence;
    
    private enum ReferenceFrameStanding
    {
        Palm_Referenced,
        Palm_Referenced_Position_Only,
        Path_Referenced
    }
    private enum ManagerState
    {
        Idle,
        AwaitingSelection, // active target is shown and we wait for the participant to select it
        IndicatingSelectionResult // participant selected target (or not-successfull), but his index finger tip 
        // has not exited targets zone yet (we are showing green/yellow on the target at the moment)
    }

    private void Start()
    {
        participantID = SocketCommandsListener.ParticipantID;
        _firstSystemClockTimestamp = DateTime.Now;

        SocketCommandsListener.goIdleSceneForced.AddListener(OnGoIdleSceneForced);
        targetManager.selectorEnteredTargetsZone.AddListener(OnSelectorEnteredTargetZone);
        targetManager.selectorExitedTargetsZone.AddListener(OnSelectorExitedTargetZone);

        // sorry for js-like code. It just inline calculates sequence of ref frames, depending on participantID
        referenceFramesSequence = new Func<ReferenceFrameStanding[]>(() =>
        {
            var frames = (ReferenceFrameStanding[]) Enum.GetValues(typeof(ReferenceFrameStanding));
            var result = new ReferenceFrameStanding[frames.Length];

            int margin = participantID % targetsCount;
            
            int added = 0;
            for (int i = margin; i < frames.Length; i++) result[added++] = frames[i];
            for (int i = 0; i < margin; i++) result[added++] = frames[i];

            return result;
        })();
        
        try
        {
            // TODO: add sessionID to filename
            // string _fileName = $"{participantID.ToString()}_{0}_selections.csv";
            string _fileName = $"{participantID.ToString()}_{(new System.Random().Next() % 100)}_selections.csv";

            // Logging stuff
            selectionsLogger = new AsyncHighFrequencyCSVLogger(_fileName);
            if (!selectionsLogger.HasBeenInitialised())
            {
                // Adding a header
                selectionsLogger.AddColumns(selectionsLogColumns);
                selectionsLogger.Initialise();
            }
            else
            {
                Debug.LogError("logger has already been initialized");
            }
        }
        catch (IOException)
        {
            Debug.LogError("some problem with initializing logger");
        }
    }

    private void OnDestroy()
    {
        SocketCommandsListener.goIdleSceneForced.RemoveListener(OnGoIdleSceneForced);
    }

    private void LateUpdate()
    {
        // high frequency logging stuff here
    }

    private GameObject GetReferenceFrameAnchor(ReferenceFrameStanding rf)
    {
        switch (rf)
        {
            case ReferenceFrameStanding.Palm_Referenced:
                return PalmReferencedAnchor;
            case ReferenceFrameStanding.Palm_Referenced_Position_Only:
                return PalmReferencedPositionOnlyAnchor;
            case ReferenceFrameStanding.Path_Referenced:
                return PathReferencedAnchor;
            default:
                throw new Exception($"No such StandingReferenceFrame {rf}");
        }
    }

    private static IEnumerable<ReferenceFrameStanding> GetReferenceFrameSequence(int participantID)
    {
        var frames = (ReferenceFrameStanding[])Enum.GetValues(typeof(ReferenceFrameStanding));

        int margin = participantID % targetsCount;

        for (int i = margin; i < targetsCount; i++) yield return frames[i];
        for (int i = 0; i < margin; i++) yield return frames[i];
    }

    private void TerminateTrial()
    {
        // TODO: go fack
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void HandleIdleState([CallerMemberName] string eventName = "")
    {
        switch (eventName)
        {
            case nameof(OnSelectorEnteredTargetZone):
                throw new Exception($"{nameof(OnSelectorEnteredTargetZone)} got called in the '{ManagerState.Idle}' state. It is not supposed to happen");
            case nameof(OnSelectorExitedTargetZone):
                throw new Exception($"{nameof(OnSelectorExitedTargetZone)} got called in the '{ManagerState.Idle}' state. It is not supposed to happen");
            case nameof(OnGoIdleSceneForced):
                StartCoroutine(SceneSwitchingController.LoadSceneAsync(SceneSwitchingController.Scene.Idle));
                break;
            default:
                throw new Exception($"{nameof(HandleIdleState)} got called from the unknown method: {eventName}");
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void HandleAwaitingSelectionState([CallerMemberName] string eventName = "")
    {
        switch (eventName)
        {
            case nameof(OnSelectorEnteredTargetZone):
                
                // зафиксировать выбор и залогировать
                // перевести в состояние "ждем выхода из зоны"
                // не забыть проверить, что это не первый выбор в этом фрэйме
                var row = selectionsLogger.CurrentRow;
                row.SetColumnValue("ParticipantID", participantID.ToString());
                row.SetColumnValue("SessionID", 0);
                // row.SetColumnValue("SelectionID");
                break;
                
                
            case nameof(OnSelectorExitedTargetZone):
                throw new Exception($"{nameof(OnSelectorExitedTargetZone)} got called in the '{ManagerState.AwaitingSelection}' state. It is not supposed to happen");
            case nameof(OnGoIdleSceneForced):
                // TODO: add terminate stuff here
                StartCoroutine(SceneSwitchingController.LoadSceneAsync(SceneSwitchingController.Scene.Idle));
                break;
            default:
                throw new Exception($"{nameof(HandleAwaitingSelectionState)} got called from the unknown method: {eventName}");
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void HandleIndicatingSelectionResultState([CallerMemberName] string eventName = "")
    {
        switch (eventName)
        {
            case nameof(OnSelectorExitedTargetZone):
                // запустить таймер для следующего выбора цели
                // поменять referenceFrame, если закончил с выбором семи целей здесь
                // перевести в состояние awaitingSelection
                // если респондет все сделал, то сохранить данные на диск в логере, удалить цели и перевести в состояние idle  
            case nameof(OnSelectorEnteredTargetZone):
                throw new Exception($"{nameof(OnSelectorEnteredTargetZone)} got called in the '{ManagerState.IndicatingSelectionResult}' state. It is not supposed to happen");
            case nameof(OnGoIdleSceneForced):
                // TODO: add terminate stuff here
                StartCoroutine(SceneSwitchingController.LoadSceneAsync(SceneSwitchingController.Scene.Idle));
                break;
            default:
                throw new Exception($"{nameof(HandleIndicatingSelectionResultState)} got called from the unknown method: {eventName}");
        }
    }

    #region EventRedirectingMethods
    public void OnSelectorEnteredTargetZone()
    {
        switch (_state)
        {
            case ManagerState.Idle:
                HandleIdleState();
                break;
            case ManagerState.AwaitingSelection:
                HandleAwaitingSelectionState();
                break;
            case ManagerState.IndicatingSelectionResult:
                HandleIndicatingSelectionResultState();
                break;
            default:
                throw new Exception($"It seems that you've implemented the new state '{_state}' but forgot to add it here");
        }
    }

    public void OnSelectorExitedTargetZone()
    {
        switch (_state)
        {
            case ManagerState.Idle:
                HandleIdleState();
                break;
            case ManagerState.AwaitingSelection:
                HandleAwaitingSelectionState();
                break;
            case ManagerState.IndicatingSelectionResult:
                HandleIndicatingSelectionResultState();
                break;
            default:
                throw new Exception($"It seems that you've implemented the new state '{_state}' but forgot to add it here");
        }
    }

    private void OnGoIdleSceneForced()
    {
        switch (_state)
        {
            case ManagerState.Idle:
                HandleIdleState();
                break;
            case ManagerState.AwaitingSelection:
                HandleAwaitingSelectionState();
                break;
            case ManagerState.IndicatingSelectionResult:
                HandleIndicatingSelectionResultState();
                break;
            default:
                throw new Exception($"It seems that you've implemented the new state '{_state}' but forgot to add it here");
        }
    }
    #endregion // EventRedirectingMethods
}