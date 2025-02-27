using System.Collections.Generic;
using System.Globalization;
using AutobiographicMemory;
using TactilePerception;
using UnityEngine;
using UnityEngine.Serialization;
using WellFormedNames;

/**
 * The purpose of this script is to be the link between FAtiMA actions model
 * and Unity animations, touch, etc. This is a separate class because
 * it is completely ad-hoc.
 * 
 * For example, when the human touches Camille, this script will generate a FAtiMA action
 * and send it to FAtiMA.
 * 
 * Human and agent names (in FAtiMA's model) will be taken from FAtiMAManager values.
 * 
 * Then, when FAtiMA decides what to do, this script will "convert" the decision
 * into a concrete animation, gesture, or whatever it could be in the Unity/Greta world.
 */
public class FAtiMATouchAnimationBridge : MonoBehaviour
{
    [Tooltip("GameObject with a FAtiMAManager script, to send and receive actions from FAtiMA")]
    public GameObject fatimaManager;

    [FormerlySerializedAs("handTouchManager")] [Tooltip("GameObject with a HandTouchManager script, to subscribe to touch events from human right hand to Camille")]
    public GameObject rightHandTouchManager;
    
    [Tooltip("GameObject with a HandTouchManager script, to subscribe to touch events from human left hand to Camille")]
    public GameObject leftHandTouchManager;

    [Tooltip(
        "GameObject with a DistanceInterpretation script, to subscribe to changes of distance between Camille and human")]
    public GameObject distanceInterpretation;

    [Tooltip("GameObject with a LookCamille script, to subscribe to changes of look from human to Camille")]
    public GameObject lookCamille;

    private DistanceInterpretation _distanceInterpretation;

    private FAtiMAManager _fatimaManager;
    private TouchPerceptionManager _rightHandTouchManager;
    private TouchPerceptionManager _leftHandTouchManager;
    private LookCamille _lookCamille;

    [Tooltip(
        "GameObject with a ExperimentManager, to alert of a human touch while we figure out how to properly take touch into account.")]
    public GameObject experimentManager;
    private Experiment1Manager _experiment1Manager;
    
    [Tooltip(
        "GameObject with a DemoManager, to alert of a human touch while we figure out how to properly take touch into account.")]
    public GameObject demoManager;
    private DemoManager _demoManager;

    private void Start()
    {
        _fatimaManager = fatimaManager.GetComponent<FAtiMAManager>();
        if (_fatimaManager == null)
            Debug.LogError("No FAtiMAManager script found : won't send nor receive events to FAtiMA.");

        _experiment1Manager = experimentManager.GetComponent<Experiment1Manager>();
        if (_experiment1Manager == null)
            Debug.LogError("No ExperimentManager script found : won't be able to alert the scenario of touches on agent.");

        _demoManager = demoManager.GetComponent<DemoManager>();
        if (_demoManager == null)
            Debug.LogError("No DemoManager script found : won't be able to alert the scenario of touches on agent.");

        
        _rightHandTouchManager = rightHandTouchManager.GetComponent<TouchPerceptionManager>();
        _leftHandTouchManager = leftHandTouchManager.GetComponent<TouchPerceptionManager>();
        if (_leftHandTouchManager == null && _rightHandTouchManager == null)
        {
            Debug.LogWarning("No HandTouchManager script found : won't send touch events to FAtiMA");
        }
        else
        {
            // Subscribe to all events so we can send FAtiMA events in realtime
            _rightHandTouchManager.TouchStarted += OnTouchStarted;
            _rightHandTouchManager.TouchChanged += OnTouchChanged;
            _rightHandTouchManager.TouchEnded += OnTouchEnded;
            _rightHandTouchManager.EtherealBodyEntered += OnEtherealBodyEntered;
            _rightHandTouchManager.EtherealBodyLeft += OnEtherealBodyLeft;
            
            _leftHandTouchManager.TouchStarted += OnTouchStarted;
            _leftHandTouchManager.TouchChanged += OnTouchChanged;
            _leftHandTouchManager.TouchEnded += OnTouchEnded;
            _leftHandTouchManager.EtherealBodyEntered += OnEtherealBodyEntered;
            _leftHandTouchManager.EtherealBodyLeft += OnEtherealBodyLeft;
        }

        _distanceInterpretation = distanceInterpretation.GetComponent<DistanceInterpretation>();
        if (_distanceInterpretation == null)
            Debug.LogWarning("No DistanceInterpretation script found : won't send proximity events to FAtiMA");
        else
            _distanceInterpretation.DistanceInterpretationChanged += OnDistanceChanged;
        _lookCamille = lookCamille.GetComponent<LookCamille>();
        if (_lookCamille == null) Debug.LogWarning("No LookCamille script found : won't send look events to FAtiMA");
        else
            _lookCamille.LookAtCamilleChanged += OnLookChanged;
    }

    #region Event subscribers
    private void OnTouchStarted(object sender, TouchPerceptionManager.TouchEventArgs e)
    {
        _fatimaManager.DoHumanAction(
            "TouchEvent",
            TouchEventArgsToList(e),
            AMConsts.ACTION_START
        );
        //_fatimaManager.DoAgentNonSpeakAction();
        //_experiment1Manager.AgentTouched();
        _experiment1Manager.AgentTouched(true);
    }

    private void OnTouchChanged(object sender, TouchPerceptionManager.TouchEventArgs e)
    {
        _fatimaManager.DoHumanAction(
            "TouchEvent",
            TouchEventArgsToList(e),
            AMConsts.ACTION_UPDATE
        );
        //_fatimaManager.DoAgentNonSpeakAction();
    }

    private void OnTouchEnded(object sender, TouchPerceptionManager.TouchEventArgs e)
    {
        _fatimaManager.DoHumanAction(
            "TouchEvent",
            TouchEventArgsToList(e),
            AMConsts.ACTION_END
        );
        //_fatimaManager.DoAgentNonSpeakAction();
        _experiment1Manager.AgentTouched(false);
    }

    private void OnEtherealBodyEntered(object sender, TouchPerceptionManager.EtherealBodyEventArgs e)
    {
        _fatimaManager.DoHumanAction(
            "EtherealBodyEvent",
            new List<Name>
            {
                (Name) ((decimal)e.Start).ToString(CultureInfo.InvariantCulture),
                (Name) ((decimal)e.Duration).ToString(CultureInfo.InvariantCulture)
            },
            AMConsts.ACTION_START
        );
        //_fatimaManager.DoAgentNonSpeakAction();
    }

    private void OnEtherealBodyLeft(object sender, TouchPerceptionManager.EtherealBodyEventArgs e)
    {
        _fatimaManager.DoHumanAction(
            "EtherealBodyEvent",
            new List<Name>
            {
                (Name) ((decimal)e.Start).ToString(CultureInfo.InvariantCulture),
                (Name) ((decimal)e.Duration).ToString(CultureInfo.InvariantCulture)
            },
            AMConsts.ACTION_END
        );
        //_fatimaManager.DoAgentNonSpeakAction();
    }

    private void OnDistanceChanged(object sender, DistanceInterpretation.DistanceInterpretationEventArgs e)
    {
        _fatimaManager.DoHumanAction(
            "DistanceChanged",
            new List<Name> { (Name) e.DistanceInterpretationClass},
            AMConsts.ACTION_END
        );
        //_fatimaManager.DoAgentNonSpeakAction();
    }

    private void OnLookChanged(object send, LookCamille.LookAtCamilleEventArgs e)
    {
        _fatimaManager.DoHumanAction(
            "LookDirectionChanged",
            new List<Name>
            {
                (Name) e.LookingBody.ToString(),
                (Name) e.LookingHead.ToString(),
                (Name) e.LookingEye.ToString()
            },
            AMConsts.ACTION_END
        );
        //_fatimaManager.DoAgentNonSpeakAction();
    }
    
    #endregion
    
    #region Helpers

    private static IEnumerable<Name> TouchEventArgsToList(TouchPerceptionManager.TouchEventArgs e)
    {
        return new List<Name>
        {
            (Name) ((decimal)e.Start).ToString(CultureInfo.InvariantCulture),
            (Name) ((decimal)e.End).ToString(CultureInfo.InvariantCulture),
            (Name) e.Localization.ToString(),
            (Name) e.Type.ToString(),
            (Name) ((decimal)e.Duration).ToString(CultureInfo.InvariantCulture),
            (Name) e.DurationInterpretation,
            (Name) ((decimal)e.CurrentVelocity).ToString(CultureInfo.InvariantCulture),
            (Name) ((decimal)e.ImpactVelocity).ToString(CultureInfo.InvariantCulture),
            (Name) e.ImpactVelocityInterpretation,
            (Name) ((decimal)e.MeanForce).ToString(CultureInfo.InvariantCulture),
            (Name) e.MeanForceInterpretation,
            (Name) ((decimal)e.MeanVelocity).ToString(CultureInfo.InvariantCulture),
            (Name) e.MeanVelocityInterpretation
        };
    }
    #endregion
}