using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Random = System.Random;

public class TargetsManager : MonoBehaviour
{
    private static Random _random = new Random();
    
    [SerializeField]
    private GameObject targetPrefab;

    [SerializeField]
    private float radius = .2f;
    [FormerlySerializedAs("targetScale")] [SerializeField]
    private float targetScaleFactor = 1.4f;

    private List<GameObject> _targets = new List<GameObject>(11);

    private void Awake()
    {
        CreateTargets();
    }
    
    void Start()
    {
        SetRandomTarget();
    }

    void CreateTargets()
    {
        for (int i = 0; i < 11; i++)
        {
            var target = Instantiate(targetPrefab, transform);
            target.transform.localScale *= targetScaleFactor;
            target.transform.localPosition = Vector3.right * radius;
            var angle = i * 360f / 11;
            target.transform.RotateAround(transform.position, Vector3.forward, angle);
            target.GetComponent<CollideTarget>().selectEvent.AddListener(SetRandomTarget);
            _targets.Add(target);
        }
    }
    
    void SetRandomTarget()
    {
        int randomIndex = _random.Next(0, 11);
        _targets[randomIndex].GetComponent<CollideTarget>().MakeActive();
    }
}
