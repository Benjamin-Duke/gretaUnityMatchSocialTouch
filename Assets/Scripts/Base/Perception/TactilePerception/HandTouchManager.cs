using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace TactilePerception
{
    public class HandTouchManager : MonoBehaviour
    {
        #region Variables

        [Header("Touch manager setup")]
        [SerializeField]
        [Tooltip(
            "GameObject corresponding to the hand with a handManager component.")]
        public GameObject handObject;
        
        [FormerlySerializedAs("springObject")] [Tooltip("GameObject corresponding either to the object directly linked to the tracked hand via Leap Motion or the spring with a springManager component.")]
        public GameObject physicalObject;

        private HandManager _hM;
        private SpringManager _sM;

        #region Variables

        [Header("Event accuracy setup")]
        [SerializeField]
        [Tooltip("Velocity difference between two collided frames to create a new tactile event.")]
        private float _velocityThreshold = 0.5f;

        [SerializeField] [Tooltip("Force difference between two collided frames to create a new tactile event.")]
        private float _forceThreshold = 0.5f;

        [SerializeField] [Tooltip("Time difference between two collided frames to create a new tactile event.")]
        private float _timeThreshold = 0.3f;

        [SerializeField] [Tooltip("Accepted time when no collisions are detected before stopping the sequence.")]
        private float _timeLimit = 10.05f;

        [SerializeField] [Tooltip("Print debug information")]
        private bool _debug;

        [SerializeField]
        [Tooltip(
            "Inverse of the virtual stiffness of the spring in Hooke's law.\nIt is used to get a force from the position difference between the spring object and the hand object.\nThe best value is to choose the maximum admitted displacement between the two objects to get a normalized estimated force.")]
        private float _inverseStiffness = 0.1f;
        
        [SerializeField] [Tooltip("Set to true if you're using the Leap Motion to track the hands of the user.\nSet to false if you use the legacy setup with the manually created hands attached to a spring.")]
        private bool leapMotion;

        #endregion

        #region Status variables

        public bool IsInEtheralBody { get; private set; }

        // Time since startup, last time the human
        // entered the agent ethereal body
        public float EtherealBodyTimeEnter { get; private set; }
        public bool IsColliding { get; private set; }
        

        private bool _isWritingSeq;
        private List<Transform> _currentlyActiveColliders;
        private List<Transform> _currentlyActiveTriggers;

        #endregion

        #region Result variables

        private TactileSequence _currentEventsSequence;
        private List<TactileSequence> OldSequences { get; set; }

        private List<HandPose> PoseList { get; set; } = new List<HandPose>();

        #endregion

        #endregion

        #region Unity methods

        protected void Start()
        {
            _hM = handObject.GetComponent<HandManager>();
            if (!leapMotion && physicalObject != null)
                _sM = physicalObject.GetComponent<SpringManager>();
            _currentlyActiveColliders = new List<Transform>();
            _currentlyActiveTriggers = new List<Transform>();
            PoseList = new List<HandPose>();
            OldSequences = new List<TactileSequence>();
            _isWritingSeq = false;
            IsInEtheralBody = false;
        }

        private void Update()
        {
            UpdateWriting();
        }

        #endregion

        #region Event Methods

        public void HandTriggerEnter(Collider other)
        {
            var colTransform = other.transform;

            var currentTime = Time.realtimeSinceStartup;
            // Checking a collision with Camille's body
            if (colTransform.CompareTag("CamilleCollision"))
            {
                if (_debug) Debug.Log(name + " detected a collision with Camille at " + colTransform.name);

                if (!_isWritingSeq)
                {
                    if (_debug) Debug.Log(name + " is starting a new sequence.");
                    _isWritingSeq = true;
                    IsColliding = true;
                    _currentEventsSequence = new TactileSequence(currentTime, _hM.largestVelocity);
                    _currentEventsSequence.initialImpactVelocity = _hM.largestVelocity;
                }

                var localisation = _hM.ComputeLocalPosition(colTransform, out var anatomyParameters);
                var newEvent = new TactileEvent(
                    currentTime,
                    Time.frameCount,
                    colTransform.name,
                    _hM.largestVelocity,
                    GetEstimatedForce(),
                    localisation,
                    anatomyParameters.type
                );
                var newHandPose = new HandPose
                {
                    TimeStamp = currentTime,
                    Position = handObject.transform.position,
                    Rotation = handObject.transform.rotation
                };
                _currentEventsSequence.tactileEventList.Add(newEvent);
                PoseList.Add(newHandPose);

                if (!_currentlyActiveColliders.Contains(colTransform)) _currentlyActiveColliders.Add(colTransform);

                OnTouchStarted(BuildTouchEventArgs());
            }
            // Checking a collision with Camille's ethereal body
            else if (colTransform.CompareTag("CamilleEtheral"))
            {
                if (!IsInEtheralBody)
                {
                    if (_debug) Debug.Log(name + " is starting to track in the etheral body.");
                    EtherealBodyTimeEnter = currentTime;
                    OnEtherealBodyEntered(new EtherealBodyEventArgs {Start = EtherealBodyTimeEnter, Duration = -1});
                    IsInEtheralBody = true;
                }

                if (_debug) Debug.Log(name + " detected an approach in the etheral body at " + colTransform.name);
                if (!_currentlyActiveTriggers.Contains(colTransform)) _currentlyActiveTriggers.Add(colTransform);
            }
        }

        public void HandTriggerStay(Collider other)
        {
            var colTransform = other.transform;
            if (colTransform.CompareTag("CamilleCollision"))
            {
                float
                    currentTime = Time.realtimeSinceStartup,
                    currentForce = GetEstimatedForce();
                var tempList = _currentEventsSequence.tactileEventList;
                var lastEvent = tempList[tempList.Count - 1];

                // creating a new event each time the velocity changes or each time interval
                if (currentTime - lastEvent.timeStamp > _timeThreshold ||
                    Mathf.Abs(_hM.largestVelocity - lastEvent.velocity) > _velocityThreshold ||
                    Mathf.Abs(currentForce - lastEvent.force) > _forceThreshold
                )
                {
                    var localisation = _hM.ComputeLocalPosition(colTransform, out var anatomyParameters);
                    if (_debug) Debug.Log("Current collision position on " + colTransform.name + ": " + localisation);
                    var newEvent = new TactileEvent(
                        currentTime,
                        Time.frameCount,
                        colTransform.name,
                        _hM.largestVelocity,
                        currentForce,
                        localisation,
                        anatomyParameters.type);
                    _currentEventsSequence.tactileEventList.Add(newEvent);
                    var newHandPose = new HandPose
                    {
                        TimeStamp = currentTime,
                        Position = handObject.transform.position,
                        Rotation = handObject.transform.rotation
                    };
                    PoseList.Add(newHandPose);

                    // Don't send event at regular interval, only if the velocity or the force have changed significatively
                    if(Mathf.Abs(_hM.largestVelocity - lastEvent.velocity) > _velocityThreshold ||
                        Mathf.Abs(currentForce - lastEvent.force) > _forceThreshold) {
                        OnTouchChanged(BuildTouchEventArgs());
                    }
                }
            }
            else if (colTransform.CompareTag("CamilleEtheral"))
            {
            }
        }

        public void HandTriggerExit(Collider other)
        {
            var colTransform = other.transform;
            if (colTransform.CompareTag("CamilleCollision"))
            {
                // We only remove the collider, we then check in UpdateWriting if the list is empty after a given time to stop the sequence.
                if (_debug) Debug.Log("Exiting the collision trigger of " + colTransform.name);
                _currentlyActiveColliders.Remove(colTransform.transform);
                OnTouchEnded(BuildTouchEventArgs());
            }
            else if (colTransform.CompareTag("CamilleEtheral"))
            {
                _currentlyActiveTriggers.Remove(colTransform.transform);
                if (_currentlyActiveTriggers.Count == 0)
                {
                    IsInEtheralBody = false;
                    OnEtherealBodyLeft(new EtherealBodyEventArgs {Start = EtherealBodyTimeEnter, Duration = Time.realtimeSinceStartup - EtherealBodyTimeEnter});
                }
            }
        }

        #endregion

        #region Processing Methods

        /**
         * Build a TouchEventArgs objects with values
         * taken from last touch event
         */
        private TouchEventArgs BuildTouchEventArgs()
        {
            // Will compute intermediate mean velocity, etc
            _currentEventsSequence.ComputeSequenceValues();
            return new TouchEventArgs
            {
                Start = _currentEventsSequence.tactileEventList.First().timeStamp,
                // If sequence has ended, use last timestamp, else -1
                End = _currentEventsSequence.isComplete ? _currentEventsSequence.tactileEventList.Last().timeStamp : -1,
                Localization = _currentEventsSequence.tactileEventList.Last().cellType,
                // Partial interpretation : touch sequence has just started...
                Type = _currentEventsSequence.InterpretCompleteSequence(),
                Duration = _currentEventsSequence.totalDuration,
                ImpactVelocity = _currentEventsSequence.initialImpactVelocity,
                CurrentVelocity = _hM.largestVelocity,
                MeanForce = _currentEventsSequence.meanForce,
                MeanVelocity = _currentEventsSequence.meanVelocity,
                DurationInterpretation =
                    SequenceInterpreter.durationClasses.MaximumConfidenceClassName(_currentEventsSequence
                        .totalDuration),
                ImpactVelocityInterpretation =
                    SequenceInterpreter.impactVelocityClasses.MaximumConfidenceClassName(_currentEventsSequence
                        .initialImpactVelocity),
                MeanForceInterpretation =
                    SequenceInterpreter.meanForceClasses.MaximumConfidenceClassName(
                        _currentEventsSequence.meanForce),
                MeanVelocityInterpretation =
                    SequenceInterpreter.meanVelocityClasses.MaximumConfidenceClassName(_currentEventsSequence
                        .meanVelocity)
            };
        }

        private void UpdateWriting()
        {
            // if there are no colliders activated and we are still writing a sequence, we check the time since the last event 
            if (_isWritingSeq && _currentlyActiveColliders.Count == 0)
            {
                // There are no more collision at the moment, if we wait a fraction of time dT we should interrupt the current sequence
                var lastEventTime = _currentEventsSequence
                    .tactileEventList[_currentEventsSequence.tactileEventList.Count - 1].timeStamp;
                if (Time.realtimeSinceStartup - lastEventTime > _timeLimit)
                {
                    if (_debug) Debug.Log("End of the sequence");
                    // Add this ending event to the current Sequence and send the Sequence for analysis, then archive it in oldSequences and reset the current Sequence
                    _currentEventsSequence.CompleteSequence(lastEventTime);
                    _currentEventsSequence.InterpretCompleteSequence();

                    //SaveToFile(_currentEventsSequence);
                    OldSequences.Add(new TactileSequence(_currentEventsSequence));
                    _currentEventsSequence.Clear();
                    IsColliding = false;
                    _isWritingSeq = false;
                }
            }
        }

        private float GetEstimatedForce()
        {
            if (leapMotion)
            {
                return (physicalObject.transform.position - handObject.transform.position).magnitude / _inverseStiffness;
            }
            else
            {
                return (handObject.transform.position - physicalObject.transform.position).magnitude / _inverseStiffness;
            }
        }

        /* private void SaveToFile(TactileSequence tSeq)
        {
            var path = Application.streamingAssetsPath + "/SavedData.txt";
            var sw = File.AppendText(path);
            sw.WriteLine(DateTime.Now.ToString());
            // tSeq.inferredTouchType = tSeq.sequenceType;
            var message = "New Sequence information:\n" +
                          " - Estimated Type = " + tSeq.sequenceTouchType + "\n" +
                          " - Duration = " + tSeq.totalDuration + " s\n" +
                          " - Impact velocity = " + tSeq.initialImpactVelocity + " m/frame\n" +
                          " - Mean force = " + tSeq.meanForce + " a.u.\n" +
                          " - Mean velocity = " + tSeq.meanVelocity + " m/frame\n" +
                          " - Details of the type results:\n";

            for (var i = 0; i < SequenceInterpreter.typeNb; i++)
                message += "    # " + (TouchType) i + ": " + tSeq.rawTypeResult[i] + "\n";
            message += " - Details of the sequence events:\n";
            for (var i = 0; i < tSeq.tactileEventList.Count; i++)
            {
                var tEvent = tSeq.tactileEventList[i];
                message += "\n    # Frame: " + tEvent.frame + "\n" +
                           "    # Time: " + tEvent.timeStamp + " seconds after startup\n" +
                           "    # Velocity: " + tEvent.velocity + " m/frame\n" +
                           "    # Force: " + tEvent.force + " a.u.\n" +
                           "    # Tactile cell: " + tEvent.tactileCell + "\n" +
                           "    # Position on cell: ";
                switch (tEvent.cellType)
                {
                    case AnatomyParameters.AnatomyType.Torso:
                        message += "Normalized distance along the torso axis = " + tEvent.localisationParameters.x +
                                   "; Normalized distance across the torso axis =  " + tEvent.localisationParameters.y +
                                   "\n";
                        break;
                    case AnatomyParameters.AnatomyType.Head:
                        message += "Rotation around the head on the neck axis = " + tEvent.localisationParameters.x +
                                   "; Rotation around the head on the ear-ear axis  =  " +
                                   tEvent.localisationParameters.y + "\n";
                        break;
                    default:
                        message += "Normalized distance along the member axis = " + tEvent.localisationParameters.x +
                                   "; Rotation around the member " + tEvent.localisationParameters.y + "\n";
                        break;
                }
            }

            message += "\n\n";
            sw.Write(message);
            sw.Flush();
            sw.Close();
        } */

        #endregion

        #region C# Events (touch started, touch ongoing, touch ending...)

        public event EventHandler<TouchEventArgs> TouchStarted;
        public event EventHandler<TouchEventArgs> TouchChanged;
        public event EventHandler<TouchEventArgs> TouchEnded;
        public event EventHandler<EtherealBodyEventArgs> EtherealBodyEntered;
        public event EventHandler<EtherealBodyEventArgs> EtherealBodyLeft;

        protected virtual void OnTouchStarted(TouchEventArgs e)
        {
            TouchStarted?.Invoke(this, e);
        }

        protected virtual void OnTouchChanged(TouchEventArgs e)
        {
            TouchChanged?.Invoke(this, e);
        }

        protected virtual void OnTouchEnded(TouchEventArgs e)
        {
            TouchEnded?.Invoke(this, e);
        }

        protected virtual void OnEtherealBodyEntered(EtherealBodyEventArgs e)
        {
            EtherealBodyEntered?.Invoke(this, e);
        }

        protected virtual void OnEtherealBodyLeft(EtherealBodyEventArgs e)
        {
            EtherealBodyLeft?.Invoke(this, e);
        }

        /**
         * We use this data object to convey all information and interpretation we can get
         * from a touch at any point (started, ongoing, ended).
         * If the touch has just started, the interpretation class will be inaccurate, but better that nothing.
         * The end will be -1, etc. But it allows us to trigger events every time something happen (velocity evolves etc),
         * and to see the evolution of the interpretation classes. E.g. we thought it was a hit, but it is a tap. It is
         * really useful for "realtime" reaction of the agent (we cannot wait the touch to end to react).
         */
        public class TouchEventArgs : EventArgs
        {
            // Velocity in unit per second (usually meters) at the time the event is triggered
            public float CurrentVelocity;

            // Current duration of the tactile sequence in seconds
            public float Duration;

            // Interpretation class name of the current duration
            public string DurationInterpretation;

            // In seconds, subtract Start to get the duration, -1 if not finished yet
            public float End;

            // Raw velocity in unit per second (usually meters) when the touch started
            public float ImpactVelocity;

            // Interpretation class name of the initial velocity
            public string ImpactVelocityInterpretation;

            // Member, torso or head
            public AnatomyParameters.AnatomyType Localization;

            // Mean force of all tactile events of the sequence
            public float MeanForce;

            // Interpretation class name of the mean force
            public string MeanForceInterpretation;

            // Mean velocity of all tactile events of the sequence
            public float MeanVelocity;

            // Interpretation class name of the mean velocity
            public string MeanVelocityInterpretation;

            // In seconds, since the start of the program. Acts as an identifier
            public float Start;

            // Interpretation class of the touch type, given other interpretation classes (mean velocity, mean force...)
            public TouchType Type;
        }

        public class EtherealBodyEventArgs : EventArgs
        {
            // In seconds, since the start of the program. Acts as an identifier
            public float Start;

            // In seconds, duration of stay into the ethereal body
            public float Duration;
        }

        #endregion
    }
}