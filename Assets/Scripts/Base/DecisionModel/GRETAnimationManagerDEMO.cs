using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.ThirdPerson;

public class GRETAnimationManagerDEMO : MonoBehaviour
{
    public Transform UserHead;
    private GretaAnimatorBridge gretaAnim;
    private AudioSource audioSource;
    private HeadLookController _headLook;
    private AICharacterControl _navAgent;

    // Start is called before the first frame update
    void Start()
    {
        gretaAnim = GetComponent<GretaAnimatorBridge>();
        audioSource = GetComponentInChildren<AudioSource>();
        _headLook = GetComponent<HeadLookController>();
        _navAgent = GetComponent<AICharacterControl>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetHeadLookTarget()
    {
        _headLook.target = UserHead;
    }

    public void SetMoveTowardsTarget()
    {
        _navAgent.target = UserHead;
    }

    public void ResetHeadLookTarget()
    {
        _headLook.target = null;
    }

    public void ResetMoveTowardsTarget()
    {
        _navAgent.target = null;
    }

    IEnumerator GoToTargetIfSpeakTouch(string fileName)
    {
        yield return new WaitUntil(() => !audioSource.isPlaying);
        if (fileName.Contains("Touch"))
        {
            SetMoveTowardsTarget();
            Debug.Log("!! Playing FML file : " + fileName);
            gretaAnim.PlayAgentAnimation(Application.streamingAssetsPath + "/FMLs/" + fileName);
            yield return new WaitUntil(() => audioSource.isPlaying);
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            ResetMoveTowardsTarget();
        }
        else
        {
            Debug.Log("!! Playing FML file : " + fileName);
            gretaAnim.PlayAgentAnimation(Application.streamingAssetsPath + "/FMLs/" + fileName);
            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }

    }

    IEnumerator GoToTargetIfTouch(string fileName)
    {
        //SetHeadLookTarget();
        if (fileName.Contains("Touch"))
        {
            SetMoveTowardsTarget();
            Debug.Log("!! Playing FML file : " + fileName);
            gretaAnim.PlayAgentAnimation(Application.streamingAssetsPath + "/FMLs/" + fileName);
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            ResetMoveTowardsTarget();
        }
        else
        {
            Debug.Log("!! Playing FML file : " + fileName);
            gretaAnim.PlayAgentAnimation(Application.streamingAssetsPath + "/FMLs/" + fileName);
            while (audioSource.isPlaying)
            {
                yield return null;
            }
        }

    }

    public void PlayFMLMove(string fileName)
    {
        //var audioS = audioSourceGO.GetComponent<AudioSource>();
        /*if (!audioS.isPlaying)
        {
            Debug.Log("!! Playing FML file : " + fileName);
            gretaAnim.PlayAgentAnimation(Application.streamingAssetsPath + "/FMLs/" + fileName);
        }*/
        StartCoroutine(GoToTargetIfTouch(fileName));
        
        Debug.Log("!! Playing FML file : " + fileName);
        gretaAnim.PlayAgentAnimation(Application.streamingAssetsPath + "/FMLs/" + fileName);

    }

    public void PlayFML(string fileName)
    {
        if (!audioSource.isPlaying)
        {
            Debug.Log("!! Playing FML file : " + fileName);
            gretaAnim.PlayAgentAnimation(fileName);
        }
    }
}
