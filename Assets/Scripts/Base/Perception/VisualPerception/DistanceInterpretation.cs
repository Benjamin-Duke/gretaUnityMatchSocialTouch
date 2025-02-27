using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DistanceInterpretation : MonoBehaviour
{
    [SerializeField] [Tooltip("Transform of the first object to use.")]
    private Transform _target;

    [SerializeField] [Tooltip("Transform of the second object to use.")]
    private Transform _sensor;

    [SerializeField] [Tooltip("Print debug information")]
    private bool _debug;

    private Vector4 _classResults;

    // Keep in memory the current class so we
    // can trigger an event on change
    private string _currentClass;

    private float _distance;

    private float[] _partitionValues;

    private InterpretationClass _personalSpaceClass, _closeClass, _mediumClass, _farClass;

    private void Start()
    {
        _classResults = new Vector4(0, 0, 0, 0);

        // We set the partition values as described in the excel table.
        // Use the file to preview the corresponding linear partition.
        _partitionValues = new[] {0f, 0.4f, 0.52f, 1.1f, 1.34f, 3.5f, 3.9f, 10f};

        _personalSpaceClass = new InterpretationClass
        {
            parameters = new Vector4(Mathf.NegativeInfinity, Mathf.NegativeInfinity, _partitionValues[1],
                _partitionValues[2]),
            className = "Intimate"
        };
        _closeClass = new InterpretationClass
        {
            parameters =
                new Vector4(_partitionValues[1], _partitionValues[2], _partitionValues[3], _partitionValues[4]),
            className = "Personal"
        };
        _mediumClass = new InterpretationClass
        {
            parameters =
                new Vector4(_partitionValues[3], _partitionValues[4], _partitionValues[5], _partitionValues[6]),
            className = "Social"
        };
        _farClass = new InterpretationClass
        {
            parameters = new Vector4(_partitionValues[5], _partitionValues[6], Mathf.Infinity, Mathf.Infinity),
            className = "Public"
        };
    }

    private void Update()
    {
        var newDistance = GetDistance();
        if (Mathf.Abs(_distance - newDistance) < 0.0001f) return;
        _classResults = EvaluateDistance(newDistance);
        _distance = newDistance;
        var tempCurrentClass = GetMaxConfidenceClassName();
        if (tempCurrentClass != _currentClass)
            OnDistanceInterpretationChanged(new DistanceInterpretationEventArgs
                {DistanceInterpretationClass = tempCurrentClass});

        _currentClass = tempCurrentClass;
        if (_debug)
        {
            var message = "Distance interpretation results:\n" +
                          "Distance = " + _distance + " m\n" +
                          _personalSpaceClass.className + " Space = " + _classResults.x + "\n" +
                          _closeClass.className + " Space = " + _classResults.y + "\n" +
                          _mediumClass.className + " Space = " + _classResults.z + "\n" +
                          _farClass.className + " Space = " + _classResults.w + "\n";
            Debug.Log(message);
        }
    }

    public event EventHandler<DistanceInterpretationEventArgs> DistanceInterpretationChanged;

    public string GetCurrentClass()
    {
        return _currentClass;
    }

    private string GetMaxConfidenceClassName()
    {
        var results = new List<float> {_classResults.x, _classResults.y, _classResults.z, _classResults.w};
        var classNames = new List<string>
            {_personalSpaceClass.className, _closeClass.className, _mediumClass.className, _farClass.className};
        return classNames[results.IndexOf(results.Max())];
    }

    private void OnDistanceInterpretationChanged(DistanceInterpretationEventArgs e)
    {
        DistanceInterpretationChanged?.Invoke(this, e);
    }

    private float GetDistance()
    {
        return (_sensor.position - _target.position).magnitude;
    }

    private Vector4 EvaluateDistance(float value)
    {
        return new Vector4(
            _personalSpaceClass.EvaluateClass(_distance),
            _closeClass.EvaluateClass(_distance),
            _mediumClass.EvaluateClass(_distance),
            _farClass.EvaluateClass(_distance));
    }

    public class DistanceInterpretationEventArgs : EventArgs
    {
        public string DistanceInterpretationClass;
    }
}
