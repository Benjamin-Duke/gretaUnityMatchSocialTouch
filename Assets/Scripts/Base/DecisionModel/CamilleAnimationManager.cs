using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

public class CamilleAnimationManager : MonoBehaviour
{
    public bool FollowHeadOnTouch;

    public Transform UserHead;
    public GameObject audioSourceGO;
    private GretaCharacterAnimator gretaAnim;

    // Use this for initialization
    private void Start()
    {
        //gretaAnim = GameObject.FindWithTag("GretAnimationRoot").GetComponent<GretaCharacterAnimator>();
        gretaAnim = GetComponentInChildren<GretaCharacterAnimator>();
        //audioSourceGO = GameObject.FindWithTag("CamilleHead");
    }

    // Update is called once per frame
    private void Update()
    {
        /*if (Input.GetKeyDown(KeyCode.J))
            PlayFML("Joy");
        else if (Input.GetKeyDown(KeyCode.S))
            PlayFML("Sad");*/
        if (Input.GetKeyDown(KeyCode.T))
            PlayFML("JoyTap");
        /*else if (Input.GetKeyDown(KeyCode.H)) 
            PlayFML("AngryHit");*/
    }

    // Touch on a global collider, start to follow user (hands and eyes)
    public void OnTriggerEnter(Collider other)
    {
        if (FollowHeadOnTouch)
        {
            GetComponent<AICharacterControl>().target = UserHead;
            GetComponent<HeadLookController>().target = UserHead;
        }
    }

    public void PlayFML(string fileName)
    {
        if (audioSourceGO != null)
        {
            var audioS = audioSourceGO.GetComponent<AudioSource>();
            if (!audioS.isPlaying)
            {
                Debug.Log("!! " + fileName);
                gretaAnim.PlayAgentAnimation(Application.streamingAssetsPath + "/FMLs/" + fileName);
            }
        }
    }
}