using UnityEngine;
using FMODUnity;

public class ArmContact : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private int animatorLayer = 2;

    [Header("FMOD Emitters")]
    [SerializeField] private StudioEventEmitter strokeEmitter;
    [SerializeField] private StudioEventEmitter hitEmitter;

    private int caresseID = 1;
    private int hitID = 3;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("CamilleCollision"))
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);

            if (stateInfo.IsName("caresse"))
            {
                if (strokeEmitter.IsPlaying()){return;}
                else
                {
                    strokeEmitter.Stop();
                    strokeEmitter.Play();
                    if(TCPManager.Instance != null)
                    {
                        TCPManager.Instance.EnqueueData(caresseID.ToString());
                    }
                }
            }
            else if (stateInfo.IsName("hit"))
            {
                if (hitEmitter.IsPlaying()){return;}
                else
                {
                    hitEmitter.Stop();
                    hitEmitter.Play();
                    if(TCPManager.Instance != null)
                    {
                        TCPManager.Instance.EnqueueData(hitID.ToString());
                    }
                }
            }
            else
            {
                // Si aucune animation connue n'est en cours, on peut choisir de ne rien faire ou de gérer un autre cas
                Debug.Log("Aucune animation connue en cours.");
            }
        }
    }
}
// using FMODUnity;
// using FMOD.Studio;
// using UnityEngine;

// public class ArmContact : MonoBehaviour
// {
//     [SerializeField] private Animator animator;
//     [SerializeField] private int animatorLayer = 2;

//     [Header("FMOD Events")]
//     [SerializeField] private EventReference strokeEvent;
//     [SerializeField] private EventReference hitEvent;

//     private EventInstance strokeInstance;
//     private EventInstance hitInstance;

//     private int caresseID = 1;
//     private int hitID = 3;

//     private bool hasSentCaresse = false;
//     private bool hasSentHit = false;

//     void Start()
//     {
//         strokeInstance = RuntimeManager.CreateInstance(strokeEvent);
//         hitInstance = RuntimeManager.CreateInstance(hitEvent);
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         if (!other.CompareTag("CamilleCollision")) return;

//         AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);

//         if (stateInfo.IsName("caresse"))
//         {
//             strokeInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
//             strokeInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
//             strokeInstance.start();

//             if (!hasSentCaresse)
//             {
//                 if (TCPManager.Instance != null)
//                 {
//                     TCPManager.Instance.EnqueueData(caresseID.ToString());
//                 }
//                 hasSentCaresse = true;
//                 hasSentHit = false; // reset pour pouvoir renvoyer plus tard si anim change
//             }
//         }
//         else if (stateInfo.IsName("hit"))
//         {
//             hitInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
//             hitInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));
//             hitInstance.start();

//             if (!hasSentHit)
//             {
//                 if (TCPManager.Instance != null)
//                 {
//                     TCPManager.Instance.EnqueueData(hitID.ToString());
//                 }
//                 hasSentHit = true;
//                 hasSentCaresse = false; // reset
//             }
//         }
//     }

//     void OnDestroy()
//     {
//         strokeInstance.release();
//         hitInstance.release();
//     }
// }

