using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Dbag : MonoBehaviour
{
    private static float delay = 1f;

    private IEnumerator Start()
    {
        int i = 0;
        while (true)
        {
            // Invoke("Funcc", delay);
            // Funcc();
            if (i % 2 == 1)
            {
                Cancel();
            }

            i++;
            yield return new WaitForSeconds(5);
        }
    }

    private void Funcc()
    {
        Debug.Log("Func");
    }

    private void Cancel()
    {
        Debug.Log("Cancelled");
        CancelInvoke("Funcc");
    }
}