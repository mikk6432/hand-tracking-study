using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PassthroughBlur : MonoBehaviour
{
    [SerializeField] private GameObject headset;
    [SerializeField] private GameObject anchor;
    [SerializeField] private float distance;

    // Update is called once per frame
    void Update()
    {
        var vector = anchor.transform.position - headset.transform.position;
        vector = vector.normalized * distance;
        transform.position = headset.transform.position + vector;
    }
}
