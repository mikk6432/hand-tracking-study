using UnityEngine;

namespace HandInteractionsOnTheGo
{
    public class PalmRefContent: MonoBehaviour
    {
        [SerializeField] [Tooltip("The game object representing the palm")]
        protected Transform palmCenter;

        [SerializeField] [Tooltip("The content appears beside the pinky finger of the used hand. Default is left hand")]
        protected bool rightHandMode;
    
        [SerializeField] [Tooltip("Offset from palm center. If rightHandMode=true, than X coord is inverted")]
        protected Vector3 offset = new(.15f, 0, 0);

        protected virtual void OnEnable()
        {
            if (!palmCenter) 
            {
                Debug.LogError($"{nameof(PalmRefContent)}: the '{nameof(palmCenter)}' object is not set. Disabling the script.");
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
            rotation = palmCenter.rotation;

            var offsetVector = new Vector3(
                rightHandMode ? -offset.x : offset.x,
                offset.y,
                offset.z
            );
            
            position = palmCenter.position + offsetVector;
        }
    }
}