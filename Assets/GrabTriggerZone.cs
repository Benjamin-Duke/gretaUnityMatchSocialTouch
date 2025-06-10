// using UnityEngine;

// public class GrabTriggerZone : MonoBehaviour
// {
//     private AutoGrabAndRelease parentGrab;

//     void Start()
//     {
//         parentGrab = GetComponentInParent<AutoGrabAndRelease>();
//         Debug.Log(parentGrab != null);
//     }

//     void OnTriggerEnter(Collider other)
//     {
//         Debug.Log($"OnTriggerEnter: {other.name}");
//         Debug.Log($"Parent Grab: {parentGrab != null}");
//         Debug.Log($"Parent Hand Root: {parentGrab?.handRoot != null}");
//         if (parentGrab == null || parentGrab.handRoot == null)
//             return;
//         Debug.Log($"Other Transform Parent: {other.transform.parent != null}");
//         Debug.Log($"Other Transform Parent Hand Root: {other.transform.parent?.IsChildOf(parentGrab.handRoot.transform)}");
//         if (other.transform.IsChildOf(parentGrab.handRoot.transform))
//         {
//             parentGrab.TryGrab(other.transform);
//         }
//     }
// }
