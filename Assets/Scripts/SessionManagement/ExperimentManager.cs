using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Logging;
using SpatialUIPlacement;
using UnityEngine;
using UnityEngine.Events;
using Utils;
using Math = Utils.Math;

public partial class ExperimentManager: MonoBehaviour
{
    // not used for trainings. To finish training, method (OnServerSaidFinishTraining should be called)
    public readonly UnityEvent trialsFinished = new();
    public readonly UnityEvent requestTrialValidation = new();
    public readonly UnityEvent<string> unexpectedErrorOccured = new();

    private State _state;
    private RunConfig _runConfig;

    #region MonoBehaviour methods

    private void Start()
    {
        targetsManager.selectorEnteredTargetsZone.AddListener(OnSelectorEnteredTargetZone);
        targetsManager.selectorExitedTargetsZone.AddListener(OnSelectorExitedTargetZone);
        
        walkingStateTrigger.ParticipantEntered.AddListener(OnParticipantEnteredTrack);
        walkingStateTrigger.ParticipantSwervedOff.AddListener(OnParticipantSwervedOffTrack);
        walkingStateTrigger.ParticipantSlowedDown.AddListener(OnParticipantSlowedDown);
        walkingStateTrigger.ParticipantFinished.AddListener(OnParticipantFinishedTrack);
    }

    private void OnDestroy()
    {
        targetsManager.selectorEnteredTargetsZone.RemoveListener(OnSelectorEnteredTargetZone);
        targetsManager.selectorExitedTargetsZone.RemoveListener(OnSelectorExitedTargetZone);
        
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
            Debug.Log("Calling doOnNextUpdate and make it null");
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
    private bool listeningTargetsEventsFlag;

    private static IEnumerator<TargetsManager.TargetSizeVariant> GenerateTargetSizesSequence(int participantId, ReferenceFrame referenceFrame, Context context, bool isTraining = false)
    {
        var seed = participantId * 100
                   + (int)referenceFrame * 10
                   + (int)context;
        var random = new System.Random(seed);

        // same sequence for equal (participantId, referenceFrame, context) because of random-seed
        var seq = new List<TargetsManager.TargetSizeVariant>
            {
                TargetsManager.TargetSizeVariant.Small,
                TargetsManager.TargetSizeVariant.Medium,
                TargetsManager.TargetSizeVariant.Big,
                TargetsManager.TargetSizeVariant.VeryBig,
            }
            .Select(size => new { size, rnd = random.Next() })
            .OrderBy(x => x.rnd)
            .Select(x => x.size);

        if (!isTraining) return seq.GetEnumerator();
        else return Enumerable.Repeat(seq, 20) // 20 is 100% enough
            .Aggregate((a, b) => a.Concat(b))
            .GetEnumerator();
    }

    private static IEnumerator<int> GenerateTargetsIndexesSequence()
    {
        return Math.FittsLaw(TargetsManager.TargetsCount)
            .Take(TargetsManager.TargetsCount + 1)
            .GetEnumerator();
    }
    #endregion

    #region Track & Light stuff
    [Space]
    [SerializeField] private GameObject track;
    [SerializeField] private GameObject sceneLight; // remark: we interpret it as track in standing context. Hand Ref and path ref depend on it, actually
    
    [SerializeField] private WalkingStateTrigger walkingStateTrigger;
    private bool listeningTrackEventsFlag;
    
    [SerializeField] private GameObject walkingDirection; // walking context (relative to track)
    [SerializeField] private GameObject standingDirection; // standing context (relative to light)

    private void PlaceTrackForwardFromHeadset()
    {
        var (position, rotation) = HeadsetOXZProjection();
        float halfTrackLength = 2.75f;
        position += rotation * new Vector3(0,0,halfTrackLength + .3f); // half track length and small offset more 
        track.transform.SetPositionAndRotation(position, rotation);
    }
    
    private void PlaceLightWhereHeadset()
    {
        var (position, rotation) = HeadsetOXZProjection();
        sceneLight.transform.SetPositionAndRotation(position, rotation);
    }
    
    private void PlaceLightWhereTrack()
    {
        var trackTransform = track.transform;
        sceneLight.transform.SetPositionAndRotation(trackTransform.position, trackTransform.rotation);
    }

    #endregion
    
    #region Sound stuff
    [Space]
    [SerializeField] private Metronome metronome;
    [SerializeField] private GameObject errorIndicator;
    
    void ShowErrorToParticipant()
    {
        // var headsetTransform = headset.transform;
        // var errorPosition = headsetTransform.position + headsetTransform.forward * 0.5f;
        // errorIndicator.transform.SetPositionAndRotation(errorPosition, headsetTransform.rotation);
        
        // we assume that errorIndicator is child of headset with just forward offset about 0.5 meters.
        errorIndicator.SetActive(true); // will be set inactive automatically 
    }
    #endregion

    #region Head stuff
    [Space]
    [SerializeField] private GameObject headset;
    [SerializeField] private GameObject neckBase;
    // SimplifiedComfortUIPlacement position.y depends on headset.
    // We refresh it when we want (usually after "prepare" command from server) by calling comfortUICoordinateY.Refresh()
    // Path-refFrame positioning depend on it.
    [SerializeField] private SimplifiedComfortUIPlacement comfortUICoordinateY;
    
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
            leftIndexTip.SetActive(true);
            rightIndexTip.SetActive(false);
            leftPalmCenter.SetActive(true);
            rightPalmCenter.SetActive(true);

            dominantHandPalmCenter = leftPalmCenter;
            dominantHandIndexTip = leftIndexTip;
            weakHandPalmCenter = rightPalmCenter;
        }
        else
        {
            leftIndexTip.SetActive(false);
            rightIndexTip.SetActive(true);
            leftPalmCenter.SetActive(true);
            rightPalmCenter.SetActive(true);
            
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
    [SerializeField] private GameObject palmRefFrameLeftHand;
    [SerializeField] private GameObject palmRefFrameRightHand;
    [SerializeField] private GameObject handRefFrameLeftHandStanding;
    [SerializeField] private GameObject handRefFrameRightHandStanding;
    [SerializeField] private GameObject handRefFrameLeftHandWalking;
    [SerializeField] private GameObject handRefFrameRightHandWalking;
    [SerializeField] private GameObject pathRefFrameStanding;
    [SerializeField] private GameObject pathRefFrameWalking;
    [SerializeField] private GameObject pathRefFrameNeckStanding;
    [SerializeField] private GameObject pathRefFrameNeckWalking;
    private GameObject activeRefFrame;
    private GameObject[] inactiveRefFrames;
    
    private void ActualizeReferenceFrames()
    {
        GameObject[] allRefFrames =
        {
            palmRefFrameLeftHand,
            palmRefFrameRightHand,
            handRefFrameLeftHandStanding,
            handRefFrameRightHandStanding,
            handRefFrameLeftHandWalking,
            handRefFrameRightHandWalking,
            pathRefFrameStanding,
            pathRefFrameWalking,
            pathRefFrameNeckStanding,
            pathRefFrameNeckWalking
        };

        GameObject active;
        switch (_runConfig.referenceFrame)
        {
            case ReferenceFrame.PalmReferenced:
                active = _runConfig.leftHanded ? palmRefFrameRightHand : palmRefFrameLeftHand;
                break;
            case ReferenceFrame.PathReferenced:
                active = _runConfig.context == Context.Standing ? pathRefFrameStanding : pathRefFrameWalking;
                break;
            case ReferenceFrame.PathReferencedNeck:
                active = _runConfig.context == Context.Standing ? pathRefFrameNeckStanding : pathRefFrameNeckWalking;
                break;
            case ReferenceFrame.HandReferenced:
                if (_runConfig is { context: Context.Standing, leftHanded: false }) active = handRefFrameLeftHandStanding;
                else if (_runConfig is { context: Context.Standing, leftHanded: true }) active = handRefFrameRightHandStanding;
                else if (_runConfig is { context: Context.Walking, leftHanded: false }) active = handRefFrameLeftHandWalking;
                else if (_runConfig is { context: Context.Walking, leftHanded: true }) active = handRefFrameRightHandWalking;
                else throw new NotSupportedException();
                break;
            default:
                throw new NotSupportedException();
        }

        activeRefFrame = active;
        inactiveRefFrames = allRefFrames.Where(rf => rf != active).ToArray();
        
        activeRefFrame.SetActive(true);
        foreach (var refFrame in inactiveRefFrames)
            refFrame.SetActive(false);
    }
    #endregion
    
    #region Current stuff (time, sequences and so on)
    private int _targetsSelected;
    private int _selectionsValidated; // used only for walking trials to reset _targetsSelected when server responded with invalidate result
    private int _measurementId;
    private DateTime activateFirstTargetMoment;
    private DateTime selectFirstTargetMoment;
    private DateTime selectPreviousTargetMoment;
    private IEnumerator<TargetsManager.TargetSizeVariant> targetSizesSequence;
    private IEnumerator<int> targetsIndexesSequence;

    private float GenerateTimeToActivateFirstTarget()
    {
        var minimum = _runConfig.isTraining ? 1f : 2f;
        if (_runConfig.context == Context.Standing) return minimum;
        
        float stepFrequencyInSeconds = 60f / metronome.Tempo;
        return minimum + UnityEngine.Random.Range(0f, 2f * stepFrequencyInSeconds);
    }
    #endregion
    
    #region Logging stuff
    private static readonly string[] selectionLogColumns =
    {
        // ids
        "ParticipantID",
        "SelectionID",
        
        // conditions
        "Walking", // 0 – means Standing, 1 – means Walking
        "ReferenceFrame", // 0 – palmReferenced, 1 – handReferenced, 2 – pathReferenced, 3 – pathReferencedNeck
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
        "Walking", // 0 – means Standing, 1 – means Walking
        "ReferenceFrame", // 0 – palmReferenced, 1 – handReferenced, 2 – pathReferenced, 3 – pathReferencedNeck
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
        bool isFirstWithSuchSize = _targetsSelected % (TargetsManager.TargetsCount + 1) == 1;

        var now = TimeMeasurementHelper.GetHighResolutionDateTime();
                    
        int systemClockMilliseconds, selectionDurationMilliseconds;
        if (isFirstWithSuchSize)
        {
            selectFirstTargetMoment = now;
            systemClockMilliseconds = 0;
            selectionDurationMilliseconds = 0;
        }
        else
        {
            systemClockMilliseconds = (int)(now - selectFirstTargetMoment).TotalMilliseconds;
            selectionDurationMilliseconds = (int)(now - selectPreviousTargetMoment).TotalMilliseconds;
        }

        selectPreviousTargetMoment = now;
        
        var row = _selectionsLogger.CurrentRow;
        
        // ids
        row.SetColumnValue("ParticipantID", _runConfig.participantID);
        row.SetColumnValue("SelectionID", _targetsSelected);

        var selection = targetsManager.LastSelectionData;
        
        // conditions
        row.SetColumnValue("Walking", _runConfig.context == Context.Standing ? 0 : 1);
        row.SetColumnValue("ReferenceFrame", Enum.GetName(typeof(ReferenceFrame), _runConfig.referenceFrame));
        row.SetColumnValue("TargetSize", selection.targetSize);
        row.SetColumnValue("DominantHand", _runConfig.leftHanded ? "Left" : "Right");

        var currentTime = TimeMeasurementHelper.GetHighResolutionDateTime();
        
        // time
        row.SetColumnValue("HumanReadableTimestampUTC", currentTime.ToString("dd-MM-yyyy HH:mm:ss.ffffff", CultureInfo.InvariantCulture));
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

    private void LogHighFrequencyRow()
    {
        var row = _highFrequencyLogger.CurrentRow;
        
        // ids
        row.SetColumnValue("ParticipantID", _runConfig.participantID);
        row.SetColumnValue("MeasurementID", ++_measurementId);
        
        // conditions
        row.SetColumnValue("Walking", _runConfig.context == Context.Walking ? 1 : 0);
        row.SetColumnValue("ReferenceFrame", Enum.GetName(typeof(ReferenceFrame), _runConfig.referenceFrame));
        row.SetColumnValue("TargetSize", TargetsManager.GetTargetDiameter(targetSizesSequence.Current));
        row.SetColumnValue("DominantHand", _runConfig.leftHanded ? "Left" : "Right");
        
        // time
        var now = TimeMeasurementHelper.GetHighResolutionDateTime();
        row.SetColumnValue("HumanReadableTimestampUTC", now.ToString("dd-MM-yyyy HH:mm:ss.ffffff", CultureInfo.InvariantCulture));
        row.SetColumnValue("SystemClockTimestampMs", (int)(now - activateFirstTargetMoment).TotalMilliseconds);


        var trackTransform = _runConfig.context == Context.Walking ? track.transform : sceneLight.transform;
        LogObjectTransform("Track", trackTransform);
        
        var walkingDirectionTransform = _runConfig.context == Context.Walking ? walkingDirection.transform : standingDirection.transform;
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
            activateFirstTargetMoment = TimeMeasurementHelper.GetHighResolutionDateTime();
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
            Debug.LogError(e.Message);
            Debug.LogError(e.StackTrace);
            unexpectedErrorOccured.Invoke(e.Message);
        }
    }

    private void HandleIdleState(string eventName)
    {
        Debug.Log($"{nameof(HandleIdleState)} handles {eventName} event");
        switch (eventName)
        {
            case nameof(OnServerSaidPrepare):
                if (_runConfig.isMetronomeTraining)
                {
                    walkingStateTrigger.enabled = true; // just show track, but not listening events yet
                    _state = State.Preparing;
                    HandlePreparingState(nameof(OnServerSaidPrepare));
                    break;
                }

                ActualizeHands();
                ActualizeReferenceFrames();
                targetsManager.Anchor = activeRefFrame;
                
                targetSizesSequence = GenerateTargetSizesSequence(_runConfig.participantID, _runConfig.referenceFrame, _runConfig.context);
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

                if (_runConfig.context == Context.Walking)
                {
                    walkingStateTrigger.enabled = true; // This is walking context. Just show track, but not listening events yet
                    PlaceLightWhereTrack();
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
        Debug.Log($"{nameof(HandlePreparingState)} handles {eventName} event");
        switch (eventName)
        {
            case nameof(OnServerSaidPrepare):
                if (_runConfig.isMetronomeTraining)
                {
                    PlaceTrackForwardFromHeadset();
                    PlaceLightWhereTrack();
                    break;
                }

                comfortUICoordinateY.Refresh(); // for both walking and standing context
                if (_runConfig.context == Context.Standing)
                {
                    PlaceLightWhereHeadset();
                }

                break;
            case nameof(OnServerSaidStart):
                if (_runConfig.isMetronomeTraining || _runConfig.context == Context.Walking)
                {
                    // We have to wait for the participant to enter the track (no matter if this is training with metronome or not)
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

    private void HandleWalkingWithMetronomeTrainingState(string eventName)
    {
        Debug.Log($"{nameof(HandleWalkingWithMetronomeTrainingState)} handles {eventName} event");
        switch (eventName)
        {
            case nameof(OnParticipantEnteredTrack):
            case nameof(OnParticipantFinishedTrack):
                break; // this is ok. Good job, participant! Move on!
            case nameof(OnParticipantSlowedDown):
            case nameof(OnParticipantSwervedOffTrack):
                ShowErrorToParticipant();
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
        Debug.Log($"{nameof(HandleSelectingTargetsStandingState)} handles {eventName} event");
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
                    
                    targetsManager.EnsureTargetsHidden();

                    if (_runConfig.isTraining) Invoke(nameof(OnServerValidatedTrial), 2f); // imitating, always success for training
                    else requestTrialValidation.Invoke();
                    _state = State.AwaitingServerValidationOfLastTrial;

                    /*bool hasNextTargetSize = targetSizesSequence.MoveNext();
                    if (hasNextTargetSize)
                    {
                        targetsManager.TargetSize = targetSizesSequence.Current;
                        targetsIndexesSequence = GenerateTargetsIndexesSequence();
                        Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                        break;
                    }

                    if (_runConfig.isTraining)
                    {
                        // rerun the whole pipeline if this was training
                        targetSizesSequence = GenerateTargetSizesSequence(_runConfig.participantID, _runConfig.referenceFrame, _runConfig.context);
                        targetSizesSequence.MoveNext();
                        targetsManager.TargetSize = targetSizesSequence.Current;
                        targetsIndexesSequence = GenerateTargetsIndexesSequence();
                        Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                    }
                    else
                    {
                        // Now trial is finished (all three target sizes). Stop logging, cleanup and tell server
                        
                        _measurementId = 0;
                        _targetsSelected = 0;
                        targetSizesSequence = null;
                        targetsIndexesSequence = null;
                        
                        targetsManager.HideTargets();
                        
                        _selectionsLogger.SaveDataToDisk();
                        _highFrequencyLogger.SaveDataToDisk();

                        _state = State.Idle;
                        trialsFinished.Invoke();
                    }*/
                }
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
        Debug.Log($"{nameof(HandleAwaitingParticipantEnterTrackState)} handles {eventName} event");
        switch (eventName)
        {
            case nameof(OnParticipantFinishedTrack):
            case nameof(OnParticipantSlowedDown):
            case nameof(OnParticipantSwervedOffTrack):
                Debug.Log("Ignore this event, cause we are interested only in entering track event");
                break; // These events can happen but we ignore them. We are interested only in "OnParticipantEnteredTrack" event
            case nameof(OnParticipantEnteredTrack):
                if (_runConfig.isMetronomeTraining)
                {
                    Debug.Log("Set state to walking with metronome. It can only go to Idle when server says finish");
                    _state = State.WalkingWithMetronomeTraining;
                    break;
                }
                Debug.Log("Set state to SelectingTargetsWalking and setTimeout to call OnCountdownFinished");
                Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                _state = State.SelectingTargetsWalking;
                break;
            case nameof(OnServerSaidFinishTraining):
                if (_runConfig is { isTraining: false, isMetronomeTraining: false }) throw new ArgumentException($"{nameof(HandleAwaitingParticipantEnterTrackState)}: Cannot stop trials");
                
                Cleanup();
                _state = State.Idle;
                /*if (_runConfig.isMetronomeTraining)
                {
                    listeningTrackEventsFlag = false;
                    metronome.enabled = false;
                    walkingStateTrigger.enabled = false; // also hides track borders
                    _state = State.Idle;
                }
                else
                {
                    if (!_runConfig.isTraining) throw new ArgumentException($"{nameof(HandleAwaitingParticipantEnterTrackState)}: Cannot stop trials");
                    
                    Debug.Log("Participant has not entered track. So, stop training, hide targets and cancel invokes if present");
                    // we now assume we are in walking training with selecting targets. Cleanup!
                    CancelInvoke(nameof(OnCountdownFinished));
                    CancelInvoke(nameof(OnServerValidatedTrial));
                    CancelInvoke(nameof(OnServerInvalidatedTrial));
                    listeningTrackEventsFlag = false;
                    listeningTargetsEventsFlag = false;
                    metronome.enabled = false;
                    walkingStateTrigger.enabled = false; // also hides track borders
                    targetsManager.EnsureTargetsHidden();
                    _state = State.Idle;
                }*/
                break;
            default: 
                throw eventRedirectingMethods.Contains(eventName)
                    ? NotSupposedException(eventName, State.AwaitingParticipantEnterTrack)
                    : DefaultException(nameof(HandleAwaitingParticipantEnterTrackState));
        }
    }
    
    private void HandleSelectingTargetsWalkingState(string eventName)
    {
        Debug.Log($"{nameof(HandleSelectingTargetsWalkingState)} handles {eventName} event");
        switch (eventName)
        {
            case nameof(OnCountdownFinished):
                // participant is walking, countdown finished. Let's activate target and start logging if this is trial
                StartSelectingPipeline();
                break;
            case nameof(OnParticipantFinishedTrack):
            case nameof(OnParticipantSlowedDown):
            case nameof(OnParticipantSwervedOffTrack):
                // participant has to select all targets first. We assume this as an error 
                Debug.Log("Participant has not selected all targets, but finished walking. Show error and clear unsaved data (if this is trial). And go AwaitingEnterTrack state");
                ShowErrorToParticipant();
                targetsManager.EnsureNoActiveTargets();
                
                _targetsSelected = _selectionsValidated;

                if (!_runConfig.isTraining)
                {
                    highFrequencyLoggingIsOnFlag = false;
                    _selectionsLogger.ClearUnsavedData();
                    _highFrequencyLogger.ClearUnsavedData();   
                }

                CancelInvoke(nameof(OnCountdownFinished)); // curious situation, when participant has run the whole track before countdown finished
                _state = State.AwaitingParticipantEnterTrack;
                break;
            case nameof(OnSelectorEnteredTargetZone):
                // participant selected target? Good job!
                _targetsSelected++;
                Debug.Log($"Selected target N_{_targetsSelected}");
                if (!_runConfig.isTraining)
                {
                    LogSelectionRow();
                }
                break;
            case nameof(OnSelectorExitedTargetZone):
                // let's activate next target if needed, or set next target size, or rerun training, or request server validation
                if (targetsIndexesSequence.MoveNext())
                {
                    Debug.Log($"Activating target N_{targetsIndexesSequence.Current}");
                    targetsManager.ActivateTarget(targetsIndexesSequence.Current);
                }
                else
                {
                    Debug.Log($"Targets with {Enum.GetName(typeof(TargetsManager.TargetSizeVariant), targetSizesSequence.Current)?.ToUpper()} size have finished");
                    Debug.Log($"HighFrequency is 100% not working, not listening to targetsEvents or trackEvents");
                    listeningTargetsEventsFlag = false;
                    highFrequencyLoggingIsOnFlag = false;
                    listeningTrackEventsFlag = false;
                    
                    metronome.enabled = false;
                    targetsManager.EnsureTargetsHidden();

                    if (_runConfig.isTraining) Invoke(nameof(OnServerValidatedTrial), 2f); // imitating, always success for training
                    else requestTrialValidation.Invoke();
                    _state = State.AwaitingServerValidationOfLastTrial;
                    
                    /*listeningTargetsEventsFlag = false;
                    listeningTrackEventsFlag = false;
                    highFrequencyLoggingIsOnFlag = false;
                    
                    if (_runConfig.isTraining)
                    {
                        if (!targetSizesSequence.MoveNext())
                        {
                            // rerun the whole training pipeline
                            targetSizesSequence = GenerateTargetSizesSequence(_runConfig.participantID,
                                _runConfig.referenceFrame, _runConfig.context);
                            targetSizesSequence.MoveNext();
                        }
                        Debug.Log($"It was training. Next target size is {Enum.GetName(typeof(TargetsManager.TargetSizeVariant), targetSizesSequence.Current)?.ToUpper()}. Go AwaitingParticipantEnterTrack state and start listen track events");
                        
                        targetsManager.TargetSize = targetSizesSequence.Current;
                        // targetsIndexesSequence = GenerateTargetsIndexesSequence();

                        listeningTrackEventsFlag = true;
                        _state = State.AwaitingParticipantEnterTrack;
                    }
                    else
                    {
                        // request server validation. Keep in mind, that logging data is still in RAM memory (not on disk) and can be cleared
                        
                        Debug.Log($"Trial with {Enum.GetName(typeof(TargetsManager.TargetSizeVariant), targetSizesSequence.Current)?.ToUpper()} is finished. Disable metronome, hide targets and wait for server validation");
                        
                        metronome.enabled = false; // we don't need metronome sound when participant is waiting for the validation
                        targetsManager.HideTargets();
                        _state = State.AwaitingServerValidationOfLastTrial;
                        requestTrialValidation.Invoke();
                    }*/
                }
                break;
            case nameof(OnServerSaidFinishTraining): 
                if (!_runConfig.isTraining) throw new ArgumentException($"{nameof(HandleSelectingTargetsWalkingState)}: Cannot stop trials");
                
                Cleanup();
                /*Debug.Log($"Set all flags to false and go Idle state");
                // we now assume we are in walking training with selecting targets. Cleanup!
                CancelInvoke(nameof(OnCountdownFinished));
                CancelInvoke(nameof(OnServerValidatedTrial));
                CancelInvoke(nameof(OnServerInvalidatedTrial));
                listeningTrackEventsFlag = false;
                listeningTargetsEventsFlag = false;
                metronome.enabled = false;
                walkingStateTrigger.enabled = false; // also hides track borders
                targetsManager.EnsureTargetsHidden();*/
                _state = State.Idle;
                break;
            default: 
                throw eventRedirectingMethods.Contains(eventName)
                    ? NotSupposedException(eventName, State.SelectingTargetsWalking)
                    : DefaultException(nameof(HandleSelectingTargetsWalkingState));
        }
    }

    private void HandleAwaitingServerValidationOfLastTrialState(string eventName)
    {
        Debug.Log($"{nameof(HandleAwaitingServerValidationOfLastTrialState)} handles {eventName} event");
        switch (eventName)
        {
            case nameof(OnServerValidatedTrial):
                // was success. Let's check if more target sizes participant has to make a trial with
                Debug.Log($"Before: _targetsSelected={_targetsSelected},  _selectionsValidated={_selectionsValidated}");
                _selectionsValidated += TargetsManager.TargetsCount + 1;
                Debug.Log($"After: _targetsSelected={_targetsSelected},  _selectionsValidated={_selectionsValidated}");
                if (!targetSizesSequence.MoveNext())
                {
                    // we now assume we are in trials, not training
                    Debug.Log($"Targets sizes are finished. Save data, set all flags to false, disable metronome and track and tell server");
                    // this is end. Clean up, save data and tell server trials are finished
                    
                    walkingStateTrigger.enabled = false;
                    SaveLoggersDataAsync(() =>
                    {
                        trialsFinished.Invoke();
                        _state = State.Idle;
                    });
                }
                else
                {
                    targetsManager.TargetSize = targetSizesSequence.Current;
                    
                    var NextStandingTrial = new Action(() =>
                    {
                        targetsManager.EnsureTargetsShown();
                        Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                        _state = State.SelectingTargetsStanding;
                    });

                    var NextWalkingTrial = new Action(() =>
                    {
                        metronome.enabled = true;
                        listeningTrackEventsFlag = true;
                        targetsManager.EnsureTargetsShown();
                        _state = State.AwaitingParticipantEnterTrack;
                    });

                    var nextTrial = _runConfig.context == Context.Standing ? NextStandingTrial : NextWalkingTrial;

                    if (_runConfig.isTraining) nextTrial(); // sync variant
                    else SaveLoggersDataAsync(nextTrial); // async variant
                    
                    /*if (_runConfig.isTraining)
                    {
                        if (_runConfig.context == Context.Standing)
                        {
                            targetsManager.ShowTargets();
                            Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                            _state = State.SelectingTargetsStanding;
                        }
                        else
                        {
                            metronome.enabled = true;
                            listeningTrackEventsFlag = true;
                            targetsManager.ShowTargets();
                            _state = State.AwaitingParticipantEnterTrack;
                        }
                    }
                    else
                    {
                        SaveLoggersDataAsync(() =>
                        {
                            
                        });
                    }*/
                    
                    // previous was validated, but we have at least one more target size. Save data to disk and wait for use enter track
                    // remark: saveDataToDisk is needed, because if next trial will be invalidated, we will not have an opportunity to clearUnSavedData only of it
                    // remark2: saveDataToDisk is not thread safe. We just pray, that it will finish until
                    // UPD: mind-breaking situation, because of multithreading (if we just call SaveDataDisk(), the application will freeze => we need to wait)

                    /*Debug.Log($"Next targetSize will be {Enum.GetName(typeof(TargetsManager.TargetSizeVariant), targetSizesSequence.Current)?.ToUpper()}. But we have to make a trick with multithreading");
                    targetsManager.TargetSize = targetSizesSequence.Current;
                    
                    var startNextTrial = new Action(() =>
                    {
                        Debug.Log($"Successfully saved all data. Now activate next trial with size = {Enum.GetName(typeof(TargetsManager.TargetSizeVariant), targetSizesSequence.Current)?.ToUpper()}. Turn on metronome, start listening track events and go AwaitingEnter track state");
                        metronome.enabled = true;
                        listeningTrackEventsFlag = true;
                        targetsManager.ShowTargets();

                        _selectionsLogger.DataSavedToDiskCallback = null;
                        _highFrequencyLogger.DataSavedToDiskCallback = null;

                        _state = State.AwaitingParticipantEnterTrack;
                    });
                    
                    SaveLoggersDataAsync(() =>
                    {
                        metronome.enabled = true;
                        listeningTrackEventsFlag = true;
                        targetsManager.ShowTargets();
                        _state = State.AwaitingParticipantEnterTrack;
                    });

                    object callbackLock = new();
                    int saved = 0;
                    
                    var callback = new Action(() =>
                    {
                        lock (callbackLock)
                        {
                            saved++;
                            Debug.Log($"Now saved = {saved}");
                            if (saved == 2) doOnNextUpdate = startNextTrial;
                        }
                    });

                    _selectionsLogger.DataSavedToDiskCallback = callback;
                    _highFrequencyLogger.DataSavedToDiskCallback = callback;
                    
                    _selectionsLogger.SaveDataToDisk();
                    _highFrequencyLogger.SaveDataToDisk();*/
                }
                break;
            case nameof(OnServerInvalidatedTrial):
                // server said no (as usual, because participant has walked without metronome)
                // Let's clearUnsavedData in loggers (if this is not training) and rerun trial with this size once more
                Debug.Log("Invalid trial? Let's show error to participant");
                ShowErrorToParticipant();
                Debug.Log($"Before: _targetsSelected={_targetsSelected},  _selectionsValidated={_selectionsValidated}");
                _targetsSelected = _selectionsValidated;
                Debug.Log($"After: _targetsSelected={_targetsSelected},  _selectionsValidated={_selectionsValidated}");

                if (!_runConfig.isTraining)
                {
                    _selectionsLogger.ClearUnsavedData();
                    _highFrequencyLogger.ClearUnsavedData();
                }
                
                if (_runConfig.context == Context.Standing)
                {
                    targetsManager.EnsureTargetsShown();
                    Invoke(nameof(OnCountdownFinished), GenerateTimeToActivateFirstTarget());
                    _state = State.SelectingTargetsStanding;
                }
                else
                {
                    metronome.enabled = true;
                    listeningTrackEventsFlag = true;
                    targetsManager.EnsureTargetsShown();
                    _state = State.AwaitingParticipantEnterTrack;
                }
                
                /*listeningTrackEventsFlag = true;
                metronome.enabled = true;
                targetsManager.ShowTargets();
                
                _selectionsLogger.ClearUnsavedData();
                _highFrequencyLogger.ClearUnsavedData();
                
                Debug.Log($"We cleared data and started listening track events. Go AwaitingParticipantEnterTrack state");

                _state = State.AwaitingParticipantEnterTrack;*/
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
        nameof(OnCountdownFinished), nameof(OnSelectorEnteredTargetZone), nameof(OnSelectorExitedTargetZone),
        nameof(OnParticipantEnteredTrack), nameof(OnParticipantFinishedTrack), nameof(OnParticipantSwervedOffTrack), nameof(OnParticipantSlowedDown),
        nameof(OnServerSaidStart), nameof(OnServerSaidPrepare), nameof(OnServerSaidFinishTraining), nameof(OnServerValidatedTrial), nameof(OnServerInvalidatedTrial)
    };
    
    private void OnCountdownFinished() => HandleState(nameof(OnCountdownFinished));
    private void OnSelectorEnteredTargetZone() { if (listeningTargetsEventsFlag) HandleState(nameof(OnSelectorEnteredTargetZone)); }
    private void OnSelectorExitedTargetZone() { if (listeningTargetsEventsFlag) HandleState(nameof(OnSelectorExitedTargetZone)); }
    private void OnParticipantEnteredTrack() { if (listeningTrackEventsFlag) HandleState(nameof(OnParticipantEnteredTrack)); }
    private void OnParticipantFinishedTrack() { if (listeningTrackEventsFlag) HandleState(nameof(OnParticipantFinishedTrack)); }
    private void OnParticipantSwervedOffTrack() { if (listeningTrackEventsFlag) HandleState(nameof(OnParticipantSwervedOffTrack)); }
    private void OnParticipantSlowedDown() { if (listeningTrackEventsFlag) HandleState(nameof(OnParticipantSlowedDown)); }
    public void OnServerSaidStart() => HandleState(nameof(OnServerSaidStart));
    public void OnServerSaidFinishTraining() => HandleState(nameof(OnServerSaidFinishTraining));

    public void OnServerSaidPrepare(RunConfig config)
    {
        if (_state == State.Idle) _runConfig = config;
        
        HandleState(nameof(OnServerSaidPrepare));
    }
    public void OnServerValidatedTrial() => HandleState(nameof(OnServerValidatedTrial));
    public void OnServerInvalidatedTrial() => HandleState(nameof(OnServerInvalidatedTrial));
    #endregion
}

partial class ExperimentManager
{
    public enum Context
    {
        Standing,
        Walking,
    }
    
    public enum ReferenceFrame
    {
        PalmReferenced, // both rotation and position by hand
        HandReferenced, // position only
        PathReferenced, // head
        PathReferencedNeck // neck
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
        public readonly ReferenceFrame referenceFrame;

        public RunConfig(int participantID, bool leftHanded, bool isMetronomeTraining, bool isTraining, Context context, ReferenceFrame referenceFrame)
        {
            this.participantID = participantID;
            this.leftHanded = leftHanded;
            this.isMetronomeTraining = isMetronomeTraining;
            this.isTraining = isTraining;
            this.context = context;
            this.referenceFrame = referenceFrame;
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
