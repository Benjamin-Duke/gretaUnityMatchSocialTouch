using UnityEngine;
using Valve.VR;

public class PlayerStartPosition : MonoBehaviour
{
    public Transform startPoint; // Drag ton point de départ ici dans l'inspecteur

    void Start()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
            transform.rotation = startPoint.rotation;
        }
    }
}
