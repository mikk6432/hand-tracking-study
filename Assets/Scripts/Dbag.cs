using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dbag : MonoBehaviour
{
    private static float delay = 1f;

    private void Start()
    {
        IEnumerator<TargetsManager.TargetSizeVariant> TrainingSequence()
        {
            while (true)
            {
                yield return TargetsManager.TargetSizeVariant.Big;
                yield return TargetsManager.TargetSizeVariant.Medium;
            }
        }

        var seq = TrainingSequence();

        int i = 0;
        while (i < 7)
        {
            if (seq.MoveNext())
            {
                Debug.Log($"$Next: {seq.Current}");
            }
            else
            {
                Debug.Log("no");
            }
            i++;
        }
        Debug.Log("finished");
    }
}