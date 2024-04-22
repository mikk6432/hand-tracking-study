using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

// ReSharper disable once CheckNamespace
public partial class ExperimentManager : MonoBehaviour
{
    // not used for trainings. To finish training, method (OnServerSaidFinishTraining should be called)
    public readonly UnityEvent trialsFinished = new();
    public readonly UnityEvent requestTrialValidation = new();
    public readonly UnityEvent<string> unexpectedErrorOccured = new();
    public readonly UnityEvent<string> userMistake = new();

    private State _state;
    private RunConfig _runConfig;

    #region MonoBehaviour methods

    private void Start()
    {
        targetsManager.selectorEnteredTargetsZone.AddListener(OnSelectorEnteredTargetZone);
        targetsManager.selectorExitedTargetsZone.AddListener(OnSelectorExitedTargetZone);
        targetsManager.selectorExitedWrongSide.AddListener(OnSelectorExitedWrongSide);
        metronome.enabled = false;

        // Start app with track and cube invisible
        targetsManager.hideCube();
        walkingStateTrigger.enabled = false;

        walkingStateTrigger.ParticipantEntered.AddListener(OnParticipantEnteredTrack);
        walkingStateTrigger.ParticipantSwervedOff.AddListener(OnParticipantSwervedOffTrack);
        walkingStateTrigger.ParticipantSlowedDown.AddListener(OnParticipantSlowedDown);
        walkingStateTrigger.ParticipantFinished.AddListener(OnParticipantFinishedTrack);
        walkingStateTrigger.enabled = false;
    }

    private void OnDestroy()
    {
        targetsManager.selectorEnteredTargetsZone.RemoveListener(OnSelectorEnteredTargetZone);
        targetsManager.selectorExitedTargetsZone.RemoveListener(OnSelectorExitedTargetZone);
        targetsManager.selectorExitedWrongSide.RemoveListener(OnSelectorExitedWrongSide);

        walkingStateTrigger.ParticipantEntered.RemoveListener(OnParticipantEnteredTrack);
        walkingStateTrigger.ParticipantSwervedOff.RemoveListener(OnParticipantSwervedOffTrack);
        walkingStateTrigger.ParticipantSlowedDown.RemoveListener(OnParticipantSlowedDown);
        walkingStateTrigger.ParticipantFinished.RemoveListener(OnParticipantFinishedTrack);
    }

    // absolutely crazy thing
    private Action doOnNextUpdate;
    private void Update()
    {
        if (doOnNextUpdate != null)
        {
            UnityEngine.Debug.Log("Calling doOnNextUpdate and make it null");
            doOnNextUpdate();
            doOnNextUpdate = null;
        }
    }

    private void LateUpdate()
    {
        // asyncHighFrequencyLogging stuff goes here

        if (!highFrequencyLoggingIsOnFlag) return;
        // now we assume that we are inside trial session and logger is initialized already and participant is selecting targets

        try
        {
            LogHighFrequencyRow();
        }
        catch (Exception e)
        {
            unexpectedErrorOccured.Invoke($"LateUpdate: {e.Message}\n\n {e.StackTrace.Substring(0, 70)}");
        }
    }
    #endregion

    #region Targets stuff
    [SerializeField] private TargetsManager targetsManager;
    [SerializeField] private SelectorAnimatedProjector selectorProjector;
    private bool listeningTargetsEventsFlag;

    private static IEnumerator<TargetsManager.TargetSizeVariant> GenerateTargetSizesSequence(bool isTraining = false)
    {
        return TargetsManager.GenerateTargetSizesSequence(isTraining);
    }

    private static IEnumerator<TargetsManager.TargetSizeVariant> ReGenerateTargetSizesSequence(IEnumerator<TargetsManager.TargetSizeVariant> prev, bool isTraining = false)
    {
        if (isTraining) return prev;
        var newSequence = GenerateTargetSizesSequence(isTraining);
        var prevList = new List<TargetsManager.TargetSizeVariant>
        {
            prev.Current
        };
        while (prev.MoveNext())
        {
            prevList.Add(prev.Current);
        }
        var newSequenceList = new List<TargetsManager.TargetSizeVariant>();
        while (newSequence.MoveNext())
        {
            foreach (var prevItem in prevList)
            {
                if (prevItem == newSequence.Current)
                {
                    newSequenceList.Add(newSequence.Current);
                    break;
                }
            }
        }
        UnityEngine.Debug.Log("ReGenerated target sizes sequence");
        UnityEngine.Debug.Log($"Prev: {string.Join(", ", prevList.Select(ts => ts.ToString()))}");
        UnityEngine.Debug.Log($"New: {string.Join(", ", newSequenceList.Select(ts => ts.ToString()))}");
        return newSequenceList.GetEnumerator();
    }

    private static IEnumerator<int> GenerateTargetsIndexesSequence()
    {
        return TargetsManager.GenerateTargetsIndexesSequence();
    }
    #endregion

    [SerializeField] private GameObject hmdAdjustmentText;

    #region Track & Light stuff
    [Space]
    [SerializeField] private GameObject straightTrack;
    [SerializeField] private GameObject circleTrack;
    [SerializeField] private GameObject sceneLight; // remark: we interpret it as track in standing context. Hand Ref and path ref depend on it, actually

    [SerializeField] private WalkingStateTrigger walkingStateTrigger;
    private bool listeningTrackEventsFlag;

    [SerializeField] private GameObject walkingDirection; // walking context (relative to track)
    [SerializeField] private GameObject standingDirection; // standing context (relative to light)
    [SerializeField] private GameObject directionArrow;

    private static CircleDirections GetRandomcircleDirection()
    {
        var random = new System.Random();
        var values = Enum.GetValues(typeof(CircleDirections));

        // Select a random value from the enum
        CircleDirections randomDirection = (CircleDirections)values.GetValue(random.Next(values.Length));
        return randomDirection;
    }

    public enum CircleDirections
    {
        Clockwise,
        CounterClockwise,
    }

    private void UpdateDirectionArrow()
    {
        var newCircleDirection = GetRandomcircleDirection();
        if (newCircleDirection == CircleDirections.Clockwise) directionArrow.transform.localEulerAngles = new Vector3(-90, 0, 0);
        else directionArrow.transform.localEulerAngles = new Vector3(-90, 180, 0);
        currentCircleDirection = newCircleDirection;
    }

    #endregion

    #region Sound stuff
    [Space]
    [SerializeField] private Metronome metronome;
    [SerializeField] private uint walkingTempo = 90;
    //[SerializeField] private uint joggingTempo = 140;
    [SerializeField] private GameObject errorIndicator;

    void ShowErrorToParticipant(string msg)
    {
        // var headsetTransform = headset.transform;
        // var errorPosition = headsetTransform.position + headsetTransform.forward * 0.5f;
        // errorIndicator.transform.SetPositionAndRotation(errorPosition, headsetTransform.rotation);

        // we assume that errorIndicator is child of headset with just forward offset about 0.5 meters.
        userMistake.Invoke(msg);
        errorIndicator.SetActive(true); // will be set inactive automatically 
    }
    #endregion

    #region Head stuff
    [Space]
    [SerializeField] private GameObject headset;
    [SerializeField] private GameObject neckBase;

    private (Vector3 position, Quaternion rotation) HeadsetOXZProjection()
    {
        var headsetTransform = headset.transform;
        var headsetPosition = headsetTransform.position;
        var position = new Vector3(headsetPosition.x, 0, headsetPosition.z);

        var headsetForward = headsetTransform.forward;
        var rotation = Quaternion.LookRotation(new Vector3(headsetForward.x, 0, headsetForward.z));
        return (position, rotation);
    }
    #endregion

    #region Hands stuff
    [Space]
    // oculus hands here. Note, that we keep inactive gameObjects which we don't use 
    [SerializeField] private GameObject leftIndexTip;
    [SerializeField] private GameObject rightIndexTip;
    [SerializeField] private GameObject leftPalmCenter;
    [SerializeField] private GameObject rightPalmCenter;
    private GameObject dominantHandIndexTip; // holds selector
    private GameObject weakHandPalmCenter; // holds target
    private GameObject dominantHandPalmCenter; // just for logging

    private void ActualizeHands()
    {
        if (_runConfig.leftHanded)
        {
            targetsManager.ChangeHandedness(TargetsManager.Handed.Left);

            dominantHandPalmCenter = leftPalmCenter;
            dominantHandIndexTip = leftIndexTip;
            weakHandPalmCenter = rightPalmCenter;
        }
        else
        {
            targetsManager.ChangeHandedness(TargetsManager.Handed.Right);

            dominantHandIndexTip = rightIndexTip;
            weakHandPalmCenter = leftPalmCenter;
            dominantHandPalmCenter = rightPalmCenter;
        }
    }
    #endregion

    #region Reference Frames stuff
    [Space] // reference frames go here. Note, that we keep inactive gameObjects which we don't use
    // left-right for palmRef
    // left-right * standing-walking for handRef
    // standing-walking for pathRef
    // only walking for pathRefNeck
    [SerializeField] private GameObject[] leftHandedReferenceFrames;
    [SerializeField] private GameObject[] rightHandedReferenceFrames;
    private GameObject activeRefFrame;
    private int activeRefFrameIndex;
    // as path referenced, but depends on hand (not head)
    [Space]
    [SerializeField] private GameObject UIPlacerPath;
    // [SerializeField] private GameObject UIPlacerChest;

    private void ActualizeReferenceFrames()
    {
        var refFrames = _runConfig.leftHanded ? leftHandedReferenceFrames : rightHandedReferenceFrames;
        activeRefFrame = Array.Find(refFrames, rf => rf.GetComponent<ReferenceFrame>().referenceFrameName == _runConfig.referenceFrame);
        targetsManager.Anchor = activeRefFrame;
    }

    private void UpdatePathRefFrames()
    {
        foreach (var refFrame in leftHandedReferenceFrames.Concat(rightHandedReferenceFrames))
        {
            // if (refFrame.GetComponent<ReferenceFrame>().referenceFrameName == ExperimentReferenceFrame.ChestReferenced)
            // {
            //     refFrame.GetComponent<ReferenceFrame>().UpdateReferenceFrame(UIPlacerChest.transform);
            // }
            if (refFrame.GetComponent<ReferenceFrame>().referenceFrameName == ExperimentReferenceFrame.PathReferenced)
            {
                refFrame.GetComponent<ReferenceFrame>().UpdateReferenceFrame(UIPlacerPath.transform);
            }
        }
    }
    #endregion

    #region Current stuff (time, sequences and so on)
    private int _targetsSelected;
    private int _selectionsValidated; // used only for walking trials to reset _targetsSelected when server responded with invalidate result
    private int _measurementId;
    private float activateFirstTargetMoment;
    private float selectFirstTargetMoment;
    private float selectPreviousTargetMoment;
    private IEnumerator<TargetsManager.TargetSizeVariant> targetSizesSequence;
    private CircleDirections currentCircleDirection;
    private IEnumerator<int> targetsIndexesSequence;

    private float GenerateTimeToActivateFirstTarget()
    {
        if (_runConfig.context == Context.Standing)
            return 1f + UnityEngine.Random.Range(0f, 1f);

        float stepFrequencyInSeconds = 60f / metronome.Tempo;
        return 0.5f + UnityEngine.Random.Range(0f, 2f * stepFrequencyInSeconds);
    }
    #endregion

    #region Logging stuff
    private static readonly string[] selectionLogColumns =
    {
        // ids
        "ParticipantID",
        "SelectionID",
        
        // conditions
        "Context", // Standing, Walking, Circle
        "CircleDirection", // Clockwise, CounterClockwise
        "ReferenceFrame", // 0 – palmReferenced, 1 – handReferenced, 2 – pathReferenced 
        "TargetSize", // 0.015 or 0.025 or 0.035
        "DominantHand", // Which hand index tip selects targets. 0 – means right, 1 – means left
        
        // time
        "HumanReadableTimestampUTC", // absolute time of the selection
        "SystemClockTimestampMs", // time passed from selecting the first target. For the 1st selection equal to 0
        
        // selection
        "ActiveTargetIndex",
        "AbsoluteTargetPositionX",
        "AbsoluteTargetPositionY",
        "AbsoluteSelectionPositionX",
        "AbsoluteSelectionPositionY",
        "LocalSelectionPositionX",
        "LocalSelectionPositionY",
        "Success", // 0 or 1, where 1 means that selection was successful
        "SelectionDuration" // time passed from the previous selection. For the 1st selection equal to 0
    };
    private static readonly string[] highFrequencyLogColumns =
    {
        // ids
        "ParticipantID",
        "MeasurementID", // increments every measurement (90Hz). Starts with 0
        
        // conditions
        "Context", // Standing, Walking, Circle
        "CircleDirection", // Clockwise, CounterClockwise
        "ReferenceFrame", // 0 – palmReferenced, 1 – handReferenced, 2 – pathReferenced
        "TargetSize", // 0.015 or 0.025 or 0.035
        "DominantHand", // Which hand index tip selects targets. 0 – means right, 1 – means left
        
        // time
        "HumanReadableTimestampUTC", // absolute time of the selection
        "SystemClockTimestampMs", // time passed from start (activating the first target)
        
        // track transform
        "TrackPositionX",
        "TrackPositionY",
        "TrackPositionZ",
        "TrackForwardX",
        "TrackForwardY",
        "TrackForwardZ",
        "TrackUpX",
        "TrackUpY",
        "TrackUpZ",
        "TrackQuaternionX",
        "TrackQuaternionY",
        "TrackQuaternionZ",
        "TrackQuaternionW",
        
        // WalkingDirectionTransform
        "WalkingDirectionPositionX",
        "WalkingDirectionPositionY",
        "WalkingDirectionPositionZ",
        "WalkingDirectionForwardX",
        "WalkingDirectionForwardY",
        "WalkingDirectionForwardZ",
        "WalkingDirectionUpX",
        "WalkingDirectionUpY",
        "WalkingDirectionUpZ",
        "WalkingDirectionQuaternionX",
        "WalkingDirectionQuaternionY",
        "WalkingDirectionQuaternionZ",
        "WalkingDirectionQuaternionW",
        
        // head (CenterEyeAnchor)
        "HeadPositionX",
        "HeadPositionY",
        "HeadPositionZ",
        "HeadForwardX",
        "HeadForwardY",
        "HeadForwardZ",
        "HeadUpX",
        "HeadUpY",
        "HeadUpZ",
        "HeadQuaternionX",
        "HeadQuaternionY",
        "HeadQuaternionZ",
        "HeadQuaternionW",
        
        // neck model
        "NeckBasePositionX",
        "NeckBasePositionY",
        "NeckBasePositionZ",
        "NeckBaseForwardX",
        "NeckBaseForwardY",
        "NeckBaseForwardZ",
        "NeckBaseUpX",
        "NeckBaseUpY",
        "NeckBaseUpZ",
        "NeckBaseQuaternionX",
        "NeckBaseQuaternionY",
        "NeckBaseQuaternionZ",
        "NeckBaseQuaternionW",

        // dominant hand palmCenterAnchor
        "DominantPalmCenterPositionX",
        "DominantPalmCenterPositionY",
        "DominantPalmCenterPositionZ",
        "DominantPalmCenterForwardX",
        "DominantPalmCenterForwardY",
        "DominantPalmCenterForwardZ",
        "DominantPalmCenterUpX",
        "DominantPalmCenterUpY",
        "DominantPalmCenterUpZ",
        "DominantPalmCenterQuaternionX",
        "DominantPalmCenterQuaternionY",
        "DominantPalmCenterQuaternionZ",
        "DominantPalmCenterQuaternionW",
        
        // dominant hand index finger tip
        "DominantIndexTipPositionX",
        "DominantIndexTipPositionY",
        "DominantIndexTipPositionZ",
        "DominantIndexTipForwardX",
        "DominantIndexTipForwardY",
        "DominantIndexTipForwardZ",
        "DominantIndexTipUpX",
        "DominantIndexTipUpY",
        "DominantIndexTipUpZ",
        "DominantIndexTipQuaternionX",
        "DominantIndexTipQuaternionY",
        "DominantIndexTipQuaternionZ",
        "DominantIndexTipQuaternionW",
        
        // weak hand palmCenterAnchor
        "WeakPalmCenterPositionX",
        "WeakPalmCenterPositionY",
        "WeakPalmCenterPositionZ",
        "WeakPalmCenterForwardX",
        "WeakPalmCenterForwardY",
        "WeakPalmCenterForwardZ",
        "WeakPalmCenterUpX",
        "WeakPalmCenterUpY",
        "WeakPalmCenterUpZ",
        "WeakPalmCenterQuaternionX",
        "WeakPalmCenterQuaternionY",
        "WeakPalmCenterQuaternionZ",
        "WeakPalmCenterQuaternionW",
        
        // all targets (center of targets circle)
        "AllTargetsPositionX",
        "AllTargetsPositionY",
        "AllTargetsPositionZ",
        "AllTargetsForwardX",
        "AllTargetsForwardY",
        "AllTargetsForwardZ",
        "AllTargetsUpX",
        "AllTargetsUpY",
        "AllTargetsUpZ",
        "AllTargetsQuaternionX",
        "AllTargetsQuaternionY",
        "AllTargetsQuaternionZ",
        "AllTargetsQuaternionW",
        
        // active target
        "ActiveTargetPositionX",
        "ActiveTargetPositionY",
        "ActiveTargetPositionZ",
        "ActiveTargetForwardX",
        "ActiveTargetForwardY",
        "ActiveTargetForwardZ",
        "ActiveTargetUpX",
        "ActiveTargetUpY",
        "ActiveTargetUpZ",
        "ActiveTargetQuaternionX",
        "ActiveTargetQuaternionY",
        "ActiveTargetQuaternionZ",
        "ActiveTargetQuaternionW",
        
        // 2d coordinates
        "SelectorProjectionOntoAllTargetsX",
        "SelectorProjectionOntoAllTargetsY",
        "ActiveTargetIndex",
        "ActiveTargetInsideAllTargetsX",
        "ActiveTargetInsideAllTargetsY",
        "IsSelectorInsideCollider", // 0 or 1, where 1 means yes he is inside
        "DistanceFromSelectorToAllTargetsOXYPlane", // maybe negative (if he is inside). Note, that collider is not infinite, just about 20-25cm. If outside it, always positive distance
    };

    private bool highFrequencyLoggingIsOnFlag;
    private AsyncHighFrequencyCSVLogger _selectionsLogger;
    private AsyncHighFrequencyCSVLogger _highFrequencyLogger;
    private string currentSelectionsLoggerFilename;
    private string currentHighFrequencyLoggerFilename;

    private void LogSelectionRow()
    {
        bool isFirstWithSuchSize = _targetsSelected % TargetsManager.TargetsCount == 1;

        int systemClockMilliseconds, selectionDurationMilliseconds;
        if (isFirstWithSuchSize)
        {
            selectFirstTargetMoment = Time.realtimeSinceStartup;
            systemClockMilliseconds = 0;
            selectionDurationMilliseconds = 0;
            selectPreviousTargetMoment = Time.realtimeSinceStartup;
        }
        else
        {
            systemClockMilliseconds = (int)((Time.realtimeSinceStartup - selectFirstTargetMoment) * 1000);
            selectionDurationMilliseconds = (int)((Time.realtimeSinceStartup - selectPreviousTargetMoment) * 1000);
            selectPreviousTargetMoment = Time.realtimeSinceStartup;
        }

        var row = _selectionsLogger.CurrentRow;

        // ids
        row.SetColumnValue("ParticipantID", _runConfig.participantID);
        row.SetColumnValue("SelectionID", _targetsSelected);

        var selection = targetsManager.LastSelectionData;

        // conditions
        row.SetColumnValue("Context", Enum.GetName(typeof(Context), _runConfig.context));
        row.SetColumnValue("CircleDirection", _runConfig.context == Context.Circle ? Enum.GetName(typeof(CircleDirections), currentCircleDirection) : "");
        row.SetColumnValue("ReferenceFrame", Enum.GetName(typeof(ExperimentReferenceFrame), _runConfig.referenceFrame));
        row.SetColumnValue("TargetSize", selection.targetSize);
        row.SetColumnValue("DominantHand", _runConfig.leftHanded ? "Left" : "Right");

        // time
        var currentTime = Time.realtimeSinceStartup;
        row.SetColumnValue("HumanReadableTimestampUTC", currentTime.ToString());
        row.SetColumnValue("SystemClockTimestampMs", systemClockMilliseconds);

        // selection
        row.SetColumnValue("ActiveTargetIndex", selection.activeTargetIndex);
        row.SetColumnValue("AbsoluteTargetPositionX", selection.targetAbsoluteCoordinates.x);
        row.SetColumnValue("AbsoluteTargetPositionY", selection.targetAbsoluteCoordinates.y);
        row.SetColumnValue("AbsoluteSelectionPositionX", selection.selectionAbsoluteCoordinates.x);
        row.SetColumnValue("AbsoluteSelectionPositionY", selection.selectionAbsoluteCoordinates.y);
        row.SetColumnValue("LocalSelectionPositionX", selection.selectionLocalCoordinates.x);
        row.SetColumnValue("LocalSelectionPositionY", selection.selectionLocalCoordinates.y);
        row.SetColumnValue("Success", selection.success ? 1 : 0);
        row.SetColumnValue("SelectionDuration", selectionDurationMilliseconds);

        row.LogAndClear(); // no write to file yet, just enqueue. Maybe it will be deleted by calling "_selectionsLogger.ClearUnsavedData()"
    }

    private bool IsMovingContext(Context context)
    {
        return context == Context.Walking || context == Context.Circle; // || context == Context.Jogging;
    }

    private void LogHighFrequencyRow()
    {
        var row = _highFrequencyLogger.CurrentRow;

        // ids
        row.SetColumnValue("ParticipantID", _runConfig.participantID);
        row.SetColumnValue("MeasurementID", ++_measurementId);

        // conditions
        row.SetColumnValue("Context", Enum.GetName(typeof(Context), _runConfig.context));
        row.SetColumnValue("CircleDirection", _runConfig.context == Context.Circle ? Enum.GetName(typeof(CircleDirections), currentCircleDirection) : "");
        row.SetColumnValue("ReferenceFrame", Enum.GetName(typeof(ExperimentReferenceFrame), _runConfig.referenceFrame));
        row.SetColumnValue("TargetSize", targetsManager.GetTargetDiameter(targetSizesSequence.Current));
        row.SetColumnValue("DominantHand", _runConfig.leftHanded ? "Left" : "Right");

        // time
        var currentTime = Time.realtimeSinceStartup;
        row.SetColumnValue("HumanReadableTimestampUTC", currentTime.ToString());
        row.SetColumnValue("SystemClockTimestampMs", (int)(currentTime - activateFirstTargetMoment));


        var trackTransform = _runConfig.context == Context.Walking ? straightTrack.transform : _runConfig.context == Context.Circle ? circleTrack.transform : sceneLight.transform;
        LogObjectTransform("Track", trackTransform);

        var walkingDirectionTransform = IsMovingContext(_runConfig.context) ? walkingDirection.transform : standingDirection.transform;
        LogObjectTransform("WalkingDirection", walkingDirectionTransform);

        LogObjectTransform("Head", headset.transform); // center eye anchor
        LogObjectTransform("NeckBase", neckBase.transform);

        LogObjectTransform("DominantPalmCenter", dominantHandPalmCenter.transform);
        LogObjectTransform("DominantIndexTip", dominantHandIndexTip.transform); // holds selector
        LogObjectTransform("WeakPalmCenter", weakHandPalmCenter.transform); // holds targets

        var allTargets = targetsManager.transform;
        var activeTarget = targetsManager.ActiveTarget.target.transform;
        LogObjectTransform("AllTargets", allTargets);
        LogObjectTransform("ActiveTarget", activeTarget);

        var selectorPosition = dominantHandIndexTip.transform.position;
        var selectorProjection = Math.ProjectPointOntoOXYPlane(allTargets, selectorPosition);
        row.SetColumnValue("SelectorProjectionOntoAllTargetsX", selectorProjection.local.x);
        row.SetColumnValue("SelectorProjectionOntoAllTargetsY", selectorProjection.local.y);
        var isInside = targetsManager.IsSelectorInsideCollider;
        row.SetColumnValue("IsSelectorInsideCollider", isInside ? 1 : 0);
        var distance = (selectorPosition - selectorProjection.world).magnitude;
        var distanceToLog = isInside ? -distance : distance;
        row.SetColumnValue("DistanceFromSelectorToAllTargetsOXYPlane", distanceToLog);

        row.SetColumnValue("ActiveTargetIndex", targetsManager.ActiveTarget.targetIndex);
        var activeTargetProjection = Math.ProjectPointOntoOXYPlane(allTargets, activeTarget.position);
        row.SetColumnValue("ActiveTargetInsideAllTargetsX", activeTargetProjection.local.x);
        row.SetColumnValue("ActiveTargetInsideAllTargetsY", activeTargetProjection.local.y);

        row.LogAndClear(); // no write to file yet, just enqueue. Maybe it will be deleted by calling "_selectionsLogger.ClearUnsavedData()"
    }

    private void LogObjectTransform(string objectPrefix, Transform objectTransform)
    {
        var row = _highFrequencyLogger.CurrentRow;

        var position = objectTransform.position;
        var forward = objectTransform.forward;
        var up = objectTransform.up;
        var quaternion = objectTransform.rotation;

        row.SetColumnValue(objectPrefix + "PositionX", position.x);
        row.SetColumnValue(objectPrefix + "PositionY", position.y);
        row.SetColumnValue(objectPrefix + "PositionZ", position.z);
        row.SetColumnValue(objectPrefix + "ForwardX", forward.x);
        row.SetColumnValue(objectPrefix + "ForwardY", forward.y);
        row.SetColumnValue(objectPrefix + "ForwardZ", forward.z);
        row.SetColumnValue(objectPrefix + "UpX", up.x);
        row.SetColumnValue(objectPrefix + "UpY", up.y);
        row.SetColumnValue(objectPrefix + "UpZ", up.z);
        row.SetColumnValue(objectPrefix + "QuaternionX", quaternion.x);
        row.SetColumnValue(objectPrefix + "QuaternionY", quaternion.y);
        row.SetColumnValue(objectPrefix + "QuaternionZ", quaternion.z);
        row.SetColumnValue(objectPrefix + "QuaternionW", quaternion.w);
    }

    private void EnsureSelectionsLoggerInitialized()
    {
        try
        {
            var filename = $"{_runConfig.participantID}_selections.csv";
            if (currentSelectionsLoggerFilename == filename) return;

            _selectionsLogger = new AsyncHighFrequencyCSVLogger(filename);

            if (!_selectionsLogger.HasBeenInitialised())
            {
                _selectionsLogger.AddColumns(selectionLogColumns);
                _selectionsLogger.Initialise();
            }

            currentSelectionsLoggerFilename = filename;
        }
        catch (Exception e)
        {
            throw new Exception($"Error while initializing selections logger: Message= {e.Message}\n\nInner= {e.InnerException?.Message}");
        }
    }

    private void EnsureFrequencyLoggerInitialized()
    {
        try
        {
            var filename = $"{_runConfig.participantID}_highFrequency.csv";
            if (currentHighFrequencyLoggerFilename == filename) return;
            _highFrequencyLogger = new AsyncHighFrequencyCSVLogger(filename);

            if (!_highFrequencyLogger.HasBeenInitialised())
            {
                _highFrequencyLogger.AddColumns(highFrequencyLogColumns);
                _highFrequencyLogger.Initialise();
            }

            currentHighFrequencyLoggerFilename = filename;
        }
        catch (Exception e)
        {
            throw new Exception($"Error while initializing highFrequency logger: Message= {e.Message}\n\nInner= {e.InnerException?.Message}");
        }
    }

    private void SaveLoggersDataAsync(Action then)
    {
        object callbackLock = new();
        int saved = 0;

        var callback = new Action(() =>
        {
            lock (callbackLock)
            {
                saved++;
                if (saved == 2) doOnNextUpdate = () =>
                {
                    _selectionsLogger.DataSavedToDiskCallback = null;
                    _highFrequencyLogger.DataSavedToDiskCallback = null;
                    then();
                };
            }
        });

        _selectionsLogger.DataSavedToDiskCallback = callback;
        _highFrequencyLogger.DataSavedToDiskCallback = callback;

        _selectionsLogger.SaveDataToDisk();
        _highFrequencyLogger.SaveDataToDisk();
    }
    #endregion

    #region HandleState stuff
    private static ArgumentException NotSupposedException(string eventName, State state)
    {
        return new ArgumentException($"{eventName} got called in {Enum.GetName(typeof(State), state)} state. This is not supposed to happen");
    }
    private static ArgumentException DefaultException(string handlerName)
    {
        return new ArgumentException($"It seems you have implemented new event but forgot to handle in method {handlerName}");
    }

    private void StartSelectingPipeline()
    {
        targetsIndexesSequence = GenerateTargetsIndexesSequence();
        targetsIndexesSequence.MoveNext();
        targetsManager.ActivateTarget(targetsIndexesSequence.Current);
        listeningTargetsEventsFlag = true;

        bool isTrial = !_runConfig.isTraining;
        if (isTrial)
        {
            activateFirstTargetMoment = Time.realtimeSinceStartup;
            highFrequencyLoggingIsOnFlag = true;
        }
    }

    private void Cleanup()
    {
        CancelInvoke(nameof(OnCountdownFinished));
        CancelInvoke(nameof(OnServerValidatedTrial));
        CancelInvoke(nameof(OnServerInvalidatedTrial));
        listeningTrackEventsFlag = false;
        listeningTargetsEventsFlag = false;
        highFrequencyLoggingIsOnFlag = false;
        metronome.enabled = false;
        walkingStateTrigger.enabled = false; // also hides track borders
        targetsManager.EnsureTargetsHidden();
        selectorProjector.enabled = false;
        targetsManager.hideCube();
    }

    private void HandleInvalid()
    {
        if (_runConfig.isMetronomeTraining)
        {
            UpdateDirectionArrow();
            directionArrow.SetActive(true);
            return;
        }
        _targetsSelected = _selectionsValidated;
        targetSizesSequence = ReGenerateTargetSizesSequence(targetSizesSequence, _runConfig.isTraining);
        targetSizesSequence.MoveNext();
        targetsManager.TargetSize = targetSizesSequence.Current;
        targetsManager.EnsureTargetsShown();
        if (!_runConfig.isTraining)
        {
            highFrequencyLoggingIsOnFlag = false;
            _selectionsLogger.ClearUnsavedData();
            _highFrequencyLogger.ClearUnsavedData();
        }
        UpdateDirectionArrow();
        directionArrow.SetActive(true);
    }

    private void HandleState(string eventName = "")
    {
        try
        {
            switch (_state)
            {
                case State.Idle:
                    HandleIdleState(eventName);
                    break;
                case State.Preparing:
                    HandlePreparingState(eventName);
                    break;
                case State.WalkingWithMetronomeTraining:
                    HandleWalkingWithMetronomeTrainingState(eventName);
                    break;
                case State.SelectingTargetsStanding:
                    HandleSelectingTargetsStandingState(eventName);
                    break;
                case State.AwaitingParticipantEnterTrack:
                    HandleAwaitingParticipantEnterTrackState(eventName);
                    break;
                case State.SelectingTargetsWalking:
                    HandleSelectingTargetsWalkingState(eventName);
                    break;
                case State.AwaitingServerValidationOfLastTrial:
                    HandleAwaitingServerValidationOfLastTrialState(eventName);
                    break;
                default:
                    throw new ArgumentException(
                        $"It seems that you have implemented new State, but forget to add it to {nameof(HandleState)}");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError(e.Message);
            UnityEngine.Debug.LogError(e.StackTrace);
            unexpectedErrorOccured.Invoke(e.Message);
        }
    }

    private void HandleIdleState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnServerSaidPrepare):
                /* if (_runConfig.isInitialStandingTraining)
                {
                    ActualizeHands();
                    _state = State.Preparing;
                    break;
                } */
                if (_runConfig.isMetronomeTraining)
                {

                    walkingStateTrigger.track = _runConfig.context;
                    walkingStateTrigger.enabled = true; // just show track, but not listening events yet
                    UpdateDirectionArrow();
                    directionArrow.SetActive(true);
                    _state = State.Preparing;
                    targetsManager.hideCube();
                    HandlePreparingState(nameof(OnServerSaidPrepare));
                    break;
                }
                UpdatePathRefFrames();
                ActualizeHands();
                ActualizeReferenceFrames();
                targetsManager.showCube();
                targetsManager.Anchor = activeRefFrame;
                selectorProjector.Selector = dominantHandIndexTip.transform;
                selectorProjector.enabled = true;

                targetSizesSequence = GenerateTargetSizesSequence(_runConfig.isTraining);
                targetSizesSequence.MoveNext();
                targetsManager.TargetSize = targetSizesSequence.Current;
                targetsManager.EnsureTargetsShown();

                _targetsSelected = 0;
                _measurementId = 0;
                _selectionsValidated = 0;

                bool isTrial = !_runConfig.isTraining;
                if (isTrial)
                {
                    EnsureSelectionsLoggerInitialized();
                    EnsureFrequencyLoggerInitialized();
                }

                if (IsMovingContext(_runConfig.context))
                {
                    walkingStateTrigger.track = _runConfig.context;
                    walkingStateTrigger.enabled = true; // This is walking context. Just show track, but not listening events yet
                    UpdateDirectionArrow();
                    directionArrow.SetActive(true);
                }
                else
                {
                    walkingStateTrigger.enabled = false; // This is standing context.
                }

                _state = State.Preparing;
                HandlePreparingState(nameof(OnServerSaidPrepare));
                break;
            default:
                throw eventRedirectingMethods.Contains(eventName)
                    ? NotSupposedException(eventName, State.Idle)
                    : DefaultException(nameof(HandleIdleState));
        }
    }

    private void HandlePreparingState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnServerSaidPrepare):
                UpdatePathRefFrames();
                break;
            case nameof(OnServerSaidStart):
                /* if (_runConfig.isInitialStandingTraining)
                {
                    UpdatePathRefFrames();
                    _state = State.Idle;
                    trialsFinished.Invoke(); // and call "finished"
                    break;
                } */
                if (_runConfig.isMetronomeTraining || IsMovingContext(_runConfig.context))
                {
                    // We have to wait for the participant to enter the track (no matter if this is training with metronome or not)
                    SetMetronomeTempo(_runConfig.context);
                    metronome.enabled = true;
                    listeningTrackEventsFlag = true;
                    _state = State.AwaitingParticipantEnterTrack;
                }
                else
                {
                    // We are now in standing context. We assume, that targets have already been shown.
                    // We need just to activate first target after _timeUntilPrompt seconds
                    Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                    _state = State.SelectingTargetsStanding;
                }
                break;
            default:
                throw eventRedirectingMethods.Contains(eventName)
                    ? NotSupposedException(eventName, State.Preparing)
                    : DefaultException(nameof(HandlePreparingState));
        }
    }

    private void ShowDirectionArrow() { directionArrow.SetActive(true); }

    private void HandleWalkingWithMetronomeTrainingState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnParticipantEnteredTrack):
                break; // this is ok. Good job, participant! Move on!
            case nameof(OnParticipantFinishedTrack):
                directionArrow.SetActive(false);
                UpdateDirectionArrow();
                Invoke(nameof(ShowDirectionArrow), 1.5f);
                break; // this is ok. Good job, participant! Move on!
            case nameof(OnParticipantSlowedDown):
                ShowErrorToParticipant("Participant slowed down.");
                break;
            case nameof(OnParticipantSwervedOffTrack):
                ShowErrorToParticipant("Participant swerved off track.");
                break;
            case nameof(OnServerSaidFinishTraining):
                listeningTrackEventsFlag = false;
                metronome.enabled = false;
                walkingStateTrigger.enabled = false; // also hides track borders
                _state = State.Idle;
                break;
            default:
                throw eventRedirectingMethods.Contains(eventName)
                    ? NotSupposedException(eventName, State.WalkingWithMetronomeTraining)
                    : DefaultException(nameof(HandleWalkingWithMetronomeTrainingState));
        }
    }

    private void HandleSelectingTargetsStandingState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnCountdownFinished):
                StartSelectingPipeline();
                break;
            case nameof(OnSelectorEnteredTargetZone):
                _targetsSelected++;
                if (!_runConfig.isTraining)
                {
                    LogSelectionRow();
                }
                break;
            case nameof(OnSelectorExitedTargetZone):
                if (targetsIndexesSequence.MoveNext())
                {
                    targetsManager.ActivateTarget(targetsIndexesSequence.Current);
                }
                else
                {
                    // We go here if the just-selected target was the last with such size

                    listeningTargetsEventsFlag = false;
                    highFrequencyLoggingIsOnFlag = false;

                    selectorProjector.enabled = false;
                    targetsManager.EnsureTargetsHidden();

                    if (_runConfig.isTraining) Invoke(nameof(OnServerValidatedTrial), 2f); // imitating, always success for training
                    else requestTrialValidation.Invoke();
                    _state = State.AwaitingServerValidationOfLastTrial;
                }
                break;
            case nameof(OnSelectorExitedWrongSide):
                ShowErrorToParticipant("Selector exited wrong side.");
                HandleInvalid();
                Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                break;
            case nameof(OnServerSaidFinishTraining):
                if (!_runConfig.isTraining) throw new ArgumentException($"{eventName} got called in SelectingTargetsStanding state while trials. This is not supposed to happen");

                Cleanup();
                _state = State.Idle;
                break;
            default:
                throw eventRedirectingMethods.Contains(eventName)
                    ? NotSupposedException(eventName, State.SelectingTargetsStanding)
                    : DefaultException(nameof(HandleSelectingTargetsStandingState));
        }
    }

    private void HandleAwaitingParticipantEnterTrackState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnParticipantFinishedTrack):
            case nameof(OnParticipantSlowedDown):
            case nameof(OnParticipantSwervedOffTrack):
                break; // These events can happen but we ignore them. We are interested only in "OnParticipantEnteredTrack" event
            case nameof(OnParticipantEnteredTrack):
                Vector3 relativePosition = directionArrow.transform.InverseTransformPoint(headset.transform.position);
                if (relativePosition.x > 0 && _runConfig.context == Context.Circle)
                {
                    ShowErrorToParticipant("Participant entered track from the wrong side.");
                    targetsManager.EnsureNoActiveTargets();
                    HandleInvalid();
                    _state = State.AwaitingParticipantEnterTrack;
                    break;
                }
                if (_runConfig.isMetronomeTraining)
                {
                    _state = State.WalkingWithMetronomeTraining;
                    break;
                }
                Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                _state = State.SelectingTargetsWalking;
                break;
            case nameof(OnServerSaidFinishTraining):
                if (_runConfig is { isTraining: false, isMetronomeTraining: false })
                    throw new ArgumentException($"{nameof(HandleAwaitingParticipantEnterTrackState)}: Cannot stop trials");

                Cleanup();
                _state = State.Idle;
                break;
            default:
                throw eventRedirectingMethods.Contains(eventName)
                    ? NotSupposedException(eventName, State.AwaitingParticipantEnterTrack)
                    : DefaultException(nameof(HandleAwaitingParticipantEnterTrackState));
        }
    }

    private void HandleSelectingTargetsWalkingState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnCountdownFinished):
                // participant is walking, countdown finished. Let's activate target and start logging if this is trial
                StartSelectingPipeline();
                break;
            case nameof(OnParticipantFinishedTrack):
                ShowErrorToParticipant("Participant finished track early.");
                targetsManager.EnsureNoActiveTargets();
                HandleInvalid();

                CancelInvoke(nameof(OnCountdownFinished)); // curious situation, when participant has run the whole track before countdown finished
                _state = State.AwaitingParticipantEnterTrack;
                break;
            case nameof(OnParticipantSlowedDown):
                ShowErrorToParticipant("Participant slowed down.");
                targetsManager.EnsureNoActiveTargets();
                HandleInvalid();

                CancelInvoke(nameof(OnCountdownFinished)); // curious situation, when participant has run the whole track before countdown finished
                _state = State.AwaitingParticipantEnterTrack;
                break;
            case nameof(OnParticipantSwervedOffTrack):
                // participant has to select all targets first. We assume this as an error 
                ShowErrorToParticipant("Participant swerved off track.");
                targetsManager.EnsureNoActiveTargets();
                HandleInvalid();

                CancelInvoke(nameof(OnCountdownFinished)); // curious situation, when participant has run the whole track before countdown finished
                _state = State.AwaitingParticipantEnterTrack;
                break;
            case nameof(OnSelectorEnteredTargetZone):
                // participant selected target? Good job!
                _targetsSelected++;
                if (!_runConfig.isTraining)
                {
                    LogSelectionRow();
                }
                break;
            case nameof(OnSelectorExitedTargetZone):
                // let's activate next target if needed, or set next target size, or rerun training, or request server validation
                if (targetsIndexesSequence.MoveNext())
                {
                    targetsManager.ActivateTarget(targetsIndexesSequence.Current);
                }
                else
                {
                    listeningTargetsEventsFlag = false;
                    highFrequencyLoggingIsOnFlag = false;
                    listeningTrackEventsFlag = false;
                    selectorProjector.enabled = false;
                    directionArrow.SetActive(false);

                    metronome.enabled = false;
                    targetsManager.EnsureTargetsHidden();

                    if (_runConfig.isTraining) Invoke(nameof(OnServerValidatedTrial), 2f); // imitating, always success for training
                    else
                    {
                        requestTrialValidation.Invoke();
                    }
                    _state = State.AwaitingServerValidationOfLastTrial;
                }
                break;
            case nameof(OnSelectorExitedWrongSide):
                // participant has to select all targets first. We assume this as an error 
                ShowErrorToParticipant("Selector exited wrong side.");
                targetsManager.EnsureNoActiveTargets();
                HandleInvalid();
                CancelInvoke(nameof(OnCountdownFinished));
                _state = State.AwaitingParticipantEnterTrack;
                break;
            case nameof(OnServerSaidFinishTraining):
                if (!_runConfig.isTraining) throw new ArgumentException($"{nameof(HandleSelectingTargetsWalkingState)}: Cannot stop trials");

                Cleanup();
                _state = State.Idle;
                break;
            default:
                throw eventRedirectingMethods.Contains(eventName)
                    ? NotSupposedException(eventName, State.SelectingTargetsWalking)
                    : DefaultException(nameof(HandleSelectingTargetsWalkingState));
        }
    }

    private void SetMetronomeTempo(Context context)
    {
        if (IsMovingContext(_runConfig.context)) metronome.SetTempo(walkingTempo);
        // if (_runConfig.context == Context.Jogging) metronome.SetTempo(joggingTempo);
    }
    private void HandleAwaitingServerValidationOfLastTrialState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnServerValidatedTrial):
                // was success. Let's check if more target sizes participant has to make a trial with
                _selectionsValidated += TargetsManager.TargetsCount;
                if (!targetSizesSequence.MoveNext())
                {
                    // we now assume we are in trials, not training
                    // this is end. Clean up, save data and tell server trials are finished

                    walkingStateTrigger.enabled = false;
                    SaveLoggersDataAsync(() =>
                    {
                        targetsManager.hideCube();
                        trialsFinished.Invoke();
                        _state = State.Idle;
                    });
                }
                else
                {
                    targetsManager.TargetSize = targetSizesSequence.Current;
                    UpdateDirectionArrow();
                    directionArrow.SetActive(true);

                    var NextStandingTrial = new Action(() =>
                    {
                        selectorProjector.enabled = true;
                        targetsManager.EnsureTargetsShown();
                        Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                        _state = State.SelectingTargetsStanding;
                    });

                    var NextWalkingTrial = new Action(() =>
                    {
                        selectorProjector.enabled = true;
                        SetMetronomeTempo(_runConfig.context);
                        metronome.enabled = true;
                        listeningTrackEventsFlag = true;
                        targetsManager.EnsureTargetsShown();
                        _state = State.AwaitingParticipantEnterTrack;
                    });

                    var nextTrial = _runConfig.context == Context.Standing ? NextStandingTrial : NextWalkingTrial;

                    if (_runConfig.isTraining) nextTrial(); // sync variant
                    else SaveLoggersDataAsync(nextTrial); // async variant
                }
                break;
            case nameof(OnServerInvalidatedTrial):
                // server said no (as usual, because participant has walked without metronome)
                // Let's clearUnsavedData in loggers (if this is not training) and rerun trial with this size once more
                // ShowErrorToParticipant("Server invalidated trial. Please, try again.");
                HandleInvalid();

                if (_runConfig.context == Context.Standing)
                {
                    selectorProjector.enabled = true;
                    targetsManager.EnsureTargetsShown();
                    Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                    _state = State.SelectingTargetsStanding;
                }
                else
                {
                    selectorProjector.enabled = true;
                    SetMetronomeTempo(_runConfig.context);
                    metronome.enabled = true;
                    listeningTrackEventsFlag = true;
                    targetsManager.EnsureTargetsShown();
                    _state = State.AwaitingParticipantEnterTrack;
                }
                break;
            case nameof(OnServerSaidFinishTraining):
                if (!_runConfig.isTraining) throw new ArgumentException($"{nameof(HandleSelectingTargetsWalkingState)}: Cannot stop trials");

                Cleanup();
                _state = State.Idle;
                break;
            default:
                throw eventRedirectingMethods.Contains(eventName)
                    ? NotSupposedException(eventName, State.AwaitingServerValidationOfLastTrial)
                    : DefaultException(nameof(HandleAwaitingServerValidationOfLastTrialState));
        }
    }
    #endregion

    #region Event Redirecting Methods
    private static readonly List<string> eventRedirectingMethods = new List<string>
    {
        nameof(OnCountdownFinished), nameof(OnSelectorEnteredTargetZone), nameof(OnSelectorExitedTargetZone), nameof(OnSelectorExitedWrongSide),
        nameof(OnParticipantEnteredTrack), nameof(OnParticipantFinishedTrack), nameof(OnParticipantSwervedOffTrack), nameof(OnParticipantSlowedDown),
        nameof(OnServerSaidStart), nameof(OnServerSaidPrepare), nameof(OnServerSaidFinishTraining), nameof(OnServerValidatedTrial), nameof(OnServerInvalidatedTrial)
    };

    private void OnCountdownFinished() => HandleState(nameof(OnCountdownFinished));
    private void OnSelectorEnteredTargetZone() { if (listeningTargetsEventsFlag) HandleState(nameof(OnSelectorEnteredTargetZone)); }
    private void OnSelectorExitedTargetZone() { if (listeningTargetsEventsFlag) HandleState(nameof(OnSelectorExitedTargetZone)); }
    private void OnSelectorExitedWrongSide() { if (listeningTargetsEventsFlag) HandleState(nameof(OnSelectorExitedWrongSide)); }
    private void OnParticipantEnteredTrack() { if (listeningTrackEventsFlag) HandleState(nameof(OnParticipantEnteredTrack)); }
    private void OnParticipantFinishedTrack() { if (listeningTrackEventsFlag) HandleState(nameof(OnParticipantFinishedTrack)); }
    private void OnParticipantSwervedOffTrack() { if (listeningTrackEventsFlag) HandleState(nameof(OnParticipantSwervedOffTrack)); }
    private void OnParticipantSlowedDown() { if (listeningTrackEventsFlag) HandleState(nameof(OnParticipantSlowedDown)); }
    public void OnServerSaidStart() => HandleState(nameof(OnServerSaidStart));
    public void OnServerSaidFinishTraining() => HandleState(nameof(OnServerSaidFinishTraining));
    public void OnServerSetPathRefHeight() => UpdatePathRefFrames();

    public void OnServerSaidPrepare(RunConfig config)
    {
        if (_state == State.Idle) _runConfig = config;

        HandleState(nameof(OnServerSaidPrepare));
    }
    public void OnServerValidatedTrial() => HandleState(nameof(OnServerValidatedTrial));
    public void OnServerInvalidatedTrial() => HandleState(nameof(OnServerInvalidatedTrial));
    public void OnToggleHeadsetAdjustmentText(bool shouldShow)
    {
        hmdAdjustmentText.gameObject.SetActive(shouldShow);
    }
    #endregion
}

partial class ExperimentManager
{
    public enum Context
    {
        Standing,
        Walking,
        // Jogging
        Circle
    }

    public enum ExperimentReferenceFrame
    {
        PalmReferenced, // both rotation and position by hand
        PalmWORotation, // position only
        PathReferenced, // head
        // PathReferencedNeck, // neck
        // ChestReferenced // chest
    }

    public struct RunConfig
    {
        public readonly int participantID;
        public readonly bool leftHanded; // indicates, that dominant hand is left

        // not clean solution, but ok.
        // Indicates if this is training to go with metronome tempo.
        // If true, then other fields don't matter
        public readonly bool isMetronomeTraining;

        // indicates, that this is training with such context and refFrame
        // Firstly, logging is off
        // Secondly, training is infinite (stops by "finishTraining" command from server)
        public readonly bool isTraining;
        public readonly Context context;
        public readonly ExperimentReferenceFrame referenceFrame;

        // not fitting project architecture, but we are short of time
        // used for participant to place with his hand where he would comfortably set UI path-referenced (Z offset from headset and Y offset from floor)
        // after "start" command, fixes that offsets and calls "trialsFinished" to make server to go to next step
        public readonly bool isInitialStandingTraining;

        public readonly bool isBreak;

        public RunConfig(int participantID, bool leftHanded, bool isMetronomeTraining, bool isTraining, Context context, ExperimentReferenceFrame referenceFrame, bool isInitialStandingTraining, bool isBreak = false)
        {
            this.participantID = participantID;
            this.leftHanded = leftHanded;
            this.isMetronomeTraining = isMetronomeTraining;
            this.isTraining = isTraining;
            this.context = context;
            this.referenceFrame = referenceFrame;
            this.isInitialStandingTraining = isInitialStandingTraining;
            this.isBreak = isBreak;
        }
    }

    private enum State
    {
        Idle, // before first run or between runs
        Preparing, // preparing (update track, light ans so on)

        AwaitingParticipantEnterTrack, // used for metronome training and training/trials in walking context
        WalkingWithMetronomeTraining, // only when walkingWithMetronomeTraining was started

        SelectingTargetsStanding, // used both for training and trials, it is enough

        // these two states (together with AwaitingParticipantEnterTrack) make walking context (both training and trials) easy
        SelectingTargetsWalking,
        AwaitingServerValidationOfLastTrial
    }
}
