using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ZMD;

public class PhysicalFollowManager : TargetManagerProto
{

    //When the invisible hand loses the tracking and is disabled, we must un-assign the targets of the bones.
    //Eventually, we should also reset the visible hand position to a default one and cancel any remaining velocity.
    public override void UnsetFollow()
    {
        foreach (var follow in GetComponentsInChildren<PhysicsFollowMono>())
        {
            if (follow.GetTarget() == null) continue;
            if (!follow.gameObject.name.Equals(follow.GetTarget().name))
            {
                //Debug.Log(joint.gameObject.ToString() + "--- : " + joint.connectedBody.ToString() + "---");
                continue;
            }

            //var objectToReposition = follow.GetTarget();
            follow.SetTarget(null);
            var followRb = follow.gameObject.GetComponent<Rigidbody>();
            followRb.velocity = Vector3.zero;
            followRb.angularVelocity = Vector3.zero;
            /*var resetPos = startPositions.FirstOrDefault(t => t.Key.Equals(objectToReposition.name)).Value;
            objectToReposition.position = resetPos.position;
            objectToReposition.rotation = resetPos.rotation;*/
        }
    }

    // We first set the position of the bones to match those of the invisible hand bones.
    // Then we assign the targets of our physical hand bones to the invisible hand bones.
    // Here we assume that the respective bone hierarchies are similar and use similar names.
    // Later developments might require a more robust way to assign respective targets.
    private protected override void SetAllChildrenFollowInvisibleHand(GameObject obj)
    {
        if (obj.GetComponent<Rigidbody>() != null && obj.GetComponent<PhysicsFollowMono>().GetTarget() == null)
        {
            obj.GetComponent<PhysicsFollowMono>().SetTargetLover();
        }
        foreach (Transform childBone in obj.transform)
        {
            SetAllChildrenFollowInvisibleHand(childBone.gameObject);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public float GetEstimatedForce(string boneName, float inverseStiffness)
    {
        var correspondingBone = invisibleHandBones.Find(t => t.ToString() == boneName);
        Transform physicalBone = null;
        foreach (Transform child in gameObject.transform)
        {
            if (child.gameObject.ToString() == boneName)
                physicalBone = child;
        }

        if (!physicalBone || !correspondingBone) return 0f;
        var estimatedForce = (physicalBone.transform.position - correspondingBone.transform.position).magnitude / inverseStiffness;
        return float.IsNaN(estimatedForce) ? 0f : estimatedForce;
    }
}
