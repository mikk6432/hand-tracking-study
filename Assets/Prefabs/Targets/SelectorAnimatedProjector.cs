using System;
using System.Collections.Generic;
using UnityEngine;
using Math = Utils.Math;

public class SelectorAnimatedProjector: MonoBehaviour
{
    private static Vector3 targetsColliderScale = new(.2f, .2f, .1f);

    [SerializeField] private LineRenderer projectionLineRenderer;
    // [SerializeField] private LineRenderer selectorLineRenderer;
    
    [SerializeField] private List<GameObject> borders = new(12);
    private Gradient gradient;

    public Transform Selector;

    private void OnEnable()
    {
        projectionLineRenderer.gameObject.SetActive(true);
        // selectorLineRenderer.gameObject.SetActive(true);
        borders.ForEach(b => b.SetActive(true));
        enabled = true;
        Update();
    }

    private void OnDisable()
    {
        projectionLineRenderer.gameObject.SetActive(false);
        // selectorLineRenderer.gameObject.SetActive(false);
        borders.ForEach(b => b.SetActive(false));
    }

    private void Awake()
    {
        DrawCircle(projectionLineRenderer);
        // DrawCircle(selectorLineRenderer);
        
        // todo: create borders of the collider
        
        gradient = new Gradient();

        var colorKey = new GradientColorKey[4];
        colorKey[0].color = Color.yellow;
        colorKey[0].time = 0f;
        colorKey[1].color = Color.yellow;
        colorKey[1].time = 0.5f;
        colorKey[2].color = Color.blue;
        colorKey[2].time = .55f;
        colorKey[2].color = Color.blue;
        colorKey[2].time = 1f;

        // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
        var alphaKey = new GradientAlphaKey[6];
        alphaKey[0].alpha = 0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = .1f;
        alphaKey[1].time = 0.0001f;
        alphaKey[2].alpha = .5f;
        alphaKey[2].time = 0.3f;
        alphaKey[3].alpha = 1f;
        alphaKey[3].time = .45f;
        alphaKey[4].alpha = 1f;
        alphaKey[4].time = .55f;
        alphaKey[5].alpha = .2f;
        alphaKey[5].alpha = 1f;
        
        gradient.SetKeys(colorKey, alphaKey);
    }

    private void Update()
    {
        if (!Selector) return;

        var time = CalcTime();
        var color = gradient.Evaluate(time);

        projectionLineRenderer.material.color = color;

        var (_, local) = Math.ProjectPointOntoOXYPlane(transform, Selector.position);

        projectionLineRenderer.gameObject.transform.localPosition = local;
        // projectionLineRenderer.gameObject.transform.localScale = new Vector3();

        // todo: color borders

    }

    private float CalcTime()
    {
        var (_, local) = Math.ProjectPointOntoOXYPlane(transform, Selector.position);

        var insideOXYBorders = Mathf.Abs(local.x) < targetsColliderScale.x / 2 &&
                               Mathf.Abs(local.y) < targetsColliderScale.y / 2;

        if (!insideOXYBorders) return 0f;

        var insideCollider = local.z >= 0 && local.z < targetsColliderScale.z;

        if (insideCollider)
        {
            return 0.5f + 0.5f * (local.z / targetsColliderScale.z);
        }

        var tooFar = local.z > .5f;

        if (tooFar) return .0001f;
        return 0.5f * (local.z / .5f);
        
    }

    private static void DrawCircle(LineRenderer lineRenderer)
    {
        lineRenderer.positionCount = 100;
        for (int i = 0; i < 100; i++)
        {
            float progress = ((float)i) / 100;
            float currentRadian = progress * 2 * Mathf.PI;

            float xScaled = Mathf.Cos(currentRadian);
            float yScaled = Mathf.Sin(currentRadian);
            
            lineRenderer.SetPosition(i, new Vector3(xScaled, yScaled, 0));
        }
    }
}
