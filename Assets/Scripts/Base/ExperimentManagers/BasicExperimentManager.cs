using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Leap.Unity.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;
using ExperimentUtility;

public class BasicExperimentManager : GenericExperimentManager
{
    #region Attributes

    [SerializeField] private List<string> animations;
    
    [SerializeField] private List<string> patterns;
    
    [SerializeField] private List<AudioClip> audioStimuli;

    // Combinations to test in the experiment (Conditions for preparing the stimuli)
    public bool visualAudioVibro = false;
    public bool visualAudio = false;
    public bool visualHaptic = false;
    public bool visual = false;

    [Tooltip("GameObject with the GRETAnimationManager for the Agent Character")]
    public GameObject agent;
    
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
        //Otherwise, we can go ahead
        base.InitializeExperiment();
        Stimuli.ResetTotalCount();

        if (visualAudioVibro)
        {
            foreach (var anim in animations)
            {
                var stimulus = new MultiModalGRETAStimulus()
                {
                    Animation = anim
                };

                if (anim == "NeutralHitL") stimulus.WaitinTime = 1f;

                if (anim == "NeutralStrokeL") stimulus.WaitinTime = 1.05f;

                if (anim == "NeutralIrregularTapL") stimulus.WaitinTime = 0.7f;

                foreach (var patt in patterns)
                {
                    stimulus.Pattern = patt;
                
                    foreach (var audioSt in audioStimuli)
                    {
                        stimulus.AudioStimulus = audioSt;
                        Stimuli.Add(new MultiModalGRETAStimulus()
                        {
                            Animation = stimulus.Animation,
                            AudioStimulus = stimulus.AudioStimulus,
                            Pattern = stimulus.Pattern,
                            WaitinTime = stimulus.WaitinTime
                        });
                    }
                }
            }
        }

        if (visualAudio)
        {
            foreach (var anim in animations)
            {
                var stimulus = new MultiModalGRETAStimulus()
                {
                    Animation = anim
                };
                
                if (anim == "NeutralHitL") stimulus.WaitinTime = 1f;

                if (anim == "NeutralStrokeL") stimulus.WaitinTime = 1.05f;

                if (anim == "NeutralIrregularTapL") stimulus.WaitinTime = 0.7f;
            
                foreach (var audioSt in audioStimuli)
                {
                    stimulus.AudioStimulus = audioSt;
                    Stimuli.Add(new MultiModalGRETAStimulus()
                    {
                        Animation = stimulus.Animation,
                        AudioStimulus = stimulus.AudioStimulus,
                        Pattern = stimulus.Pattern,
                        WaitinTime = stimulus.WaitinTime
                    });
                }
            }
        }

        if (visualHaptic)
        {
            foreach (var anim in animations)
            {
                var stimulus = new MultiModalGRETAStimulus()
                {
                    Animation = anim
                };
                
                if (anim == "NeutralHitL") stimulus.WaitinTime = 1f;

                if (anim == "NeutralStrokeL") stimulus.WaitinTime = 1.05f;

                if (anim == "NeutralIrregularTapL") stimulus.WaitinTime = 0.7f;
            
                foreach (var patt in patterns)
                {
                    stimulus.Pattern = patt;
                    Stimuli.Add(new MultiModalGRETAStimulus()
                    {
                        Animation = stimulus.Animation,
                        AudioStimulus = stimulus.AudioStimulus,
                        Pattern = stimulus.Pattern,
                        WaitinTime = stimulus.WaitinTime
                    });
                }
            }
        }

        if (visual)
        {
            foreach (var anim in animations)
            {
                var stimulus = new MultiModalGRETAStimulus()
                {
                    Animation = anim
                };
                if (anim == "NeutralHitL") stimulus.WaitinTime = 1f;

                if (anim == "NeutralStrokeL") stimulus.WaitinTime = 1.05f;

                if (anim == "NeutralIrregularTapL") stimulus.WaitinTime = 0.7f;
                Stimuli.Add(new MultiModalGRETAStimulus()
                {
                    Animation = stimulus.Animation,
                    AudioStimulus = stimulus.AudioStimulus,
                    Pattern = stimulus.Pattern,
                    WaitinTime = stimulus.WaitinTime
                });
            }
        }

        Stimuli.ShuffleStimuli();
    }

    #endregion

    #region ExperimentFlow

    // If we need to override something of the experiment flow
    
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

    public void PlayPerfectStroke(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var stroke = new MultiModalGRETAStimulus()
        {
            Animation = animations.Find(a => a.Contains("Stroke")),
            AudioStimulus = audioStimuli.Find(a => a.ToString().Contains("stroke")),
            Pattern = patterns.Find(a => a.Contains("stroke")),
            WaitinTime = 1.05f
        };
        StartCoroutine(stroke.PlayStimulus(stimParams, this));
    }
    
    public void PlayPerfectHit(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var hit = new MultiModalGRETAStimulus()
        {
            Animation = animations.Find(a => a.Contains("Hit")),
            AudioStimulus = audioStimuli.Find(a => a.ToString().Contains("hit")),
            Pattern = patterns.Find(a => a.Contains("hit")),
            WaitinTime = 1f
        };
        StartCoroutine(hit.PlayStimulus(stimParams, this));
    }
    
    public void PlayPerfectTap(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var tap = new MultiModalGRETAStimulus()
        {
            Animation = animations.Find(a => a.Contains("Tap")),
            AudioStimulus = audioStimuli.Find(a => a.ToString().Contains("tap")),
            Pattern = patterns.Find(a => a.Contains("tap")),
            WaitinTime = 0.7f
        };
        StartCoroutine(tap.PlayStimulus(stimParams, this));;
    }

    #endregion
}