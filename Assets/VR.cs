using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VRMap
{
    public Transform vrTarget;
    public Transform rigTarget;
    public Vector3 trackingPositionOffset;
    public Vector3 trackingRotationOffset;

    public void Map()
    {
        rigTarget.position = vrTarget.TransformPoint(trackingPositionOffset);
        rigTarget.rotation = vrTarget.rotation * Quaternion.Euler(trackingRotationOffset);
    }

}
public class VR : MonoBehaviour
{
    public float turnSmoothness = 1f;
    public VRMap head;
    //public VRMap rightHand;
    //public VRMap leftHand;
    
    public Transform headConstraint;
    public Vector3 headBodyOffset;

    // Start is called before the first frame update
    void Start()
    {
        headBodyOffset = transform.position - headConstraint.position;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        transform.position = headConstraint.position + headBodyOffset;
        transform.forward = Vector3.ProjectOnPlane(headConstraint.forward, Vector3.up).normalized;
        /*transform.forward = Vector3.Lerp(transform.forward,
            Vector3.ProjectOnPlane(headConstraint.forward, Vector3.up).normalized, Time.deltaTime * turnSmoothness);*/
        head.Map();
        //leftHand.Map();
        //rightHand.Map();
    }
}
