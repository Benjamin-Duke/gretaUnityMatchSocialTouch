using System;
using System.Collections;
using System.Collections.Generic;
using Minigame;
using UnityEngine;

public class FloorManager : MonoBehaviour
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
        _minigameManager.ResetTetro(other.gameObject.GetComponentInParent<TetroID>().tetrotype);

    }
}
