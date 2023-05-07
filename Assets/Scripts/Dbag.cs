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
    [SerializeField] private GameObject colliderVisualizer;
    [SerializeField] private GameObject side;

    Gradient gradient;
    GradientColorKey[] colorKey;
    GradientAlphaKey[] alphaKey;
    private void Start()
    {
        tm.TargetSize = TargetsManager.TargetSizeVariant.VeryBig;
        tm.EnsureTargetsShown();
        
        gradient = new Gradient();

        // Populate the color keys at the relative time 0 and 1 (0 and 100%)
        colorKey = new GradientColorKey[2];
        colorKey[0].color = Color.red;
        colorKey[0].time = 0.0f;
        colorKey[1].color = Color.blue;
        colorKey[1].time = 1.0f;

        // Populate the alpha  keys at relative time 0 and 1  (0 and 100%)
        alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = .1f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = .1f;
        alphaKey[1].time = 1.0f;
        
        gradient.SetKeys(colorKey, alphaKey);

        gradient.Evaluate(.8f);

        StartCoroutine(ShowCollider());
    }

    private IEnumerator ShowCollider()
    {
        bool active = false;
        int i = 0;
        while (true)
        {
            colliderVisualizer.SetActive(active);
            // side.GetComponent<Renderer>().material.color = gradient.Evaluate((i + 1f) * (i + 2f) % 1f);
            side.GetComponent<Renderer>().material.color = gradient.Evaluate(.2f);

            active = !active;
            i++;
            yield return new WaitForSeconds(2);
        }
        colliderVisualizer.SetActive(false);
        
    }
}