using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class CircleTrack : MonoBehaviour
{
    [SerializeField] public float halfTrackLength = 1f;
    [SerializeField] public float halfTrackWidth = 1f;
    [SerializeField] private Transform inner;
    [SerializeField] private Transform outer;
    [SerializeField] private Transform startingPosition;
    [SerializeField] private Transform _headset;

    private float innerRadius;
    private float outerRadius;
    private void Start()
    {
        innerRadius = halfTrackLength - halfTrackWidth;
        outerRadius = halfTrackLength + halfTrackWidth;
        inner.localScale = new Vector3(innerRadius * 2, 0.01f, innerRadius * 2);
        outer.localScale = new Vector3(outerRadius * 2, 0.01f, outerRadius * 2);
        startingPosition.localPosition = new Vector3(0, 0.01f, halfTrackLength);
        startingPosition.localScale = new Vector3(halfTrackWidth * 2, 0.01f, halfTrackWidth * 2);
    }
    public (bool withinLength, bool withinWidth) IsInsideTheTrack()
    {
        // Transform the head's position to the coordinate system of the track
        var floorPos = new Vector3(_headset.position.x, 0, _headset.position.z);
        var distance = Vector3.Distance(floorPos, transform.position);
        var onStart = Vector3.Distance(floorPos, startingPosition.position) < halfTrackWidth;
        return (!onStart && distance < outerRadius && distance > innerRadius, distance < outerRadius && distance > innerRadius);
    }

    public Transform WalkingDirection()
    {
        var centerToHeadset = _headset.position - transform.parent.position;
        centerToHeadset.y = 0;
        // rotate to the left by 90 degrees
        var centerToHeadsetLeft = new Vector3(centerToHeadset.z, 0, -centerToHeadset.x);
        var sameDirection = Vector3.Dot(_headset.forward, centerToHeadsetLeft);
        // Always parallel to the track
        var result = new GameObject().transform;
        result.rotation = Quaternion.LookRotation(sameDirection > 0 ? centerToHeadsetLeft : -centerToHeadsetLeft);
        result.position = transform.parent.position + centerToHeadset.normalized * halfTrackLength;
        return result;
    }

}