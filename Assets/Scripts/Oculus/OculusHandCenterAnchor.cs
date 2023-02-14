using System;
using System.Collections;
using System.Linq;
using UnityEngine;

public class OculusHandCenterAnchor: MonoBehaviour
{
    [SerializeField] private OVRSkeleton ovrSkeleton;
    
    private OVRBone _bone;

    private static Quaternion rotateFactor;

    static OculusHandCenterAnchor()
    {
        rotateFactor = Quaternion.AngleAxis(180, Vector3.right) *
                       Quaternion.AngleAxis(90, Vector3.up) *
                       Quaternion.AngleAxis(-90, Vector3.right);
    }

    private void OnEnable()
    {
        if (!ovrSkeleton)
        {
            Debug.LogError($"{nameof(OculusHandCenterAnchor)}: the '{nameof(ovrSkeleton)}' object is not set. Disabling the script.");
            enabled = false;
            return;
        }

        Update();
    }

    private void Update()
    {
        if (_bone == null) _bone = ovrSkeleton.Bones.FirstOrDefault(bone => bone.Id == OVRSkeleton.BoneId.Hand_WristRoot);
        
        transform.position = _bone.Transform.position;
        transform.rotation = _bone.Transform.rotation;
        transform.localRotation *= rotateFactor;
    }
}