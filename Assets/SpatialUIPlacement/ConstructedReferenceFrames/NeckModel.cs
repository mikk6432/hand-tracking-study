using UnityEngine;

namespace SpatialUIPlacement
{
    [ExecuteAlways]
    public class NeckModel : MonoBehaviour
    {
        private static readonly Vector3 NECK_OFFSET = new Vector3(0.0f, 0.075f, 0.08f);

#pragma warning disable 649
        [SerializeField]
        private Transform _headset;
#pragma warning restore 649
        [SerializeField]
        private bool GoogleWay = false;

        private void Start()
        {
            if (Application.isPlaying && _headset == null && (_headset = Camera.main.transform) == null)
            {
                Debug.LogError($"{nameof(NeckModel)}: The '{nameof(_headset)}' field cannot be left unassigned. Disabling the script");
                enabled = false;
                return;
            }
        }

        private void Update()
        {
            if (_headset == null) return;
            
            if (GoogleWay)
                transform.position = ApplyInverseNeckModel(_headset.position, _headset.rotation);
            else
            {
                var rotatedNeckOffset = _headset.rotation * NECK_OFFSET;
                transform.position = _headset.position - rotatedNeckOffset;
            }

            var rotation = _headset.forward;
            rotation.y = 0f;
            transform.rotation = Quaternion.LookRotation(rotation.normalized);
        }

        private static Vector3 ApplyInverseNeckModel(Vector3 headPosition, Quaternion headRotation)
        {
            Vector3 rotatedNeckOffset =
                (headRotation * NECK_OFFSET) - (NECK_OFFSET.y * Vector3.up);
            headPosition -= rotatedNeckOffset;

            return headPosition;
        }
    }
}
