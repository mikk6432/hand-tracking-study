using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Math = Utils.Math;

public class Dbag : MonoBehaviour
{
    [SerializeField] private TargetsManager tm;

    private void Start()
    {
        tm.TargetSize = TargetsManager.TargetSizeVariant.VeryBig;
        tm.ShowTargets();
    }
}