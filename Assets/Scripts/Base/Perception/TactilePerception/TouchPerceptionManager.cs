using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TactilePerception
{
    /*public class CollisionDifferences
    {
        private string _handPart;
        private Dictionary<string, float> _velocityDiff, _forceDiff;

        public CollisionDifferences(string handPart)
        {
            _handPart = handPart;
        }

        public Dictionary<string, KeyValuePair<string, float>> LargestDifferences()
        {
            var maxVelDiff = new KeyValuePair<string, float>("base", 0f);
            foreach (var velDiff in _velocityDiff)
            {
                if (Math.Abs(velDiff.Value) > Math.Abs(maxVelDiff.Value))
                    maxVelDiff = velDiff;
            }
            
            var maxForDiff = new KeyValuePair<string, float>("base", 0f);
            foreach (var forceDiff in _forceDiff)
            {
                if (Math.Abs(forceDiff.Value) > Math.Abs(maxForDiff.Value))
                    maxForDiff = forceDiff;
            }

            Dictionary<string, KeyValuePair<string, float>> results = new Dictionary<string, KeyValuePair<string, float>>();
            results.Add("force", maxForDiff);
            results.Add("velocity", maxVelDiff);

            return results;
        }
    }*/
    public class TouchPerceptionManager : MonoBehaviour
    {
        #region Variables

        [Header("Touch manager setup")]
        //[Tooltip("GameObject corresponding to the object directly linked to the tracked hand.")]
        //public GameObject physicalObject;

        private PhysicalFollowManager _physicalObjectFollowScript;
        
        //[Tooltip("GameObject corresponding to the object directly linked to the tracked hand.")]
        //public GameObject invisibleTrackedHand;

        [Header("Event accuracy setup")]
        [SerializeField]
        [Tooltip("Velocity difference between two collided frames to create a new tactile event.")]
        private float velocityThreshold = 0.5f;

        [SerializeField] [Tooltip("Force difference between two collided frames to create a new tactile event.")]
        private float forceThreshold = 0.5f;

        [SerializeField] [Tooltip("Time difference between two collided frames to create a new tactile event.")]
        private float timeThreshold = 0.3f;

        [SerializeField] [Tooltip("Accepted time when no collisions are detected before stopping the sequence.")]
        private float timeLimit = 10.05f;

        [SerializeField] [Tooltip("Print debug information")]
        private bool debug;

        [SerializeField]
        [Tooltip(
            "Inverse of the virtual stiffness of the spring in Hooke's law.\nIt is used to get a force from the position difference between the spring object and the hand object.\nThe best value is to choose the maximum admitted displacement between the two objects to get a normalized estimated force.")]
        private float inverseStiffness = 0.1f;
        
        [SerializeField] [Tooltip("Set to true if you're using the Leap Motion to track the hands of the user.\nSet to false if you use the legacy setup with the manually created hands attached to a spring.")]
        private bool leapMotion;
        
        private enum Touchtypes
        {
            Unspecified,
            Hit,
            Caress,
            Tap,
            Maintained
        }
        
        [SerializeField] [Tooltip("The touchtype we want to simulate for the touchtypes database")]
        private Touchtypes attemptedtouchtype;

        #region Status variables

        public bool IsInEtherealBody { get; private set; }

        // Time since startup, last time the human
        // entered the agent ethereal body
        public float EtherealBodyTimeEnter { get; private set; }
        public bool IsColliding { get; private set; }

        private bool _isWritingSeq;
        private List<KeyValuePair<GameObject, GameObject>> _currentlyActiveColliders;
        private List<KeyValuePair<GameObject, GameObject>> _currentlyActiveTriggers;
        //private List<CollisionDifferences> _tempDifferences;

        #endregion

        #region Result variables

        private TactileSequence _currentEventsSequence;
        private List<TactileSequence> OldSequences { get; set; }

        private List<HandPose> PoseList { get; set; } = new List<HandPose>();

        #endregion

        #endregion
        // Start is called before the first frame update
        void Start()
        {
            _physicalObjectFollowScript = GetComponent<PhysicalFollowManager>();
            _currentlyActiveColliders = new List<KeyValuePair<GameObject, GameObject>>();
            _currentlyActiveTriggers = new List<KeyValuePair<GameObject, GameObject>>();
            PoseList = new List<HandPose>();
            OldSequences = new List<TactileSequence>();
            _isWritingSeq = false;
            IsInEtherealBody = false;
        }

        // Update is called once per frame
        void Update()
        {
            /*if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                attemptedtouchtype = attemptedtouchtype == Touchtypes.Maintained ? Touchtypes.Unspecified : Touchtypes.Maintained;
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                attemptedtouchtype = attemptedtouchtype == Touchtypes.Caress ? Touchtypes.Unspecified : Touchtypes.Caress;
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                attemptedtouchtype = attemptedtouchtype == Touchtypes.Tap ? Touchtypes.Unspecified : Touchtypes.Tap;
            }
            if (Input.GetKeyDown(KeyCode.Alpha0))
            {
                attemptedtouchtype = attemptedtouchtype == Touchtypes.Hit ? Touchtypes.Unspecified : Touchtypes.Hit;
            }
            */
            UpdateWriting();
        }
        
        #region Event Methods
        
        public void PartCollisionEnter(Collision other, float velocityPhys, float velocityTrans, GameObject colliderObj, Vector2 localisation, AnatomyParameters anatomyParameters)
        {
            var colTransform = other.collider.transform;

            var currentTime = Time.realtimeSinceStartup;

            float velocity;

            if (!_isWritingSeq || _currentlyActiveColliders.Count == 0)
            {
                velocity = velocityPhys;
            }
            else
            {
                velocity = velocityTrans;
            }
            // Checking a collision with Camille's body
            if (colTransform.CompareTag("CamilleCollision"))
            {
                if (debug) Debug.Log(name + " detected a collision with Camille at " + colTransform.name);

                if (!_isWritingSeq)
                {
                    if (debug) Debug.Log(name + " is starting a new sequence.");
                    _isWritingSeq = true;
                    IsColliding = true;
                    _currentEventsSequence = new TactileSequence(currentTime, velocity);
                    _currentEventsSequence.initialImpactVelocity = velocity;
                }
                
                var newEvent = new TactileEvent(
                    currentTime,
                    Time.frameCount,
                    colTransform.name,
                    velocity,
                    _physicalObjectFollowScript.GetEstimatedForce(colliderObj.transform.gameObject.ToString(), inverseStiffness),
                    localisation,
                    anatomyParameters.type,
                    colliderObj.name
                );
                var newHandPose = new HandPose
                {
                    TimeStamp = currentTime,
                    Position = colliderObj.transform.position,
                    Rotation = colliderObj.transform.rotation
                };
                _currentEventsSequence.tactileEventList.Add(newEvent);
                PoseList.Add(newHandPose);

                var activeCollision = new KeyValuePair<GameObject, GameObject>(other.gameObject, colliderObj);

                if (!_currentlyActiveColliders.Contains(activeCollision)) _currentlyActiveColliders.Add(activeCollision);

                OnTouchStarted(BuildTouchEventArgs(velocity));
            }
        }

        public void EtherealTriggerEnter(Collider other, GameObject handColliderObj)
        {
            var colTransform = other.transform;
            var currentTime = Time.realtimeSinceStartup;
            // Checking a collision with Camille's ethereal body
            if (colTransform.CompareTag("CamilleEtheral"))
            {
                if (!IsInEtherealBody)
                {
                    if (debug) Debug.Log(name + " is starting to track in the ethereal body.");
                    if (debug) Debug.Log(name + " detected an approach in the ethereal body at " + colTransform.name);
                    EtherealBodyTimeEnter = currentTime;
                    OnEtherealBodyEntered(new EtherealBodyEventArgs {Start = EtherealBodyTimeEnter, Duration = -1});
                    IsInEtherealBody = true;
                }

                var activeTriggerings = new KeyValuePair<GameObject, GameObject>(other.gameObject, handColliderObj);
                
                if (!_currentlyActiveTriggers.Contains(activeTriggerings)) _currentlyActiveTriggers.Add(activeTriggerings);
            }
        }
        
        public void PartCollisionStay(Collision other, float velocity, GameObject colliderObj, Vector2 localisation, AnatomyParameters anatomyParameters)
        {
            var colTransform = other.collider.transform;
            if (colTransform.CompareTag("CamilleCollision"))
            {
                float
                    currentTime = Time.realtimeSinceStartup,
                    currentForce = _physicalObjectFollowScript.GetEstimatedForce(colliderObj.transform.gameObject.ToString(), inverseStiffness);
                var tempList = _currentEventsSequence.tactileEventList;
                var lastEvent = tempList.FindLast(t => t.handCollider.Equals(colliderObj.name));

                if (lastEvent.frame == 0 )
                {
                    return;
                }

                // creating a new event each time the velocity changes or each time interval
                if (currentTime - lastEvent.timeStamp > timeThreshold ||
                    Mathf.Abs(velocity - lastEvent.velocity) > velocityThreshold ||
                    Mathf.Abs(currentForce - lastEvent.force) > forceThreshold
                )
                {
                    if (debug) Debug.Log("Current collision position on " + colTransform.name + ": " + localisation);
                    var newEvent = new TactileEvent(
                        currentTime,
                        Time.frameCount,
                        colTransform.name,
                        velocity,
                        currentForce,
                        localisation,
                        anatomyParameters.type,
                        colliderObj.name);
                    _currentEventsSequence.tactileEventList.Add(newEvent);
                    var newHandPose = new HandPose
                    {
                        TimeStamp = currentTime,
                        Position = colliderObj.transform.position,
                        Rotation = colliderObj.transform.rotation
                    };
                    PoseList.Add(newHandPose);

                    // Don't send event at regular interval, only if the velocity or the force have changed significatively
                    if(Mathf.Abs(velocity - lastEvent.velocity) > velocityThreshold ||
                        Mathf.Abs(currentForce - lastEvent.force) > forceThreshold) {
                        OnTouchChanged(BuildTouchEventArgs(velocity));
                    }
                }
            }
            else if (colTransform.CompareTag("CamilleEtheral"))
            {
            }
        }
        
        public void PartCollisionExit(Collision other, float velocity, GameObject colliderObj)
        {
            var colTransform = other.collider.transform;
            if (colTransform.CompareTag("CamilleCollision"))
            {
                // We only remove the colliders pair, we then check in UpdateWriting if the list is empty after a given time to stop the sequence.
                if (debug) Debug.Log("Exiting the collision trigger of " + colTransform.name);
                _currentlyActiveColliders.Remove(new KeyValuePair<GameObject, GameObject>(other.gameObject, colliderObj));
                OnTouchEnded(BuildTouchEventArgs(velocity));
            }
        }

        public void EtherealTriggerExit(Collider other, GameObject handColliderObj)
        {
            var colTransform = other.transform;
            if (colTransform.CompareTag("CamilleEtheral"))
            {
                _currentlyActiveTriggers.Remove(new KeyValuePair<GameObject, GameObject>(other.gameObject, handColliderObj));
                if (_currentlyActiveTriggers.Count == 0)
                {
                    IsInEtherealBody = false;
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
        private TouchEventArgs BuildTouchEventArgs(float velocity)
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
                CurrentVelocity = velocity,
                HandPart = _currentEventsSequence.tactileEventList.Last().handCollider,
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
                if (Time.realtimeSinceStartup - lastEventTime > timeLimit)
                {
                    if (debug) Debug.Log("End of the sequence");
                    // Add this ending event to the current Sequence and send the Sequence for analysis, then archive it in oldSequences and reset the current Sequence
                    _currentEventsSequence.CompleteSequence(lastEventTime);
                    _currentEventsSequence.InterpretCompleteSequence();

                    //SaveToTxtFile(_currentEventsSequence);
                    SaveToCSVFile(_currentEventsSequence);
                    SaveToArffFile(_currentEventsSequence);
                    OldSequences.Add(new TactileSequence(_currentEventsSequence));
                    _currentEventsSequence.Clear();
                    IsColliding = false;
                    _isWritingSeq = false;
                }
            }
        }

        private void SaveToTxtFile(TactileSequence tSeq)
        {
            var path = Application.streamingAssetsPath + "/SavedDataNewVersion.txt";
            var sw = File.AppendText(path);
            sw.WriteLine(DateTime.Now.ToString());
            // tSeq.inferredTouchType = tSeq.sequenceType;
            var message = "New Sequence information:\n" +
                          " - Hand = " + gameObject.name + "\n" +
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
                           "    # Hand part: " + tEvent.handCollider + "\n" +
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
        }
        
        private void SaveToCSVFile(TactileSequence tSeq)
        {
            var path = Application.streamingAssetsPath + "/CSVTouchSequences/" + GenerateFileName("Sequence") + ".csv";
            var sw = File.AppendText(path);
            sw.WriteLine("totalDuration,initialImpactVelocity,meanForce,meanVelocity,frame,timestep,velocity,force,tactileCell,handPart,positionHeadX,positionHeadY,positionTorsoX,positionTorsoY,positionMemberX,positionMemberY");
            // tSeq.inferredTouchType = tSeq.sequenceType;
            var sb = new StringBuilder();
            sb.Append(tSeq.totalDuration + "," + tSeq.initialImpactVelocity + "," + tSeq.meanForce + "," + tSeq.meanVelocity + ",,,,,,,,,,,,");
            sw.WriteLine(sb);
            foreach (var tEvent in tSeq.tactileEventList)
            {
                sb = new StringBuilder();
                sb.Append(",,,,");
                sb.Append(tEvent.frame + ",");
                sb.Append(tEvent.timeStamp + ",");
                sb.Append(tEvent.velocity + ",");
                sb.Append(tEvent.force + ",");
                sb.Append(tEvent.tactileCell + ",");
                sb.Append(tEvent.handCollider + ",");
                switch (tEvent.cellType)
                {
                    case AnatomyParameters.AnatomyType.Torso:
                        sb.Append(",," + tEvent.localisationParameters.x + "," + tEvent.localisationParameters.y +
                                  ",,");
                        break;
                    case AnatomyParameters.AnatomyType.Head:
                        sb.Append(tEvent.localisationParameters.x + "," + tEvent.localisationParameters.y + ",,,,");
                        break;
                    default:
                        sb.Append(",,,," + tEvent.localisationParameters.x + "," + tEvent.localisationParameters.y);
                        break;
                }
                sw.WriteLine(sb);
            }
            sw.Flush();
            sw.Close();
        }
        
        private void SaveToArffFile(TactileSequence tSeq)
        {
            var path = Application.streamingAssetsPath + "/ARFF/TouchSequences.arff";
            var sw = File.AppendText(path);
            var sb = new StringBuilder();
            sb.Append(tSeq.totalDuration + ";" + tSeq.initialImpactVelocity + ";" + tSeq.meanForce + ";" + tSeq.meanVelocity + ";");
            var tactileCells = new StringBuilder("'");
            var handParts = new StringBuilder("'");
            foreach (var tEvent in tSeq.tactileEventList)
            {
                tactileCells.Append(tEvent.tactileCell + ",");
                handParts.Append(tEvent.handCollider + ",");
            }
            tactileCells.Length -= 1;
            handParts.Length -= 1;
            sb.Append(tactileCells + "';" + handParts + "';");
            if (attemptedtouchtype == Touchtypes.Unspecified)
                sb.Append("?");
            else
            {
                sb.Append(attemptedtouchtype);
            }
            sw.WriteLine(sb);
            sw.Flush();
            sw.Close();
        }
        
        public string GenerateFileName(string context)
        {
            return context + "_" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "_" + Guid.NewGuid().ToString("N");
        }

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

            // The part of the hand that touched the agent
            public string HandPart;

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