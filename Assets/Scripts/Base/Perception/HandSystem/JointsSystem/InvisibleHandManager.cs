using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Leap;
using Leap.Unity;
using UnityEngine;

public class InvisibleHandManager : MonoBehaviour
{
    public class StartupPosRot
    {
        public Vector3 position;
        public Quaternion rotation;

    }
    
    // To use to reset the positions of the hand bones when we lose the tracking.
    [SerializeField] private readonly Dictionary<string, StartupPosRot> StartPositions = new Dictionary<string, StartupPosRot>();
    
    // To store the collision information of the children, so that we may wait for all collisions to end before unfreezing.
    [SerializeField] private List<string> _currentCollisions = new List<string>();

    [Tooltip("GameObject corresponding to the root of the tracked hand to follow with an enable/disable script.")]
    [SerializeField] public GameObject invisibleHand;
    
    [Tooltip("GameObject corresponding to the root of the visible hand to update, which has a ManageFixedJoints script.")]
    [SerializeField] public GameObject fixedJointsManager;

    private ManageFixedJoints _fixedJointsManager;
    private GameObject _invisibleHandActivator;
    private bool _activeLeapHand;
    
    
    
    // Start is called before the first frame update
    // We initialize the references to the invisible hand and to the fixed joints manager.
    // We particularly subscribe to the events that warn the enabling or disabling of the invisible hand so that we may adapt the fixed joints.
    void Start()
    {
        foreach (Transform child in transform)
        {
            StartPositions.Add(child.name, new StartupPosRot(){position = child.position, rotation = child.rotation});
            //Debug.Log("Storing position of " + child.name + " in " + StartPositions);
            var childFreeze = child.GetComponent<RotationFreezeOnCollision>();
            if (childFreeze != null)
            {
                childFreeze.Freeze += OnFreezeStarted;
                childFreeze.FreezeStop += OnFreezeStopped;
            }
            
        }
        
        _invisibleHandActivator = invisibleHand.GetComponent<HandEnableDisable>().gameObject;
        if (_invisibleHandActivator == null)
            Debug.LogError("No invisible hand specified or no HandEnableDisable script found, the hand won't move.");
        _fixedJointsManager = fixedJointsManager.GetComponent<ManageFixedJoints>();
        if (_fixedJointsManager == null)
            Debug.LogError("No ManageFixedJoints script specified, the hand won't be able to move.");
    }
    
    private void FixedUpdate()
    {
        if (_invisibleHandActivator.activeSelf && !_activeLeapHand)
        {
            _activeLeapHand = true;
            OnEnableHand();
        }
        else if (!_invisibleHandActivator.activeSelf && _activeLeapHand)
        {
            _activeLeapHand = false;
            OnDisableHand();
        }
    }

    private void OnEnableHand()
    {
        Debug.Log("Trying to populate the joints.");
        _fixedJointsManager.PopulateJoints();
    }
    
    private void OnDisableHand()
    {
        Debug.Log("Destroying the joints.");
        _fixedJointsManager.DestroyJoints(StartPositions);
    }

    private void OnFreezeStarted(object sender, RotationFreezeOnCollision.FreezeEventArgs freezeEventArgs)
    {
        _currentCollisions.Add(freezeEventArgs.partId);
        foreach (Transform child in transform)
        {
            if (child.GetComponent<RotationFreezeOnCollision>() == null) continue;
            if (_currentCollisions.Contains("Palm"))
                child.GetComponent<Rigidbody>().freezeRotation = true;
            else if (_currentCollisions.Contains("Finger"))
            {
                if (child.GetComponent<RotationFreezeOnCollision>().handPart.ToString()
                    .Equals("Finger", StringComparison.InvariantCultureIgnoreCase))
                    child.GetComponent<Rigidbody>().freezeRotation = true;
            }
        }
    }
    
    private void OnFreezeStopped(object sender, RotationFreezeOnCollision.FreezeEventArgs freezeEventArgs)
    {
        _currentCollisions.Remove(freezeEventArgs.partId);
        if (_currentCollisions.Contains("Palm")) return;
        if (_currentCollisions.Contains("Finger"))
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<RotationFreezeOnCollision>() == null) continue;
                if (child.GetComponent<RotationFreezeOnCollision>().handPart.ToString().Equals("Palm", StringComparison.InvariantCultureIgnoreCase))
                    child.GetComponent<Rigidbody>().freezeRotation = false;
            }
        }
        else 
            foreach (Transform child in transform)
            {
                if (child.GetComponent<RotationFreezeOnCollision>() == null) continue;
                child.GetComponent<Rigidbody>().freezeRotation = false;
            }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
