using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class ArmContact : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private int animatorLayer = 2;

    [Header("FMOD Events")]
    [SerializeField] private EventReference strokeEvent;
    [SerializeField] private EventReference rubbingEvent;
    [SerializeField] private EventReference tapEvent;
    [SerializeField] private EventReference hitEvent;

    [Header("FMOD Parameters")]
    [SerializeField] private bool isChoiceEnabled = false;

    [Range(0, 7)]
    [SerializeField] private int soundChoiceID = 0;

    private EventInstance strokeInstance;
    private EventInstance rubbingInstance;
    private EventInstance tapInstance;
    private EventInstance hitInstance;

    private string Caresse = "Caresse";
    private string Frot = "Frottement";
    private string Tap = "Tapotement";
    private string Hit = "Frappe";

    private int caresseSoundID = 0;
    private int frotSoundID = 0;
    private int tapSoundID = 0;
    private int hitSoundID = 0;

    void Start()
    {
        strokeInstance = RuntimeManager.CreateInstance(strokeEvent);
        rubbingInstance = RuntimeManager.CreateInstance(rubbingEvent);
        tapInstance = RuntimeManager.CreateInstance(tapEvent);
        hitInstance = RuntimeManager.CreateInstance(hitEvent);
    }

    void Update()
    {
        if (isChoiceEnabled)
        {
            strokeInstance.setParameterByName("soundChoice", soundChoiceID);
            rubbingInstance.setParameterByName("soundChoice", soundChoiceID);
            tapInstance.setParameterByName("soundChoice", soundChoiceID);
            hitInstance.setParameterByName("soundChoice", soundChoiceID);
            caresseSoundID = soundChoiceID;
            frotSoundID = soundChoiceID;
            tapSoundID = soundChoiceID;
            hitSoundID = soundChoiceID;
        }
    }

    public void SetSoundIDs(string animName, int soundChoice)
    {
        if (animName == "caresse") caresseSoundID = soundChoice;
        else if (animName == "frot") frotSoundID = soundChoice;
        else if (animName == "tap") tapSoundID = soundChoice;
        else if (animName == "hit") hitSoundID = soundChoice;
    }

    private void OnTriggerEnter(Collider other)
    {

        if (!(other.CompareTag("CamilleCollision") || other.CompareTag("CamilleEtheral"))) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(animatorLayer);
        //Debug.Log("Current animator state: " + stateInfo.fullPathHash);
        if (stateInfo.IsName("caresse"))
        {
            if (!IsPlaying(strokeInstance))
            {
                Debug.Log("Caresse sound played");
                strokeInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                strokeInstance.release();
                strokeInstance = RuntimeManager.CreateInstance(strokeEvent);
                strokeInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
                strokeInstance.setParameterByName("soundChoice", caresseSoundID);
                strokeInstance.start();
                TCPManager.Instance?.EnqueueData($"{Caresse}:{caresseSoundID}");
            }
        }
        else if (stateInfo.IsName("frot"))
        {
            if (!IsPlaying(rubbingInstance))
            {
                Debug.Log("Rubbing sound played");
                rubbingInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                rubbingInstance.release();
                rubbingInstance = RuntimeManager.CreateInstance(rubbingEvent);
                rubbingInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
                rubbingInstance.setParameterByName("soundChoice", frotSoundID);
                rubbingInstance.start();
                TCPManager.Instance?.EnqueueData($"{Frot}:{frotSoundID}");
            }
        }
        else if (stateInfo.IsName("tap"))
        {
            if (!IsPlaying(tapInstance))
            {
                Debug.Log("Tap sound played");
                tapInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                tapInstance.release();
                tapInstance = RuntimeManager.CreateInstance(tapEvent);
                tapInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
                tapInstance.setParameterByName("soundChoice", tapSoundID);
                tapInstance.start();
                TCPManager.Instance?.EnqueueData($"{Tap}:{tapSoundID}");
            }
        }
        else if (stateInfo.IsName("hit"))
        {
            if (!IsPlaying(hitInstance))
            {
                Debug.Log("Hit sound played");
                hitInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                hitInstance.release();
                hitInstance = RuntimeManager.CreateInstance(hitEvent);
                hitInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
                hitInstance.setParameterByName("soundChoice", hitSoundID);
                hitInstance.start();
                TCPManager.Instance?.EnqueueData($"{Hit}:{hitSoundID}");
            }
        }
    }

    private bool IsPlaying(EventInstance instance)
    {
        PLAYBACK_STATE state;
        instance.getPlaybackState(out state);
        return state == PLAYBACK_STATE.PLAYING;
    }

    void OnDestroy()
    {
        strokeInstance.release();
        rubbingInstance.release();
        tapInstance.release();
        hitInstance.release();
    }
}
