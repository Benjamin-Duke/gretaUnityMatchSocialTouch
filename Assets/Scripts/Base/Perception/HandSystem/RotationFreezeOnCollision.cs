using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap.Unity;
using UnityEngine;

// This class triggers events when it detects that a collision is about to happen.
public class RotationFreezeOnCollision : MonoBehaviour
{
    public event EventHandler<FreezeEventArgs> Freeze;
    public event EventHandler<FreezeEventArgs> FreezeStop;
    
    [Tooltip("The layer on which to perform the incoming collision detection.")]
    [SerializeField] private LayerMask laymask;
    
    [Tooltip("The radius of the area we want to scout for incoming collisions. The bigger the area, the more inaccurate the freezing of the hand will be compared to the actual collision." +
             "Too small values will lead to deformations of the hand when colliding at higher speeds.")]
    public float sphereRadius = 0.03f;

    //private bool colliding = false;

    // Legacy data structure used to partially freeze the hand.
    // Since this led to erratic behaviours, we currently freeze the whole hand and don't use this parameter anymore.
    // We can either delete this or find a more robust way to do partial freezing of the hand to allow restricted finger or palm movements.
    public enum idPart
    {
        Palm,
        Finger
    }

    public idPart handPart;
    
    private Collider[] colliders = new Collider[10];

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // We use FixedUpdate to test for incoming collisions because FixedUpdate is called before collisions are calculated and rendered.
    // OnCollision and OnTrigger methods are unsuitable because they are called right after the calculations and rendering of physical collisions.
    // When either a collision or no collision is detected, we invoke the corresponding event.
    void FixedUpdate()
    {
        //OverlapSphereNonAlloc simulates a sphere collider of the specified radius and origin on the layers specified in the layermask.
        //Here we are only interested in collisions with the agent as manipulations of objects will be taken care of by the Ultraleap Interaction Engine.
        var size = Physics.OverlapSphereNonAlloc(transform.position, sphereRadius, colliders, laymask, QueryTriggerInteraction.Ignore);
        //Debug.Log("Hello "+ gameObject.name + "?");
        if (colliders?.Any(c => c != null && c.CompareTag("CamilleCollision")) ?? false)
        {
            //Debug.Log("There is a collision : ");
            Freeze?.Invoke(this, new FreezeEventArgs(){partId = gameObject.name});
                /*for (int i = 0; i < size; i++)
                {
    //                if (colliders[i].CompareTag("CamilleCollision") || colliders[i].CompareTag("Camille"))
                    if (colliders[i].CompareTag("CamilleCollision"))
                        Freeze?.Invoke(this, new FreezeEventArgs(){partId = gameObject.name});
                }*/
        }
        else
        {
            if (gameObject.GetComponent<Rigidbody>().freezeRotation)
                FreezeStop?.Invoke(this, new FreezeEventArgs(){partId = gameObject.name});
        }

        //colliders.ClearWithDefaults();
    }

    //We want to send the id of the hand part that is about to collide so that we may monitor the state of the hand for further operations.
    public class FreezeEventArgs : EventArgs
    {
        public string partId;

    }
}
