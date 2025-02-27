using System.Collections.Generic;
using UnityEngine;

// This script changes the positions of the bones of the visible hand so that they are the same as those of the invisible physical hand given
// in reference in the inspector.
public class VisibleHandManager : MonoBehaviour
{
    [Tooltip("The physical hand that this visible hand should follow.")]
    [SerializeField] public GameObject invisiblePhysicalHand;

    [SerializeField] private List<GameObject> invisiblePhysicalHandBones;
    // Start is called before the first frame update
    void Start()
    {
        if (invisiblePhysicalHand != null)
        {
            foreach (Transform physicalHandBone in invisiblePhysicalHand.transform)
            {
                invisiblePhysicalHandBones.Add(physicalHandBone.gameObject);
            }

            SetAllChildrenLovers(gameObject);
        }
    }

    // The script works on the assumption that the bones of the visible hand are named like the bones of the physical hand.
    // We might want to use a more robust method of attribution in later developments.
    private void SetAllChildrenLovers(GameObject obj)
    {
        foreach (Transform childBone in obj.transform)
        {
            SetAllChildrenLovers(childBone.gameObject);
        }
        var loverScript = obj.GetComponent<VisibleHandLovers>();
        if (loverScript == null) return;
        var correspondingBone = invisiblePhysicalHandBones.Find(t => t.ToString() == obj.ToString() || t.ToString() == obj.ToString().Substring(2));
        if (correspondingBone == null) return;
        loverScript.SetLover(correspondingBone.transform);
    }
}
