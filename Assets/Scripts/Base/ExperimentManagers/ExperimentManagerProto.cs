using System.Collections;
using System.Collections.Generic;
using IntegratedAuthoringTool;
using UnityEngine;
using UnityEngine.InputSystem;
using WellFormedNames;

public class ExperimentManagerProto : MonoBehaviour
{
    #region Attributes
    
    [Tooltip("GameObject with a FAtiMAManager script, to send and receive actions from FAtiMA")]
    public GameObject fatimaManager;
    protected FAtiMAManager _fatimaManager;
    
    //Relevant agent's component we will need to instantiate the FAtiMA decisions.
    [Tooltip("GameObject with the GRETAnimationManager for the Agent Character")]
    public GameObject Agent;
    protected GRETAnimationManager _agentAnim;
    protected AudioSource _agentAs;
    
    //Current state of the scenario, to keep track of when we get into and out of the minigame.
    protected string _currentState = "Start";

    public bool touchCondition = false;
    
    public bool debug = true;
    
    #endregion
    
    #region MonoBehaviour methods
    
    // Start is called before the first frame update
    protected virtual void Start()
    {
        //Store the instance of the fatimaManager in our private variable.
        _fatimaManager = fatimaManager.GetComponent<FAtiMAManager>();
        if (_fatimaManager == null)
            Debug.LogError("No FAtiMAManager script found : won't send nor receive events to FAtiMA.");
        
        _agentAnim = Agent.GetComponent<GRETAnimationManager>();
        if (_agentAnim == null)
        {
            Debug.LogError("No GRETAnimationManager found : can't play FML files.");
        }
        
        _agentAs = Agent.GetComponentInChildren<AudioSource>();
        if (_agentAs == null)
        {
            Debug.LogError("No AudioSource found : agent speech may not work.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    #endregion
    
    #region Scenario Handling

    public virtual void StartStopScenario(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (debug) Debug.Log("Trying to start/stop the scenario.");
        if (_fatimaManager.IsScenarioStarted())
        {
            if (debug) Debug.Log("Stopping the scenario.");
            _fatimaManager.StopScenario();
        }
        else
        {
            if (debug) Debug.Log("Starting the scenario and getting an agent decision.");
            _fatimaManager.StartScenario();
            var actionArgs = new List<Name>();
            if (touchCondition)
                actionArgs.Add((Name)"Touch");
            else
            {
                actionArgs.Add((Name)"NoTouch");
            }
            HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
                "UpdateCondition",
                actionArgs
            )));
            ChangeCurrentState("Start");
            AgentDecision();
        }
    }

    protected virtual void ChangeCurrentState(string newState)
    {
        _currentState = newState;
    }
    
    public virtual void OnHumanReady(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
    }
    
    public virtual void OnHumanRefuse(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
    }
    
    public virtual void AgentTouched()
    {
        
    }
    
    public virtual void AgentTouch(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        StartCoroutine(PlayFML("TestTouch", "Speak", _fatimaManager.humanCharacter));
    }
    
    public virtual void PunctualAgentDecide(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (debug) Debug.Log("Manual activation of the decision making from the agent in case something goes wrong.");
        AgentDecision();
    }
    
    #endregion
    
    #region FAtiMA Handling
    
    protected virtual void AgentDecision()
    { 
        var action = _fatimaManager.DoAgentAction();
        var processedAction = _fatimaManager.ProcessAction(action);
        HandleProcessedAction(processedAction);
    }

    protected virtual void HandleProcessedAction(ProcessedFAtiMAAction processedAction)
    {
        switch (processedAction.identifier)
        {
            case IATConsts.DIALOG_ACTION_KEY:
                HandleSpeak(processedAction);
                ChangeCurrentState(processedAction.nextState);
                if (debug) Debug.Log("Current state : " + _currentState);
                break;
            case "Backchannel":
                //TODO
                break;
            default:
                break;
        }
    }

    protected virtual void HandleSpeak(ProcessedFAtiMAAction processedAction)
    {
        if (processedAction.target == _fatimaManager.humanCharacter)
        {
            StartCoroutine(PlayFML(processedAction.content, processedAction.identifier, processedAction.target));
        }
        else if (processedAction.target == _fatimaManager.agentCharacter)
        {
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
    
    protected virtual IEnumerator PlayFML(string filename, string actionName, string target)
    {
        if (target == _fatimaManager.humanCharacter)
        {
            if (actionName == IATConsts.DIALOG_ACTION_KEY)
                yield return new WaitUntil(() => !_agentAs.isPlaying);
            _agentAnim.PlayFml(filename, actionName);
        }
    }
}
