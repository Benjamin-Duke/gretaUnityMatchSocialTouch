using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using ExperimentUtility;
using Leap.Unity.Interaction;
using Newtonsoft.Json;
using UnityEngine.InputSystem;
using Utilities;

public class QuestionnaireExperimentManager : GenericExperimentManager
{
    
    #region Attributes
    
    [SerializeField] protected List<QuestionGroup> preQuestionGroups = new List<QuestionGroup>();
    [SerializeField] protected string preCsvFile = "pre_answer.csv";
    [SerializeField] protected string preQuestionnaireFileName = "pre_questionnaire.json";
    
    [SerializeField] protected List<QuestionGroup> postQuestionGroups = new List<QuestionGroup>();
    [SerializeField] protected string postCsvFile = "post_answer.csv";
    [SerializeField] protected string postQuestionnaireFileName = "post_questionnaire.json";
    
    [SerializeField] protected List<QuestionGroup> lastQuestionGroups = new List<QuestionGroup>();
    [SerializeField] protected string lastCsvFile = "last_answer.csv";
    [SerializeField] protected string lastQuestionnaireFileName = "last_questionnaire.json";

    // Here we store the conditions (if any) that determine whether questions of a questionGroup should be displayed
    // You can initialize your particular filters by overriding the Start function in the derived classes
    protected Dictionary<string, Func<Question, bool>> Filters = new Dictionary<string, Func<Question, bool>>();

    protected bool _canGoNext = true;
    protected bool _pressed = false;
    protected bool _finishQuestion = false;
    protected bool _goNextQuestion = false;
    [SerializeField] protected bool askCertainty;
    
    // To prevent going to the next question instantly when a slider might still be selected.
    protected float _failsafeCount = 0.0f;
    
    protected Question _currentQuestion;
    
    #endregion
    
    #region Initialization

    protected override void InitializeExperiment()
    {
        if (CountBloc > 0)
        {
            ResetQuestionList(postQuestionGroups);
        }
        else
        {
            if (preQuestionnaireFileName != "")
            {
                preQuestionGroups = JsonConvert.DeserializeObject<List<QuestionGroup>>(File.ReadAllText(stimParams.filepath + preQuestionnaireFileName));
                preQuestionGroups = ResetQuestionList(preQuestionGroups);
            }
            
            if (postQuestionnaireFileName != "")
            {
                postQuestionGroups = JsonConvert.DeserializeObject<List<QuestionGroup>>(File.ReadAllText(stimParams.filepath + postQuestionnaireFileName));
                postQuestionGroups = ResetQuestionList(postQuestionGroups);
            }

            if (lastQuestionnaireFileName != "")
            {
                lastQuestionGroups = JsonConvert.DeserializeObject<List<QuestionGroup>>(File.ReadAllText(stimParams.filepath + lastQuestionnaireFileName));
                lastQuestionGroups = ResetQuestionList(lastQuestionGroups);
            }
        }

    }
    
    protected virtual List<QuestionGroup> ResetQuestionList(List<QuestionGroup> questionGroup)
    {
        foreach (var qG in questionGroup)
        {
            foreach (var question in qG.questions)
            {
                if (qG.toRemember != "" && !question.condition)
                    question.UpdateText(qG.toRemember, "{}", lang);
                question.answer = "";
                question.certainty = "";
            }
            qG.questions = qG.questions.Shuffle().ToList();
            qG.toRemember = "";
        }

        return questionGroup.Shuffle().ToList();
    }
    
    #endregion
    
    #region Question Panels Functionality
    
    // Update is called once per frame
    protected override void Update()
    {
        // Making sure we can safely go to the next question
        if (_currentQuestion == null) return;
        if (!_canGoNext)
        {
            if (_pressed)
            {
                if ( _failsafeCount < 1f)
                    _failsafeCount += Time.deltaTime;
                else
                {
                    _canGoNext = true;
                    _failsafeCount = 0f;
                    panels.Find(p => p.panelType == _currentQuestion.panelType).ChangeColor(Color.white);
                }
            }
        }
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
    
    //To connect to the slider on press to change its color and reset the failsafe timer
    public void OnPress()
    {
        //Prevent from going to the next panel if the button or slider has not been unpressed.
        _canGoNext = false;
        //Secondary measure to prevent accidental advance
        //_canGoNextFailsafe = false;
        _failsafeCount = 0f;
        if (_currentQuestion == null) return;
        panels.Find(p => p.panelType == _currentQuestion.panelType).ChangeColor(Color.gray);
    }

    // To connect to the "Next" buttons from panel
    public void OnNext()
    {
        if (string.IsNullOrEmpty(_currentQuestion.answer)) return;
        if (!_canGoNext) return;
        if (askCertainty)
            if (_currentQuestion.panelType == PanelType.Emotion || _currentQuestion.panelType == PanelType.Gesture)
                _currentQuestion.certainty = Mathf.RoundToInt(panels.Find(p => p.panelType == _currentQuestion.panelType).GetComponentInChildren<InteractionSlider>().HorizontalSliderValue).ToString();
        panels.Find(p => p.panelType == _currentQuestion.panelType).gameObject.SetActive(false);
        _finishQuestion = true;
        _canGoNext = false;
        _pressed = false;
    }
    
    // Similar to OnNext but to connect to some keyboard key so that we can take over if the participant has trouble with a button
    public void AdminOnNext(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (panels.Find(p => p.panelType == PanelType.Ready).gameObject.activeInHierarchy) return;
        if (string.IsNullOrEmpty(_currentQuestion.answer)) return;
        //if (!_canGoNext) return;
        if (askCertainty)
            if (_currentQuestion.panelType == PanelType.Emotion || _currentQuestion.panelType == PanelType.Gesture)
                _currentQuestion.certainty = Mathf.RoundToInt(panels.Find(p => p.panelType == _currentQuestion.panelType).GetComponentInChildren<InteractionSlider>().HorizontalSliderValue).ToString();
        panels.Find(p => p.panelType == _currentQuestion.panelType).gameObject.SetActive(false);
        _finishQuestion = true;
    }
    
    // To connect to buttons in multiple choice panels
    public void OnSelectButton(string toRemember)
    {
        _currentQuestion.answer = toRemember;
        panels.Find(p => p.panelType == _currentQuestion.panelType).UpdateSelectionLabel(_currentQuestion.answer);
        _pressed = true;
        //_canGoNext = true;
    }
    
    // To connect to slider value change
    public void OnSelectSlider()
    {
        if (_currentQuestion.panelType == PanelType.Slider)
            _currentQuestion.answer = Mathf.RoundToInt(panels.Find(p => p.panelType == _currentQuestion.panelType).GetComponentInChildren<InteractionSlider>().HorizontalSliderValue).ToString();
        /*
        else if (_currentQuestion.panelType == PanelType.Age)
        {
            _currentQuestion.answer = Mathf.RoundToInt(panels.Find(p => p.panelType == _currentQuestion.panelType).GetComponentInChildren<InteractionSlider>().HorizontalSliderValue).ToString();
            panels.Find(p => p.panelType == _currentQuestion.panelType).UpdateSelectionLabel(_currentQuestion.answer);
        }
        */
        else
        {
            _currentQuestion.certainty = Mathf.RoundToInt(panels.Find(p => p.panelType == _currentQuestion.panelType).GetComponentInChildren<InteractionSlider>().HorizontalSliderValue).ToString();
        }

        _pressed = true;

        //_canGoNext = true;
    }
    
    //For age obtention
    public void DecadeUp()
    {
        var panel = panels.Find(p => p.panelType == _currentQuestion.panelType);
        if (int.TryParse(panel.selectionLabel.text, out int number))
        {
            number += 10;
            _currentQuestion.answer = number.ToString();
            panel.UpdateSelectionLabel(_currentQuestion.answer);
            _pressed = true;
        }
    }
    
    public void DecadeDown()
    {
        var panel = panels.Find(p => p.panelType == _currentQuestion.panelType);
        if (int.TryParse(panel.selectionLabel.text, out int number))
        {
            number -= 10;
            if (number < 18)
                number = 18;
            _currentQuestion.answer = number.ToString();
            panel.UpdateSelectionLabel(_currentQuestion.answer);
            _pressed = true;
        }
    }
    
    public void AgeUnitUp()
    {
        var panel = panels.Find(p => p.panelType == _currentQuestion.panelType);
        if (int.TryParse(panel.selectionLabel.text, out int number))
        {
            number += 1;
            _currentQuestion.answer = number.ToString();
            panel.UpdateSelectionLabel(_currentQuestion.answer);
            _pressed = true;
        }
    }
    
    public void AgeUnitDown()
    {
        var panel = panels.Find(p => p.panelType == _currentQuestion.panelType);
        if (int.TryParse(panel.selectionLabel.text, out int number))
        {
            number -= 1;
            if (number < 18)
            {
                number = 18;
            }
            _currentQuestion.answer = number.ToString();
            panel.UpdateSelectionLabel(_currentQuestion.answer);
            _pressed = true;
        }
    }

    protected virtual IEnumerator QuestionnaireWrapper()
    {
        if (Phase == ExperimentPhase.PreQuestionnaire)
        {
            yield return StartCoroutine(Questionnaire(preQuestionGroups, preCsvFile));
            Phase = ExperimentPhase.Main;
        }
        else if (Phase == ExperimentPhase.LastQuestionnaire)
        {
            yield return StartCoroutine(Questionnaire(lastQuestionGroups, lastCsvFile));
            EndExperiment();
        }
        else
        {
            yield break;
        }
        panels.Find(p => p.panelType == PanelType.Ready).gameObject.SetActive(true);
    }
    
    protected virtual IEnumerator PresentQuestion(Question q)
    {
        var progression = Stimuli.GetCurrentProgression() + "%";
        if (Phase == ExperimentPhase.PreQuestionnaire || Phase == ExperimentPhase.LastQuestionnaire)
            progression = "";
        _currentQuestion = q;
        var panelToMonitor = panels.Find(p => p.panelType == q.panelType);
        panelToMonitor.UpdateLabels(q.GetQuestionText(lang), q.answer,progression);
        StartCoroutine(InstantiatePanel(q.panelType, true));
        yield return new WaitUntil(() => _finishQuestion);
        _finishQuestion = false;
        q.answer = _currentQuestion.answer;
        if (askCertainty)
            if (q.panelType == PanelType.Emotion || q.panelType == PanelType.Gesture)
                q.certainty = _currentQuestion.certainty;
        _goNextQuestion = true;
    }
    
    protected virtual void PresentQuestion(Question q, string toRemember)
    {
        q.UpdateText("{}",toRemember, lang);
        StartCoroutine(PresentQuestion(q));
    }
    #endregion

    //What to do when we reached the end, either because there was no more stimuli to play or because we pressed a given button
    //We can restart if we set up the experiment to have more than one block of stimuli
    protected override void EndExperiment()
    {
        CountBloc++;
        if (CountBloc < bloc)
        {
            RestartBlock();
        }
        else if (Phase == ExperimentPhase.Main && lastQuestionGroups.Count > 0)
        {
            Phase = ExperimentPhase.LastQuestionnaire;
            StartCoroutine(QuestionnaireWrapper());
        }
        else
        {
            panels.Find(p => p.panelType == PanelType.End).Enable(); //What we want to be done at the end, here, for example, making a panel telling the participant that it is over appear
        }
    }
    protected override IEnumerator StimulusAndCallback(IEnumerator callback = null, bool next = true)
    {
        if (Phase != ExperimentPhase.Main) yield break;
        if (postQuestionGroups.Count > 0)
            postQuestionGroups = ResetQuestionList(postQuestionGroups);
        StartCoroutine(base.StimulusAndCallback(callback, next));
    }

    public override void RestartStimulus(IEnumerator callback = null)
    {
        if (Phase != ExperimentPhase.Main) return;
        _currentQuestion = null;
        _finishQuestion = false;
        _goNextQuestion = false;
        if (postQuestionGroups.Count > 0)
        {
            postQuestionGroups = ResetQuestionList(postQuestionGroups);
            base.RestartStimulus(callback ?? Questionnaire(postQuestionGroups, postCsvFile, true));
        }
        else
        {
            base.RestartStimulus(callback);
        }
    }

    //Writing a full line of answers for either a pre/last questionnaire or a stimulus' post-questions, if need to save data
    protected virtual void SaveToCsv(List<QuestionGroup> questionGroups, string csv, bool stimulus = false)
    {
        var sb = new StringBuilder();

        var allQuestions = new List<Question>();
        allQuestions = questionGroups.Aggregate(allQuestions, (current, questionGroup) => current.Concat(questionGroup.questions).ToList());
        for (int i = 1; i <= allQuestions.Count; i++)
        {
            sb.Append(";");
            var question = allQuestions.Find(q => q.id == i);
            sb.Append(question.answer);
            if (!askCertainty) continue;
            if (question.panelType != PanelType.Emotion && question.panelType != PanelType.Gesture) continue;
            sb.Append(";");
            sb.Append(question.certainty);
        }
        
        SaveToCsv(csv, sb.ToString(),stimulus);
        
    }

    // Callback to manage a per stimulus questionnaire
    protected virtual IEnumerator Questionnaire(List<QuestionGroup> questionGroups, string csvToSave, bool stimulus = false)
    {
        // We manage the questionnaire
        foreach (var questionGroup in questionGroups)
        {
            if (questionGroup.conditionalGroup)
            {
                StartCoroutine(PresentQuestion(questionGroup.questions.Find(q => q.condition)));
                yield return new WaitUntil(() => _goNextQuestion);
                _goNextQuestion = false;
                questionGroup.toRemember = _currentQuestion.answer;
                foreach (var question in questionGroup.questions.FindAll(q => !q.condition))
                {
                    if (Filters.TryGetValue(questionGroup.filter, out var filter))
                        if (filter.Invoke(question))
                            continue;
                    PresentQuestion(question, questionGroup.toRemember);
                    yield return new WaitUntil(() => _goNextQuestion);
                    _goNextQuestion = false;
                }
            }
            else
            {
                foreach (var question in questionGroup.questions)
                {
                    StartCoroutine(PresentQuestion(question));
                    yield return new WaitUntil(() => _goNextQuestion);
                    _goNextQuestion = false;
                }
            }
        }
        SaveToCsv(questionGroups,csvToSave,stimulus);
        
    }
}
