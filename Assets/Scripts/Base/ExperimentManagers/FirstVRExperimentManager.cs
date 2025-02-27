using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AutobiographicMemory;
using IntegratedAuthoringTool;
using Leap.Unity;
using Minigame;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using WellFormedNames;

public class FirstVRExperimentManager : ExperimentManagerProto
{
    #region Attributes

    [Tooltip("GameObject with the MinigameManager to determine the advancement of the task.")]
    public GameObject minigameManager;
    private FirstVRMinigameManager _minigameManager;

    public bool demo;

    private int _minigameResult;

    // Monitor when we majorly reacted last
    private float _lastMajorReactionCounter;
    
    // Monitor when we minorly reacted last
    private float _lastMinorReactionCounter;
    
    // Monitor when we last managed to place a tetro
    private float _lastTetroPlaced = 50f;

    //Monitor how well the human is doing over a period of time
    private bool _combo;

    private List<string> _majorReactionsLabels = new List<string>();

    #endregion
    
    #region MonoBehaviour methods
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();

        _minigameManager = minigameManager.GetComponent<FirstVRMinigameManager>();
        if (_minigameManager == null)
        {
            Debug.LogError("No GridManager found : the minigame cannot be performed.");
        }
        
        _majorReactionsLabels.Add("Encourage");
        _majorReactionsLabels.Add("ComfortMistake");
        _majorReactionsLabels.Add("MaintainRapport");
    }

    // Update is called once per frame
    void Update()
    {
        if (_minigameManager.IsMinigameStarted() && _minigameManager.GetTimer() < 239)
        {
            _lastMajorReactionCounter += Time.deltaTime;
            _lastMinorReactionCounter += Time.deltaTime;
            _lastTetroPlaced += Time.deltaTime;
            if (_lastMajorReactionCounter >= 61f)
            {
                UpdateFAtiMATimer(_minigameManager.GetTimer());
                AgentDecision();
                _lastMajorReactionCounter = 0f;
            }
        }
    }
    
    #endregion
    
    #region Scenario Handling

/*    protected override void AgentDecision()
    {
        if (!_minigameManager.IsMinigameStarted())
        {
            base.AgentDecision();
            return;
        }
        else if (_lastReactionCounter >= 20f)
        {
            base.AgentDecision();
            _lastReactionCounter = 0f;
        }
    }*/

    protected override void ChangeCurrentState(string newState)
    {
        if (newState == _currentState) return;
        switch (newState)
        {
            case "Familiarization":
                if (debug) Debug.Log("Entering Familiarization state, setting tetros up.");
                _minigameManager.SetupTetros();
                break;
            case "Minigame":
                if (debug) Debug.Log("Entering Minigame state, initiating setup.");
                _minigameResult = 0;
                _lastMajorReactionCounter = 0f;
                _lastMinorReactionCounter = 0f;
                _lastTetroPlaced = 50f;
                _combo = false;
                _minigameManager.StartMinigame();
                break;
        }
        _currentState = newState;
    }
    public void MinigameEnded(int result)
    {
        _minigameResult = result;
        var actionArgs = new List<Name>();
        actionArgs.Add((Name)result.ToString());
        HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
            "MinigameEnded",
            actionArgs,
            AMConsts.ACTION_END
        )));
        AgentDecision();
    }

    private void UpdateFAtiMATimer(int timer)
    {
        var actionArgs = new List<Name>();
        actionArgs.Add((Name)timer.ToString());
        HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
            "UpdateTimer",
            actionArgs,
            AMConsts.ACTION_END
        )));
    }

    public void UpdateGridStatus(int count)
    {
        _combo = _lastTetroPlaced < 7f;
        _lastTetroPlaced = 0f;
        var actionArgs = new List<Name>();
        actionArgs.Add((Name)count.ToString());
        actionArgs.Add((Name)_combo.ToString());
        HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
            "UpdateGrid",
            actionArgs,
            AMConsts.ACTION_END
        )));
        AgentDecision();
    }
    
    public void TetroFell()
    {
        var actionArgs = new List<Name>();
        actionArgs.Add((Name)"Tetro");
        HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
            "Fell",
            actionArgs,
            AMConsts.ACTION_END
        )));
        AgentDecision();
    }

    public override void OnHumanReady(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (debug) Debug.Log("Sending the Ready speak action from the human to the agent if in the correct current state.");
        Name nextState;
        switch (_currentState)
        {
            case "Familiarization":
                nextState = (Name)"MinigameReady";
                break;
            case "MinigameExplanation":
                nextState = (Name) "MinigameStart";
                break;
            case "ReplayOption":
                if (demo)
                    nextState = (Name)"MinigameStart";
                else
                    nextState = (Name)"MinigameRepeat";
                break;
            case "MinigameRepeatExpl":
                nextState = (Name) "MinigameStart";
                break;
            default:
                return;
        }
        var actionArgs = new List<Name>();
        actionArgs.Add((Name)_currentState);
        actionArgs.Add(nextState);
        actionArgs.Add((Name)"Ready");
        actionArgs.Add((Name)_fatimaManager.humanCharacter);
        HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
            "Speak",
            actionArgs,
            AMConsts.ACTION_END
        )));
        AgentDecision();
    }

    public override void OnHumanRefuse(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_currentState != "ReplayOption") return;
        if (debug) Debug.Log("Sending the Refuse speak action from the human to the agent if in the replay option state.");
        var actionArgs = new List<Name>();
        actionArgs.Add((Name)_currentState);
        actionArgs.Add((Name)"Refuse");
        actionArgs.Add((Name)"Negative");
        actionArgs.Add((Name)_fatimaManager.humanCharacter);
        HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
            "Speak",
            actionArgs,
            AMConsts.ACTION_END
        )));
        AgentDecision();
    }

    public void OnFinishGrid(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        UpdateGridStatus(9);
        _minigameManager.EndMinigame();
    }

    #endregion
    
    #region FAtiMA Handling
    
    protected override void HandleSpeak(ProcessedFAtiMAAction processedAction)
    {
        if (processedAction.target == _fatimaManager.humanCharacter)
        {
            if (processedAction.content.Contains("PlayAgain"))
            {
                if (touchCondition)
                {
                    processedAction.content = processedAction.content.Replace("Gesture", "Touch");
                }
                
                if (_minigameResult > 0)
                {
                    processedAction.content += "Success";
                }
                else
                {
                    processedAction.content += "Timer";
                }
            }

            if (processedAction.content.Contains("TAvailable") && !touchCondition)
            {
                processedAction.content = processedAction.content.Replace("TAvailable", "NTAvailable");
            }
            
            StartCoroutine(PlayFML(processedAction.content, processedAction.identifier, processedAction.target));
        }
        else if (processedAction.target == _fatimaManager.agentCharacter)
        {
        }
    }
    
    protected override void HandleProcessedAction(ProcessedFAtiMAAction processedAction)
    {
        switch (processedAction.identifier)
        {
            case IATConsts.DIALOG_ACTION_KEY:
                if (processedAction.content.Contains("Minor"))
                {
                    if (_lastMinorReactionCounter < 10f || _lastMajorReactionCounter >= 57f)
                        break;
                    else
                    {
                        _lastMinorReactionCounter = 0f;
                    }
                }

                if (_majorReactionsLabels.Any(s => processedAction.content.Contains(s)))
                    processedAction.content += _minigameManager.GetTimer() < 150 ? "Early" : "Late";
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
    
    #endregion
}
