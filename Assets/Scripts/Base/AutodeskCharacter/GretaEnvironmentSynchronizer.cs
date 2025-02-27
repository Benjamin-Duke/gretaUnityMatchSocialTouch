using System.Collections.Generic;
using thriftImpl;
using UnityEngine;

/// <summary>
///     Behaviour script to synchronize objects in GRETA.
/// </summary>
public class GretaEnvironmentSynchronizer : MonoBehaviour
{
    /// <summary>The animation script linked to the GRETA agent we want to add behaviours to.</summary>
    public GretaCharacterAnimator CharacterAnimScript;

    /// <summary>
    ///     The objects which's positions, orientations and scales have to be synchronized and reproduced in the GRETA
    ///     environment.
    /// </summary>
    public List<GameObject> synchronizedObjects = new List<GameObject>();

    /// <summary>The Thrift command sender linked to our GRETA instance.</summary>
    private CommandSender _commandSender;

    /// <summary>
    ///     Indicates whether we've done the initialization of the objects synchronized in GRETA or not yet.<br />
    ///     This way, we give the object's initial position once, and then just synchronize the ones who changed.
    /// </summary>
    private bool _instantiated;

    private void Start()
    {
        _commandSender = CharacterAnimScript.commandSender;

        var synchronizedObjectsCopy = new List<GameObject>(synchronizedObjects);
        foreach (var synchronizedObject in synchronizedObjectsCopy)
            InsertObjectsToGazeAt(synchronizedObject, synchronizedObjects);

        foreach (var synchronizedObject in synchronizedObjects) synchronizedObject.transform.hasChanged = false;
    }

    private void LateUpdate()
    {
        // Using late update so that the position values we send are taken after all possible calculations (physics, etc).

        if (!_instantiated)
        {
            if (!_commandSender.isConnected()) return;

            // Initialise the GRETA environment if it hasn't been done before.
            foreach (var synchronizedObject in synchronizedObjects)
            {
                _commandSender.NotifyObject(synchronizedObject);
                synchronizedObject.transform.hasChanged = false;
            }

            _instantiated = true;
        }
        else
        {
            foreach (var synchronizedObject in synchronizedObjects)
                // If the synchronized object has changed since the last frame, update the GRETA Environment.
                if (synchronizedObject.transform.hasChanged)
                {
                    _commandSender.NotifyObject(synchronizedObject);
                    synchronizedObject.transform.hasChanged = false;
                }
        }
    }

    private void InsertObjectsToGazeAt(GameObject gameObject, List<GameObject> synchronizedObjects)
    {
        var metadata = gameObject.GetComponent<GretaObjectMetadata>();
        if (metadata != null)
        {
            var objectToGazeAt = metadata.objectToGazeAt;
            if (objectToGazeAt != null)
                if (!synchronizedObjects.Contains(objectToGazeAt))
                {
                    synchronizedObjects.Add(objectToGazeAt);
                    InsertObjectsToGazeAt(objectToGazeAt, synchronizedObjects);
                }
        }
    }
}