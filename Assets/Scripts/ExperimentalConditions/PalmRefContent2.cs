using UnityEngine;

namespace HandInteractionsOnTheGo
{
    public class PalmRefContent2 : MonoBehaviour
    {
        [SerializeField] [Tooltip("The game object representing the palm")]
        private Transform palmCenter;

        [SerializeField] [Tooltip("The game object representing the head")]
        private Transform head;

        [SerializeField] [Tooltip("Vertical offset of the UI from palm center")]
        private float yOffset = .15f;


        [SerializeField] [Tooltip("X rotation of the UI in degrees")]
        private float xRotation = 15f;

        protected virtual void OnEnable()
        {
            if (!palmCenter)
            {
                Debug.LogError(
                    $"{nameof(PalmRefContent2)}: the '{nameof(palmCenter)}' object is not set. Disabling the script.");
                enabled = false;
                return;
            }

            if (!head)
            {
                Debug.LogError(
                    $"{nameof(PalmRefContent2)}: the '{nameof(head)}' object is not set. Disabling the script.");
                enabled = false;
                return;
            }

            Update();
        }

        protected void Update()
        {
            CalculatePositionAndRotation(out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        protected virtual void CalculatePositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            var headPalmVector = palmCenter.position - head.position;

            var oxzProjection = new Vector3(headPalmVector.x, 0, headPalmVector.z);
            // normalize it
            oxzProjection = Vector3.Normalize(oxzProjection);

            // axis of rotating our oxzProjection vector
            var axis = new Vector3(oxzProjection.z, 0, -oxzProjection.x);

            var forward = Quaternion.AngleAxis(xRotation, axis) * oxzProjection;
            var upwards = Quaternion.AngleAxis(xRotation - 90, axis) * oxzProjection;

            rotation = Quaternion.LookRotation(forward, upwards);
            // ReSharper disable once Unity.InefficientPropertyAccess
            position = palmCenter.position + Vector3.up * yOffset;
        }
    }
}