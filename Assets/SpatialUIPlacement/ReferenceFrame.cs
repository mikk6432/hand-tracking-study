using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;

namespace SpatialUIPlacement
{
    // [ExecuteAlways] // comment added by not so smart Artem
    public class ReferenceFrame : MonoBehaviour
    {
        private static readonly MethodInfo _getLocalEulerAnglesInfo = typeof(Transform).GetMethod("GetLocalEulerAngles", BindingFlags.Instance | BindingFlags.NonPublic); // Source of the solution: https://forum.unity.com/threads/solved-how-to-get-rotation-value-that-is-in-the-inspector.460310/#post-3333095
        private static readonly int RotationOrderZXY = 4; // Source: https://docs.unity3d.com/Manual/QuaternionAndEulerRotationsInUnity.html

        public enum _
        {
            GlobalCoordinateSystem,
            LocalCoordinateSystem
        }

        [System.Serializable]
        protected struct Anchor
        {
            [System.Serializable]
            public struct AnchorComponent
            {
                public Transform x;
                public Transform y;
                public Transform z;
            }

            public AnchorComponent position;
            public AnchorComponent rotation;
        }

        [System.Serializable]
        public struct Pose
        {
            public Vector3 position;
            public Vector3 rotation;
        }

#pragma warning disable 649
        [SerializeField, Tooltip("Influences how axes specified below are interpreted. E.g. we copy movement along global X or the X axis which belongs to the current object's parent (excluding the immediate parent, i.e. the reference frame object). If the current object is at the top of the object hierarchy, this parameter doesn't have any effect")]
        protected _ _axesRepresent;
        [SerializeField, Tooltip("Repeat the following components of the anchor(s). Leaving a component below empty means that the object is situated in the world, i.e. within the parent object or, if it's at the top of hierarchy, within the global coordinate system")]
        protected Anchor _anchoredTo;
        /*[SerializeField]
        protected bool _createReferenceFrameObjectInEditor = true;*/ // Thought having it may help with flexibility
#pragma warning restore 649
        protected Transform _refFrameTransform;

        [Space]
        [SerializeField, Tooltip("Represents the pose of the parent object, i.e. the reference frame object. Modify it here becuase the current component takes over the reference frame's Transform")]
        protected Pose _referenceFrame;
        [SerializeField]
        public Pose _offset;

        [Space]
        [SerializeField]
        protected List<ParametrisedOffset> _parametrisedOffsetComponents = new();

        public _ CoordinateSystem => _axesRepresent;

        public void AddComponent(ParametrisedOffset component)
        {
            if (!_parametrisedOffsetComponents.Contains(component))
                _parametrisedOffsetComponents.Add(component);
        }

        public void RemoveComponent(ParametrisedOffset component) => _parametrisedOffsetComponents.Remove(component);

        protected virtual void Start()
        {
            _refFrameTransform = FishOutRefFrameObj();

            if (Application.isPlaying) // Runtime: build or Play mode in Unity Editor
            {
                // If the reference frame object is present
                if (_refFrameTransform != null)
                {
                    // Store the pose of the reference frame
                    _referenceFrame.position = _axesRepresent == _.LocalCoordinateSystem ? _refFrameTransform.localPosition : _refFrameTransform.position;
                    _referenceFrame.rotation = _axesRepresent == _.LocalCoordinateSystem ? _refFrameTransform.localEulerAngles : _refFrameTransform.rotation.eulerAngles;

                    // Store the offset within it
                    //_offset.position = transform.localPosition;
                    //_offset.rotation = GetLocalEulerAngles(transform);

                    DestroyRefFrameObj();
                }
                else
                {
                    // Store initial position and rotation
                    _referenceFrame.position = _axesRepresent == _.LocalCoordinateSystem ? transform.localPosition : transform.position;
                    _referenceFrame.rotation = _axesRepresent == _.LocalCoordinateSystem ? transform.localEulerAngles : transform.rotation.eulerAngles;
                }

                _refFrameTransform = transform;
            }
            else // Editor mode
            {
                // comment added by not so smart Artem
                // If the reference frame object doesn't exist, create one
                /*if (_refFrameTransform == null)
                    _refFrameTransform = CreateRefFrameObj(transform);

                // Store initial position and rotation
                _referenceFrame.position = _axesRepresent == _.LocalCoordinateSystem ? _refFrameTransform.localPosition : _refFrameTransform.position;
                _referenceFrame.rotation = _axesRepresent == _.LocalCoordinateSystem ? _refFrameTransform.localEulerAngles : _refFrameTransform.rotation.eulerAngles;*/
            }
        }

        protected virtual void Update()
        {
            // Check whether the reference frame object was destroyed from outside of our code
            if (_refFrameTransform == null)
            {
                // Set the positional and angular offset to 0
                _offset.position = _offset.rotation = Vector3.zero;

                _refFrameTransform = transform;

                // Store position and rotation
                _referenceFrame.position = _axesRepresent == _.LocalCoordinateSystem ? _refFrameTransform.localPosition : _refFrameTransform.position;
                _referenceFrame.rotation = _axesRepresent == _.LocalCoordinateSystem ? _refFrameTransform.localEulerAngles : _refFrameTransform.rotation.eulerAngles;
            }

            if (_axesRepresent == _.GlobalCoordinateSystem || _refFrameTransform.parent == null)
            {
                // Dealing with position: put me at the same spot where the anchor is along the specified global axis
                _referenceFrame.position.x = _anchoredTo.position.x == null ? _referenceFrame.position.x : _anchoredTo.position.x.position.x;
                _referenceFrame.position.y = _anchoredTo.position.y == null ? _referenceFrame.position.y : _anchoredTo.position.y.position.y;
                _referenceFrame.position.z = _anchoredTo.position.z == null ? _referenceFrame.position.z : _anchoredTo.position.z.position.z;

                // Dealing with rotation: orient me the same way the anchor is rotated around the specified global axis
                _referenceFrame.rotation.x = _anchoredTo.rotation.x == null ? _referenceFrame.rotation.x : GetLocalEulerAngles(_anchoredTo.rotation.x).x;
                _referenceFrame.rotation.y = _anchoredTo.rotation.y == null ? _referenceFrame.rotation.y : GetLocalEulerAngles(_anchoredTo.rotation.y).y;
                _referenceFrame.rotation.z = _anchoredTo.rotation.z == null ? _referenceFrame.rotation.z : GetLocalEulerAngles(_anchoredTo.rotation.z).z;

                foreach (var c in _parametrisedOffsetComponents)
                    if (c.enabled) c.UpdatePositionAndRotation(ref _referenceFrame, ref _offset, _refFrameTransform);

                if (_refFrameTransform == transform) // There is no refernce frame object holding us
                {
                    _refFrameTransform.position = _referenceFrame.position + (Quaternion.Euler(_referenceFrame.rotation) * _offset.position);
                    _refFrameTransform.rotation = Quaternion.Euler(_referenceFrame.rotation) * Quaternion.Euler(_offset.rotation); ;
                }
                else
                {
                    _refFrameTransform.position = _referenceFrame.position;
                    _refFrameTransform.rotation = Quaternion.Euler(_referenceFrame.rotation);

                    transform.localPosition = _offset.position;
                    transform.localRotation = Quaternion.Euler(_offset.rotation);
                }
            }
            else
            {
                // Dealing with position: put me at the same spot where the anchor is along the specified local axis
                _referenceFrame.position.x =
                    // If the positional component X hasn't been set, just maintain the fixed position.
                    _anchoredTo.position.x == null ? _referenceFrame.position.x :
                    // If it has been, and the current object and the anchor live within the same parent (or are at the top of object hierarchy), just copy its local X coordinate.
                    // However, if they live in different coordinate systems, convert the anchor's global X coordinate to local coordinate of the current object's parent and copy it
                    (_anchoredTo.position.x.parent == _refFrameTransform.parent ? _anchoredTo.position.x.localPosition.x : _refFrameTransform.parent.InverseTransformPoint(_anchoredTo.position.x.position).x);
                _referenceFrame.position.y = _anchoredTo.position.y == null ? _referenceFrame.position.y :
                    (_anchoredTo.position.y.parent == _refFrameTransform.parent ? _anchoredTo.position.y.localPosition.y : _refFrameTransform.parent.InverseTransformPoint(_anchoredTo.position.y.position).y);
                _referenceFrame.position.z = _anchoredTo.position.z == null ? _referenceFrame.position.z :
                    (_anchoredTo.position.z.parent == _refFrameTransform.parent ? _anchoredTo.position.z.localPosition.z : _refFrameTransform.parent.InverseTransformPoint(_anchoredTo.position.z.position).z);

                // Dealing with rotation: orient me the same way the anchor is rotated around the specified local axis
                if (_anchoredTo.rotation.x != null && _anchoredTo.rotation.x.parent != _refFrameTransform.parent) _refFrameTransform.rotation = _anchoredTo.rotation.x.rotation;
                _referenceFrame.rotation.x = _anchoredTo.rotation.x == null ? _referenceFrame.rotation.x :
                    (_anchoredTo.rotation.x.parent == _refFrameTransform.parent ? GetLocalEulerAngles(_anchoredTo.rotation.x).x : GetLocalEulerAngles(_refFrameTransform).x);
                if (_anchoredTo.rotation.y != null && _anchoredTo.rotation.y.parent != _refFrameTransform.parent) _refFrameTransform.rotation = _anchoredTo.rotation.y.rotation;
                _referenceFrame.rotation.y = _anchoredTo.rotation.y == null ? _referenceFrame.rotation.y :
                    (_anchoredTo.rotation.y.parent == _refFrameTransform.parent ? GetLocalEulerAngles(_anchoredTo.rotation.y).y : GetLocalEulerAngles(_refFrameTransform).y);
                if (_anchoredTo.rotation.z != null && _anchoredTo.rotation.z.parent != _refFrameTransform.parent) _refFrameTransform.rotation = _anchoredTo.rotation.z.rotation;
                _referenceFrame.rotation.z = _anchoredTo.rotation.z == null ? _referenceFrame.rotation.z :
                    (_anchoredTo.rotation.z.parent == _refFrameTransform.parent ? GetLocalEulerAngles(_anchoredTo.rotation.z).z : GetLocalEulerAngles(_refFrameTransform).z);

                _refFrameTransform.localRotation = Quaternion.Euler(_referenceFrame.rotation); // Quaternion.identity;

                Pose parametrisedOffset = new();
                foreach (var c in _parametrisedOffsetComponents)
                    if (c.enabled) c.UpdatePositionAndRotation(ref _referenceFrame, ref parametrisedOffset, _refFrameTransform);

                if (_refFrameTransform == transform) // There is no refernce frame object holding us
                {
                    _refFrameTransform.localPosition = _referenceFrame.position + (Quaternion.Euler(_referenceFrame.rotation) * _offset.position);
                    _refFrameTransform.localRotation = Quaternion.Euler(_referenceFrame.rotation) * Quaternion.Euler(_offset.rotation + parametrisedOffset.rotation);
                }
                else
                {
                    _refFrameTransform.localPosition = _referenceFrame.position;
                    _refFrameTransform.localRotation = Quaternion.Euler(_referenceFrame.rotation);

                    transform.localPosition = _offset.position + parametrisedOffset.position;
                    transform.localRotation = Quaternion.Euler(_offset.rotation + parametrisedOffset.rotation);
                }
            }
        }
        
        protected static Transform CreateRefFrameObj(Transform transform)
        {
            // Instntiate the reference frame object and mark it with ReferenceFrameIndicator
            Transform referenceFrame = (new GameObject(transform.name + "ReferenceFrame")).transform;
            referenceFrame.AddComponent<ReferenceFrameIndicator>();

            // Put it where the current object is
            referenceFrame.SetPositionAndRotation(transform.position, transform.rotation);
            // Make the current object a child of the reference frame object while preserving the hierarchy of existing objects
            referenceFrame.parent = transform.parent;
            transform.parent = referenceFrame;

            return referenceFrame;
        }

        protected Transform FishOutRefFrameObj()
        {
            if (transform.parent?.GetComponent<ReferenceFrameIndicator>() == null)
                return null;

            return transform.parent;
        }

        protected void DestroyRefFrameObj()
        {
            // Free us from the reference frame object first
            transform.parent = _refFrameTransform.parent;

            DestroyImmediate(_refFrameTransform.gameObject);
            _refFrameTransform = null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Vector3 GetLocalEulerAngles(Transform t) => (Vector3)_getLocalEulerAnglesInfo.Invoke(t, new object[] { RotationOrderZXY });
    }
}
