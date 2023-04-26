using System;
using UnityEngine;
using UnityEngine.UI;

public class Dbag : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private WalkingStateTrigger wst;
    private bool on = false;

    private void Start()
    {
        button.onClick.AddListener(() =>
        {
            wst.enabled = !on;
            on = !on;
        });
    }
}