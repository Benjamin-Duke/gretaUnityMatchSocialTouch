using System;
using System.Collections;
using System.Collections.Generic;
using ExperimentUtility;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;

public class AnticipationEM : GenericExperimentManager
{
    [SerializeField] private List<int> positionFinales;
    [SerializeField] private List<int> positionsEntrainement;
    [SerializeField] private List<string> facialExpressions;
    [SerializeField] private string animation;
    [SerializeField] private string csvFile;
    public bool agencyPressed = false;
    private bool endPause = false;
    private int answer = -1;
    [SerializeField] private int frameTest = 49;
    [SerializeField] private int trialByCombination = 8;

    private bool waitingForAnswer = false;
    
    [Tooltip("GameObject with the GRETAnimationManager for the Agent Character")]
    public GameObject agent;
    
    #region Initialization
    
    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        stimParams.agentAnim = agent.GetComponent<GRETAnimationManagerDEMO>();
        if (stimParams.agentAnim == null)
        {
            Debug.LogError("No GRETAnimationManager found : can't play FML files.");
        }

        stimParams.agentBaseAnim = agent.GetComponentInChildren<GretaAnimatorBridge>();
        if (stimParams.agentBaseAnim == null)
        {
            Debug.LogError("No GretaCharacterAnimator found : can't play FML files.");
        }
        
        stimParams.agentAs = agent.GetComponentInChildren<AudioSource>();
        if (stimParams.agentAs == null)
        {
            Debug.LogError("No AudioSource found : agent speech may not work.");
        }

        stimParams.unityAnimator = agent.GetComponent<Animator>();
        if (stimParams.unityAnimator == null)
        {
            Debug.LogError("No Unity Animator found : mocap stimulus may not work.");
        }
        
        if (stimParams.scrnFader == null)
        {
            Debug.LogError("No ScreenFader : might not work properly, no fading expected.");
        }

        stimParams.agent = agent;

        //InitializeExperiment();

    }

    //A full shuffling of all the questions and groups is done in the base.InitializeExperiment() so we need to monitor for multiple blocks.
    //We also setup the 3x3x3 stimuli to pick from.
    protected override void InitializeExperiment()
    {
        //If the stimuli stack is not empty, we need to abort the initialization.
        if (Stimuli.GetCurrentProgression() > 0)
            Stimuli = new Stimuli(this);
        base.InitializeExperiment();
        int actualPartID = int.Parse(participantID);
        bool blocAgency = false;
        if (actualPartID % 2 != 0)
        {
            blocAgency = (CountBloc+1) % 2 != 0;
        }
        else
        {
            blocAgency = (CountBloc+1) % 2 == 0;
        }
        foreach (var panel in panels)
        {
            panel.InitializeLanguage(lang);
        }
        Stimuli.ResetTotalCount();

        
        foreach (var faceExpression in facialExpressions)
        {
            if ((CountBloc+1) < 3)
            {
                foreach (var pf in positionsEntrainement)
                {
                    var stim = new AnticipationStimulus()
                    {
                        Agency = blocAgency,
                        FacialExpression = faceExpression,
                        Gesture = animation,
                        PositionFinale = pf
                    };
                    Stimuli.Add(stim);
                }
            }
            else
            {
                for (int i = 0; i < trialByCombination; i++)
                {
                    foreach (var pf in positionFinales)
                    {
                        var stim = new AnticipationStimulus()
                        {
                            Agency = blocAgency,
                            FacialExpression = faceExpression,
                            Gesture = animation,
                            PositionFinale = pf
                        };
                        Stimuli.Add(stim);
                    }
                }
                
            }
        }
        
        Stimuli.ShuffleStimuli();
    }

    #endregion
    
    #region Management of the Experiment Flow
    
    public override void OnReady(IEnumerator callback = null)
    {
        var readyPanel = panels.Find(p => p.panelType == PanelType.Ready);
        readyPanel.gameObject.SetActive(false);
        switch (Phase)
        {
            case ExperimentPhase.WaitingToStart:
                InitializeExperiment();
                Phase = ExperimentPhase.Main;
                OnReady();
                break;
            case ExperimentPhase.Main:
                if (Stimuli.GetCurrentProgression() < 100)
                {
                    if (((CountBloc+1) > 2) && (Stimuli.GetCurrentProgression() == 20 || Stimuli.GetCurrentProgression() == 40 ||
                        Stimuli.GetCurrentProgression() == 60 || Stimuli.GetCurrentProgression() == 80))
                    {
                        StartCoroutine(PauseAndResume());
                    }
                    else
                    {
                        StartCoroutine(callback != null
                            ? StimulusAndCallback(callback)
                            : StimulusAndCallback(WaitParticipantAnswer()));
                    }
                    
                }
                else
                {
                    EndExperiment();
                    /*
                    Phase = ExperimentPhase.LastQuestionnaire;
                    StartCoroutine(QuestionnaireWrapper());*/
                }
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
    
    // If we need to override something of the experiment flow
    
    //Wrapper for OnReady without callback so that we can debug stuff in desktop mode.
    public virtual void OnReadySuperWrapper(InputAction.CallbackContext context)
    {
        if (!context.performed)
        {
            return;
        }
        OnReady();
    }
    
    //We need to deal with showing/hiding the agent and fading in and out each stimulus
    protected override IEnumerator StimulusAndCallback(IEnumerator callback = null, bool next = true)
    {
        yield return StartCoroutine(base.StimulusAndCallback(callback, next));
        OnReady();
    }

    protected IEnumerator WaitParticipantAnswer()
    {
        waitingForAnswer = true;
        yield return new WaitUntil(() => answer == 0 || answer == 1);
        waitingForAnswer = false;
        SaveToCsv(csvFile, ";" + (CountBloc+1).ToString() + ";" + answer.ToString(), true);
        answer = -1;
    }

    protected override void RestartBlock()
    {
        StartCoroutine(PauseAndResume(ExperimentPhase.WaitingToStart));
    }

    protected virtual IEnumerator PauseAndResume(ExperimentPhase phase = ExperimentPhase.Main, IEnumerator callback = null)
    {
        yield return StartCoroutine(stimParams.scrnFader.FadeRoutine(0, 1, 1f));
        stimParams.unityAnimator.SetTrigger("Reset");
        stimParams.agentAnim.PlayFML(stimParams.filepath + "Neutral");
        stimParams.agentAnim.PlayFML(stimParams.filepath + "Neutral");
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(stimParams.scrnFader.FadeRoutine(1, 0, 1f));
        panels.Find(p => p.panelType == PanelType.Pause).Enable();
        yield return new WaitUntil(() => endPause == true);
        endPause = false;
        panels.Find(p => p.panelType == PanelType.Pause).gameObject.SetActive(false);
        Phase = phase;
        if (phase == ExperimentPhase.WaitingToStart)
            OnReady();
        else
        {
            StartCoroutine(callback != null
                ? StimulusAndCallback(callback)
                : StimulusAndCallback(WaitParticipantAnswer()));
        }
    }

    #endregion
    
    public void ToggleStimPanel(PanelType panelType)
    {
        if (panels.Find(p => p.panelType == panelType).gameObject.activeSelf)
            panels.Find(p => p.panelType == panelType).gameObject.SetActive(false);
        else
        {
            panels.Find(p => p.panelType == panelType).Enable();
        }
    }

    public void AnswerClose(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (waitingForAnswer)
            answer = 1;
    }
    
    public void AnswerFar(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (waitingForAnswer)
            answer = 0;
    }
    
    public void Agency(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        agencyPressed = true;
    }
    
    public void EndPause(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        endPause = true;
    }
    
    public void PlayAnimation(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        List<int> pfs = new List<int>
        {
            46,
            49,
            55,
            58,
            61
        };

        pfs = new List<int>(pfs.Shuffle());
        var stim = new AnticipationStimulus()
        {
            Agency = false,
            FacialExpression = "Joy",
            Gesture = animation,
            PositionFinale = frameTest
        };
        StartCoroutine(stim.PlayStimulus(stimParams, this));
    }
}
