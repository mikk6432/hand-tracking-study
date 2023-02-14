using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class SelectorCollisionProjection : MonoBehaviour
{
    // args – X & Y coordinates of the projection of collision onto local OXY plane 
    public readonly UnityEvent<Vector2> triggerEnterEvent = new();
    public readonly UnityEvent triggerExitEvent = new();

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("Selector")) return;
        
        var plane = new Plane(transform.forward, transform.position);

        var projection = plane.ClosestPointOnPlane(other.transform.position);
 
        // now, solve equation:  projection = transform.position + (x * transform.right) + (y * transform.up) 
        // in terms of X and Y
        var localPosition = transform.InverseTransformPoint(projection);
        var localPlaneVector = new Vector2(localPosition.x, localPosition.y);

        triggerEnterEvent.Invoke(localPlaneVector);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.gameObject.CompareTag("Selector")) return;
        
        triggerExitEvent.Invoke();
    }
}