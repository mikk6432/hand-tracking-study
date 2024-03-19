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

    public void UpdateReferenceFrame(Transform newPosition)
    {
        var temp = new GameObject("temp");
        temp.transform.position = new Vector3(0, 0, 0);
        temp.transform.rotation = locallyPositionedTo.transform.rotation;
        if (offsetReference.xReference != null)
        {
            var distance = newPosition.position - (positionReference.x ?? locallyPositionedTo).transform.position;
            offsetReference.xOffset = temp.transform.InverseTransformPoint(distance).x;
        }
        if (offsetReference.yReference != null)
        {
            var distance = newPosition.position - (positionReference.y ?? locallyPositionedTo).transform.position;
            offsetReference.yOffset = temp.transform.InverseTransformPoint(distance).y;
        }
        if (offsetReference.zReference != null)
        {
            var distance = newPosition.position - (positionReference.z ?? locallyPositionedTo).transform.position;
            offsetReference.zOffset = temp.transform.InverseTransformPoint(distance).z;
        }
        var rotation = Quaternion.Euler(
            (rotationReference.x ?? new GameObject()).transform.rotation.eulerAngles.x,
            (rotationReference.y ?? new GameObject()).transform.rotation.eulerAngles.y,
            (rotationReference.z ?? new GameObject()).transform.rotation.eulerAngles.z
        );
        offsetRotation = (Quaternion.Inverse(rotation) * newPosition.rotation).eulerAngles;
        Debug.Log("Updated reference frame");
        Debug.Log("X: " + offsetReference.xOffset);
        Debug.Log("Y: " + offsetReference.yOffset);
        Debug.Log("Z: " + offsetReference.zOffset);
    }

}
