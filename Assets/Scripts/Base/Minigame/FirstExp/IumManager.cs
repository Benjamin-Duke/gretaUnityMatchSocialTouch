using System;
using System.Collections;
using System.Collections.Generic;
using Minigame;
using UnityEngine;

public class IumManager : MonoBehaviour
{
    private FirstVRMinigameManager _minigameManager;
    
    // Start is called before the first frame update
    void Start()
    {
        _minigameManager = GetComponentInParent<FirstVRMinigameManager>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponentInParent<TetroID>() == null) return;
        _minigameManager.TetroEntered(other.gameObject.GetComponentInParent<TetroID>().tetrotype);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.GetComponentInParent<TetroID>() == null) return;
        _minigameManager.TetroExited(other.gameObject.GetComponentInParent<TetroID>().tetrotype);
    }
}
