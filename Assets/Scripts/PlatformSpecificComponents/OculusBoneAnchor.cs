using System;
using System.Collections;
using System.Linq;
using UnityEngine;

namespace HandInteractionsOnTheGo
{
    public class OculusBoneAnchor : MonoBehaviour
    {
        [SerializeField] private OVRSkeleton ovrSkeleton;
        [SerializeField] private OVRSkeleton.BoneId boneId;
        [SerializeField] private Vector3 offsetPosition;

        private OVRBone _bone;

        // to make palm look at us with fingers up
        // private static readonly Quaternion OculusRotateFactor = Quaternion.LookRotation(Vector3.up, Vector3.left);
        private static readonly Quaternion OculusRotateFactor =
                // Quaternion.identity
            // Quaternion.Inverse(
            Quaternion.LookRotation(Vector3.down, Vector3.left)
        // )
            ;

        private void OnEnable()
        {
            if (boneId == OVRSkeleton.BoneId.Invalid)
            {
                Debug.LogError(
                    $"{nameof(OculusBoneAnchor)}: the '{nameof(boneId)}' object is not set. Disabling the script.");
                enabled = false;
                return;
            }

            if (!ovrSkeleton)
            {
                Debug.LogError(
                    $"{nameof(OculusBoneAnchor)}: the '{nameof(ovrSkeleton)}' object is not set. Disabling the script.");
                enabled = false;
                return;
            }

            Update();
        }

        private void Update()
        {
            if (_bone == null) _bone = ovrSkeleton.Bones.FirstOrDefault(bone => bone.Id == boneId);

            CalculatePositionAndRotation(out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        private void CalculatePositionAndRotation(out Vector3 position, out Quaternion rotation)
        {
            var boneRotation = _bone.Transform.rotation;

            rotation = boneRotation * OculusRotateFactor;
            position = rotation * offsetPosition + _bone.Transform.position;
            // rotation = OculusRotateFactor * boneRotation;
        }
    }
}