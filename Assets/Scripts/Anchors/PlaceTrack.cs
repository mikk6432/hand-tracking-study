using System;
using UnityEngine;
using UnityEngine.Events;

public class PlaceTrack : MonoBehaviour
{
    [SerializeField] private GameObject headset;
    [SerializeField] private GameObject sceneLight1;
    [SerializeField] private GameObject sceneLight2;
    [SerializeField] private float lightAngle = 70f;
    [SerializeField] private float trackDistanceAboveGround = 0.01f;
    private void Update()
    {
        /*         if (OVRInput.Get(OVRInput.RawButton.Y))
                {
                    PlaceTrackAndLightsForwardFromHeadset();
                } */
    }

    public void PlaceTrackAndLightsForwardFromHeadset()
    {
        GameObject floor = null;
        RaycastHit[] hits = Physics.RaycastAll(headset.transform.position, Vector3.down);
        if (hits.Length > 0)
        {
            Debug.Log($"Raycast hit {hits.Length} objects:");
            foreach (RaycastHit hit in hits)
            {
                Debug.Log("Hit object: " + hit.collider.gameObject.name);
                if (hit.collider.gameObject.name == "Quad")
                {
                    floor = hit.collider.gameObject;
                }
            }
        }
        else
        {
            Debug.Log("No objects hit by the raycast.");
        }
        var (position, rotation) = HeadsetOXZProjection();
        var floorHeight = 0f;
        if (floor != null) {
            gameObject.transform.parent = floor.transform;
            floorHeight = floor.transform.position.y;
        }
        // Set track as child to floor
        float halfTrackLength = GetComponent<WalkingStateTrigger>().halfTrackLength;
        position += rotation * new Vector3(0, floorHeight + trackDistanceAboveGround, halfTrackLength + .3f); // half track length and small offset more 
        transform.SetPositionAndRotation(position, rotation);

        // Set one light to point in headset direction. Directional light positions does not matter
        Quaternion newRotation = Quaternion.Euler(lightAngle, rotation.eulerAngles.y, rotation.eulerAngles.z);
        sceneLight1.transform.SetPositionAndRotation(position, newRotation);

        // Set second light to point in opposite direction. Directional light positions does not matter
        Quaternion flippedRotation = Quaternion.Euler(lightAngle, rotation.eulerAngles.y + 180, rotation.eulerAngles.z);
        sceneLight2.transform.SetPositionAndRotation(position, flippedRotation);
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