using System;
using UnityEngine;

[Serializable]
public class BendingSegment
{
    public Transform firstTransform;
    public Transform lastTransform;
    public float thresholdAngleDifference;
    public float bendingMultiplier = 0.6f;
    public float maxAngleDifference = 30;
    public float maxBendingAngle = 80;
    public float responsiveness = 5;
    internal float angleH;
    internal float angleV;
    internal int chainLength;
    internal Vector3 dirUp;
    internal Quaternion[] origRotations;
    internal Vector3 referenceLookDir;
    internal Vector3 referenceUpDir;
}

[Serializable]
public class NonAffectedJoints
{
    public Transform joint;
    public float effect;
}

public class HeadLookController : MonoBehaviour
{
    public Transform rootNode;
    public BendingSegment[] segments;
    public NonAffectedJoints[] nonAffectedJoints;
    public Vector3 headLookVector = Vector3.forward;
    public Vector3 headUpVector = Vector3.up;
    public Transform target;
    public float effect = 1;
    public bool overrideAnimation;

    // Head of the player for default look target 
    //private Transform playerHead;

    private void Start()
    {
        if (rootNode == null) rootNode = transform;

        // We assume the head of the player will be its POV
        //playerHead = GameObject.FindGameObjectWithTag("MainCamera").transform;

        // Setup segments
        foreach (var segment in segments)
        {
            var parentRot = segment.firstTransform.parent.rotation;
            var parentRotInv = Quaternion.Inverse(parentRot);
            segment.referenceLookDir =
                parentRotInv * rootNode.rotation * headLookVector.normalized;
            segment.referenceUpDir =
                parentRotInv * rootNode.rotation * headUpVector.normalized;
            segment.angleH = 0;
            segment.angleV = 0;
            segment.dirUp = segment.referenceUpDir;

            segment.chainLength = 1;
            var t = segment.lastTransform;
            while (t != segment.firstTransform && t != t.root)
            {
                segment.chainLength++;
                t = t.parent;
            }

            segment.origRotations = new Quaternion[segment.chainLength];
            t = segment.lastTransform;
            for (var i = segment.chainLength - 1; i >= 0; i--)
            {
                segment.origRotations[i] = t.localRotation;
                t = t.parent;
            }
        }
    }

    private void LateUpdate()
    {
        if (Time.deltaTime == 0)
            return;
        UpdateHeadLook();
        
    }

    public void UpdateHeadLook()
    {
        if (target == null)
            return;

        // Remember initial directions of joints that should not be affected
        var jointDirections = new Vector3[nonAffectedJoints.Length];
        for (var i = 0; i < nonAffectedJoints.Length; i++)
            foreach (Transform child in nonAffectedJoints[i].joint)
            {
                jointDirections[i] = child.position - nonAffectedJoints[i].joint.position;
                break;
            }

        // Handle each segment
        foreach (var segment in segments)
        {
            var t = segment.lastTransform;
            if (overrideAnimation)
                for (var i = segment.chainLength - 1; i >= 0; i--)
                {
                    t.localRotation = segment.origRotations[i];
                    t = t.parent;
                }

            var parentRot = segment.firstTransform.parent.rotation;
            var parentRotInv = Quaternion.Inverse(parentRot);

            // Desired look direction in world space
            var lookDirWorld = (target.position - segment.lastTransform.position).normalized;

            // Desired look directions in neck parent space
            var lookDirGoal = parentRotInv * lookDirWorld;

            // Get the horizontal and vertical rotation angle to look at the target
            var hAngle = AngleAroundAxis(
                segment.referenceLookDir, lookDirGoal, segment.referenceUpDir
            );

            var rightOfTarget = Vector3.Cross(segment.referenceUpDir, lookDirGoal);

            var lookDirGoalinHPlane =
                lookDirGoal - Vector3.Project(lookDirGoal, segment.referenceUpDir);

            var vAngle = AngleAroundAxis(
                lookDirGoalinHPlane, lookDirGoal, rightOfTarget
            );

            // Handle threshold angle difference, bending multiplier,
            // and max angle difference here
            var hAngleThr = Mathf.Max(
                0, Mathf.Abs(hAngle) - segment.thresholdAngleDifference
            ) * Mathf.Sign(hAngle);

            var vAngleThr = Mathf.Max(
                0, Mathf.Abs(vAngle) - segment.thresholdAngleDifference
            ) * Mathf.Sign(vAngle);

            hAngle = Mathf.Max(
                Mathf.Abs(hAngleThr) * Mathf.Abs(segment.bendingMultiplier),
                Mathf.Abs(hAngle) - segment.maxAngleDifference
            ) * Mathf.Sign(hAngle) * Mathf.Sign(segment.bendingMultiplier);

            vAngle = Mathf.Max(
                Mathf.Abs(vAngleThr) * Mathf.Abs(segment.bendingMultiplier),
                Mathf.Abs(vAngle) - segment.maxAngleDifference
            ) * Mathf.Sign(vAngle) * Mathf.Sign(segment.bendingMultiplier);

            // Handle max bending angle here
            hAngle = Mathf.Clamp(hAngle, -segment.maxBendingAngle, segment.maxBendingAngle);
            vAngle = Mathf.Clamp(vAngle, -segment.maxBendingAngle, segment.maxBendingAngle);

            var referenceRightDir =
                Vector3.Cross(segment.referenceUpDir, segment.referenceLookDir);

            // Lerp angles
            segment.angleH = Mathf.Lerp(
                segment.angleH, hAngle, Time.deltaTime * segment.responsiveness
            );
            segment.angleV = Mathf.Lerp(
                segment.angleV, vAngle, Time.deltaTime * segment.responsiveness
            );

            // Get direction
            lookDirGoal = Quaternion.AngleAxis(segment.angleH, segment.referenceUpDir)
                          * Quaternion.AngleAxis(segment.angleV, referenceRightDir)
                          * segment.referenceLookDir;

            // Make look and up perpendicular
            var upDirGoal = segment.referenceUpDir;
            Vector3.OrthoNormalize(ref lookDirGoal, ref upDirGoal);

            // Interpolated look and up directions in neck parent space
            var lookDir = lookDirGoal;
            segment.dirUp = Vector3.Slerp(segment.dirUp, upDirGoal, Time.deltaTime * 5);
            Vector3.OrthoNormalize(ref lookDir, ref segment.dirUp);

            // Look rotation in world space
            var lookRot = parentRot * Quaternion.LookRotation(lookDir, segment.dirUp)
                                    * Quaternion.Inverse(
                                        parentRot * Quaternion.LookRotation(
                                            segment.referenceLookDir, segment.referenceUpDir
                                        )
                                    );

            // Distribute rotation over all joints in segment
            var dividedRotation =
                Quaternion.Slerp(Quaternion.identity, lookRot, effect / segment.chainLength);
            t = segment.lastTransform;
            for (var i = 0; i < segment.chainLength; i++)
            {
                t.rotation = dividedRotation * t.rotation;
                t = t.parent;
            }
        }

        // Handle non affected joints
        for (var i = 0; i < nonAffectedJoints.Length; i++)
        {
            var newJointDirection = Vector3.zero;

            foreach (Transform child in nonAffectedJoints[i].joint)
            {
                newJointDirection = child.position - nonAffectedJoints[i].joint.position;
                break;
            }

            var combinedJointDirection = Vector3.Slerp(
                jointDirections[i], newJointDirection, nonAffectedJoints[i].effect
            );

            nonAffectedJoints[i].joint.rotation = Quaternion.FromToRotation(
                newJointDirection, combinedJointDirection
            ) * nonAffectedJoints[i].joint.rotation;
        }
    }

    // Helper method : look at player's head
    /*public void LookPlayer()
    {
        target = playerHead;
    }*/

    // The angle between dirA and dirB around axis
    public static float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
    {
        // Project A and B onto the plane orthogonal target axis
        dirA = dirA - Vector3.Project(dirA, axis);
        dirB = dirB - Vector3.Project(dirB, axis);

        // Find (positive) angle between A and B
        var angle = Vector3.Angle(dirA, dirB);

        // Return angle multiplied with 1 or -1
        return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);
    }
}