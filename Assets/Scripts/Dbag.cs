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
    private int i = 0;
    void Foo()
    {
        Debug.Log(i++);
    }

    private void Start()
    {
        UnityEvent eventt = new();
        
        eventt.AddListener(Foo);
        
        eventt.Invoke();
        
        eventt.RemoveListener(Foo);
        
        eventt.Invoke();
        
        eventt.AddListener(Foo);
        eventt.AddListener(Foo);
        eventt.AddListener(Foo);
        
        eventt.Invoke();
        
        eventt.RemoveListener(Foo);
        
        eventt.Invoke();
    }
}