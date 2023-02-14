using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.IsolatedStorage;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Utils;
using Random = System.Random;

public class TargetManager : MonoBehaviour
{
    [SerializeField] private SelectorCollisionProjection projector;
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private float diameter = .15f;
    [SerializeField] private float targetScaleFactor = 1f;
    [SerializeField] private int targetCount = 7;
    [SerializeField] private GameObject line;

    [SerializeField] private TextMeshPro textMeshPro;

    private readonly Color _activeColor = Color.black;
    private readonly Color _inactiveColor = Color.gray;
    private readonly Color _successColor = Color.green;
    private readonly Color _failColor = Color.red;


    private List<GameObject> _targets = new();

    private void OnEnable()
    {
        if (!targetPrefab)
        {
            Debug.LogError(
                $"{nameof(TargetManager)}: the '{nameof(targetPrefab)}' object is not set. Disabling the script.");
            enabled = false;
            return;
        }
    }

    /*private IEnumerator Start()
    {
        var factors = new [] { 0.6f, 1f, 1.4f };
        for (int i = 0; ; i = (i + 1) % 3)
        {
            targetScaleFactor = factors[i];
            
            CreateTargets();
            var coroutine = StartCoroutine(TargetRandomOrder());
            yield return new WaitForSeconds(10);
            
            StopCoroutine(coroutine);
            _targets.ForEach(Destroy);
            _targets.Clear();
            
            yield return new WaitForSeconds(3);
        }
    }*/

    private void Start()
    {
        CreateTargets();
        StartCoroutine(TargetRandomOrder());
    }

    private void CreateTargets()
    {
        for (int i = 0; i < targetCount; i++)
        {
            var target = Instantiate(targetPrefab, transform);

            // TODO: scale only X and Y – without Z
            target.transform.localScale *= targetScaleFactor;
            target.transform.localPosition = Vector3.right * diameter / 2;

            var angle = i * 360f / targetCount;
            target.transform.RotateAround(transform.position, transform.forward, angle);

            target.GetComponent<Renderer>().material.color = _inactiveColor;

            _targets.Add(target);
        }
    }

    private void RedrawLine(Vector2 localPlaneOXY)
    {
        line.transform.localPosition = new Vector3(localPlaneOXY.x, localPlaneOXY.y, 0);
    }

    private IEnumerator TargetRandomOrder()
    {
        foreach (int i in FittsLawIterator(targetCount))
        {
            var target = _targets[i];

            var render = target.GetComponent<Renderer>();

            render.material.color = _activeColor;

            var waitForEvent = new WaitForEvent<Vector2>(projector.triggerEnterEvent);
            yield return waitForEvent;

            var vectorLocalCoordinates = waitForEvent.Data;

            RedrawLine(vectorLocalCoordinates);
            var targetCoords = LocalPlaneCoordinatesToTarget(vectorLocalCoordinates, i);

            var targetRadius = .025f * targetScaleFactor / 2;

            var hitSuccessful = targetCoords.magnitude <= targetRadius;
            
            textMeshPro.text = (hitSuccessful ? "success\n" : "fail\n") +
                               $"{targetCoords.x:F3}\n" +
                               $"{targetCoords.y:F3}";;

            render.material.color = hitSuccessful ? _successColor : _failColor;

            yield return  new WaitForEvent(projector.triggerExitEvent);

            render.material.color = _inactiveColor;
        }
    }

    private Vector2 LocalPlaneCoordinatesToTarget(Vector2 localPlaneOXY, int targetIndex)
    {
        var targetPosition = new Vector2(
            Mathf.Cos(2 * Mathf.PI * targetIndex / targetCount),
            Mathf.Sin(2 * Mathf.PI * targetIndex / targetCount)
        ) * diameter / 2;

        return localPlaneOXY - targetPosition;
    }

    private static IEnumerable<int> FittsLawIterator(int targetCount)
    {
        int index = 0;
        while (true)
        {
            yield return index;
            index = (index + targetCount / 2) % targetCount;
        }
    }
}