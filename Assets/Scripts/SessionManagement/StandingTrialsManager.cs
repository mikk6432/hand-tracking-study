using System;
using System.Runtime.CompilerServices;
using Logging;
using UnityEngine;

public class StandingTrialsManager : MonoBehaviour
{
    private static float[] targetSizes = new[] { 0.15f, 0.025f, 0.035f };

    private static readonly string[] selectionsLogColumns = new string[]
    {
        "ParticipantID",
        "SessionID", // not supposed to change for one participant, only if errors force rerun and writing to new file
        "SelectionID",
        
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

    private int participantID;
    private AsyncHighFrequencyCSVLogger selectionsLogger;
    
    private enum ReferenceFrameStanding
    {
        Palm_Referenced,
        Palm_Referenced_Position_Only,
        Path_Referenced
    }

    [SerializeField] private GameObject PalmReferencedAnchor;
    [SerializeField] private GameObject PalmReferencedPositionOnlyAnchor;
    [SerializeField] private GameObject PathReferencedAnchor;
    
    
    private TargetManager targetManager;
    
    private enum ManagerState
    {
        Idle,
        AwaitingSelection,
        IndicatingSelectionResult
    }

    private void Start()
    {
        participantID = SocketCommandsListener.ParticipantID;

        SocketCommandsListener.goIdleScene.AddListener(OnTerminateTrial);
    }

    private void OnDestroy()
    {
        SocketCommandsListener.goIdleScene.RemoveListener(OnTerminateTrial);
    }

    private void LateUpdate()
    {
        // high frequency logging stuff here
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void HandleIdleState([CallerMemberName] string eventName = "")
    {
        switch (eventName)
        {
            
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void HandleAwaitingSelectionState([CallerMemberName] string eventName = "")
    {
        switch (eventName)
        {
            
        }
    }
    
    [MethodImpl(MethodImplOptions.NoInlining)]
    private void HandleIndicatingSelectionResultState([CallerMemberName] string eventName = "")
    {
        switch (eventName)
        {
            
        }
    }

    #region EventRedirectingMethods
    public void OnSelectorEnteredTargetZone()
    {
        
    }

    public void OnSelectorExitedTargetZone()
    {
        
    }

    public void OnTerminateTrial()
    {
        
    }
    
    
    
    #endregion // EventRedirectingMethods
}