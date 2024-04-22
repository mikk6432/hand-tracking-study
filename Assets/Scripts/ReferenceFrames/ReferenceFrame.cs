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

    private float Offset(Vector3 from, Vector3 to, Vector3 direction)
    {
        var distance = to - from;
        var distanceInDirection = Vector3.Dot(distance, direction);
        return distanceInDirection;
    }

    public void UpdateReferenceFrame(Transform newPosition)
    {
        if (offsetReference.xReference != null)
        {
            var anchor = (positionReference.x ?? locallyPositionedTo).transform.position;
            var referenceAxis = offsetReference.xReference.transform.right;
            offsetReference.xOffset = Offset(anchor, newPosition.position, referenceAxis);
        }
        if (offsetReference.yReference != null)
        {
            var anchor = (positionReference.y ?? locallyPositionedTo).transform.position;
            var referenceAxis = offsetReference.yReference.transform.up;
            offsetReference.yOffset = Offset(anchor, newPosition.position, referenceAxis);
        }
        if (offsetReference.zReference != null)
        {
            var anchor = (positionReference.z ?? locallyPositionedTo).transform.position;
            var referenceAxis = offsetReference.zReference.transform.forward;
            offsetReference.zOffset = Offset(anchor, newPosition.position, referenceAxis);
        }
        var rotation = Quaternion.Euler(
            (rotationReference.x ?? new GameObject()).transform.rotation.eulerAngles.x,
            (rotationReference.y ?? new GameObject()).transform.rotation.eulerAngles.y,
            (rotationReference.z ?? new GameObject()).transform.rotation.eulerAngles.z
        );
        offsetRotation = (Quaternion.Inverse(rotation) * newPosition.rotation).eulerAngles;
    }

}
