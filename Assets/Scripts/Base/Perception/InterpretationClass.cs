using UnityEngine;

// Linear class for fuzzy logic
public struct InterpretationClass
{
    public Vector4 parameters;
    public string className;

    public float EvaluateClass(float value)
    {
        var result = 0f;
        if (value <= parameters.x || value >= parameters.w)
            result = 0f;
        else if (value >= parameters.y && value <= parameters.z)
            result = 1f;
        else if (value < parameters.y)
            result = (value - parameters.x) / (parameters.y - parameters.x);
        else if (value > parameters.z)
            result = (value - parameters.w) / (parameters.z - parameters.w);
        return result;
    }
}