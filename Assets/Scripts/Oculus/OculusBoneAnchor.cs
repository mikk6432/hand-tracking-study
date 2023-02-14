using System;
using System.Linq;
using UnityEngine;

public class OculusBoneAnchor: MonoBehaviour
{
    [SerializeField] private OVRSkeleton.BoneId boneId;
    [SerializeField] private OVRSkeleton ovrSkeleton;
    
    private OVRBone _bone;

    private void OnEnable()
    {
        if (boneId == OVRSkeleton.BoneId.Invalid)
        {
            Debug.LogError($"{nameof(OculusBoneAnchor)}: the '{nameof(boneId)}' object is not set. Disabling the script.");
            enabled = false;
            return;
        }
        if (!ovrSkeleton)
        {
            Debug.LogError($"{nameof(OculusBoneAnchor)}: the '{nameof(ovrSkeleton)}' object is not set. Disabling the script.");
            enabled = false;
            return;
        }

        Update();
    }

    private void Update()
    {
        if (_bone == null) _bone = ovrSkeleton.Bones.FirstOrDefault(bone => bone.Id == boneId);
        
        transform.SetPositionAndRotation(_bone.Transform.position, _bone.Transform.rotation);
    }
}
