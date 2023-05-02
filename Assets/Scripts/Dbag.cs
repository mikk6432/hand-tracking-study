using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Math = Utils.Math;

public class Dbag : MonoBehaviour
{
    [SerializeField] private TargetsManagerV2 targetsManager;

    [SerializeField] private Button showButton;
    [SerializeField] private Button hideButton;
    [SerializeField] private Button activateNextButton;

    private IEnumerator<TargetsManagerV2.TargetSizeVariant> sizesSequence;
    private IEnumerator<int> targetsSequence;

    private void Awake()
    {
        IEnumerator<TargetsManagerV2.TargetSizeVariant> aa()
        {
            yield return TargetsManagerV2.TargetSizeVariant.Big;
            yield return TargetsManagerV2.TargetSizeVariant.Medium;
            yield return TargetsManagerV2.TargetSizeVariant.Small;
        }

        sizesSequence = aa();
        targetsSequence = Math.FittsLaw(7).Take(8).GetEnumerator();
    }

    private void Start()
    {
        targetsSequence.MoveNext();
        targetsManager.TargetSize = sizesSequence.Current;
        showButton.onClick.AddListener(() =>
        {
           targetsManager.ShowTargets(); 
        });
        hideButton.onClick.AddListener(() =>
        {
            targetsManager.HideTargets(); 
        });
        activateNextButton.onClick.AddListener(() =>
        {
            if (targetsSequence.MoveNext())
            {
                targetsManager.ActivateTarget(targetsSequence.Current);
            }
            else
            {
                if (sizesSequence.MoveNext())
                {
                    targetsManager.TargetSize = sizesSequence.Current;
                    targetsSequence = Math.FittsLaw(7).Take(8).GetEnumerator();
                    targetsSequence.MoveNext();
                    targetsManager.ActivateTarget(targetsSequence.Current);
                }
            }
        });
        
        targetsManager.selectorEnteredTargetsZone.AddListener(() =>
        {
            Debug.Log($"Entered with {targetsManager.LastSelectionData.success.ToString().ToUpper()}");
        });
        targetsManager.selectorExitedTargetsZone.AddListener(() =>
        {
            Debug.Log("Exited");
        });
    }
}