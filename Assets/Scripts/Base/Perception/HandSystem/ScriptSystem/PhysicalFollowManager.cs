using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ZMD;

// public class PhysicalFollowManager : TargetManagerProto
// {
//     public float releaseDistanceThreshold = 0.15f;

//     public override void UnsetFollow()
//     {
//         foreach (var follow in GetComponentsInChildren<PhysicsFollowMono>())
//         {
//             if (follow.GetTarget() == null) continue;
//             if (!follow.gameObject.name.Equals(follow.GetTarget().name)) continue;

//             follow.SetTarget(null);
//             var followRb = follow.GetComponent<Rigidbody>();
//             followRb.velocity = Vector3.zero;
//             followRb.angularVelocity = Vector3.zero;
//         }
//     }

//     private protected override void SetAllChildrenFollowInvisibleHand(GameObject obj)
//     {
//         if (obj.GetComponent<Rigidbody>() != null && obj.GetComponent<PhysicsFollowMono>().GetTarget() == null)
//         {
//             obj.GetComponent<PhysicsFollowMono>().SetTargetLover();
//         }

//         foreach (Transform childBone in obj.transform)
//         {
//             SetAllChildrenFollowInvisibleHand(childBone.gameObject);
//         }
//     }

//     void Update()
//     {
//         foreach (var follow in GetComponentsInChildren<PhysicsFollowMono>())
//         {
//             var target = follow.GetTarget();
//             if (target == null) continue;

//             float distance = Vector3.Distance(follow.transform.position, target.position);
//             if (distance > releaseDistanceThreshold)
//             {
//                 follow.SetTarget(null);
//                 var rb = follow.GetComponent<Rigidbody>();
//                 rb.velocity = Vector3.zero;
//                 rb.angularVelocity = Vector3.zero;
//             }
//         }
//     }

//     public float GetEstimatedForce(string boneName, float inverseStiffness)
//     {
//         var correspondingBone = invisibleHandBones.Find(t => t.ToString() == boneName);
//         Transform physicalBone = null;

//         foreach (Transform child in gameObject.transform)
//         {
//             if (child.gameObject.ToString() == boneName)
//                 physicalBone = child;
//         }

//         if (!physicalBone || !correspondingBone) return 0f;

//         var estimatedForce = (physicalBone.transform.position - correspondingBone.transform.position).magnitude / inverseStiffness;
//         return float.IsNaN(estimatedForce) ? 0f : estimatedForce;
//     }
// }


public class PhysicalFollowManager : TargetManagerProto
{

    //When the invisible hand loses the tracking and is disabled, we must un-assign the targets of the bones.
    //Eventually, we should also reset the visible hand position to a default one and cancel any remaining velocity.
    public float releaseDistanceThreshold = 0.15f;

    public override void UnsetFollow()
    {
        foreach (var follow in GetComponentsInChildren<PhysicsFollowMono>())
        {
            if (follow.GetTarget() == null) continue;
            if (!follow.gameObject.name.Equals(follow.GetTarget().name)) continue;

            follow.SetTarget(null);
            var followRb = follow.GetComponent<Rigidbody>();
            followRb.velocity = Vector3.zero;
            followRb.angularVelocity = Vector3.zero;
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
        // foreach (var follow in GetComponentsInChildren<PhysicsFollowMono>())
        // {
        //     var target = follow.GetTarget();
        //     if (target == null) continue;

        //     float distance = Vector3.Distance(follow.transform.position, target.position);
        //     if (distance > releaseDistanceThreshold)
        //     {
        //         follow.SetTarget(null);
        //         var rb = follow.GetComponent<Rigidbody>();
        //         rb.velocity = Vector3.zero;
        //         rb.angularVelocity = Vector3.zero;
        //     }
        // }
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
