using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using IntegratedAuthoringTool;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations.Rigging;
using UnityEngine.Serialization;
using UnityStandardAssets.Characters.ThirdPerson;

static class RandomExtensions
{
    public static void Shuffle<T> (this System.Random rng, T[] array)
    {
        var n = array.Length;
        while (n > 1) 
        {
            var k = rng.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }
}
public class GRETAnimationManager : MonoBehaviour
{
    //public bool FollowHeadOnTouch;

    [FormerlySerializedAs("UserHead")] public Transform userHead;
    public bool isAgent = false;
    public Transform ovrtrnsfrm;
    private GretaCharacterAnimator _gretaAnim;
    private AudioSource _audioSource;
    private HeadLookController _headLookController;
    private AICharacterControl _aiCharacterControl;
    private RigBuilder _rig;
    private NavMeshAgent _navMeshAgent;

    [Tooltip("Language of the interaction (FR or EN)")]
    public string lang = "FR";
    
    [Tooltip("Experiment specific folder")]
    public string expFolder = "FirstExperiment";

    //Used to manage the variability of minor reactions for this experiment
    //Ideally, this should instead be in a more specific class dedicated to a specific experiment
    private Dictionary<string, Stack<string>> _minorReactionsFmLs = new Dictionary<string, Stack<string>>();

    [Tooltip("Names of folders containing minor reactions FMLs")]
    public List<string> minorReactionsFolders;

    // Use this for initialization
    private void Start()
    {
        //gretaAnim = GameObject.FindWithTag("GretAnimationRoot").GetComponent<GretaCharacterAnimator>();
        _gretaAnim = GetComponentInChildren<GretaCharacterAnimator>();
        _audioSource = GetComponentInChildren<AudioSource>();
        _headLookController = GetComponent<HeadLookController>();
        _aiCharacterControl = GetComponent<AICharacterControl>();
        _rig = GetComponentInChildren<RigBuilder>();
        _rig!.enabled = false;
        //_ovrtrnsfrm = GetComponentInChildren<OverrideTransform>();
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _aiCharacterControl.target = userHead;
        
        InitializeAllMinorReactions();
    }

    // Update is called once per frame
    private void Update()
    {
        
    }

    // Touch on a global collider, start to follow user (hands and eyes)
    /*public void OnTriggerEnter(Collider other)
    {
        if (FollowHeadOnTouch)
        {
            GetComponent<AICharacterControl>().target = UserHead;
            GetComponent<HeadLookController>().target = UserHead;
        }
    }*/

    private void InitializeAllMinorReactions()
    {
        _minorReactionsFmLs.Clear();

        foreach (var folder in minorReactionsFolders)
        {
            ResetMinorReaction(folder);
        }
        
        /*_minorReactionsFMLs.Clear();
        
        var success = Application.streamingAssetsPath + "/FMLs/" + expFolder + "/" + lang + "/MinorSuccess/";
        var combo = Application.streamingAssetsPath + "/FMLs/" + expFolder + "/" + lang + "/MinorCombo/";
        var fell = Application.streamingAssetsPath + "/FMLs/" + expFolder + "/" + lang + "/MinorFell/";

        var rng = new System.Random();
        var tempArray = CleanFileName(Directory.GetFiles(success, "*.xml").Select(Path.GetFileName).ToArray());
        rng.Shuffle(tempArray);
        _minorReactionsFMLs.Add("MinorSuccess", new Stack<string>(tempArray));
        
        tempArray = CleanFileName(Directory.GetFiles(combo, "*.xml").Select(Path.GetFileName).ToArray());
        rng.Shuffle(tempArray);
        _minorReactionsFMLs.Add("MinorCombo", new Stack<string>(tempArray));
        
        tempArray = CleanFileName(Directory.GetFiles(fell, "*.xml").Select(Path.GetFileName).ToArray());
        rng.Shuffle(tempArray);
        _minorReactionsFMLs.Add("MinorFell", new Stack<string>(tempArray));*/
        
    }
    
    private void ResetMinorReaction(string minorReaction)
    {
        
        var reactionToReset = Application.streamingAssetsPath + "/FMLs/" + expFolder + "/" + lang + "/" + minorReaction + "/";

        var rng = new System.Random();
        var tempArray = CleanFileName(Directory.GetFiles(reactionToReset, "*.xml").Select(Path.GetFileName).ToArray());
        rng.Shuffle(tempArray);
        if (_minorReactionsFmLs.ContainsKey(minorReaction))
            _minorReactionsFmLs[minorReaction] = new Stack<string>(tempArray);
        else
        {
            _minorReactionsFmLs.Add(minorReaction, new Stack<string>(tempArray));
        }
        
    }

    private string[] CleanFileName(IReadOnlyCollection<string> array)
    {
        var newArray = new string[array.Count];
        var i = 0;
        foreach (var str in array)
        {
            newArray[i] = str.Replace(".xml", "");
            i++;
        }

        return newArray;
    }

    public void SetHeadLookTarget()
    {
        _headLookController.target = userHead;
    }

    public void SetMoveTowardsTarget()
    {
        //_aiCharacterControl.target = UserHead;
        _navMeshAgent.stoppingDistance = 0.9f;
    }

    public void ResetHeadLookTarget()
    {
        _headLookController.target = null;
    }

    public void ResetMoveTowardsTarget()
    {
        //_aiCharacterControl.target = null;
        _navMeshAgent.stoppingDistance = 2.5f;
    }

    IEnumerator ProcessFml(string fileName, string actionName)
    {
        var filepath = Application.streamingAssetsPath + "/FMLs/" + expFolder + "/";
        if (actionName == "Dialog")//if (actionName == IATConsts.DIALOG_ACTION_KEY)
        {
            filepath += lang + "/";
            yield return new WaitUntil(() => !_audioSource.isPlaying);
        }
        //To deal with minor reactions we need to select a random file corresponding to this reaction type
        var exactFileName = fileName;
        Debug.Log("!! Playing FML file : " + exactFileName + " at path: " + filepath + exactFileName);
        if (fileName.Contains("Minor"))
        {
            exactFileName = exactFileName.Replace("Gesture", "");
            filepath += exactFileName + "/";
            if (_minorReactionsFmLs[exactFileName].Count < 1)
                ResetMinorReaction(exactFileName);
            exactFileName = _minorReactionsFmLs[exactFileName].Pop();

        }

        if (fileName.Contains("MinigameEnded"))
        {
            InitializeAllMinorReactions();
        }
        //SetHeadLookTarget();
        if (isAgent)
        {
            if (fileName.Contains("Touch"))
            {
                StartCoroutine(fileName.Contains("PlayAgain")
                    ? HandleTouchSynchronization(true)
                    : HandleTouchSynchronization(false));
            }
                
        }

        Debug.Log("!! Playing FML file : " + exactFileName);
        _gretaAnim.PlayAgentAnimation(filepath + exactFileName);
        if (actionName == IATConsts.DIALOG_ACTION_KEY || exactFileName.Contains("Touch"))
            yield return new WaitUntil(() => _audioSource.isPlaying);
        while (_audioSource.isPlaying)
        {
                yield return null;
        }
        ResetMoveTowardsTarget();
        ovrtrnsfrm.rotation = Quaternion.Euler(0,0, 0);
        _rig.enabled = false;
        if (fileName.Contains("Touch"))
            PlayFml("ResetPose", "ResetPose");
    }

    public void PlayFml(string fileName, string actionName)
    {
        StartCoroutine(ProcessFml(fileName, actionName));
    }

    private IEnumerator HandleTouchSynchronization(bool limited)
    {
        SetMoveTowardsTarget();
        yield return new WaitUntil(() => _navMeshAgent.remainingDistance <= 1f);
        ovrtrnsfrm.rotation = Quaternion.Euler(0,-90, 0);
        _rig!.enabled = true;
        if (limited)
        {
            yield return new WaitForSeconds(4);
            ResetMoveTowardsTarget();
            ovrtrnsfrm.rotation = Quaternion.Euler(0,0, 0);
            _rig.enabled = false;
        }
            
    }
}