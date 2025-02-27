using System;
using UnityEngine;

public class LookCamille : MonoBehaviour
{
    // Hypothesis: 
    // To estimate if the user is looking at the face, we use the look direction of Camille, which is assumed to be the forward of the given transform.
    // The user look direction is assumed to be the forward vector of the given transform
    // for other objects, use LookDirection

    [SerializeField] [Tooltip("Transform corresponding to the head of Camille.")]
    private Transform headCamille;

    [SerializeField]
    [Tooltip("Transform corresponding to the eyes of Camille.")]
    private Transform eyesCamille;

    [SerializeField]
    [Tooltip(
        "Transform corresponding to the head of the user. \nThe forward direction is considered as the looking direction.")]
    private Transform headUser;

    [SerializeField]
    [Tooltip(
        "Float corresponding to the look threshold towards the eyes of Camille.\nThe value should be between -1 (all directions accepted) and 1 (look direction must be perfectly aligned with the target).")]
    private float eyeThreshold = 0.9f;

    [SerializeField]
    [Tooltip(
        "Float corresponding to the look threshold towards the body. \nThe value should be between -1 (all directions accepted) and 1 (look direction must be perfectly aligned with the target).")]
    private float bodyThreshold = 0.7f;

    [SerializeField]
    [Tooltip(
        "Float corresponding to the look threshold compared to the vertical axis. \nThe value should be between -1 (all directions accepted) and 1 (look direction must be perfectly aligned with the target).")]
    private float heightThreshold = 0.2f;
    
    [SerializeField] [Tooltip("Print debug information")]
    private bool _debug;

    private float _eye, _head, _body, _height;

    private Vector3 _previousForward, _previousPosition;
    public bool IsLookingAtFace { get; private set; }

    public bool IsLookingAtHead { get; private set; }

    public bool IsLookingAtBody { get; private set; }

    public float IsLookingAtFaceCoef { get; private set; }

    public float IsLookingAtHeadCoef { get; private set; }

    public float IsLookingAtBodyCoef { get; private set; }

    private void Start()
    {
        _previousForward = headUser.forward;
        _previousPosition = headUser.position;
    }

    private void Update()
    {
        if (1 - Vector3.Dot(_previousForward, headUser.forward) < 0.001f &&
            (_previousPosition - headUser.position).magnitude < 0.001f) return;
        _previousForward = headUser.forward;
        _previousPosition = headUser.position;
        InterpretCoef();
        if (_debug)
            Debug.Log("Look direction towards Camille with the sensor " + headUser.name + " and the target " +
                      headCamille.name + ":\n" +
                      "Look towards the face:\n" +
                      "Boolean result = " + IsLookingAtFace + "\n" +
                      "Coefficient result = " + IsLookingAtFaceCoef + "\n" +
                      "Look towards the head:\n" +
                      "Boolean result = " + IsLookingAtHead + "\n" +
                      "Coefficient result = " + IsLookingAtHeadCoef + "\n" +
                      "Look towards the body:\n" +
                      "Boolean result = " + IsLookingAtBody + "\n" +
                      "Coefficient result = " + IsLookingAtBodyCoef
            );
    }

    public event EventHandler<LookAtCamilleEventArgs> LookAtCamilleChanged;

    private void OnLookAtCamille(LookAtCamilleEventArgs e)
    {
        LookAtCamilleChanged?.Invoke(this, e);
    }

    private void InterpretCoef()
    {
        _eye = GetLookFaceCoefficient();
        _head = GetLookHeadCoefficient();
        _body = GetLookBodyCoefficient();
        _height = GetHeightCoefficient();

        var tempIsLookingAtFace = _eye < -eyeThreshold;
        var tempIsLookingAtHead = _head < -eyeThreshold;
        var tempIsLookingAtBody = _body < -bodyThreshold && _height < heightThreshold;

        if (tempIsLookingAtFace)
            IsLookingAtFaceCoef = (-_eye - eyeThreshold) / (1 - eyeThreshold);
        else IsLookingAtFaceCoef = 0f;
        if (tempIsLookingAtHead)
            IsLookingAtHeadCoef = (-_head - eyeThreshold) / (1 - eyeThreshold);
        else IsLookingAtHeadCoef = 0f;
        if (tempIsLookingAtBody)
            IsLookingAtBodyCoef = (-_body - bodyThreshold) / (1 - bodyThreshold);
        else IsLookingAtBodyCoef = 0f;

        if (tempIsLookingAtBody != IsLookingAtBody || tempIsLookingAtHead != IsLookingAtHead ||
            tempIsLookingAtFace != IsLookingAtFace)
        {
            OnLookAtCamille(new LookAtCamilleEventArgs
            {
                LookingBody = tempIsLookingAtBody, 
                LookingEye = tempIsLookingAtFace, 
                LookingHead = tempIsLookingAtHead
            });            
        }
        IsLookingAtFace = tempIsLookingAtFace;
        IsLookingAtHead = tempIsLookingAtHead;
        IsLookingAtBody = tempIsLookingAtBody;
    }

    // Eye to eye direction
    private float GetLookFaceCoefficient()
    {
        return Vector3.Dot((headUser.position - eyesCamille.position).normalized, headUser.forward);
    }

    // Body to body direction
    private float GetLookBodyCoefficient()
    {
        var userLook = new Vector3(headUser.forward.x, 0, headUser.forward.z);
        userLook.Normalize();
        var camilleLook = headUser.position - headCamille.position;
        camilleLook.y = 0;
        camilleLook.Normalize();
        return Vector3.Dot(camilleLook, userLook);
    }

    // Comparison to the vertical axis
    private float GetHeightCoefficient()
    {
        return Vector3.Dot(headUser.forward, Vector3.up);
    }


    // User eye to head position
    private float GetLookHeadCoefficient()
    {
        return Vector3.Dot((headUser.position - headCamille.position).normalized, headUser.forward);
    }

    public class LookAtCamilleEventArgs : EventArgs
    {
        public bool LookingBody;
        public bool LookingHead;
        public bool LookingEye;
    }
}