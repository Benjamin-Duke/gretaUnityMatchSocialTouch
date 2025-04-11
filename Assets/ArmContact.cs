using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArmContact : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger Entered: " + other.gameObject.name);
    }
}