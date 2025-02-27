using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class IKTouchTarget : MonoBehaviour
{
    public Transform targetToTouch;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (targetToTouch != null)
        {
            transform.position = targetToTouch.position;
            transform.rotation = targetToTouch.rotation;
        }
    }
}
