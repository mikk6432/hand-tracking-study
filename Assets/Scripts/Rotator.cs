using System;
using System.Collections;
using UnityEngine;

public class Rotator: MonoBehaviour
{
    private IEnumerator Start()
    {
        while (true)
        {
            transform.localRotation *= Quaternion.AngleAxis(30, Vector3.forward);
            // transform.Rotate(new Vector3(0 , 0, 30), Space.Self);
            yield return new WaitForSeconds(2);
        }
    }
}