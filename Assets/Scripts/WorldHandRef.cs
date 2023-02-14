using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldHandRef: MonoBehaviour
{
    [SerializeField] [Tooltip("The game object representing the hand")]
    protected Transform _handCenter;

    [SerializeField] [Tooltip("The content appears beside the pinky finger of the used hand. Default is left hand")]
    protected bool rightHandMode = false;
    
    [SerializeField] [Tooltip("The distance from center of hand to object")]
    protected float offset = .15f;

    protected virtual void OnEnable()
    {
        if (!_handCenter) 
        {
            Debug.LogError($"{nameof(WorldHandRef)}: the '{nameof(_handCenter)}' object is not set. Disabling the script.");
            enabled = false;
            return;
        }
        
        Update();
    }

    protected void Update()
    {
        CalculatePositionAndRotation(out Vector3 position, out Quaternion rotation);
        transform.SetPositionAndRotation(position, rotation);
    }

    protected virtual void CalculatePositionAndRotation(out Vector3 position, out Quaternion rotation)
    {
        rotation = _handCenter.rotation;
        
        var offsetVector = _handCenter.right * offset;
        if (rightHandMode) offsetVector *= -1; // invert if needed
        
        position = _handCenter.position + offsetVector;
    }
}
