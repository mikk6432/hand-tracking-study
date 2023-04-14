
using UnityEngine;

namespace HandInteractionsOnTheGo
{
    public class AttachPoseWithOffset: MonoBehaviour
    {
        [SerializeField] [Tooltip("The game object to attach this object to")]
        private Transform attachTo;

        [SerializeField] private Vector3 offsetPosition;
        [SerializeField] private Quaternion offsetRotation;

        private void OnEnable()
        {
            if (!attachTo) 
            {
                Debug.LogError($"{nameof(AttachPoseWithOffset)}: the '{nameof(attachTo)}' object is not set. Disabling the script.");
                enabled = false;
                return;
            }
        
            Update();
        }

        protected void Update()
        {
            transform.SetPositionAndRotation(attachTo.position + offsetPosition, attachTo.rotation * offsetRotation);
        }
    }
}