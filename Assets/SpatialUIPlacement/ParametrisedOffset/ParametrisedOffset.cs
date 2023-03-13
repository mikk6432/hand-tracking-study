using UnityEngine;

namespace SpatialUIPlacement
{
    [ExecuteAlways]
    [RequireComponent(typeof(ReferenceFrame))]
    public abstract class ParametrisedOffset : MonoBehaviour
    {
        protected ReferenceFrame _referenceFrame;

        public abstract void UpdatePositionAndRotation(ref ReferenceFrame.Pose referenceFrame, ref ReferenceFrame.Pose offset, Transform dummyTransform);

        protected virtual void Awake() => _referenceFrame = GetComponent<ReferenceFrame>();

        protected virtual void Reset() => _referenceFrame.AddComponent(this);

        protected virtual void OnDestroy() => _referenceFrame.RemoveComponent(this);
    }
}
