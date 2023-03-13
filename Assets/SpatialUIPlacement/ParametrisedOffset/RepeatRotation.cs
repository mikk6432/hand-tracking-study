using UnityEngine;

namespace SpatialUIPlacement
{
    public class RepeatRotation : ParametrisedOffset
    {
        [SerializeField]
        private Transform _anchor;

        [Header("Repeat Rotation Around")]
        [SerializeField]
        private bool _xAxis = true;
        [SerializeField]
        private bool _yAxis = true;
        [SerializeField]
        private bool _zAxis = true;

        private void Start()
        {
            // Sanity check
            if (Application.isPlaying && _anchor == null)
            {
                Debug.LogError($"{nameof(RepeatRotation)}: The '{nameof(_anchor)}' field cannot be left unassigned. Disabling the script");
                enabled = false;
                return;
            }
        }

        public override void UpdatePositionAndRotation(ref ReferenceFrame.Pose referenceFrame, ref ReferenceFrame.Pose offset, Transform dummyTransform)
        {
            if (_anchor == null) return;

            Vector3 euler;
            if (_referenceFrame.CoordinateSystem == ReferenceFrame._.LocalCoordinateSystem)
            {
                dummyTransform.rotation = _anchor.rotation;
                euler = ReferenceFrame.GetLocalEulerAngles(dummyTransform);
            }
            else
                euler = _anchor.rotation.eulerAngles;

            if (_xAxis) offset.rotation.x = euler.x;
            if (_yAxis) offset.rotation.y = euler.y;
            if (_zAxis) offset.rotation.z = euler.z;
        }
    }
}
