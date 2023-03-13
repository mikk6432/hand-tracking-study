using UnityEngine;

namespace SpatialUIPlacement
{
    public class FacingAnAnchor : ParametrisedOffset
    {
        private enum _
        {
            ObjectItself,
            ReferenceFrame
        }

        [SerializeField]
        private Transform _anchor;

        [SerializeField]
        private _ _rotate = _.ObjectItself;

        [Header("Rotate Around")]
        [SerializeField]
        private bool _xAxis = true;
        [SerializeField]
        private bool _yAxis = true;

        private void Start()
        {
            // Sanity check
            if (Application.isPlaying && _anchor == null)
            {
                Debug.LogError($"{nameof(FacingAnAnchor)}: The '{nameof(_anchor)}' field cannot be left unassigned. Disabling the script");
                enabled = false;
                return;
            }
        }

        public override void UpdatePositionAndRotation(ref ReferenceFrame.Pose referenceFrame, ref ReferenceFrame.Pose offset, Transform dummyTransform)
        {
            if (_anchor == null) return;

            if (_rotate == _.ObjectItself)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - _anchor.position);
                if (dummyTransform == transform) // Temporary fix until I fugure out what's wrong with when the object looses the parent refernce frame obj
                    transform.rotation = Quaternion.Inverse(Quaternion.Euler(referenceFrame.rotation)) * transform.rotation;
                //Debug.DrawRay(transform.position,-transform.forward, Color.magenta);

                //Vector3 euler = _referenceFrame.CoordinateSystem == ReferenceFrame._.LocalCoordinateSystem ? ReferenceFrame.GetLocalEulerAngles(transform) : transform.rotation.eulerAngles; // This one has an effect on the case when there is no ref frame obj. It makes it work. However, it breaks the regular Editor one, when there is such object
                Vector3 euler = ReferenceFrame.GetLocalEulerAngles(transform);
                if (_xAxis) offset.rotation.x = euler.x;
                if (_yAxis) offset.rotation.y = euler.y;

                //Debug.DrawRay(transform.position, Quaternion.Euler(euler) * -Vector3.forward, Color.yellow);
            }
            else
            {
                dummyTransform.position = referenceFrame.position;
                dummyTransform.rotation = Quaternion.LookRotation(dummyTransform.position - _anchor.position);

                Vector3 euler = _referenceFrame.CoordinateSystem == ReferenceFrame._.LocalCoordinateSystem ? ReferenceFrame.GetLocalEulerAngles(dummyTransform) : dummyTransform.rotation.eulerAngles;
                if (_xAxis) referenceFrame.rotation.x = euler.x;
                if (_yAxis) referenceFrame.rotation.y = euler.y;
            }
        }
    }
}
