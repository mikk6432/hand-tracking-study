using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class ReferenceFrame : MonoBehaviour
{
    public ExperimentManager.ExperimentReferenceFrame referenceFrameName;
    [System.Serializable]
    public struct CustomVector3
    {
        public GameObject x;
        public GameObject y;
        public GameObject z;
    }
    [SerializeField] public GameObject locallyPositionedTo;
    [SerializeField] public CustomVector3 positionReference;
    [SerializeField] public CustomVector3 rotationReference;
    [System.Serializable]
    public struct OffsetReference
    {
        public GameObject xReference;
        public float xOffset;
        public GameObject yReference;
        public float yOffset;
        public GameObject zReference;
        public float zOffset;

    }
    [SerializeField] public OffsetReference offsetReference;
    [SerializeField] public Vector3 offsetRotation;

    private void Update()
    {
        transform.position = (locallyPositionedTo ?? new GameObject()).transform.position;
        transform.position += (locallyPositionedTo ?? new GameObject()).transform.rotation * new Vector3(
            (locallyPositionedTo ?? new GameObject()).transform.InverseTransformPoint((positionReference.x ?? locallyPositionedTo).transform.position).x,
            (locallyPositionedTo ?? new GameObject()).transform.InverseTransformPoint((positionReference.y ?? locallyPositionedTo).transform.position).y,
            (locallyPositionedTo ?? new GameObject()).transform.InverseTransformPoint((positionReference.z ?? locallyPositionedTo).transform.position).z
        );
        transform.rotation = Quaternion.Euler(
            (rotationReference.x ?? new GameObject()).transform.rotation.eulerAngles.x,
            (rotationReference.y ?? new GameObject()).transform.rotation.eulerAngles.y,
            (rotationReference.z ?? new GameObject()).transform.rotation.eulerAngles.z
        );

        transform.position += (offsetReference.xReference ?? new GameObject()).transform.rotation * new Vector3(offsetReference.xOffset, 0, 0);
        transform.position += (offsetReference.yReference ?? new GameObject()).transform.rotation * new Vector3(0, offsetReference.yOffset, 0);
        transform.position += (offsetReference.zReference ?? new GameObject()).transform.rotation * new Vector3(0, 0, offsetReference.zOffset);

        transform.rotation *= Quaternion.Euler(
            offsetRotation.x,
            offsetRotation.y,
            offsetRotation.z
        );
    }

    [SerializeField] private bool canUpdateReferenceFrame = false;
    public void UpdateReferenceFrame(Transform newPosition)
    {
        if (!canUpdateReferenceFrame)
        {
            return;
        }
        if (offsetReference.xReference != null)
        {
            var distance = newPosition.position - (positionReference.x ?? locallyPositionedTo).transform.position;
            var localCoord = offsetReference.xReference.transform;
            localCoord.position = new Vector3(0, 0, 0);
            offsetReference.xOffset = localCoord.InverseTransformPoint(distance).x;
        }
        if (offsetReference.yReference != null)
        {
            var distance = newPosition.position - (positionReference.y ?? locallyPositionedTo).transform.position;
            var localCoord = offsetReference.yReference.transform;
            localCoord.position = new Vector3(0, 0, 0);
            offsetReference.yOffset = localCoord.InverseTransformPoint(distance).y;
        }
        if (offsetReference.zReference != null)
        {
            var distance = newPosition.position - (positionReference.z ?? locallyPositionedTo).transform.position;
            var localCoord = offsetReference.zReference.transform;
            localCoord.position = new Vector3(0, 0, 0);
            offsetReference.zOffset = localCoord.InverseTransformPoint(distance).z;
        }
        Debug.Log("Updated reference frame");
        Debug.Log("X: " + offsetReference.xOffset);
        Debug.Log("Y: " + offsetReference.yOffset);
        Debug.Log("Z: " + offsetReference.zOffset);
    }

}
