using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using AutobiographicMemory;
using IntegratedAuthoringTool;
using Leap.Unity;
using Minigame;
using TactilePerception;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using WellFormedNames;

public class Experiment1Manager : MonoBehaviour
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
    
    [FormerlySerializedAs("Timer")] [Tooltip("GameObject with the TimerManager to start, stop and reset the timer when we start the task.")]
    public GameObject timer;
    private TimerManager _timerManager;
    
    [Tooltip("GameObject with the MinigameManager to determine the advancement of the task.")]
    public GameObject minigameManager;
    private Minigame.GridManager _minigameManager;

    //Current state of the scenario, to keep track of when we get into and out of the minigame.
    private string _currentState = "Start";

    //To track whether we are in the first or second iteration of the minigame.
    private int _minigamesDone = 0;

    //The prefab of the tetrominoes to destroy/instantiate on resetting the minigame.
    [FormerlySerializedAs("_tetrosPrefab")] public GameObject tetrosPrefab;

    public GameObject shapeOutline1;
    public GameObject shapeOutline2;
    public GameObject shapeOutline0;
    public GameObject exampleTetros;

    public TouchPerceptionManager leftHand;
    public TouchPerceptionManager rightHand;

    private GameObject _currentTetroPrefab;

    private int _gridStatus = 0; // 0 = empty, 1 = correct, -1 not empty not correct

    private bool _currentlyTouched = false;

    public bool debug = true;
    
    #endregion
    
    #region MonoBehaviour methods
    
    // Start is called before the first frame update
    void Start()
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

        _timerManager = timer.GetComponent<TimerManager>();
        if (_timerManager == null)
        {
            Debug.LogError("No TimerManager found : timer will not be updated during the task.");
        }

        _minigameManager = minigameManager.GetComponent<GridManager>();
        if (_minigameManager == null)
        {
            Debug.LogError("No GridManager found : the minigame cannot be performed.");
        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    #endregion

    public void TouchDemo(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        StartCoroutine(PlayFML("TestTouch", "Action", _fatimaManager.humanCharacter));

    }

    #region Scenario Handling

    public void StartStopScenario(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (debug) Debug.Log("Trying to start/stop the scenario.");
        if (_fatimaManager.IsScenarioStarted())
        {
            if (debug) Debug.Log("Stopping the scenario.");
            FullReset();
            _fatimaManager.StopScenario();
        }
        else
        {
            if (debug) Debug.Log("Starting the scenario, instantiating prefab and getting an agent decision.");
            _fatimaManager.StartScenario();
            _currentTetroPrefab = Instantiate(tetrosPrefab);
            AgentDecision();
        }
    }

    private void ChangeCurrentState(string newState)
    {
        if (newState == _currentState) return;
        if (newState == "Familiarization")
        {
            leftHand.enabled = true;
            rightHand.enabled = true;
        };
        if (newState == "MinigameExplanation")
        {
            shapeOutline0.SetActive(true);
            exampleTetros.SetActive(true);
        };
        if (newState == "MinigameStart")
        {
            shapeOutline0.SetActive(false);
            exampleTetros.SetActive(false);
        };
        if (newState == "Minigame")
        {
            if (debug) Debug.Log("Entering Minigame state, initiating setup.");
            SetupMinigame();
        }
        else if (_currentState == "Minigame")
        {
            if (debug) Debug.Log("Exiting Minigame state, resetting everything.");
            _timerManager.StartStopTimer();
            CancelInvoke();
            var actionArgs = new List<Name>();
            actionArgs.Add((Name)_minigameManager.GetCurrentShape().ToString());
            HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
                "Reset",
                actionArgs,
                AMConsts.ACTION_END
            )));
            _minigamesDone++;
        }
        _currentState = newState;
    }

    private void SetupMinigame()
    {
        if (_minigamesDone >= 1)
        {
            if (debug) Debug.Log("Setting up for 2nd Minigame.");
            //First, reset and destroy all that needs to be reset.
            shapeOutline1.SetActive(false);
            Destroy(_currentTetroPrefab);
            _timerManager.ResetTimer();
            _minigameManager.FullClear();
            
            //Then activate the correct outline
            shapeOutline2.SetActive(true);
            _minigameManager.SetShape(Shape.Shape2);
        }
        else
        {
            if (debug) Debug.Log("Setting up for 1st Minigame.");
            //First, reset and destroy all that needs to be reset.
            shapeOutline2.SetActive(false);
            Destroy(_currentTetroPrefab);
            _timerManager.ResetTimer();
            _minigameManager.FullClear();
            
            //Then activate the correct outline
            shapeOutline1.SetActive(true);
            _minigameManager.SetShape(Shape.Shape1);
        }
        
        if (debug) Debug.Log("Starting Timer, instantiating tetros and invoking agent decision every 10s.");
        //Finally, instantiate the tetros, start the timer and invoke agent help every 10 seconds.
        _currentTetroPrefab = Instantiate(tetrosPrefab);
        _timerManager.StartStopTimer();
        InvokeRepeating(nameof(AgentDecision), 50f, 15f);
        InvokeRepeating(nameof(UpdateFAtiMAGridStatus), 50f, 15f);
        StartCoroutine(CheckCorrectness());
    }

    private void FullReset()
    {
        if (debug) Debug.Log("Fully resetting the interaction");
        if (_currentTetroPrefab != null)
            Destroy(_currentTetroPrefab);
        _timerManager.ResetTimer();
        CancelInvoke();
        ChangeCurrentState("Start");
    }

    IEnumerator CheckCorrectness()
    {
        if (debug)
            Debug.Log("Starting the check for correctness of the shape.");
        yield return new WaitUntil((() => _minigameManager.IsShapeCorrect() == true));
        if (debug)
            Debug.Log("Shape is correct.");
        var actionArgs = new List<Name>();
        actionArgs.Add((Name)_minigameManager.GetCurrentShape().ToString());
        HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
            "Correct",
            actionArgs,
            AMConsts.ACTION_END
        )));
        AgentDecision();
    }

    public void OnHumanReady(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (_currentState != "Familiarization" && _currentState != "ReplayOption" && _currentState != "MinigameExplanation") return;
        if (debug) Debug.Log("Sending the Ready speak action from the human to the agent if in the correct current state.");
        var actionArgs = new List<Name>();
        actionArgs.Add((Name)_currentState);
        switch (_currentState)
        {
            case "Familiarization":
                actionArgs.Add((Name)"MinigameReady");
                break;
            case "MinigameExplanation":
                actionArgs.Add((Name) "MinigameStart");
                break;
            case "ReplayOption":
                actionArgs.Add((Name)"MinigameRepeat");
                break;
        }
        actionArgs.Add((Name)"Ready");
        actionArgs.Add((Name)_fatimaManager.humanCharacter);
        HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
            "Speak",
            actionArgs,
            AMConsts.ACTION_END
        )));
        AgentDecision();
    }
    
    public void OnHumanRefuse(InputAction.CallbackContext context)
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
    
    public void AgentTouched(bool touched)
    {
        if (debug) Debug.Log("The agent has been touched, setting up the reaction depending on the current state of the scenario.");
        if (!_currentlyTouched && touched)
        {
            _currentlyTouched = true;
            if (_currentState == "Minigame")
            {
                var actionArgs = new List<Name>();
                actionArgs.Add((Name)_fatimaManager.humanCharacter);
                HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
                    "Help",
                    actionArgs,
                    AMConsts.ACTION_END
                )));
                AgentDecision();
            }
        }
        else if (!touched)
        {
            if (_currentState == "Familiarization" && _currentlyTouched)
            {
                StartCoroutine(PlayFML("FamiliarizationTouch", "Speak", _fatimaManager.humanCharacter));
            }
            _currentlyTouched = false;
        }
    }
    
    public void PunctualAgentDecide(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
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
        if (debug) Debug.Log("Processing the following action : " + processedAction.identifier + processedAction.content + processedAction.target + processedAction.nextState);
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
            if (processedAction.content.Contains("Clue"))
            {
                switch (_gridStatus)
                {
                    case 0:
                        StartCoroutine(PlayFML("PasHesiter", processedAction.identifier,
                            processedAction.target));
                        break;
                    case 1:
                        StartCoroutine(PlayFML("BravoContinue", processedAction.identifier,
                            processedAction.target));
                        break;
                    default:
                        ReplaceFile($"{Application.streamingAssetsPath}/FMLs/{_agentAnim.lang}/ClueTest", $"{Application.streamingAssetsPath}/FMLs/{_agentAnim.lang}/ClueTestReplaced", ReplacementMethod);
                        StartCoroutine(PlayFML("ClueTestReplaced", processedAction.identifier,
                            processedAction.target));
                        break;
                }
            }
            else
            {
                StartCoroutine(PlayFML(processedAction.content, processedAction.identifier, processedAction.target));
            }
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

    private void UpdateFAtiMAGridStatus()
    {
        _gridStatus = _minigameManager.EmptyOrCurrentlyCorrect();
        var actionArgs = new List<Name>();
        switch (_gridStatus)
        {
            case 0:
                actionArgs.Add((Name)"Empty");
                HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
                    "UpdateGrid",
                    actionArgs,
                    AMConsts.ACTION_END
                )));
                break;
            case -1:
                actionArgs.Add((Name)"NotEmpty");
                HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
                    "UpdateGrid",
                    actionArgs,
                    AMConsts.ACTION_END
                )));
                break;
            case 1:
                actionArgs.Add((Name)"CurrentlyCorrect");
                HandleProcessedAction(_fatimaManager.ProcessAction(_fatimaManager.DoHumanAction(
                    "UpdateGrid",
                    actionArgs,
                    AMConsts.ACTION_END
                )));
                break;
        }
    }
    
    #endregion
    
    #region FML File Handling
    
    public string ReplacementMethod(Match m)
    {
        // Get the ID part of string $REPLACE_ID$
        string replacementID = m.Groups[1].Value;
 
        // Look for this ID in our replacement dictionary
        //if (ReplacementMap.ContainsKey(replacementID))
        //    return ReplacementMap[replacementID];
 
        // Here is an example of hardcoded replacements... they may make more sense than the above.
        var currentWorstTetro = _minigameManager.WorstPlacedTetro();
        switch (replacementID.ToLowerInvariant())
        {
            case "tetro":
                if (_agentAnim.lang == "FR")
                {
                    switch (currentWorstTetro.Key)
                    {
                        case Tetromino.Angle:
                            return "le petit objet vert pomme en forme d'angle droit";
                        case Tetromino.LongAngle:
                            return "l'objet vert d'eau en forme d'angle droit avec un long côté";
                        case Tetromino.Z:
                            return "l'objet violet qui était au bout du présentoir, près de moi";
                        case Tetromino.LongZ:
                            return "l'objet orange qui a un peu la maime forme que l'objet violet mais en plus grand";
                        case Tetromino.Square:
                            return "l'objet jaune en forme de carré";
                        case Tetromino.Cross:
                            return "l'objet rouge en forme de croix";
                        case Tetromino.Bar:
                            return "l'objet bleu clair qui a la forme d'une barre droite";
                        case Tetromino.Tri:
                            return "l'objet rose qui a trois extraimitais";
                        case Tetromino.W:
                            return "l'objet bleu fonsai en forme d'escalier";
                    }
                }
                else if (_agentAnim.lang == "EN")
                {
                    switch (currentWorstTetro.Key)
                    {
                        case Tetromino.Angle:
                            return "the green object shaped like a small right angle";
                        case Tetromino.LongAngle:
                            return "the blue green object shaped like a right angle with a longer side";
                        case Tetromino.Z:
                            return "the dark purple object that was close to me, at the extremity of the stand, in the beginning";
                        case Tetromino.LongZ:
                            return "the orange object with a similar shape to the dark purple one but longer";
                        case Tetromino.Square:
                            return "the yellow square shaped object";
                        case Tetromino.Cross:
                            return "the red object shaped like a cross";
                        case Tetromino.Bar:
                            return "the light blue object that looks like a long bar";
                        case Tetromino.Tri:
                            return "the pink object with three extremities";
                        case Tetromino.W:
                            return "the dark blue object shaped like stairs";
                    }
                }
                else return "";
                break;
            case "position":
                var replaceWith = "";
                if (_agentAnim.lang == "FR")
                {
                    switch (currentWorstTetro.Value.Key)
                    {
                        case Position.Left:
                            replaceWith = "trop à gauche";
                            break;
                        case Position.Right:
                            replaceWith = "trop à droite";
                            break;
                        case Position.Incorrect:
                            replaceWith = "pas utile pour remplir cette forme";
                            break;
                        case Position.Up:
                            replaceWith = "trop haut";
                            break;
                        case Position.Down:
                            replaceWith = "trop bas";
                            break;
                        default:
                            replaceWith = "";
                            break;
                    }
                }
                else if (_agentAnim.lang == "EN")
                {
                    switch (currentWorstTetro.Value.Key)
                    {
                        case Position.Left:
                            replaceWith = "too much on the left";
                            break;
                        case Position.Right:
                            replaceWith = "too much on the right";
                            break;
                        case Position.Incorrect:
                            replaceWith = "not included in the solution for this outline";
                            break;
                        case Position.Up:
                            replaceWith = "too high";
                            break;
                        case Position.Down:
                            replaceWith = "too low";
                            break;
                        default:
                            replaceWith = "";
                            break;
                    }
                }
                return replaceWith;
        }
 
        // Default: Replace it with nothing
        return "";
    }
    
    private Regex _replacementFinderRegex = new Regex(@"\$REPLACE_([^\$]+)\$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);
    
    public void ReplaceFile(string sourceFile, string destinationFile, MatchEvaluator replacementMethodDelegate)
    {
        // By using "using" we ensure that files are closed properly regardless what we forget to do or or error occurs
 
        // Open input file so we get a FileStream object
        using FileStream inStream = File.Open(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read);
        // Open output file so we get a FileStream object
        using FileStream outStream = File.Open(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None);
        // We use the ReplaceStream function to do the actual copying and replacement
        ReplaceStream(inStream, outStream, replacementMethodDelegate);
    }
    
    public void ReplaceStream(FileStream inStream, FileStream outStream, MatchEvaluator replacementMethodDelegate)
    {
        using (StreamReader inStreamReader = new StreamReader(inStream))
        {
            using (StreamWriter outStreamWriter = new StreamWriter(outStream))
            {
                string inLine = null;
                while ((inLine = inStreamReader.ReadLine()) != null)
                {
                    // Send the line through our replacement method using Regex
                    string outLine = inLine;
                    lock (_replacementFinderRegex)
                    {
                        outLine = _replacementFinderRegex.Replace(outLine, replacementMethodDelegate);
                    }
 
                    // Write the line to output stream
                    if (outLine != null)
                        outStreamWriter.WriteLine(outLine);
                }
            }
        }
    }
    
    private IEnumerator PlayFML(string filename, string actionName, string target)
    {
        if (target == _fatimaManager.humanCharacter)
        {
            if (actionName == IATConsts.DIALOG_ACTION_KEY)
                yield return new WaitUntil(() => !_agentAs.isPlaying);
            _agentAnim.PlayFml(filename, actionName);
        }
    }
    
    #endregion
}
