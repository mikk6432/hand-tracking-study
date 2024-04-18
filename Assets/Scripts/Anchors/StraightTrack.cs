using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class StraightTrack : MonoBehaviour
{
    [SerializeField] public float halfTrackLength = 2.75f;
    [SerializeField] public float halfTrackWidth = 0.35f;
    [SerializeField] private Transform _headset;
    [SerializeField] private Transform leftBorder;
    [SerializeField] private Transform rightBorder;
    private void Start()
    {
        leftBorder.localScale = new Vector3(0.05f, 0.01f, halfTrackLength * 2);
        rightBorder.localScale = new Vector3(0.05f, 0.01f, halfTrackLength * 2);
        leftBorder.localPosition = new Vector3(-halfTrackWidth, 0, 0);
        rightBorder.localPosition = new Vector3(halfTrackWidth, 0, 0);
    }
    public (bool withinLength, bool withinWidth) IsInsideTheTrack()
    {
        var localPos = transform.InverseTransformPoint(_headset.position);
        return (Mathf.Abs(localPos.z) < halfTrackLength,
            Mathf.Abs(localPos.x) < halfTrackWidth);
    }
    public Transform WalkingDirection(Transform _headset)
    {
        // Check which direction, relative to the track orientation, the user is looking
        bool sameDirection = Vector3.Dot(_headset.forward, transform.forward) >= 0;
        // Always parallel to the track
        var result = new GameObject().transform;
        var headsetLocally = transform.InverseTransformPoint(_headset.position);
        result.rotation = Quaternion.LookRotation(sameDirection ? transform.forward : -transform.forward);
        result.position = transform.position + transform.rotation * new Vector3(0, 0, headsetLocally.z);
        return result;

    }

}