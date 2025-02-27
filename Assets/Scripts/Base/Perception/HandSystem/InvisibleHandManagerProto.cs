using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Leap;
using Leap.Unity;
using UnityEngine;
using UnityEngine.Serialization;

//This class was initially thought to be the base class for different specialized managers (fixed joints or scripts).
//Currently, the scripts management of physical hand movements is the best one and this ended up not being implemented as a base class.
//If viable alternatives appear, we should rewrite this class and derive it accordingly.
public class InvisibleHandManagerProto : MonoBehaviour
{
    /*public class StartupPosRot
    {
        public Vector3 position;
        public Quaternion rotation;

    }*/
    
    // To use to reset the positions of the hand bones to a default position when we lose the tracking.
    //[SerializeField] private readonly Dictionary<string, StartupPosRot> StartPositions = new Dictionary<string, StartupPosRot>();
    
    // To store the collision information of the children, so that we may wait for all collisions to end before unfreezing.
    [SerializeField] private HashSet<string> _currentCollisions = new HashSet<string>();

    [Tooltip("GameObject corresponding to the root of the tracked hand to follow with an enable/disable script.")]
    [SerializeField] public GameObject invisibleHand;
    
    [Tooltip("GameObject corresponding to the root of the visible hand to update, which has a ManageFixedJoints script.")]
    [SerializeField] public GameObject targetManager;

    private TargetManagerProto _targetManager;
    private GameObject _invisibleHandActivator;
    private bool _activeLeapHand;
    
    
    
    // Start is called before the first frame update
    // We initialize the references to the invisible hand and to the target to follow manager.
    // We particularly subscribe to the events that warn the enabling or disabling of the invisible hand so that we may adapt the target assignment.
    // We also subscribe to the events that monitor the freezing of the hands on collision
    void Start()
    {
        foreach (Transform child in transform)
        {
            //StartPositions.Add(child.name, new StartupPosRot(){position = child.position, rotation = child.rotation});
            //Debug.Log("Storing position of " + child.name + " in " + StartPositions);
            var childFreeze = child.GetComponent<RotationFreezeOnCollision>();
            if (childFreeze != null)
            {
            //    childFreeze.Freeze += OnFreezeStarted;
            //    childFreeze.FreezeStop += OnFreezeStopped;
            }
            
        }
        
        _invisibleHandActivator = invisibleHand.GetComponent<HandEnableDisable>().gameObject;
        if (_invisibleHandActivator == null)
            Debug.LogError("No invisible hand specified, the hand won't move.");
        _targetManager = targetManager.GetComponent<TargetManagerProto>();
        if (_targetManager == null)
            Debug.LogError("No ManageFixedJoints script specified, the hand won't be able to move.");
        OnEnableHand();
    }

    private void FixedUpdate()
    {
        if (_invisibleHandActivator.activeSelf && !_activeLeapHand)
        {
            _activeLeapHand = true;
            //OnEnableHand();
        }
        else if (!_invisibleHandActivator.activeSelf && _activeLeapHand)
        {
            _activeLeapHand = false;
            //OnDisableHand();
        }
    }

    private void OnEnableHand()
    {
        //Debug.Log("Trying to populate the targets to follow.");
        _targetManager.PopulateFollow();
    }
    
    private void OnDisableHand()
    {
        //Debug.Log("Unsetting the targets to follow.");
        _targetManager.UnsetFollow();
    }

    //We store all the parts of the hand that are colliding with something and freeze all the bones.
    private void OnFreezeStarted(object sender, RotationFreezeOnCollision.FreezeEventArgs freezeEventArgs)
    {
        _currentCollisions.Add(freezeEventArgs.partId);
        if (_currentCollisions.Count <= 0) return;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<RotationFreezeOnCollision>() == null) continue;
            Freeze(child);
        }
    }
    
    //We remove the part of the hand that is not colliding anymore from our storage and if no hand part is colliding, we unfreeze the hand bones.
    private void OnFreezeStopped(object sender, RotationFreezeOnCollision.FreezeEventArgs freezeEventArgs)
    {
        /*foreach (var collision in _currentCollisions)
        {
            Debug.Log("Current collisions : " + collision);
        }
        Debug.Log("Parameter : " + freezeEventArgs.partId);*/
        _currentCollisions.Remove(freezeEventArgs.partId);
        //Debug.Log("Successfully removed : " + _currentCollisions.Contains(freezeEventArgs.partId));
        //Debug.Log(freezeEventArgs.partId);
        if (_currentCollisions.Count > 0) return;
        foreach (Transform child in transform)
        {
            if (child.GetComponent<RotationFreezeOnCollision>() == null) continue;
            Unfreeze(child);
        }
    }

    //To make sure that the hand won't get dislocated when hitting a collider, we freeze the rigidbody rotations
    //and lock the axis motions of the joint that attaches the hand part to its parent in the hand hierarchy.
    //This prevents any finger movement until a better way to prevent the hand from penetrating a collider is found.
    private void Freeze(Transform obj)
    {
        obj.GetComponent<Rigidbody>().freezeRotation = true;
        var joint = obj.GetComponent<ConfigurableJoint>();
        if (joint == null) return;
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

    }
    
    //We reverse the freezing by re-enabling rotations and freeing the axis motions.
    private void Unfreeze(Transform obj)
    {
        obj.GetComponent<Rigidbody>().freezeRotation = false;
        var joint = obj.GetComponent<ConfigurableJoint>();
        if (joint == null) return;
        joint.xMotion = ConfigurableJointMotion.Free;
        joint.yMotion = ConfigurableJointMotion.Free;
        joint.zMotion = ConfigurableJointMotion.Free; 
    }
    
    

    // Update is called once per frame
    void Update()
    {
        
    }
}
