using System.Linq;
using UnityEngine;

public class OculusBoneAnchor : MonoBehaviour
{
    [SerializeField] private OVRSkeleton ovrSkeleton;
    [SerializeField] private OVRSkeleton.BoneId boneId;
    [SerializeField] private Vector3 offsetPosition;
    [SerializeField] private OVRHand.Hand hand = OVRHand.Hand.HandLeft;

    private OVRBone _bone;

    // to make palm look at us with fingers up
    private static readonly Quaternion OculusLeftHandRotateFactor =
        Quaternion.LookRotation(Vector3.down, Vector3.left);
    private static readonly Quaternion OculusRightHandRotateFactor =
        Quaternion.LookRotation(Vector3.up, Vector3.right);

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
        if (_bone == null || !_bone.Transform.gameObject.activeInHierarchy) return;

        CalculatePositionAndRotation(out Vector3 position, out Quaternion rotation);
        // if (position.magnitude < 0.05f) return; // hand tracking working bad, hands are blinking and jumping to 0
        // upd: disabled previous line, because participant will not understand that his hands are blinking
        transform.SetPositionAndRotation(position, rotation);
    }

    private void CalculatePositionAndRotation(out Vector3 position, out Quaternion rotation)
    {
        var boneRotation = _bone.Transform.rotation;

        rotation = boneRotation *
                   (hand == OVRHand.Hand.HandLeft
                       ? OculusLeftHandRotateFactor
                       : OculusRightHandRotateFactor);
        position = rotation * offsetPosition + _bone.Transform.position;
    }
}
