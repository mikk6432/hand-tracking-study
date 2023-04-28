using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class StandingTrainingManager : MonoBehaviour
{
    public UnityEvent<bool, int> selectionDone = new();

    [SerializeField] private TargetsManager targetsManager;
    [Space] [SerializeField] private Transform sceneLight;
    [Space] [SerializeField] private Transform headset;
    [Space] 
    [SerializeField] private GameObject palmRefFrame;
    [SerializeField] private GameObject handRefFrame;
    [SerializeField] private GameObject pathRefFrame;

    [SerializeField] private GameObject metronome;
    
    private StandingReferenceFrame currentRefFrame;

    private StandingReferenceFrame CurrentRefFrame
    {
        set
        {
            currentRefFrame = value;

            GameObject anchor;
            if (currentRefFrame == StandingReferenceFrame.PalmReferenced)
            {
                anchor = palmRefFrame;
                palmRefFrame.SetActive(true);
                handRefFrame.SetActive(false);
                pathRefFrame.SetActive(false);
            }
            else if (currentRefFrame == StandingReferenceFrame.HandReferenced)
            {
                anchor = handRefFrame;
                palmRefFrame.SetActive(false);
                handRefFrame.SetActive(true);
                pathRefFrame.SetActive(false);
            }
            else
            {
                anchor = pathRefFrame;
                palmRefFrame.SetActive(false);
                handRefFrame.SetActive(false);
                pathRefFrame.SetActive(true);
            }
            
            targetsManager.Anchor = anchor;
        }
    }

    private int selectionsDoneInTrial;

    public enum StandingReferenceFrame
    {
        PalmReferenced,
        HandReferenced, // position only
        PathReferenced
    }

    private void Start()
    {
        selectionsDoneInTrial = 0;
        CurrentRefFrame = StandingReferenceFrame.PalmReferenced;

        PlaceLightWhereHeadset();

        targetsManager.TargetSize = TargetsManager.TargetSizeVariant.Big;
        targetsManager.selectionDone.AddListener(OnSelectionDone);
        targetsManager.ShowTargets();
        targetsManager.ActivateFirstTarget();
    }

    private void PlaceLightWhereHeadset()
    {
        var headsetPosition = headset.position;
        var headsetForward = headset.forward;

        var position = new Vector3(headsetPosition.x, 0, headsetPosition.y);

        var rotation = Quaternion.LookRotation(
            new Vector3(headsetForward.x, 0, headsetForward.z)
        );
        
        sceneLight.SetPositionAndRotation(position, rotation);
    }

    private void IncrementRefFrame()
    {
        if (currentRefFrame == StandingReferenceFrame.PalmReferenced)
            CurrentRefFrame = StandingReferenceFrame.HandReferenced;
        else if (currentRefFrame == StandingReferenceFrame.HandReferenced)
            CurrentRefFrame = StandingReferenceFrame.PathReferenced;
        else CurrentRefFrame = StandingReferenceFrame.PalmReferenced;
    }

    private void OnSelectionDone(TargetsManager.SelectionDonePayload payload)
    {
        selectionsDoneInTrial++;

        if (selectionsDoneInTrial == 7)
        {
            selectionsDoneInTrial = 0;
            targetsManager.HideTargets();
            IncrementRefFrame();
            targetsManager.ShowTargets();
            targetsManager.ActivateFirstTarget();
        }
        
        metronome.SetActive(!metronome.activeSelf);

        selectionDone.Invoke(payload.success, payload.activeTargetIndex);
    }
}