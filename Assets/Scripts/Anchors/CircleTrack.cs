using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class CircleTrack : MonoBehaviour
{
    [SerializeField] public float halfTrackLength = 1f;
    [SerializeField] public float halfTrackWidth = 1f;
    [SerializeField] public float startPosLength = 1f;
    [SerializeField] private Ring inner;
    [SerializeField] private Ring outer;
    [SerializeField] private GameObject directionArrow;
    [SerializeField] private Transform _headset;

    private float innerRadius;
    private float outerRadius;
    private void Start()
    {
        innerRadius = halfTrackLength - halfTrackWidth;
        outerRadius = halfTrackLength + halfTrackWidth;
        inner.radius = innerRadius;
        inner.width = 0.05f;
        inner.height = 0.01f;
        inner.lengthOfC = startPosLength / halfTrackLength * innerRadius;
        outer.radius = outerRadius;
        outer.width = 0.05f;
        outer.height = 0.01f;
        outer.lengthOfC = startPosLength / halfTrackLength * outerRadius;
        inner.Render();
        outer.Render();
        /* startingPosition.localPosition = new Vector3(0, 0.0005f, -halfTrackLength);
        startingPosition.localScale = new Vector3(halfTrackWidth * 2, 0.01f, halfTrackWidth * 2);*/
    }
    public (bool withinLength, bool withinWidth) IsInsideTheTrack()
    {
        // Transform the head's position to the coordinate system of the track
        var floorPos = new Vector3(_headset.position.x, transform.position.y, _headset.position.z);
        var distance = Vector3.Distance(floorPos, transform.position);
        var fwd = new Vector3(0, 0, -halfTrackLength);
        var angle = Vector3.SignedAngle(fwd, transform.InverseTransformPoint(floorPos), Vector3.up);
        var startPosVector = new Vector3(-startPosLength / 2, 0, -MathF.Sqrt(halfTrackLength * halfTrackLength - startPosLength * startPosLength / 4));
        var startPosAngle = Vector3.SignedAngle(fwd, startPosVector, Vector3.up);
        var onStart = angle < startPosAngle && angle > -startPosAngle;
        return (!onStart, distance < outerRadius && distance > innerRadius);
    }

    public Transform WalkingDirection(Transform _headset)
    {
        var centerToHeadset = _headset.position - transform.position;
        centerToHeadset.y = transform.position.y;
        // rotate to the left by 90 degrees
        var centerToHeadsetLeft = new Vector3(centerToHeadset.z, transform.position.y, -centerToHeadset.x);
        var sameDirection = Vector3.Dot(_headset.forward, centerToHeadsetLeft);
        // Always parallel to the track
        var result = new GameObject().transform;
        result.rotation = Quaternion.LookRotation(sameDirection > 0 ? centerToHeadsetLeft : -centerToHeadsetLeft);
        result.position = transform.position + centerToHeadset.normalized * halfTrackLength;
        return result;
    }

}