using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowLegs : MonoBehaviour
{
    //In the editor set this variable to the Hip object attached to the Legs
    public Transform HipTofollow;
    
    //This object's transform
    private Transform t;
    
    void Start()
    {
        t = gameObject.transform;
        t.parent = HipTofollow;
    }
}
