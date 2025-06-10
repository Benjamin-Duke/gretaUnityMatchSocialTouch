using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class MeshColliderVisualizer : MonoBehaviour
{
    void Start()
    {
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider != null && collider.sharedMesh == null)
        {
            Debug.LogWarning("MeshCollider does not have a shared mesh assigned.");
        }
    }
    void OnDrawGizmos()
    {
        MeshCollider collider = GetComponent<MeshCollider>();
        if (collider != null && collider.sharedMesh != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireMesh(collider.sharedMesh, transform.position, transform.rotation, transform.lossyScale);
        }
    }
}
