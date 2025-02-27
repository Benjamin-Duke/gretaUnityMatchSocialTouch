using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WellFormedNames;
using ActionLibrary;
using Newtonsoft.Json;
using System.IO;
using IntegratedAuthoringTool;
using AutobiographicMemory;

public class FAtiMAHumanControl : MonoBehaviour
{

    [Tooltip("GameObject with a FAtiMAManager script, to send and receive actions from FAtiMA")]
    public GameObject fatimaManager;

    [Tooltip("JSON file name of the prepared human simulated actions in StreamingAssets folder")]
    public string simulationFile;
    
    [Tooltip("JSON file name of the prepared interaction in StreamingAssets folder")]
    public string presetSimulationFile;

    [Tooltip("GameObject with the GRETAnimationManager for the Human Character")]
    public GameObject Human;

    [Tooltip("GameObject with the GRETAnimationManager for the Agent Character")]
    public GameObject Agent;

    public bool debug = false;

    private GRETAnimationManager HumAnim;
    private GRETAnimationManager AgentAnim;

    private AudioSource HumanAS;
    private AudioSource AgentAS;

    //The type to deserialize the json stored simulated actions to.
    private class FAtiMAction
    {
        public string actionName;

        public string[] actionArgs;

        public string actionType;
    }

    private class PresetActions
    {
        public string FMLfile;

        public string character;

        public string actionType;
    }
    //FAtiMA actions to execute during the simulation. They must be ordered from beginning to end in the json file.
    private List<FAtiMAction> simulatedActions = new List<FAtiMAction>();

    //Alternatively, if we want to play a prespecified scenario without decision model, we can store presetActions. They must be ordered from beginning to end in the json file.
    private List<PresetActions> presetActions = new List<PresetActions>();

    private FAtiMAManager _fatimaManager;

    //Index to enumerate over the simulatedActions list and allow for a reset at runtime.
    private int index = 0;

    private int prevIndex = -1;

    //Variable to monitor the estimated arousal of the human in FAtiMA  (necessary until we have an actual automated system to determine this)
    private int arousal = 1;

    private bool targetSet = false;

    public bool mockup = false;

    public string _touchAvoidance = "Low";    


    // Start is called before the first frame update
    void Start()
    {
        //Store the instance of the fatimaManager in our private variable.
        _fatimaManager = fatimaManager.GetComponent<FAtiMAManager>();
        if (_fatimaManager == null)
            Debug.LogError("No FAtiMAManager script found : won't send nor receive events to FAtiMA.");

        //Recuperate the path of the prepared simulated actions file and deserialize it.
        var scenarioPath = $"{Application.streamingAssetsPath}/{simulationFile}";
        
        var interactionPath = $"{Application.streamingAssetsPath}/{presetSimulationFile}";

        HumAnim = Human.GetComponent<GRETAnimationManager>();
        AgentAnim = Agent.GetComponent<GRETAnimationManager>();
        if (HumAnim == null || AgentAnim == null)
        {
            Debug.LogError("No GRETAnimationManager found : can't play FML files.");
        }

        HumanAS = Human.GetComponentInChildren<AudioSource>();
        AgentAS = Agent.GetComponentInChildren<AudioSource>();

        simulatedActions = JsonConvert.DeserializeObject<List<FAtiMAction>>(File.ReadAllText(scenarioPath));
        presetActions = JsonConvert.DeserializeObject<List<PresetActions>>(File.ReadAllText(interactionPath));
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            StartCoroutine(PlayFML("SimpleTouchR", "Touch", "Human"));
        }
        
        if (Input.GetKeyDown(KeyCode.N))
        {
            StartCoroutine(PlayFML("AngryHit", "Touch", "Human"));
        }
                    
        if (mockup)
        {
            if (Input.GetKeyDown(KeyCode.W))
            {
                StartCoroutine(PlayFML(presetActions[index].FMLfile, presetActions[index].actionType, presetActions[index].character));
                index++;
                prevIndex++;
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                if (prevIndex < 0)
                {
                    return;
                }
                StartCoroutine(PlayFML(presetActions[prevIndex].FMLfile, presetActions[prevIndex].actionType, presetActions[prevIndex].character));
            }

            if (Input.GetKeyDown(KeyCode.F))
            {
                if (!targetSet)
                {
                    AgentAnim.SetMoveTowardsTarget();
                    targetSet = true;
                }
                else
                {
                    AgentAnim.ResetMoveTowardsTarget();
                    targetSet = false;
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                index = 0;
                prevIndex = -1;
            }
        }
        else
        {
            // Start or stop the simulation on FAtiMA's side.
            if (Input.GetKeyDown(KeyCode.S))
            {
                if (_fatimaManager.IsScenarioStarted())
                {
                    _fatimaManager.StopScenario();
                    index = 0;
                    prevIndex = -1;
                }
                else
                {
                    _fatimaManager.StartScenario();
                }
            }
            
            //Press W successively to perform the prepared actions of the simulated human in order.
            if (Input.GetKeyDown(KeyCode.W))
            {
                if (simulatedActions[index] != null)
                {
                    var actionH = SendActionFromHumanToAgent(simulatedActions[index].actionName, ActionArgsToList(simulatedActions[index].actionArgs), simulatedActions[index].actionType);
                    StartCoroutine(PlayFML(actionH.content, actionH.identifier, actionH.target));
                    index++;
                    if (actionH.identifier == IATConsts.DIALOG_ACTION_KEY)
                    {
                        StartCoroutine(HandleHumanSpeech());
                    }
                }
            }
            
            //Press E to initiate a decision from Camille.
            if (Input.GetKeyDown(KeyCode.E))
            {
                var actionA = AgentReact();
                StartCoroutine(PlayFML(actionA.content, actionA.identifier, actionA.target));
            }
            
            //Send the current arousal value to FAtiMA
            if (Input.GetKeyDown(KeyCode.A))
            {
                var actionArgs = new List<Name> {(Name)arousal.ToString()};
                var actionArgsTA = new List<Name> {(Name)_touchAvoidance};

                _fatimaManager.DoHumanAction(
                    "Arousal",
                    actionArgs,
                    AMConsts.ACTION_END
                );
                
                _fatimaManager.DoHumanAction(
                    "TouchAvoidance",
                    actionArgsTA,
                    AMConsts.ACTION_END
                );
            }
            
            //Increase current temp arousal by 1
            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (arousal < 7)
                    arousal++;
                if (debug)
                {
                    Debug.Log("Arousal : " + arousal.ToString());
                }
            }
    
            //Reduce current temp arousal by 1
            if (Input.GetKeyDown(KeyCode.X))
            {
                if (arousal > 1)
                    arousal--;
                if (debug)
                {
                    Debug.Log("Arousal : " + arousal.ToString());
                }
            }
        }
    }

    #region Helpers

    //Automatically fill the targets and subjects of the action with the direction Human -> Agent.
    private ProcessedFAtiMAAction SendActionFromHumanToAgent(string actionName, IEnumerable<Name> actionArgs, string actionType)
    {
        if (_fatimaManager is null) return null;

        Name action = _fatimaManager.DoHumanAction(actionName, actionArgs, actionType);
        var processedAction = _fatimaManager.ProcessAction(action);
        if (debug)
            Debug.Log(processedAction.identifier + " : " + processedAction.content);
        //test the type of the action and call the appropriate PlayFML methode of the corresponding GRETAnimationCharacter.

        return processedAction;
    }

    //Automatically call for an Agent (Camille) decision.
    private ProcessedFAtiMAAction AgentReact()
    {
        if (_fatimaManager is null) return null;

        Name action = _fatimaManager.DoAgentAction();
        var processedAction = _fatimaManager.ProcessAction(action);
        if (debug)
            Debug.Log(processedAction.identifier + " : " + processedAction.content);
        //test the type of the action and call the appropriate PlayFML methode of the corresponding GRETAnimationCharacter.

        return processedAction;
    }

    private IEnumerator HandleHumanSpeech()
    {
        yield return new WaitUntil(() => HumanAS.isPlaying);
        List<Name> actionArgs = new List<Name>();
        actionArgs.Add((Name)"True");

        _fatimaManager.DoHumanAction(
            "Speech",
            actionArgs,
            AMConsts.ACTION_END
        );
        yield return new WaitUntil(() => !HumanAS.isPlaying);

        actionArgs.Clear();
        actionArgs.Add((Name)"False");

        _fatimaManager.DoHumanAction(
            "Speech",
            actionArgs,
            AMConsts.ACTION_END
        );
    }

    private IEnumerator PlayFML(string filename, string actionName, string target)
    {
        if (target == "Human")
        {
            if (actionName == IATConsts.DIALOG_ACTION_KEY)
                yield return new WaitUntil(() => !HumanAS.isPlaying);
            AgentAnim.PlayFml(filename, actionName);
        }
        else if (target == "Camille")
        {
            if (actionName == IATConsts.DIALOG_ACTION_KEY)
                yield return new WaitUntil(() => !AgentAS.isPlaying);
            HumAnim.PlayFml(filename, actionName);
        }
    }

    //Helper to quickly format a list of strings as a list of Names for FAtiMA arguments.
    private static IEnumerable<Name> ActionArgsToList(IEnumerable<string> args)
    {
        var actionArgs = new List<Name>();
        foreach (var arg in args)
        {
            actionArgs.Add((Name)arg);
        }
        return actionArgs;
    }
    #endregion
}
