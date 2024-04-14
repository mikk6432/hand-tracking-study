using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class WalkingStateTrigger : MonoBehaviour
{
    [SerializeField] public float repeatRate = 1f; // Every second

    [SerializeField] private Transform _headset;

    public ExperimentManager.Context track = ExperimentManager.Context.Standing;

    [SerializeField] private GameObject standing;
    [SerializeField] private GameObject circleTrack;
    [SerializeField] private GameObject straightTrack;
    [SerializeField] private CircleTrack circleTrackObj;
    [SerializeField] private StraightTrack straightTrackObj;

    [SerializeField, Tooltip("Minimum walking speed which is considered acceptable in meters per second")]
    private float _thresholdSpeed = 1f;

    public UnityEvent ParticipantEntered = new();
    public UnityEvent ParticipantFinished = new();
    public UnityEvent ParticipantSwervedOff = new();
    public UnityEvent ParticipantSlowedDown = new();


    private bool _walking;
    private (bool withinLength, bool withinWidth) _prevLocation;
    private Vector3 _prevCoords;

    private void OnEnable()
    {
        _walking = false;
        _prevLocation = (false, false);

        standing.SetActive(false);

        if (track == ExperimentManager.Context.Circle)
        {
            circleTrack.SetActive(true);
            FindObjectOfType<SimplifiedWalkingDirection>().track = ExperimentManager.Context.Circle;
        }
        else if (track == ExperimentManager.Context.Walking)
        {
            straightTrack.SetActive(true);
            FindObjectOfType<SimplifiedWalkingDirection>().track = ExperimentManager.Context.Walking;
        }
        else
        {
            standing.SetActive(true);
            FindObjectOfType<SimplifiedWalkingDirection>().track = ExperimentManager.Context.Standing;
        }


        ParticipantEntered.AddListener(OnParticipantEntered);
        ParticipantFinished.AddListener(OnWalkingDone);
        ParticipantSwervedOff.AddListener(OnWalkingDone);
        ParticipantSlowedDown.AddListener(OnWalkingDone);

        _prevLocation = track == ExperimentManager.Context.Circle
            ? circleTrackObj.IsInsideTheTrack()
            : track == ExperimentManager.Context.Walking ? straightTrackObj
            .IsInsideTheTrack() : (withinLength: false, withinWidth: false);
    }

    private void OnDisable()
    {
        circleTrack.SetActive(false);
        straightTrack.SetActive(false);
        standing.SetActive(true);
        FindObjectOfType<SimplifiedWalkingDirection>().track = ExperimentManager.Context.Standing;

        ParticipantEntered.RemoveListener(OnParticipantEntered);
        ParticipantFinished.RemoveListener(OnWalkingDone);
        ParticipantSwervedOff.RemoveListener(OnWalkingDone);
    }

    private void Update()
    {
        if (track == ExperimentManager.Context.Standing)
            return;
        var currentLocation = track == ExperimentManager.Context.Circle
            ? circleTrackObj.IsInsideTheTrack()
            : track == ExperimentManager.Context.Walking ? straightTrackObj
            .IsInsideTheTrack() : (withinLength: false, withinWidth: false);
        // If the participant crosses the start line from outside the track
        if (!_prevLocation.withinLength && currentLocation.withinWidth && currentLocation.withinLength)
            ParticipantEntered.Invoke();

        // If the participant crosses the side line while walking
        else if (_walking && _prevLocation.withinWidth && !currentLocation.withinWidth)
            ParticipantSwervedOff.Invoke();

        // If the participant crosses the finish line
        else if (_walking && _prevLocation.withinLength && !currentLocation.withinLength)
            ParticipantFinished.Invoke();

        _prevLocation = currentLocation;
    }

    private void CheckWalkingSpeed()
    {
        // If in the past second the participant has walked less than _thresholdSpeed (m/sec)
        if ((_prevCoords - _headset.position).magnitude < _thresholdSpeed)
            ParticipantSlowedDown.Invoke();

        _prevCoords = _headset.position;
    }

    private void OnParticipantEntered()
    {
        _prevCoords = _headset.position;
        InvokeRepeating(nameof(CheckWalkingSpeed), repeatRate, repeatRate);

        _walking = true;
    }

    private void OnWalkingDone()
    {
        _walking = false;
        CancelInvoke(nameof(CheckWalkingSpeed));
    }
}