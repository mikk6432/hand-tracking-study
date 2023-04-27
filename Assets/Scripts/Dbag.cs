using System;
using UnityEngine;
using UnityEngine.UI;

public class Dbag : MonoBehaviour
{
    // walking state trigger
    /*[SerializeField] private Button button;
    [SerializeField] private WalkingStateTrigger wst;
    private bool on = false;

    private void Start()
    {
        button.onClick.AddListener(() =>
        {
            wst.enabled = !on;
            on = !on;
        });
    }*/


    [SerializeField] private Button showButton;
    [SerializeField] private Button hideButton;
    [SerializeField] private Button activateFirstButton;
    [SerializeField] private TargetsManager targetsManager;
    [SerializeField] private GameObject anchor;
    [SerializeField] private GameObject poseDebug;

    private void Start()
    {
        showButton.onClick.AddListener(targetsManager.ShowTargets);
        hideButton.onClick.AddListener(targetsManager.HideTargets);
        activateFirstButton.onClick.AddListener(targetsManager.ActivateFirstTarget);

        targetsManager.TargetSize = TargetsManager.TargetSizeVariant.Small;
        targetsManager.Anchor = anchor;
        
        targetsManager.selectionDone.AddListener(payload =>
        {
            Debug.Log($"\nsuccess = {payload.success}\n" +
                      $"activeTargetIndex = {payload.activeTargetIndex}\n" +
                      $"targetSize = {payload.targetSize}\n" +
                      $"targetAbsoluteCoordinates = {payload.targetAbsoluteCoordinates}\n" +
                      $"selectionAbsoluteCoordinate = {payload.selectionAbsoluteCoordinates}\n" +
                      $"selectionLocalCoordinates = {payload.selectionLocalCoordinates}\n");
        });
    }

    // active target
    /*private void Update()
    {
        var activeTarget = targetsManager.ActiveTarget;
        if (activeTarget.target && activeTarget.target.activeInHierarchy)
        {
            poseDebug.SetActive(true);
            poseDebug.transform.SetPositionAndRotation(activeTarget.target.transform.position, activeTarget.target.transform.rotation);
        }
        else
        {
            poseDebug.SetActive(false);
        }
    }*/
}