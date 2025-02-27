using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VRFootScript : MonoBehaviour
{
    private Animator _animator;
    public Vector3 footOffset;
    [Range(0, 1)]
    public float rightFootPosWeight = 1;
    [Range(0, 1)]
    public float leftFootPosWeight = 1;
    [Range(0, 1)]
    public float rightFootRotWeight = 1;
    [Range(0, 1)]
    public float leftFootRotWeight = 1;

    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    private void OnAnimatorIK(int layerIndex)
    {
        Vector3 rightFootPos = _animator.GetIKPosition(AvatarIKGoal.RightFoot);
        RaycastHit hit;
        bool hasHit = Physics.Raycast(rightFootPos + Vector3.up, Vector3.down, out hit);
        if (hasHit)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, rightFootPosWeight);
            _animator.SetIKPosition(AvatarIKGoal.RightFoot, hit.point + footOffset);
            Quaternion footRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, hit.normal),hit.normal);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, rightFootRotWeight);
            _animator.SetIKRotation(AvatarIKGoal.RightFoot, footRotation);
        }
        else
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 0);
        }

        Vector3 leftFootPos = _animator.GetIKPosition(AvatarIKGoal.LeftFoot);
        
        hasHit = Physics.Raycast(leftFootPos + Vector3.up, Vector3.down, out hit);
        if (hasHit)
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, leftFootPosWeight);
            _animator.SetIKPosition(AvatarIKGoal.LeftFoot, hit.point+footOffset);
            Quaternion footRotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, hit.normal),hit.normal);
            _animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, leftFootRotWeight);
            _animator.SetIKRotation(AvatarIKGoal.LeftFoot, footRotation);
        }
        else
        {
            _animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 0);
        }
    }
}
