using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ZMD;

// Prototype of the scripts that assign the invisible hand bones to follow to the physical hand bones.
// Currently, PhysicalFollowManager is the only derived class as it was the best performing one.
// The ManageFixedJoints script could be rewritten to inherit from this class if we find a way to make fixed joints a good alternative.
public class TargetManagerProto : MonoBehaviour
{
    [Tooltip("The object moved by the tracking data from the LeapMotion that this hand should follow.")]
    [SerializeField] public GameObject invisibleHand;

    [SerializeField] private protected List<GameObject> invisibleHandBones;

    // Start is called before the first frame update
    // We get a reference on the bones of the invisible hand so that we may attach the visible hand bones to their corresponding bone via fixed joint.
    void Start()
    {
        if (invisibleHand.ToString() == "")
        {
            Debug.Log("The invisible hand object given is missing or incorrect.");
        }
        else
        {
            GetAllChildrenTransform(invisibleHand);
            SetAllLovers(gameObject);
        }
    }

    // We call the function that will populate the bones with their respective targets to follow.
    public void PopulateFollow()
    {
        SetAllChildrenFollowInvisibleHand(gameObject);
    }
    
    //When the invisible hand loses the tracking and is disabled, we must un-assign the targets of the bones.
    //Eventually, we should also reset the visible hand position to a default one and cancel any remaining velocity.
    public virtual void UnsetFollow(){}

    protected static void SetPositionRotation(Transform bone, Transform targetBone)
    {
        bone.position = targetBone.position;
        bone.rotation = targetBone.rotation;
    }

    private void GetAllChildrenTransform(GameObject obj)
    {
        invisibleHandBones.Add(obj);
        foreach (Transform bone in obj.transform)
        {
            GetAllChildrenTransform(bone.gameObject);
        }
    }

    private void SetAllLovers(GameObject obj)
    {
        var physicsFollowScript = obj.GetComponent<PhysicsFollowMono>();
        if (obj.GetComponent<Rigidbody>() != null && physicsFollowScript.GetLover() == null)
        {
            var correspondingBone = invisibleHandBones.Find(t => t.ToString() == obj.ToString() || t.ToString() == obj.ToString().Substring(2));
            if (correspondingBone == null) return;
            physicsFollowScript.SetLover(correspondingBone.transform);
        }
        foreach (Transform childBone in obj.transform)
        {
            SetAllLovers(childBone.gameObject);
        }
    }

    // We first set the position of the bones to match those of the invisible hand bones.
    // Then we assign the targets of our physical hand bones to the invisible hand bones.
    private protected virtual void SetAllChildrenFollowInvisibleHand(GameObject obj)
    {
        
    }
}
