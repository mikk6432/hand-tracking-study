using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Linq;
using System.Runtime.CompilerServices;
using Logging;
using SpatialUIPlacement;
using UnityEngine;
using UnityEngine.Events;
using Utils;
using Math = Utils.Math;

public partial class ExperimentManager: MonoBehaviour
{
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
    
    void LogLastSelectionIntoSelectionLogger(int systemClockMilliseconds, int selectionDurationMilliseconds)
    {
        var row = _selectionsLogger.CurrentRow;
        
        // ids
        row.SetColumnValue("ParticipantID", _runConfig.participantID);
        row.SetColumnValue("SelectionID", _targetsSelected);

        var selection = targetsManager.LastSelectionData;
        
        // conditions
        row.SetColumnValue("Walking", _runConfig.context == Context.Standing ? 0 : 1);
        row.SetColumnValue("ReferenceFrame", (int)_runConfig.referenceFrame);
        row.SetColumnValue("TargetSize", selection.targetSize);
        row.SetColumnValue("DominantHand", _runConfig.leftHanded ? 1 : 0);

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

    private static readonly string[] highFrequencyLogColumns =
    {
        // ids
        "ParticipantID",
        "MeasurementID", // increments every measurement (90Hz). Starts with 0
        
        // conditions
        "Walking", // 0 or 1. 0 – false, means Standing, 1 – true, means Walking
        "ReferenceFrame", // 0 or 1 or 2 or 3. 0 – palmReferenced, 1 – handReferenced, 2 – pathReferenced, 3 – pathReferencedNeck
        "TargetSize", // 0.015 or 0.025 or 0.035
        
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
    
    // not used for trainings. To finish training, method (OnServerSaidFinishTraining should be called)
    public readonly UnityEvent trialFinished = new();
    public readonly UnityEvent<string> unexpectedErrorOccured = new();

    private State _state;
    private RunConfig _runConfig; 
    
    [SerializeField] private TargetsManager targetsManager;
    [SerializeField] private WalkingStateTrigger walkingStateTrigger;
    
    
    [Space]
    [SerializeField] private GameObject metronome;
    [SerializeField] private GameObject errorIndicator;
    [SerializeField] private GameObject track;
    [SerializeField] private GameObject sceneLight; // remark: we interpret it as track in standing context. Hand Ref and path ref depend on it, actually

    [SerializeField] private GameObject walkingDirection; // walking context (relative to track)
    [SerializeField] private GameObject standingDirection; // standing context (relative to light)

    [SerializeField] private GameObject headset;
    [SerializeField] private GameObject neckBase;
    // SimplifiedComfortUIPlacement position.y depends on headset.
    // We refresh it when we want (usually after "prepare" command from server) by calling comfortUICoordinateY.Refresh()
    // Path-refFrame positioning depend on it.
    [SerializeField] private SimplifiedComfortUIPlacement comfortUICoordinateY;

    [Space] // oculus hands here. Note, that we keep inactive gameObjects which we don't use 
    [SerializeField] private GameObject leftIndexTip;
    [SerializeField] private GameObject rightIndexTip;
    [SerializeField] private GameObject leftPalmCenter;
    [SerializeField] private GameObject rightPalmCenter;
    private GameObject dominantHandIndexTip; // holds selector
    private GameObject weakHandPalmCenter; // holds target
    private GameObject dominantHandPalmCenter; // just for logging

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
    
    [SerializeField, Tooltip("Minimum number of seconds since the moment the participant enters the track until the prompt appears. The random value will be add up to it to make the participant acquire the target at different phases of walking")]
    private float _timeUntilPrompt = 0.6f;

    // public bool IsRunning { get; private set; }

    private int _targetsSelected = 0;
    private DateTime activateFirstTargetMoment;
    private DateTime selectFirstTargetMoment;
    private DateTime selectPreviousTargetMoment;
    private IEnumerator<TargetsManager.TargetSizeVariant> targetSizesSequence;
    private IEnumerator<int> targetsIndexesSequence;

    private bool highFrequencyLoggingIsOnFlag;
    private AsyncHighFrequencyCSVLogger _selectionsLogger;
    private AsyncHighFrequencyCSVLogger _highFrequencyLogger;

    private void Start()
    {
    }

    private void LateUpdate()
    {
        // asyncHighFrequencyLogging stuff goes here

        if (!highFrequencyLoggingIsOnFlag) return;
        // now we assume that we are inside trial session and logger is initialized already

        var row = _highFrequencyLogger.CurrentRow;
        
        // TODO: add Ids, Conditions, Times

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
        var selectorProjection = Utils.Math.ProjectPointOntoOXYPlane(allTargets, selectorPosition);
        row.SetColumnValue("SelectorProjectionOntoAllTargetsX", selectorProjection.local.x);
        row.SetColumnValue("SelectorProjectionOntoAllTargetsY", selectorProjection.local.y);
        var isInside = targetsManager.IsSelectorInsideCollider;
        row.SetColumnValue("IsSelectorInsideCollider", isInside ? 1 : 0);
        var distance = (selectorPosition - selectorProjection.world).magnitude;
        var distanceToLog = isInside ? -distance : distance;
        row.SetColumnValue("DistanceFromSelectorToAllTargetsOXYPlane", distanceToLog);
        
        row.SetColumnValue("ActiveTargetIndex", targetsManager.ActiveTarget.targetIndex);
        var activeTargetProjection = Utils.Math.ProjectPointOntoOXYPlane(allTargets, activeTarget.position);
        row.SetColumnValue("ActiveTargetInsideAllTargetsX", activeTargetProjection.local.x);
        row.SetColumnValue("ActiveTargetInsideAllTargetsY", activeTargetProjection.local.y);
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

    void StartListeningTrackEvents()
    {
        walkingStateTrigger.ParticipantEntered.AddListener(OnParticipantEnteredTrack);
        walkingStateTrigger.ParticipantSwervedOff.AddListener(OnParticipantSwervedOffTrack);
        walkingStateTrigger.ParticipantSlowedDown.AddListener(OnParticipantSlowedDown);
        walkingStateTrigger.ParticipantFinished.AddListener(OnParticipantFinishedTrack);
    }

    void StopListeningTrackEvents()
    {
        walkingStateTrigger.ParticipantEntered.RemoveListener(OnParticipantEnteredTrack);
        walkingStateTrigger.ParticipantSwervedOff.RemoveListener(OnParticipantSwervedOffTrack);
        walkingStateTrigger.ParticipantSlowedDown.RemoveListener(OnParticipantSlowedDown);
        walkingStateTrigger.ParticipantFinished.RemoveListener(OnParticipantFinishedTrack);
    }

    void StartListeningTargetsEvents()
    {
        targetsManager.selectorEnteredTargetsZone.AddListener(OnSelectorEnteredTargetZone);
        targetsManager.selectorExitedTargetsZone.AddListener(OnSelectorExitedTargetZone);
    }
    
    void StopListeningTargetsEvents()
    {
        targetsManager.selectorEnteredTargetsZone.RemoveListener(OnSelectorEnteredTargetZone);
        targetsManager.selectorExitedTargetsZone.RemoveListener(OnSelectorExitedTargetZone);
    }
    
    void ShowErrorToParticipant()
    {
        var headsetTransform = headset.transform;
        var errorPosition = headsetTransform.position + headsetTransform.forward * 0.5f;
        errorIndicator.transform.SetPositionAndRotation(errorPosition, headsetTransform.rotation);
        errorIndicator.SetActive(true); // will be set inactive automatically 
    }

    (Vector3 position, Quaternion rotation) HeadsetOXZProjection()
    {
        var headsetTransform = headset.transform;
        var headsetPosition = headsetTransform.position;
        var position = new Vector3(headsetPosition.x, 0, headsetPosition.z);

        var headsetForward = headsetTransform.forward;
        var rotation = Quaternion.LookRotation(new Vector3(headsetForward.x, 0, headsetForward.z));
        return (position, rotation);
    }
    
    void PlaceTrackWhereHeadset()
    {
        var (position, rotation) = HeadsetOXZProjection();
        track.transform.SetPositionAndRotation(position, rotation);
    }
    
    void PlaceLightWhereHeadset()
    {
        var (position, rotation) = HeadsetOXZProjection();
        sceneLight.transform.SetPositionAndRotation(position, rotation);
    }
    
    void PlaceLightWhereTrack()
    {
        var trackTransform = track.transform;
        sceneLight.transform.SetPositionAndRotation(trackTransform.position, trackTransform.rotation);
    }

    void InitSelectionsLogger()
    {
        try
        {
            var filename = $"{_runConfig.participantID}_selections.csv";
            _selectionsLogger = new AsyncHighFrequencyCSVLogger(filename);

            if (!_selectionsLogger.HasBeenInitialised())
            {
                _selectionsLogger.AddColumns(selectionLogColumns);
                _selectionsLogger.Initialise();
            }
        }
        catch (Exception e)
        {
            throw new Exception($"Message= {e.Message}\n\nInner= {e?.InnerException?.ToString()}\n\nStackTrace= {e.StackTrace}");
        }
    }

    void InitHighFrequencyLogger()
    {
        var filename = $"{_runConfig.participantID}_highFrequency.csv";
        _highFrequencyLogger = new AsyncHighFrequencyCSVLogger(filename);

        if (!_highFrequencyLogger.HasBeenInitialised())
        {
            _highFrequencyLogger.AddColumns(highFrequencyLogColumns);
            _highFrequencyLogger.Initialise();
        }
    }

    private static IEnumerator<TargetsManager.TargetSizeVariant> GenerateTargetSizesSequence(bool isTraining)
    {
        IEnumerator<TargetsManager.TargetSizeVariant> TrainingSequence()
        {
            while (true)
            {
                yield return TargetsManager.TargetSizeVariant.Big;
                yield return TargetsManager.TargetSizeVariant.Medium;
                yield return TargetsManager.TargetSizeVariant.Small;
            }
        }

        IEnumerator<TargetsManager.TargetSizeVariant> TrialSequence()
        {
            // TODO: implement random
            yield return TargetsManager.TargetSizeVariant.Big;
            yield return TargetsManager.TargetSizeVariant.Medium;
            yield return TargetsManager.TargetSizeVariant.Small;
        }

        return isTraining ? TrainingSequence() : TrialSequence();
    }

    private static IEnumerator<int> GenerateTargetsIndexesSequence(bool isTraining)
    {
        return Math.FittsLaw(TargetsManager.TargetsCount)
            .Take(isTraining ? TargetsManager.TargetsCount : TargetsManager.TargetsCount + 1)
            .GetEnumerator();
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
                case State.WalkingWithMetronomeTraining:
                    HandleWalkingWithMetronomeTrainingState(eventName);
                    break;
                case State.SelectingTargetsStanding:
                    HandleSelectingTargetsStandingState(eventName);
                    break;
                default:
                    throw new ArgumentException(
                        $"It seems that you have implemented new State, but forget to add it to {nameof(HandleState)}");
            }
        }
        catch (Exception e)
        {
            unexpectedErrorOccured.Invoke(e.Message);
        }
    }

    /*void HandConcreteStateTemplate(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnCountdownFinished):
                break;
            case nameof(OnParticipantSelectedTarget):
                break;
            case nameof(OnParticipantEnteredTrack):
                break;
            case nameof(OnParticipantFinishedTrack):
                break;
            case nameof(OnParticipantSwervedOffTrack):
                break;
            case nameof(OnParticipantSlowedDown):
                break;
            case nameof(OnServerSaidStart):
                break;
            case nameof(OnServerSaidFinishTraining):
                break;
            case nameof(OnServerSaidPrepare):
                break;
            default:
                throw new ArgumentException($"It seems you have implemented new event but forgot to handle in method {nameof(HandConcreteStateTemplate)}");
        }
    }*/
    
    void HandleIdleState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnCountdownFinished):
            case nameof(OnSelectorEnteredTargetZone):
            case nameof(OnSelectorExitedTargetZone):
            case nameof(OnParticipantEnteredTrack):
            case nameof(OnParticipantFinishedTrack):
            case nameof(OnParticipantSwervedOffTrack):
            case nameof(OnParticipantSlowedDown):
            case nameof(OnServerSaidFinishTraining):
                throw new ArgumentException($"{eventName} got called in Idle state. This is not supposed to happen");
            case nameof(OnServerSaidPrepare):
                if (_runConfig.isMetronomeTraining)
                {
                    PlaceTrackWhereHeadset();
                    PlaceLightWhereTrack();
                    walkingStateTrigger.enabled = true; // but no listening yet
                    break;
                }

                if (_runConfig.context == Context.Standing)
                {
                    PlaceLightWhereHeadset();
                    comfortUICoordinateY.Refresh();

                    bool prepareCalledFirstTime = !targetsManager.IsShowingTargets;
                    if (prepareCalledFirstTime)
                    {
                        ActualizeHands();
                        ActualizeReferenceFrames();
                        targetsManager.Anchor = activeRefFrame;
                        
                        bool isTrial = !_runConfig.isTraining;
                        if (isTrial)
                        {
                            InitSelectionsLogger();
                            // InitHighFrequencyLogger(); // todo: uncomment this
                        }
                        
                        targetSizesSequence = GenerateTargetSizesSequence(_runConfig.isTraining);
                        targetsIndexesSequence = GenerateTargetsIndexesSequence(_runConfig.isTraining);

                        targetSizesSequence.MoveNext();
                        targetsManager.TargetSize = targetSizesSequence.Current;
                        _targetsSelected = 0;
                        targetsManager.ShowTargets();
                    }
                    break;
                }
                break;
            case nameof(OnServerSaidStart):
                if (_runConfig.isMetronomeTraining)
                {
                    metronome.SetActive(true);
                    StartListeningTrackEvents();
                    _state = State.WalkingWithMetronomeTraining;
                    break;
                }

                if (_runConfig.context == Context.Standing)
                {
                    if (!targetsManager.IsShowingTargets)
                        throw new ArgumentException("Cannot call start before prepare");

                    targetsIndexesSequence.MoveNext();
                    targetsManager.ActivateTarget(targetsIndexesSequence.Current);
                    StartListeningTargetsEvents();

                    if (!_runConfig.isTraining)
                    {
                        activateFirstTargetMoment = TimeMeasurementHelper.GetHighResolutionDateTime();
                        // highFrequencyLoggingIsOnFlag = true; // todo: uncomment this
                    }
                    
                    _state = State.SelectingTargetsStanding;
                    break;
                }

                throw new ArgumentException("for some reason context is not standing"); // todo: delete
                break;
            default:
                throw new ArgumentException($"It seems you have implemented new event but forgot to handle in method {nameof(HandleIdleState)}");
        }
    }

    void HandleWalkingWithMetronomeTrainingState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnCountdownFinished):
            case nameof(OnSelectorEnteredTargetZone):
            case nameof(OnSelectorExitedTargetZone):
            case nameof(OnServerSaidPrepare):
            case nameof(OnServerSaidStart):
                throw new ArgumentException($"{eventName} got called in WalkingWithMetronomeTraining state. This is not supposed to happen");
            case nameof(OnParticipantEnteredTrack):
            case nameof(OnParticipantFinishedTrack):
                break;
            case nameof(OnParticipantSwervedOffTrack):
            case nameof(OnParticipantSlowedDown):
                ShowErrorToParticipant();
                break;
            case nameof(OnServerSaidFinishTraining):
                StopListeningTrackEvents();
                metronome.SetActive(false);
                walkingStateTrigger.enabled = false; // also hides track borders
                _state = State.Idle;
                break;
            default:
                throw new ArgumentException($"It seems you have implemented new event but forgot to handle in method {nameof(HandleWalkingWithMetronomeTrainingState)}");
        }
    }
    
    void HandleSelectingTargetsStandingState(string eventName)
    {
        switch (eventName)
        {
            case nameof(OnCountdownFinished):
            case nameof(OnParticipantEnteredTrack):
            case nameof(OnParticipantFinishedTrack):
            case nameof(OnParticipantSwervedOffTrack):
            case nameof(OnParticipantSlowedDown):
            case nameof(OnServerSaidStart):
            case nameof(OnServerSaidPrepare):
                throw new ArgumentException(
                    $"{eventName} got called in SelectingTargetsStanding state. This is not supposed to happen"
                    );
            case nameof(OnSelectorEnteredTargetZone):
                _targetsSelected++;
                if (!_runConfig.isTraining)
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

                    LogLastSelectionIntoSelectionLogger(systemClockMilliseconds, selectionDurationMilliseconds);
                }
                break;
            case nameof(OnSelectorExitedTargetZone):
                bool isLastWithSuchSize = !targetsIndexesSequence.MoveNext();
                if (isLastWithSuchSize)
                {
                    bool hasNextTargetSize = targetSizesSequence.MoveNext();

                    if (hasNextTargetSize)
                    {
                        targetsManager.TargetSize = targetSizesSequence.Current;
                        targetsIndexesSequence = GenerateTargetsIndexesSequence(_runConfig.isTraining);
                        targetsIndexesSequence.MoveNext();
                        targetsManager.ActivateTarget(targetsIndexesSequence.Current);
                    }
                    else
                    {
                        // we now sure this is not training, because in training
                        // targetSizesSequence.MoveNext() always returns true cause sequence is infinite
                        highFrequencyLoggingIsOnFlag = false;
                        _targetsSelected = 0;
                        targetSizesSequence = null;
                        targetsIndexesSequence = null;
                        
                        StopListeningTargetsEvents();
                        targetsManager.HideTargets();
                        
                        _selectionsLogger.SaveDataToDisk();
                        // _highFrequencyLogger.SaveDataToDisk(); // TODO: uncomment this
                        
                        _state = State.Idle;
                        trialFinished.Invoke();
                    }
                    
                    /*targetsManager.HideTargets(); 

                    if (_runConfig.isTraining)
                    {
                        if (targetSizesSequence.MoveNext())
                        {
                            targetsManager.TargetSize = targetSizesSequence.Current;
                            targetsManager.ShowTargets();
                            targetsManager.ActivateFirstTarget();
                        }
                        else
                        {
                            StopListeningTargetsEvents();
                            _targetsSelected = 0;
                            targetSizesSequence = null;
                        }
                    }
                    else
                    {
                        bool hasNextSizeToMeasure = targetSizesSequence.MoveNext(); // if false, then stop logging, save data and go Idle
                        if (hasNextSizeToMeasure)
                        {
                            targetsManager.TargetSize = targetSizesSequence.Current;
                            targetsManager.ShowTargets();
                            targetsManager.ActivateFirstTarget();
                            // now continue logging with new target size
                        }
                        else
                        {
                            highFrequencyLoggingIsOnFlag = false;
                            StopListeningTargetsEvents();
                            _targetsSelected = 0;
                            targetSizesSequence = null;
                            
                            _selectionsLogger.SaveDataToDisk();
                            // _highFrequencyLogger.SaveDataToDisk(); // TODO: uncomment this
                            _state = State.Idle;
                            trialFinished.Invoke();
                        }
                    }*/
                }
                else
                {
                    targetsManager.ActivateTarget(targetsIndexesSequence.Current);
                }
                break;
            case nameof(OnServerSaidFinishTraining):
                if (!_runConfig.isTraining) throw new ArgumentException($"{eventName} got called in SelectingTargetsStanding state while trials. This is not supposed to happen");
                
                targetsManager.HideTargets();
                StopListeningTargetsEvents();
                _targetsSelected = 0;
                targetSizesSequence = null;
                targetsIndexesSequence = null;
                _state = State.Idle;
                break;
            default:
                throw new ArgumentException($"It seems you have implemented new event but forgot to handle in method {nameof(HandleSelectingTargetsStandingState)}");
        }
    }


    private void OnCountdownFinished() => HandleState(nameof(OnCountdownFinished));
    private void OnSelectorEnteredTargetZone() => HandleState(nameof(OnSelectorEnteredTargetZone));
    private void OnSelectorExitedTargetZone() => HandleState(nameof(OnSelectorExitedTargetZone));
    private void OnParticipantEnteredTrack() => HandleState(nameof(OnParticipantEnteredTrack));
    private void OnParticipantFinishedTrack() => HandleState(nameof(OnParticipantFinishedTrack));
    private void OnParticipantSwervedOffTrack() => HandleState(nameof(OnParticipantSwervedOffTrack));
    private void OnParticipantSlowedDown() => HandleState(nameof(OnParticipantSlowedDown));
    public void OnServerSaidStart() => HandleState(nameof(OnServerSaidStart));
    public void OnServerSaidFinishTraining() => HandleState(nameof(OnServerSaidFinishTraining));

    public void OnServerSaidPrepare(RunConfig config)
    {
        _runConfig = config;
        
        HandleState(nameof(OnServerSaidPrepare));
    }
}

partial class ExperimentManager
{
    [Serializable]
    public enum Context
    {
        Standing,
        Walking,
    }
    
    [Serializable]
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
        Idle,
        WalkingWithMetronomeTraining,
        SelectingTargetsStanding,
    }
}
