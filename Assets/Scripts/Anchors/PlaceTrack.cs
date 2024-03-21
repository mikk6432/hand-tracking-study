using System;
using UnityEngine;
using UnityEngine.Events;

public class PlaceTrack : MonoBehaviour
{
    [SerializeField] private GameObject headset;
    [SerializeField] private float trackDistanceAboveGround = 0.01f;
    private void Update()
    {
        /*         if (OVRInput.Get(OVRInput.RawButton.Y))
                {
                    PlaceTrackForwardFromHeadset();
                } */
    }

    public void PlaceTrackForwardFromHeadset()
    {
        RaycastHit[] hits = Physics.RaycastAll(headset.transform.position, Vector3.down);
        if (hits.Length > 0)
        {
            Debug.Log($"Raycast hit {hits.Length} objects:");
            foreach (RaycastHit hit in hits)
            {
                Debug.Log("Hit object: " + hit.collider.gameObject.name);
                if (hit.collider.gameObject.name == "Quad")
                {
                    var floor = hit.collider.gameObject.transform;
                    var (position, rotation) = HeadsetOXZProjection();
                    float halfTrackLength = GetComponent<WalkingStateTrigger>().halfTrackLength;
                    position += rotation * new Vector3(0, floor.position.y + trackDistanceAboveGround, halfTrackLength + .3f); // half track length and small offset more 
                    transform.SetPositionAndRotation(position, rotation);
                }
            }
        }
        else
        {
            Debug.Log("No objects hit by the raycast.");
        }
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