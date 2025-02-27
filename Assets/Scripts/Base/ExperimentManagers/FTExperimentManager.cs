using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Leap.Unity.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Newtonsoft.Json;
using Utilities;
using ExperimentUtility;

public class FTExperimentManager : QuestionnaireExperimentManager
{
    #region Attributes

    [SerializeField] private string unityAnimation;
    
    [SerializeField] private string fixedAnimation;
    
    [SerializeField] private string gretaAnimation;
    
    [SerializeField] private string pattern;
    
    [SerializeField] private AudioClip audioStimulus;

    [SerializeField] private Transform farMarker;

    [SerializeField] private float totalAnimationTime = 6f;
    
    [SerializeField] private float waitTime = 1f;

    public ScreenFader screenFader;

    // Combinations to test in the experiment (Conditions for preparing the stimuli)
    public bool audioVibro = false;
    public bool audioCond = false;
    public bool haptic = false;
    public bool visualOnly = false;

    [Tooltip("GameObject with the GRETAnimationManager for the Agent Character")]
    public GameObject agent;

    private Vector3 initPos;
    
    #endregion

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

        stimParams.stimulusAs = stimParams.sleeveConnector.touchHandTarget.GetComponent<AudioSource>();
        if (stimParams.stimulusAs == null)
        {
            Debug.LogError("No AudioSource found : stimulus may be muted.");
        }
        
        stimParams.unityAnimator = agent.GetComponent<Animator>();
        if (stimParams.unityAnimator == null)
        {
            Debug.LogError("No Unity Animator found : mocap stimulus may not work.");
        }

        stimParams.scrnFader = screenFader;
        if (stimParams.scrnFader == null)
        {
            Debug.LogError("No ScreenFader : might not work properly, no fading expected.");
        }

        stimParams.agent = agent;
        initPos = agent.transform.position;

        //InitializeExperiment();
        /*
        Debug.Log(preQuestionGroups.First().questions.First().GetQuestionText());
        Debug.Log(postQuestionGroups.First().questions.First().GetQuestionText());
        */

    }

    //A full shuffling of all the questions and groups is done in the base.InitializeExperiment() so we need to monitor for multiple blocks.
    //We also setup the 3x3x3 stimuli to pick from.
    protected override void InitializeExperiment()
    {
        //If the stimuli stack is not empty, we need to abort the initialization.
        if (Stimuli.GetCurrentProgression() > 0)
            return;
        base.InitializeExperiment();
        foreach (var panel in panels)
        {
            panel.InitializeLanguage(lang);
        }
        Stimuli.ResetTotalCount();

        if (audioVibro)
        {
            var gesture = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                AudioStimulus = audioStimulus,
                Pattern = pattern,
                Far = false,
                FixedGesture = false,
                UnityAnimation = unityAnimation,
                GretaAnimation = gretaAnimation
            };
            var fixedArm = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                AudioStimulus = audioStimulus,
                Pattern = pattern,
                Far = false,
                FixedGesture = true,
                UnityAnimation = fixedAnimation,
                GretaAnimation = gretaAnimation
            };
            var noAnim = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                AudioStimulus = audioStimulus,
                Pattern = pattern,
                Far = false,
                FixedGesture = false,
                UnityAnimation = "",
                GretaAnimation = gretaAnimation
            };
            var farNoAnim = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                AudioStimulus = audioStimulus,
                Pattern = pattern,
                Far = true,
                FixedGesture = false,
                UnityAnimation = "",
                GretaAnimation = gretaAnimation,
                Pos = farMarker.position
            };
            Stimuli.Add(gesture);
            Stimuli.Add(fixedArm);
            Stimuli.Add(noAnim);
            Stimuli.Add(farNoAnim);
        }

        if (audioCond)
        {
            var gesture = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                AudioStimulus = audioStimulus,
                Far = false,
                FixedGesture = false,
                UnityAnimation = unityAnimation,
                GretaAnimation = gretaAnimation
            };
            var fixedArm = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                AudioStimulus = audioStimulus,
                Far = false,
                FixedGesture = true,
                UnityAnimation = fixedAnimation,
                GretaAnimation = gretaAnimation
            };
            var noAnim = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                AudioStimulus = audioStimulus,
                Far = false,
                FixedGesture = false,
                UnityAnimation = "",
                GretaAnimation = gretaAnimation
            };
            var farNoAnim = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                AudioStimulus = audioStimulus,
                Far = true,
                FixedGesture = false,
                UnityAnimation = "",
                GretaAnimation = gretaAnimation,
                Pos = farMarker.position
            };
            Stimuli.Add(gesture);
            Stimuli.Add(fixedArm);
            Stimuli.Add(noAnim);
            Stimuli.Add(farNoAnim);
        }

        if (haptic)
        {
            var gesture = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                Pattern = pattern,
                Far = false,
                FixedGesture = false,
                UnityAnimation = unityAnimation,
                GretaAnimation = gretaAnimation
            };
            var fixedArm = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                Pattern = pattern,
                Far = false,
                FixedGesture = true,
                UnityAnimation = fixedAnimation,
                GretaAnimation = gretaAnimation
            };
            var noAnim = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                Pattern = pattern,
                Far = false,
                FixedGesture = false,
                UnityAnimation = "",
                GretaAnimation = gretaAnimation
            };
            var farNoAnim = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                Pattern = pattern,
                Far = true,
                FixedGesture = false,
                UnityAnimation = "",
                GretaAnimation = gretaAnimation,
                Pos = farMarker.position
            };
            Stimuli.Add(gesture);
            Stimuli.Add(fixedArm);
            Stimuli.Add(noAnim);
            Stimuli.Add(farNoAnim);
        }

        if (visualOnly)
        {
            var gesture = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                Far = false,
                FixedGesture = false,
                UnityAnimation = unityAnimation,
                GretaAnimation = gretaAnimation
            };
            var fixedArm = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                Far = false,
                FixedGesture = true,
                UnityAnimation = fixedAnimation,
                GretaAnimation = gretaAnimation
            };
            var noAnim = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                Far = false,
                FixedGesture = false,
                UnityAnimation = "",
                GretaAnimation = gretaAnimation
            };
            var farNoAnim = new UnityGRETAStimulus()
            {
                WaitinTime = waitTime,
                TotalTime = totalAnimationTime,
                Far = true,
                FixedGesture = false,
                UnityAnimation = "",
                GretaAnimation = gretaAnimation,
                Pos = farMarker.position
            };
            Stimuli.Add(gesture);
            Stimuli.Add(fixedArm);
            Stimuli.Add(noAnim);
            Stimuli.Add(farNoAnim);
        }

        Stimuli.ShuffleStimuli();
    }

    #endregion

    #region ExperimentFlow
    
    public override void StartExperiment(InputAction.CallbackContext context)
    {
        if (!context.performed) return; //Check that the keyboard button has been pressed
        var readyPanel = panels.Find(p => p.panelType == PanelType.Ready);
        if (readyPanel == null)
        {
            Debug.LogError("No UI panel for Ready situation was assigned : experiment cannot be run.");
            return;
        }
        readyPanel.InitializeLanguage(lang);
        //_agentAnim.PlayFML(_filepath + "ExperimentIntro");
        StartCoroutine(FadeAgent(InstantiatePanel(PanelType.Ready)));// For simplicity we use the PanelType class but if we want new PanelTypes we can define a new PanelType class in this script file instead and adjust the corresponding Panel scripts accordingly.
    }

    public override void OnReady(IEnumerator callback = null)
    {
        var readyPanel = panels.Find(p => p.panelType == PanelType.Ready);
        readyPanel.DisableSlider();
        readyPanel.gameObject.SetActive(false);
        switch (Phase)
        {
            case ExperimentPhase.WaitingToStart:
                InitializeExperiment();
                if (preQuestionGroups.Count > 0)
                {
                    Phase = ExperimentPhase.PreQuestionnaire;
                    StartCoroutine(QuestionnaireWrapper());
                }
                else
                {
                    Phase = ExperimentPhase.Main;
                    OnReady();
                }
                break;
            case ExperimentPhase.Familiarization:
                break;
            case ExperimentPhase.PreQuestionnaire:
                break;
            case ExperimentPhase.LastQuestionnaire:
                break;
            case ExperimentPhase.Main:
                if (Stimuli.GetCurrentProgression() < 100)
                {
                    StartCoroutine(callback != null
                        ? StimulusAndCallback(callback)
                        : StimulusAndCallback(Questionnaire(postQuestionGroups, postCsvFile, true)));
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
        if (postQuestionGroups.Count > 0)
            postQuestionGroups = ResetQuestionList(postQuestionGroups);
        // We play the stimulus and wait for it to end before doing anything else
        animEnded = false;
        if (!next)
        {
            yield return StartCoroutine(Stimuli.PlayCurrentStimulus(stimParams, 1));
        }
            
        else
        {
            yield return StartCoroutine(Stimuli.PlayNextStimulus(stimParams, 1));
        }
        yield return new WaitUntil(() => animEnded == true);
        animEnded = false;
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(FadeAgent());
        if (callback != null)
            yield return StartCoroutine(callback);
        panels.Find(p => p.panelType == PanelType.Ready).gameObject.SetActive(true);
    }

    protected virtual IEnumerator FadeAgent(IEnumerator callback = null)
    {
        yield return StartCoroutine(screenFader.FadeRoutine(0, 1));
        stimParams.unityAnimator.SetTrigger("Reset");
        agent.transform.position = initPos;
        agent.SetActive(!agent.activeInHierarchy);
        yield return new WaitForSeconds(2f);
        yield return StartCoroutine(screenFader.FadeRoutine(1, 0));
        if (callback != null)
            StartCoroutine(callback);
    }

    #endregion

    #region Management of Stimuli and Questionnaires

    protected override IEnumerator InstantiatePanel(PanelType panelTypes, bool wait = false)
    {
        if (wait)
        {
            yield return new WaitForSeconds(1f);
            yield return new WaitUntil(() => !stimParams.agentAs.isPlaying);
            yield return new WaitUntil(() => !stimParams.agentBaseAnim.agentPlaying);
        }
        else 
            yield return new WaitForSeconds(0.25f);
        panels.Find(p => p.panelType == panelTypes).Enable();

    }
    #endregion

    #region CalibrationAndDebug

    public void PlayPerfectTap(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var tap = new UnityGRETAStimulus()
        {
            WaitinTime = waitTime,
            TotalTime = totalAnimationTime,
            AudioStimulus = audioStimulus,
            Pattern = pattern,
            Far = false,
            FixedGesture = false,
            GretaAnimation = "",
            UnityAnimation = "TouchTap"
        };
        StartCoroutine(tap.PlayStimulus(stimParams, this));
    }
    
    public void TestAction(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        var tap = new UnityGRETAStimulus()
        {
            WaitinTime = waitTime,
            TotalTime = totalAnimationTime,
            AudioStimulus = audioStimulus,
            Pattern = pattern,
            Far = false,
            FixedGesture = true,
            GretaAnimation = "",
            UnityAnimation = "Fixed"
        };
        StartCoroutine(tap.PlayStimulus(stimParams, this));
        //stimParams.unityAnimator.SetTrigger("TouchTap");
    }

    #endregion
}