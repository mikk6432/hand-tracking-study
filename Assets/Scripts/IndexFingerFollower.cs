using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IndexFingerFollower : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Skeleton to take hand transform data from")]
    private OVRSkeleton _skeleton;

    [SerializeField]
    // [Tooltip("Skeleton to take hand transform data from")]
    private GameObject tipPrefab;
    
    private OVRBone _indexBone;
    private GameObject _tip;

    private void OnEnable()
    {
        // if (!_skeleton) enabled = false;
    }

    private void Awake()
    {
        // _indexBone = _skeleton.Bones.First(bone => bone.Id == OVRSkeleton.BoneId.Hand_Index3);
        // _indexBone = _skeleton.Bones[(int)OVRSkeleton.BoneId.Hand_IndexTip];
    }

    // Start is called before the first frame update
    void Start()
    {
        if (!_skeleton)
        {
            Debug.Log("No skeleton");
            return;
        }

        _indexBone = _skeleton.Bones?.FirstOrDefault(bone => bone.Id == OVRSkeleton.BoneId.Hand_IndexTip);
        if (_indexBone == null)
        {
            Debug.Log("No index bone");
            return;
        }
        _tip = Instantiate(tipPrefab, _indexBone.Transform);
    }

    // Update is called once per frame
    void Update()
    {
        if(!_tip) return;
        _tip.transform.position = _indexBone.Transform.position;
        _tip.transform.rotation = _indexBone.Transform.rotation;
    }
}
