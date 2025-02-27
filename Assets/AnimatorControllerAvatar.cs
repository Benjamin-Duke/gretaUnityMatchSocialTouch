using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorControllerAvatar : MonoBehaviour
{
    public float speedThreshold = 0.1f;
    [Range(0, 1)] public float smoothing = 1f;
    private Animator _animator;

    private Vector3 _previousPos;
    private Vector3 _previousForward;

    private VR _vrRig;
    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
        _vrRig = GetComponent<VR>();
        _previousPos = _vrRig.head.vrTarget.position;
        _previousForward = _vrRig.head.vrTarget.forward;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        var currentPos = _vrRig.head.vrTarget.position;
        var currentForward = _vrRig.head.vrTarget.forward;
        //Compute the speed
        var headsetSpeed = (currentPos - _previousPos) / Time.deltaTime;
        headsetSpeed.y = 0;
        //Local speed
        var localHeadsetSpeed = transform.InverseTransformDirection(headsetSpeed);
        //var dot = Vector3.Dot(_previousPos.forward, currentPos.forward);
        var deltaAngleNormalized = (Vector3.SignedAngle(currentForward, _previousForward, Vector3.up) / 180) / Time.deltaTime;
        _previousPos = currentPos;
        _previousForward = currentForward;

        //Set Animator Values
        var previousDirectionX = _animator.GetFloat("TurningSpeed");
        var previousAngSpeed = _animator.GetFloat("AngularSpeed");
        var previousDirectionY = _animator.GetFloat("Speed");
        
        _animator.SetBool("isMoving", headsetSpeed.magnitude > speedThreshold);
        _animator.SetFloat("TurningSpeed", Mathf.Lerp( previousDirectionX, Mathf.Clamp(localHeadsetSpeed.x, -1, 1), smoothing));
        _animator.SetFloat("AngularSpeed", Mathf.Lerp( previousAngSpeed, deltaAngleNormalized, smoothing));
        //_animator.SetFloat("TurningSpeed", deltaAngleNormalized);
        _animator.SetFloat("Speed", Mathf.Lerp( previousDirectionY, Mathf.Clamp(localHeadsetSpeed.z, -1, 1), smoothing));
        
        
    }
}
