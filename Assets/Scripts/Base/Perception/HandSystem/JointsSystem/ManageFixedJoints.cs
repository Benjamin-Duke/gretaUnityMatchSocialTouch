using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Leap.Unity;
using UnityEngine;

public class ManageFixedJoints : MonoBehaviour
{
    [Tooltip("The object moved by the tracking data from the LeapMotion that this hand should follow.")]
    [SerializeField] public GameObject invisibleHand;

    [SerializeField] private List<GameObject> invisibleHandBones;

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
        }

        /*foreach (Transform child  in transform)
        {
            SetVisibleHandAllChildrenFixedJoints(gameObject, child.gameObject);
        }*/
    }

    // We first set the position of the bones to match those of the invisible hand bones.
    // Then we create the fixed joints that will attach the visible hand bones to the invisible hand bones.
    public void PopulateJoints()
    {
        SetAllChildrenFixedJointsToInvisibleHand(gameObject);
    }
    
    //When the invisible hand loses the tracking and is disabled, we destroy the joints so that we may recreate them later.
    //Eventually, we should reset the visible hand position to a default one.
    public void DestroyJoints(Dictionary<string, InvisibleHandManager.StartupPosRot> startPositions)
    {
        foreach (var joint in GetComponentsInChildren<FixedJoint>())
        {
            if (!joint.gameObject.name.Equals(joint.connectedBody.name))
            {
                //Debug.Log(joint.gameObject.ToString() + "--- : " + joint.connectedBody.ToString() + "---");
                continue;
            }

            var objectToReposition = joint.gameObject.transform;
            Destroy(joint);
            var resetPos = startPositions.FirstOrDefault(t => t.Key.Equals(objectToReposition.name)).Value;
            objectToReposition.position = resetPos.position;
            objectToReposition.rotation = resetPos.rotation;
        }
    }

    private static void SetPositionRotation(Transform bone, Transform targetBone)
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

    private void SetAllChildrenFixedJointsToInvisibleHand(GameObject obj)
    {
        if (obj.GetComponent<Rigidbody>() != null && obj.GetComponent<FixedJoint>() == null)
        {
            Debug.Log("Trying to set fixed joint to" + obj);
            Debug.Log("Result of the query on the invisible Hand : " + invisibleHandBones.Find(t => t.ToString() == obj.ToString()));
            var correspondingBone = invisibleHandBones.Find(t => t.ToString() == obj.ToString())?.GetComponent<Rigidbody>();
            if (correspondingBone == null) return;
            SetPositionRotation(obj.transform, correspondingBone.transform);
            obj.AddComponent<FixedJoint>();
            obj.GetComponent<FixedJoint>().connectedBody = correspondingBone;
        }
        foreach (Transform childBone in obj.transform)
        {
            SetAllChildrenFixedJointsToInvisibleHand(childBone.gameObject);
        }
    }
    
    private void SetVisibleHandAllChildrenFixedJoints(GameObject parent, GameObject child)
    {
        if (child.GetComponent<Rigidbody>() != null && parent.GetComponent<Rigidbody>() != null && child.GetComponent<FixedJoint>() == null)
        {
            Debug.Log("Attaching child " + child + "to parent " + parent);
            child.AddComponent<FixedJoint>();
            child.GetComponent<FixedJoint>().connectedBody = parent.GetComponent<Rigidbody>();
        }
        foreach (Transform childBone in child.transform)
        {
            SetVisibleHandAllChildrenFixedJoints(child, childBone.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
