using UnityEngine;

public class LookDirection : MonoBehaviour
{
    [SerializeField] [Tooltip("Transform corresponding to the target the sensor looks at.")]
    private Transform target;

    [SerializeField]
    [Tooltip(
        "Transform corresponding to the user looking at the target.\nThe forward direction is used as the look direction.")]
    private Transform sensor;

    [SerializeField]
    [Tooltip(
        "Float corresponding to the look threshold. \nThe value should be between -1 (all directions accepted) and 1 (look direction must be perfectly aligned with the target).\n")]
    private float perceptionThreshold = 0.9f;

    [SerializeField] [Tooltip("Print debug information")]
    private bool _debug;
    
    private float _dotProd;


    private Vector3 _previousForward, _previousPosition;
    public bool isLookingAtTarget { get; private set; }

    public float isLookingAtTargetCoef { get; private set; }

    private void Start()
    {
        _previousForward = sensor.forward;
        _previousPosition = sensor.position;
    }

    private void Update()
    {
        if (1 - Vector3.Dot(_previousForward, sensor.forward) < 0.0001f &&
            (_previousPosition - sensor.position).magnitude < 0.0001f) return;
        _previousForward = sensor.forward;
        _previousPosition = sensor.position;
        InterpretCoef();
        if (_debug)
            Debug.Log("Look direction with the sensor " + sensor.name + " and the target " + target.name + ":\n" +
                      "Boolean result = " + isLookingAtTarget + "\n" +
                      "Coefficient result = " + isLookingAtTargetCoef);
    }

    private void InterpretCoef()
    {
        _dotProd = GetLookCoefficient();

        isLookingAtTarget = _dotProd < -1 * perceptionThreshold;
        if (isLookingAtTarget)
            isLookingAtTargetCoef = (-_dotProd - perceptionThreshold) / (1 - perceptionThreshold);
        else isLookingAtTargetCoef = 0f;
    }

    private float GetLookCoefficient()
    {
        return Vector3.Dot((sensor.position - target.position).normalized, sensor.forward);
    }
}