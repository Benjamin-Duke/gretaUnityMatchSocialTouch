using UnityEngine;
using FMODUnity;

public class FMODParameterController : MonoBehaviour
{
    [Tooltip("Le Studio Event Emitter ciblé")]
    public StudioEventEmitter targetEmitter;

    public string parameterName = "soundChoice";
    [Range(0, 7)]
    public int parameterValue = 0;

    void Start()
    {
        if (targetEmitter != null && targetEmitter.EventInstance.isValid())
        {
            targetEmitter.SetParameter(parameterName, parameterValue);
        }
    }

    void OnValidate()
    {
        if (targetEmitter != null && targetEmitter.EventInstance.isValid())
        {
            targetEmitter.SetParameter(parameterName, parameterValue);
        }
    }

    void Update()
    {
        if (targetEmitter != null && targetEmitter.EventInstance.isValid())
        {
            targetEmitter.SetParameter(parameterName, parameterValue);
        }
    }
}
