using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using Utilities;

namespace ExperimentUtility
{
    [Serializable] public class Question
    {
        private static int _currentID;
        
        protected Question()
        {
            this.id = GetNextID();
            this.answer = "NA";
            this.certainty = "NA";
            this.condition = false;
        }
        
        protected Question(Dictionary<string, string> questionsText, string answer = "NA", string certnty = "NA", bool depend = false/*, bool display = true*/)
        {
            this.id = GetNextID();
            this.answer = answer;
            this.certainty = certnty;
            this.condition = depend;
            //this.display = display;
        }

        static Question() => _currentID = 0;
        
        protected int GetNextID() => ++_currentID;
        
        // The id is used when writing the questions in the file so that it should be in the correct order, you should give ascending numbers as id
        // And the ids should keep increasing even throughout the question groups
        public int id;
        public string answer;
        public Dictionary<string, string> text;
        public PanelType panelType;
        public string certainty;
        // Because we have some questions whose label need to be updated based on another answer, we use this attribute to know when we can't fully shuffle
        // Should be true when we want this question to be asked first in a given QuestionGroup
        public bool condition;

        public virtual string GetQuestionText()
        {
            if (text.TryGetValue("EN", out var txt)) return txt;
            return "No text found";
        }
        public virtual string GetQuestionText(string lang)
        {
            if (text.TryGetValue(lang, out var txt)) return txt;
            return "No text found";
        }

        public virtual void UpdateText(string oldVal, string replacement, string lang = null)
        {
            if (lang != null && text.ContainsKey(lang))
            {
                text[lang] = text[lang].Replace(oldVal, replacement);
            }
            else if (text.ContainsKey("EN"))
            {
                text["EN"] = text["EN"].Replace(oldVal, replacement);
            }
        }

        // If we don't want a specific question asked, set this to false
        //public bool display;
    }
    
    /*
    [Serializable] public class LocalizedQuestion: Question
    {
        public LocalizedQuestion() {}
        public LocalizedQuestion(Dictionary<string, string> questionsText, string answer = "NA", string certnty = "NA", bool depend = false) : base(answer, certnty, depend)
        {
            this.text = questionsText;
        }
        public Dictionary<string,string> text;

        public override string GetQuestionText(string lang)
        {
            if (text.TryGetValue(lang, out var txt)) return txt;
            return "No text found";
        }
    }
    
    [Serializable] public class BasicQuestion: Question
    {
        public BasicQuestion() {}
        public BasicQuestion(string questionText, string answer = "NA", string certnty = "NA", bool depend = false) : base(answer, certnty, depend)
        {
            this.text = questionText;
        }
        public string text;
        
        public override void UpdateText(string oldVal, string replacement)
        {
            text = text.Replace(oldVal, replacement);
        }
        
        public override string GetQuestionText()
        {
            return text;
        }
    }
    */

    [Serializable] public class QuestionGroup
    {
        public QuestionGroup(int id, List<Question> questions, string filter = "", bool condGroup = false)
        {
            this.id = id;
            this.questions = questions;
            this.conditionalGroup = condGroup;
            this.filter = filter;
        }
        public List<Question> questions;
        public int id;
        //This allows us to know when we will need to remember an answer and to present the condition question first despite the shuffling.
        //Preferably, the id of the condition question needs to be smaller than the id of the questions that depend on it
        public bool conditionalGroup;
        //We store here the word we need to use to complete the text of the following questions. 
        public string toRemember;
        public string filter;
    }
    
    [JsonConverter(typeof(StringEnumConverter))]  
    [Serializable] public enum PanelType
    {
        Emotion,
        Gesture,
        Slider,
        Ready,
        Gender,
        Age,
        End,
        Agency,
        NoAgency,
        Pause
    }
    
    [Serializable] public enum ExperimentPhase
    {
        WaitingToStart,
        Familiarization,
        PreQuestionnaire,
        LastQuestionnaire,
        Main
    }

    //To generalize the calls to PlayStimulus in IStimulus classes.
    //This is not ideal as many attributes could be useless for other types of stimuli but there is no way to make it fully flexible in a non-redundant manner.
    //It could be enough to use a params object[] parameter instead in the PlayStimulus method definition of IStimulus, but this is error prone as we need to carefully monitor
    //for the order in which we pass our arg array and decode it appropriately. At the moment, I'd rather have a quickly understandable data structure.
    //Since we don't have other types of stimuli for now, and we can't anticipate what they may look like in the future, this will do.
    [Serializable]
    public class PlayStimParams
    {
        public string filepath;
        public GameObject agent;
        //Attributes to play a MultimodalGreta stimulus (audio, haptic pattern and animation)
        public AudioSource agentAs;
        public GRETAnimationManagerDEMO agentAnim;
        public GretaAnimatorBridge agentBaseAnim;
        public AudioSource stimulusAs;
        public TCPCommunication sleeveConnector;
        public Animator unityAnimator;
        public ScreenFader scrnFader;

        public PlayStimParams(string fp, GameObject a, AudioSource aas, GRETAnimationManagerDEMO aan, GretaAnimatorBridge aba, TCPCommunication sc, Animator anim, ScreenFader sfd)
        {
            filepath = fp;
            agent = a;
            agentAs = aas;
            agentAnim = aan;
            agentBaseAnim = aba;
            sleeveConnector = sc;
            unityAnimator = anim;
            stimulusAs = sleeveConnector.touchHandTarget.GetComponent<AudioSource>();
            scrnFader = sfd;
        }
    }

    public interface IStimulus
    {
        public IEnumerator PlayStimulus(PlayStimParams args, MonoBehaviour coroutineRunner, int times = 1);

        public string SaveToFile();
    }

    public class Stimuli
    {
        protected MonoBehaviour CoroutineRunner;
        protected Stack<IStimulus> StimuliStack = new Stack<IStimulus>();
        public IStimulus CurrentStimulus { get; set; }
        protected int StimuliTotalCount = 0;

        public Stimuli(MonoBehaviour coroutineRunner)
        {
            CoroutineRunner = coroutineRunner;
        }

        public int GetCurrentProgression()
        {
            if (StimuliTotalCount < 1) return -1;
            return Mathf.RoundToInt((StimuliTotalCount - StimuliStack.Count) / (float)StimuliTotalCount * 100f);
        }

        public IEnumerator PlayNextStimulus(PlayStimParams args, int times = 1)
        {
            if (StimuliStack.Count == 0)
                yield break;
            CurrentStimulus = StimuliStack.Pop();
            yield return CoroutineRunner.StartCoroutine(CurrentStimulus.PlayStimulus(args, CoroutineRunner, times));
        }

        public IEnumerator PlayCurrentStimulus(PlayStimParams args, int times = 1)
        {
            yield return CoroutineRunner.StartCoroutine(CurrentStimulus.PlayStimulus(args, CoroutineRunner, times));
        }

        public void Add(IStimulus stimulus)
        {
            StimuliStack.Push(stimulus);
            StimuliTotalCount = StimuliStack.Count();
        }

        public void ShuffleStimuli()
        {
            StimuliStack = new Stack<IStimulus>(StimuliStack.Shuffle());
        }

        public void ResetTotalCount()
        {
            StimuliTotalCount = StimuliStack.Count;
        }

    }

    public class MultiModalGRETAStimulus : IStimulus
    {
        public AudioClip AudioStimulus { get; set; }
        public string Pattern { get; set; }
        public string Animation { get; set; }
        public float WaitinTime { get; set; }

        public IEnumerator PlayStimulus(PlayStimParams args, MonoBehaviour coroutineRunner, int times = 1)
        {
            if (times < 1) yield break;
            yield return new WaitUntil(() => !args.agentAs.isPlaying);
            var debugLog = "Attempting to play stimulus : " + Animation;
            Debug.Log(debugLog);
            args.agentAnim.PlayFML(args.filepath + Animation);
            yield return new WaitForSeconds(WaitinTime);
            if (args.agentBaseAnim.agentPlaying)
            {
                if (AudioStimulus != null)
                {
                    debugLog = "with " + AudioStimulus.ToString();
                    Debug.Log(debugLog);
                    //StartCoroutine(sleeveConnector.PlayAudio(stim.AudioStimulus));
                    args.stimulusAs.clip = AudioStimulus;
                    args.stimulusAs.Play();
                }

                if (!string.IsNullOrEmpty(Pattern))
                {
                    debugLog = "and " + Pattern;
                    Debug.Log(debugLog);
                    //StartCoroutine(sleeveConnector.PlayPattern(stim.Pattern));
                    yield return coroutineRunner.StartCoroutine(args.sleeveConnector.PlayPattern(Pattern));
                }
            }
            else
            {
                Debug.Log("Animation did not trigger, trying again now.");
                yield return coroutineRunner.StartCoroutine(PlayStimulus(args, coroutineRunner, times));
            }

            yield return new WaitUntil(() => !args.agentBaseAnim.agentPlaying);
            yield return coroutineRunner.StartCoroutine(PlayStimulus(args, coroutineRunner, times - 1));
        }

        public string SaveToFile()
        {
            return ";" + AudioStimulus + ";" + Pattern + ";" + Animation;
        }
    }
    
    public class UnityGRETAStimulus : IStimulus
    {
        public AudioClip AudioStimulus { get; set; }
        public string Pattern { get; set; }
        public string GretaAnimation { get; set; }
        public string UnityAnimation { get; set; }
        public float WaitinTime { get; set; }
        public float TotalTime { get; set; }
        public bool FixedGesture { get; set; }
        
        public Vector3 Pos { get; set; }
        
        public bool Far { get; set; }

        public IEnumerator PlayStimulus(PlayStimParams args, MonoBehaviour coroutineRunner, int times = 1)
        {
            if (times < 1)
            {
                if (FixedGesture || Far || UnityAnimation == "")
                {
                    coroutineRunner.GetComponent<FTExperimentManager>().animEnded = true;
                }
                yield break;
            }
            var debugLog = "Attempting to play stimulus : " + UnityAnimation;
            
            //Fading the agent in
            yield return coroutineRunner.StartCoroutine(args.scrnFader.FadeRoutine(0, 1));
            args.unityAnimator.SetTrigger("Reset");
            if (Far)
            {
                args.agent.transform.position = Pos;
            }
            args.agent.SetActive(true);
            if (FixedGesture && UnityAnimation != "")
            {
                args.unityAnimator.SetTrigger(UnityAnimation);
            }
            yield return new WaitForSeconds(2f);
            yield return coroutineRunner.StartCoroutine(args.scrnFader.FadeRoutine(1, 0));
            
            if (GretaAnimation != "")
            {
                yield return new WaitUntil(() => !args.agentAs.isPlaying);
                Debug.Log(debugLog);
                args.agentAnim.PlayFML(args.filepath + GretaAnimation);
                yield return new WaitForSeconds(0.1f);
                if (!args.agentBaseAnim.agentPlaying)
                {
                    Debug.Log("Animation did not trigger, trying again now.");
                    yield return coroutineRunner.StartCoroutine(PlayStimulus(args, coroutineRunner, times));
                }
            }

            if (UnityAnimation != "" && !FixedGesture)
            {
                args.unityAnimator.SetTrigger(UnityAnimation);
            }
            yield return new WaitForSeconds(WaitinTime);
            if (AudioStimulus != null)
            {
                debugLog = "with " + AudioStimulus.ToString();
                Debug.Log(debugLog);
                //StartCoroutine(sleeveConnector.PlayAudio(stim.AudioStimulus));
                args.stimulusAs.clip = AudioStimulus;
                args.stimulusAs.Play();
            }
            if (!string.IsNullOrEmpty(Pattern))
            {
                debugLog = "and " + Pattern;
                Debug.Log(debugLog);
                //StartCoroutine(sleeveConnector.PlayPattern(stim.Pattern));
                coroutineRunner.StartCoroutine(args.sleeveConnector.PlayPattern(Pattern));
            }

            if (GretaAnimation != "")
            {
                yield return new WaitUntil(() => !args.agentBaseAnim.agentPlaying);
            }

            if (FixedGesture || Far || UnityAnimation == "")
            {
                yield return new WaitForSeconds(TotalTime - WaitinTime);
            }
            yield return coroutineRunner.StartCoroutine(PlayStimulus(args, coroutineRunner, times - 1));
        }

        public string SaveToFile()
        {
            var sb = new StringBuilder();
            sb.Append(";");
            sb.Append(AudioStimulus != null ? AudioStimulus.ToString() : "");
            sb.Append(";");
            sb.Append(Pattern);
            sb.Append(";");
            if (UnityAnimation == "")
            {
                sb.Append(Far ? "Far" : "Close");
            }
            else
            {
                sb.Append(UnityAnimation);
            }
            return sb.ToString();
        }
    }
    
    public class AnticipationStimulus : IStimulus
    {
        public string FacialExpression { get; set; }
        public string Gesture { get; set; }
        //public float FadeTime { get; set; }
        //public float TotalTime { get; set; }
        public int PositionFinale { get; set; }
        public bool Agency { get; set; }

        public IEnumerator PlayStimulus(PlayStimParams args, MonoBehaviour coroutineRunner, int times = 1)
        {
            if (times < 1)
                yield break;
            AnticipationEM cRunner = (AnticipationEM)coroutineRunner;
            /*if (times < 1)
            {
                if (FixedGesture || Far || UnityAnimation == "")
                {
                    coroutineRunner.GetComponent<FTExperimentManager>().animEnded = true;
                }
                yield break;
            }*/
            var debugLog = "Attempting to play stimulus : " + FacialExpression + " with " + PositionFinale;
            
            yield return coroutineRunner.StartCoroutine(args.scrnFader.FadeRoutine(0, 1, 1f));
            args.unityAnimator.SetTrigger("Reset");
            yield return cRunner.StartCoroutine(PlayFacialExpression(args.agentAnim, args.agentBaseAnim,
                coroutineRunner, args.filepath + "Neutral", 0f));
            yield return cRunner.StartCoroutine(PlayFacialExpression(args.agentAnim, args.agentBaseAnim,
                coroutineRunner, args.filepath + "Neutral", 0f));
            yield return new WaitForSeconds(2f);
            yield return coroutineRunner.StartCoroutine(args.scrnFader.FadeRoutine(1, 0, 1f));

            if (Agency)
            {
                cRunner.ToggleStimPanel(PanelType.Agency);
                yield return new WaitUntil(() => cRunner.agencyPressed);
                cRunner.agencyPressed = false;
                cRunner.ToggleStimPanel(PanelType.Agency);
            }
            else
            {
                cRunner.ToggleStimPanel(PanelType.NoAgency);
                yield return new WaitForSeconds(2f);
                cRunner.ToggleStimPanel(PanelType.NoAgency);
            }

            if (FacialExpression != "")
            {
                Debug.Log(debugLog);
                if (FacialExpression != "Neutral")
                {
                    yield return cRunner.StartCoroutine(PlayFacialExpression(args.agentAnim, args.agentBaseAnim,
                        coroutineRunner, args.filepath + FacialExpression, 0.5f));
                }
                yield return new WaitForSeconds(1f);
            }

            if (Gesture != "")
            {
                args.unityAnimator.Play(Gesture, 2);
                yield return new WaitUntil((() => args.unityAnimator.GetCurrentAnimatorStateInfo(2).normalizedTime >= 1 && args.unityAnimator.GetCurrentAnimatorStateInfo(2).IsName(Gesture)));
            }

            if (PositionFinale != 0)
            {
                //Fading the position in
                coroutineRunner.StartCoroutine(args.scrnFader.FadeRoutine(0, 1));
                args.unityAnimator.Play("PF", 2, PositionFinale/400f);
                yield return coroutineRunner.StartCoroutine(args.scrnFader.FadeRoutine(1, 0));
            }
            
            
            yield return coroutineRunner.StartCoroutine(PlayStimulus(args, coroutineRunner, times - 1));
        }

        public IEnumerator PlayFacialExpression(GRETAnimationManagerDEMO gretAnim, GretaAnimatorBridge gretAnimBridge, MonoBehaviour coroutineRunner, string file, float delay = 0.1f)
        {
            gretAnim.PlayFML(file);
            if (delay == 0f) yield break;
            yield return new WaitForSeconds(delay);
            if (gretAnimBridge.agentPlaying) yield break;
            Debug.Log("Animation did not trigger, trying again now.");
            yield return coroutineRunner.StartCoroutine(PlayFacialExpression(gretAnim, gretAnimBridge, coroutineRunner, file));
        }

        public string SaveToFile()
        {
            var sb = new StringBuilder();
            sb.Append(";");
            sb.Append(FacialExpression ?? "");
            sb.Append(";");
            sb.Append(Agency ? "True" : "False");
            sb.Append(";");
            sb.Append(PositionFinale.ToString() ?? "");
            return sb.ToString();
        }
    }

    public class Stimulus
    {
        public AudioClip AudioStimulus { get; set; }
        public string Pattern { get; set; }
        public string Animation { get; set; }
        public float WaitinTime { get; set; }
    }
}
