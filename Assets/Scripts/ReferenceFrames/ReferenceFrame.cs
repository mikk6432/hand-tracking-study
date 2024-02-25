using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

public class ReferenceFrame : MonoBehaviour
{
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

    private void Start()
    {
        if (locallyPositionedTo == null)
        {
            locallyPositionedTo = new GameObject();
        }
        if (positionReference.x == null)
        {
            positionReference.x = locallyPositionedTo;

        }
        if (positionReference.y == null)
        {
            positionReference.y = locallyPositionedTo;
        }
        if (positionReference.z == null)
        {
            positionReference.z = locallyPositionedTo;
        }
        if (rotationReference.x == null)
        {
            rotationReference.x = new GameObject();

        }
        if (rotationReference.y == null)
        {
            rotationReference.y = new GameObject();
        }
        if (rotationReference.z == null)
        {
            rotationReference.z = new GameObject();
        }
        if (offsetReference.xReference == null)
        {
            offsetReference.xReference = new GameObject();
        }
        if (offsetReference.yReference == null)
        {
            offsetReference.yReference = new GameObject();
        }
        if (offsetReference.zReference == null)
        {
            offsetReference.zReference = new GameObject();
        }
    }

    private void Update()
    {
        transform.position = locallyPositionedTo.transform.position;
        transform.position += locallyPositionedTo.transform.rotation * new Vector3(
            locallyPositionedTo.transform.InverseTransformPoint(positionReference.x.transform.position).x,
            locallyPositionedTo.transform.InverseTransformPoint(positionReference.y.transform.position).y,
            locallyPositionedTo.transform.InverseTransformPoint(positionReference.z.transform.position).z
        );
        transform.rotation = Quaternion.Euler(
            rotationReference.x.transform.rotation.eulerAngles.x,
            rotationReference.y.transform.rotation.eulerAngles.y,
            rotationReference.z.transform.rotation.eulerAngles.z
        );

        transform.position += offsetReference.xReference.transform.rotation * new Vector3(offsetReference.xOffset, 0, 0);
        transform.position += offsetReference.yReference.transform.rotation * new Vector3(0, offsetReference.yOffset, 0);
        transform.position += offsetReference.zReference.transform.rotation * new Vector3(0, 0, offsetReference.zOffset);

        transform.rotation *= Quaternion.Euler(
            offsetRotation.x,
            offsetRotation.y,
            offsetRotation.z
        );
    }

}
