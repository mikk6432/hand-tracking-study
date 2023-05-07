using System;
using System.Collections.Generic;
using UnityEngine;
using Math = Utils.Math;

public class SelectorAnimatedProjector: MonoBehaviour
{
    private static Vector3 targetsColliderScale = new(.22f, .22f, .06f);
    private static Color insideColor = Color.black;
    private static Color outsideColor = Color.white;
    
    private GameObject colliderVisualizer;
    private List<Renderer> bordersRenderers;
    private Transform projection;
    private Renderer projectionRenderer;

    private (bool insideCollider, float distanceToOXY) prevIsInside;
    
    public Transform Selector;

    private void OnEnable()
    {
        if (!Selector) enabled = false;
        
        colliderVisualizer.SetActive(true);
        projectionRenderer.gameObject.SetActive(true);

        prevIsInside = IsSelectorInside();
        ApplyColor(prevIsInside.insideCollider);
        ApplyDistance(prevIsInside.distanceToOXY);
        ApplyPosition();
    }

    private void OnDisable()
    {
        colliderVisualizer.SetActive(false);
        projectionRenderer.gameObject.SetActive(false);
        prevIsInside = default(ValueTuple<bool, float>);
    }

    private void Awake()
    {
        colliderVisualizer = transform.Find("ColliderVisualizer").gameObject;
        bordersRenderers = new List<Renderer>(12);
        foreach (Transform border in colliderVisualizer.transform)
            bordersRenderers.Add(border.GetComponent<Renderer>());

        projection = transform.Find("SelectorProjection");
        projectionRenderer = projection.Find("Cylinder").GetComponent<Renderer>();
    }

    private void Update()
    {
        if (!Selector) return;

        var isInside = IsSelectorInside();
        
        if (isInside.insideCollider != prevIsInside.insideCollider) 
            ApplyColor(isInside.insideCollider);
        
        ApplyDistance(isInside.distanceToOXY);
        ApplyPosition();

        prevIsInside = isInside;
    }

    private (bool insideCollider, float distanceToOXY) IsSelectorInside()
    {
        var selectorPosition = Selector.position;
        var (world, local) = Math.ProjectPointOntoOXYPlane(transform, selectorPosition);

        var fromSelectorToProjection = selectorPosition - world;
        var distanceToOXY = fromSelectorToProjection.magnitude;

        var inside = Mathf.Abs(local.x) < targetsColliderScale.x / 2 &&
                     Mathf.Abs(local.y) < targetsColliderScale.y / 2 &&
                     Vector3.Dot(transform.forward, fromSelectorToProjection) >= 0 &&
                     distanceToOXY <= targetsColliderScale.z;

        return (inside, distanceToOXY);
    }

    private void ApplyColor(bool inside)
    {
        var color = inside ? insideColor : outsideColor;
        projectionRenderer.material.color = color;
        bordersRenderers.ForEach(r => r.material.color = color);
    }

    private const float minBorder = .01f;
    private const float maxBorder = .25f;
    private const float minSize = .005f;
    private const float maxSize = .02f;
    private static Func<float, float> interpolate = 
        distance => 
            minSize + 
            (maxSize - minSize) * (distance - minBorder) / (maxBorder - minBorder);
    private void ApplyDistance(float distance)
    {
        float size = distance < minBorder ? minSize :
            distance > maxBorder ? maxSize :
            interpolate(distance);
        projection.localScale = new Vector3(size, size, 0.0001f);
    }

    private void ApplyPosition()
    {
        var (_, local) = Math.ProjectPointOntoOXYPlane(transform, Selector.position);
        projection.localPosition = new Vector3(local.x, local.y, -0.001f);
    }
}
