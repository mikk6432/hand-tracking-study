using UnityEngine;

namespace HandInteractionsOnTheGo
{
    public class PoseAttacher: MonoBehaviour
    {
        [SerializeField] [Tooltip("The game object to attach this object to")]
        private Transform attachTo;

        private void OnEnable()
        {
            if (!attachTo) 
            {
                Debug.LogError($"{nameof(PoseAttacher)}: the '{nameof(attachTo)}' object is not set. Disabling the script.");
                enabled = false;
                return;
            }

            Update();
        }

        protected void Update()
        {
            transform.SetPositionAndRotation(attachTo.position, attachTo.rotation);
        }
    }
}