using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineBetween : MonoBehaviour
{
    [SerializeField] private Transform Center;
    [SerializeField] private Transform LookFrom;

    // Update is called once per frame
    void Update()
    {
        if (Center == null || LookFrom == null) return;

        var line = Center.position - LookFrom.position;
        transform.position = Center.position;
        transform.rotation = Quaternion.LookRotation(line);

    }
}
