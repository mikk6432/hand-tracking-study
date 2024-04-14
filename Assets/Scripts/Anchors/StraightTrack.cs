using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class StraightTrack : MonoBehaviour
{
    [SerializeField] public float halfTrackLength = 2.75f;
    [SerializeField] public float halfTrackWidth = 0.35f;
    [SerializeField] private Transform _headset;
    private void Start()
    {
        transform.localScale = new Vector3(halfTrackWidth * 2, 0.01f, halfTrackLength * 2);
    }
    public (bool withinLength, bool withinWidth) IsInsideTheTrack()
    {
        var localPos = transform.parent.InverseTransformPoint(_headset.position);
        return (Mathf.Abs(localPos.z) < halfTrackLength,
            Mathf.Abs(localPos.x) < halfTrackWidth);
    }
    public Transform WalkingDirection()
    {
        // Check which direction, relative to the track orientation, the user is looking
        bool sameDirection = Vector3.Dot(_headset.forward, transform.parent.forward) >= 0;
        // Always parallel to the track
        var result = new GameObject().transform;
        result.rotation = Quaternion.LookRotation(sameDirection ? transform.parent.forward : -transform.parent.forward);
        result.position = transform.parent.position;
        return result;

    }

}