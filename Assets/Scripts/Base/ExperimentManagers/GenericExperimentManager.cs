using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Leap.Unity.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities;
using ExperimentUtility;
using JetBrains.Annotations;
using UnityEngine.Serialization;

public class GenericExperimentManager : MonoBehaviour
{
    #region Attributes

    protected Stimuli Stimuli;

    [FormerlySerializedAs("StimParams")] [SerializeField] protected PlayStimParams stimParams;
    
    protected ExperimentPhase Phase = ExperimentPhase.WaitingToStart;

    //Managing the experimental setup
    public string participantID = "partID";
    public int bloc = 1;
    protected int CountBloc = 0;

    public List<PanelManager> panels;

    public string lang = "EN";
    [FormerlySerializedAs("fmlFolder")] public string experimentFolder = "PatternsExperiment";
    public bool animEnded = false;
    
    #endregion

    #region Initialization
    
    // Start is called before the first frame update
    protected virtual void Start()
    {
        Stimuli = new Stimuli(this);
        stimParams.filepath = Application.streamingAssetsPath + "/" + experimentFolder + "/";

    }

    // Update is called once per frame
    protected virtual void Update()
    {

    }

    //Shuffle questions and setup the stimuli to pick from
    protected virtual void InitializeExperiment()
    {
        //Do some stuff to initialize your stimuli
    }
    
    #endregion
    
    #region ExperimentFlow

    //What happens when the InputAction linked to this method is activated, typically via a keyboard press
    public virtual void StartExperiment(InputAction.CallbackContext context)
    {
        if (!context.performed) return; //Check that the keyboard button has been pressed
        if (panels.Find(p => p.panelType == PanelType.Ready) == null)
        {
            Debug.LogError("No UI panel for Ready situation was assigned : experiment cannot be run.");
        }
        //_agentAnim.PlayFML(_filepath + "ExperimentIntro");
        StartCoroutine(InstantiatePanel(PanelType.Ready)); // For simplicity we use the PanelType class but if we want new PanelTypes we can define a new PanelType class in this script file instead and adjust the corresponding Panel scripts accordingly.
    }
    
    protected virtual void RestartExperiment(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        CountBloc = 0;
        this.InitializeExperiment();
        //_agentAnim.PlayFML(_filepath + "ExperimentSupplementaryBloc");
        panels.Find(p => p.panelType == PanelType.Ready).EnableSlider();
        StartCoroutine(InstantiatePanel(PanelType.Ready));
    }
    
    protected virtual void RestartBlock()
    {
        this.InitializeExperiment();
        //_agentAnim.PlayFML(_filepath + "ExperimentSupplementaryBloc");
        panels.Find(p => p.panelType == PanelType.Ready).EnableSlider();
        StartCoroutine(InstantiatePanel(PanelType.Ready));
    }
    
    //Wrapper for OnReady without callback so that the button can connect to it.
    public virtual void OnReadyWrapper()
    {
        OnReady();
    }

    //What happens when pressing the ready button on the panel
    public virtual void OnReady(IEnumerator callback = null)
    {
        InitializeExperiment();
        if (Stimuli.GetCurrentProgression() < 100)
        {
            StartCoroutine(StimulusAndCallback(callback));
        }
        else 
            EndExperiment();

        var readyPanel = panels.Find(p => p.panelType == PanelType.Ready);
        readyPanel.DisableSlider();
        readyPanel.gameObject.SetActive(false);
    }

    public virtual void RestartStimulusWrapper(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        RestartStimulus();
    }

    public virtual void RestartStimulus(IEnumerator callback = null)
    {
        StopAllCoroutines();
        foreach (var panel in panels)
        {
            panel.gameObject.SetActive(false);
        }
        StartCoroutine(StimulusAndCallback(callback, false));
        
    }

    //What to do when we reached the end, either because there was no more stimuli to play or because we pressed a given button
    //We can restart if we set up the experiment to have more than one block of stimuli
    protected virtual void EndExperiment()
    {
        CountBloc++;
        if (CountBloc < bloc)
        {
            RestartBlock();
        }
        else
        {
            //_agentAnim.PlayFML(_filepath + "ExperimentOutro");
            StartCoroutine(InstantiatePanel(PanelType.End)); //What we want to be done at the end, here, for example, making a panel telling the participant that it is over appear
        }
    }
    
    #endregion
    
    #region Management of Stimuli and Panels

    protected virtual IEnumerator InstantiatePanel(PanelType panelType, bool wait = false)
    {
        if (wait)
            yield return new WaitForSeconds(3.0f);
        panels.Find(p => p.panelType == panelType).Enable();

    }


    //Wrapper method for managing the proper order of the experiment, as we want a method to have a very specific task to accomplish and we don't want to put sequencing elements in PlayStimulus() for example.
    protected virtual IEnumerator StimulusAndCallback(IEnumerator callback = null, bool next = true)
    {
        // We play the stimulus and wait for it to end before doing anything else
        if (!next)
            yield return StartCoroutine(Stimuli.PlayCurrentStimulus(stimParams, 1));
        else
            yield return StartCoroutine(Stimuli.PlayNextStimulus(stimParams, 1));
        if (callback != null)
            yield return StartCoroutine(callback);
        panels.Find(p => p.panelType == PanelType.Ready).gameObject.SetActive(true);
    }
    
    #endregion

    #region SaveResults

    protected virtual void SaveToCsv(string csv, string preprocessedResults = null, bool stimulus = false)
    {
        var path = stimParams.filepath + csv;
        var sw = File.AppendText(path);
        var sb = new StringBuilder();
        sb.Append(participantID);
        if (stimulus) sb.Append(Stimuli.CurrentStimulus.SaveToFile());
        if (!string.IsNullOrEmpty(preprocessedResults)) sb.Append(preprocessedResults);

        sw.WriteLine(sb);

        sw.Flush();
        sw.Close();
    }

    #endregion
    
    #region CalibrationAndDebug
    //This region offers example of failsafe methods to use to test things out or take back manual control if something goes badly, those methods are meant to be connected
    //to an InputAction such as a keyboard press.
    

    #endregion
}
