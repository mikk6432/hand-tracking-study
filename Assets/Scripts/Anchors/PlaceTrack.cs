using System;
using UnityEngine;
using UnityEngine.Events;

public class PlaceTrack : MonoBehaviour
{
    [SerializeField] private GameObject headset;
    private void Update()
    {
        if (OVRInput.Get(OVRInput.RawButton.Y))
        {
            PlaceTrackForwardFromHeadset();
        }
    }

    public void PlaceTrackForwardFromHeadset()
    {
        var (position, rotation) = HeadsetOXZProjection();
        float halfTrackLength = 2.75f;
        position += rotation * new Vector3(0, 0, halfTrackLength + .3f); // half track length and small offset more 
        transform.SetPositionAndRotation(position, rotation);
    }

    private (Vector3 position, Quaternion rotation) HeadsetOXZProjection()
    {
        var headsetTransform = headset.transform;
        var headsetPosition = headsetTransform.position;
        var position = new Vector3(headsetPosition.x, 0, headsetPosition.z);

        var headsetForward = headsetTransform.forward;
        var rotation = Quaternion.LookRotation(new Vector3(headsetForward.x, 0, headsetForward.z));
        return (position, rotation);
    }

}