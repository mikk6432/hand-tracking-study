using System;
using UnityEngine;
using UnityEngine.Events;

public class WalkingStateTrigger : MonoBehaviour
{
    private static readonly float HALF_TRACK_LENGTH = 2.75f;
    private static readonly float HALF_TRACK_WIDTH = 0.35f;
    private static readonly float REPEAT_RATE = 1f; // Every second

    [SerializeField] private Transform _headset;
    private Transform westBorder;
    private Transform eastBorder;
    
    [SerializeField, Tooltip("Minimum walking speed which is considered acceptable in meters per second")]
    private float _thresholdSpeed = 1f;
    
    public UnityEvent ParticipantEntered = new();
    public UnityEvent ParticipantFinished = new();
    public UnityEvent ParticipantSwervedOff = new();
    public UnityEvent ParticipantSlowedDown = new();
    

    private bool _walking;
    private (bool withinLength, bool withinWidth) _prevLocation;
    private Vector3 _prevCoords;

    private void Awake()
    {
        westBorder = transform.Find("West Border");
        eastBorder = transform.Find("East Border");

        if (westBorder == null)
        {
            Debug.LogError($"{nameof(westBorder)}: object not found in children");
            enabled = false;
            return;
        }
        if (eastBorder == null)
        {
            Debug.LogError($"{nameof(eastBorder)}: object not found in children");
            enabled = false;
            return;
        }
        if (_headset == null)
        {
            Debug.LogError($"{nameof(WalkingStateTrigger)}: _headset is not provided. Disabling the script");
            enabled = false;
            return;
        }
    }

    private void OnEnable()
    {
        _walking = false;
        _prevLocation = (false, false);
        
        westBorder.gameObject.SetActive(true);
        eastBorder.gameObject.SetActive(true);
        
        ParticipantEntered.AddListener(OnParticipantEntered);
        ParticipantFinished.AddListener(OnWalkingDone);
        ParticipantSwervedOff.AddListener(OnWalkingDone);
        ParticipantSlowedDown.AddListener(OnWalkingDone);
        
        _prevLocation = IsInsideTheTrack();
    }

    private void OnDisable()
    {
        westBorder.gameObject.SetActive(false);
        eastBorder.gameObject.SetActive(false);
        
        ParticipantEntered.RemoveListener(OnParticipantEntered);
        ParticipantFinished.RemoveListener(OnWalkingDone);
        ParticipantSwervedOff.RemoveListener(OnWalkingDone);
    }

    private void Update()
    {
        var currentLocation = IsInsideTheTrack();

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

    private (bool withinLength, bool withinWidth) IsInsideTheTrack()
    {
        // Transform the head's position to the coordinate system of the track
        Vector3 localPos = transform.InverseTransformPoint(_headset.position);

        return (Mathf.Abs(localPos.z) < HALF_TRACK_LENGTH,
            Mathf.Abs(localPos.x) < HALF_TRACK_WIDTH);
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
        InvokeRepeating(nameof(CheckWalkingSpeed), REPEAT_RATE, REPEAT_RATE);
        
        _walking = true;
    }

    private void OnWalkingDone()
    {
        _walking = false;
        CancelInvoke(nameof(CheckWalkingSpeed));
    }
}