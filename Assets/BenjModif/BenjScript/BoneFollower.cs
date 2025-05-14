using UnityEngine;

public class BoneFollower : MonoBehaviour
{
    public Transform animatedRoot;      // Le root du personnage animé
    public Transform colliderSkeleton;  // Le root du squelette de colliders

    void LateUpdate()
    {
        SyncBones(animatedRoot, colliderSkeleton);
    }

    void SyncBones(Transform source, Transform target)
    {
        target.position = source.position;
        target.rotation = source.rotation;

        for (int i = 0; i < source.childCount; i++)
        {
            Transform srcChild = source.GetChild(i);
            Transform tgtChild = target.Find(srcChild.name);

            if (tgtChild != null)
            {
                SyncBones(srcChild, tgtChild);
            }
        }
    }
}
