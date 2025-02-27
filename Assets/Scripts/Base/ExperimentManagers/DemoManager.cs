using System.Collections;
using System.Collections.Generic;
using AutobiographicMemory;
using IntegratedAuthoringTool;
using Minigame;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using WellFormedNames;

public class DemoManager : MonoBehaviour
{
    #region Attributes
    [Tooltip("GameObject with a FAtiMAManager script, to send and receive actions from FAtiMA")]
    public GameObject fatimaManager;
    private FAtiMAManager _fatimaManager;
    
    //Relevant agent's component we will need to instantiate the FAtiMA decisions.
    [Tooltip("GameObject with the GRETAnimationManager for the Agent Character")]
    public GameObject Agent;
    private GRETAnimationManager _agentAnim;
    private AudioSource _agentAs;

    //Current state of the scenario, to keep track of when we get into and out of the minigame.
    private string _currentState;
    
    public InputAction testTouch;
    public InputAction testStartSc;
    public InputAction testDecide;

    public bool debug = true;
    
    #endregion
    
    #region MonoBehaviour methods
    
    // Start is called before the first frame update
    void Start()
    {
        testTouch.performed += ctx => AgentTouch();
        testStartSc.performed += ctx => StartStopScenario();
        testDecide.performed += ctx => PunctualAgentDecide();
        
        testStartSc.Enable();
        testDecide.Enable();
        testTouch.Enable();
        
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

    public void StartStopScenario()
    {
        if (debug) Debug.Log("Trying to start/stop the scenario.");
        if (_fatimaManager.IsScenarioStarted())
        {
            if (debug) Debug.Log("Stopping the scenario.");
            _fatimaManager.StopScenario();
        }
        else
        {
            if (debug) Debug.Log("Starting the scenario, instantiating prefab and getting an agent decision.");
            _fatimaManager.StartScenario();
            AgentDecision();
        }
    }
    
    private void ChangeCurrentState(string newState)
    {
        _currentState = newState;
    }
    
    public void AgentTouched()
    {
        if (debug) Debug.Log("The agent has been touched, setting up the reaction depending on the current state of the scenario.");
        StartCoroutine(PlayFML("Joy", "Action", _fatimaManager.humanCharacter));
    }
    
    public void AgentTouch()
    {
        if (debug) Debug.Log("Demonstrating an Agent Touch.");
        StartCoroutine(PlayFML("DemoTouch", "Action", _fatimaManager.humanCharacter));
    }
    
    public void PunctualAgentDecide()
    {
        if (debug) Debug.Log("Manual activation of the decision making from the agent in case something goes wrong.");
        AgentDecision();
    }
    
    #endregion
    
    #region FAtiMA Handling
    
    private void AgentDecision()
    { 
        var action = _fatimaManager.DoAgentAction();
        var processedAction = _fatimaManager.ProcessAction(action);
        HandleProcessedAction(processedAction);
    }

    private void HandleProcessedAction(ProcessedFAtiMAAction processedAction)
    {
        switch (processedAction.identifier)
        {
            case IATConsts.DIALOG_ACTION_KEY:
                HandleSpeak(processedAction);
                ChangeCurrentState(processedAction.nextState);
                if (debug) Debug.Log("Current state : " + _currentState);
                break;
            case "Help":
                AgentDecision();
                if (debug) Debug.Log("Requesting Agent Help");
                break;
            case "Backchannel":
                //TODO
                break;
            default:
                break;
        }
    }

    private void HandleSpeak(ProcessedFAtiMAAction processedAction)
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
    
    private IEnumerator PlayFML(string filename, string actionName, string target)
    {
        if (target == _fatimaManager.humanCharacter)
        {
            if (actionName == IATConsts.DIALOG_ACTION_KEY)
                yield return new WaitUntil(() => !_agentAs.isPlaying);
            _agentAnim.PlayFml(filename, actionName);
        }
    }
}
