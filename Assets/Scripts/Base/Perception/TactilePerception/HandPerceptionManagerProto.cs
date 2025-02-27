using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TactilePerception
{
    [RequireComponent(typeof(Rigidbody))]
    public abstract class HandPerceptionManagerProto : MonoBehaviour
    {
        [SerializeField] [Tooltip("Reference to the TouchPerceptionManager associated with this hand.")]
        protected TouchPerceptionManagerProto _touchManager;
        private Rigidbody _rb;
        // rbVelocity should be used for initial impact and transformVelocity for movement gestures
        [HideInInspector] public float largestVelocity { get; private set; }
        [HideInInspector] public float transformVelocity { get; private set; }

        protected Vector3 _previousPosition;

        [HideInInspector] public List<Vector3> positionList { get; private set; }
        
        #region Process Methods

        public Vector2 ComputeLocalPosition(Transform localTransform, out AnatomyParameters currentParameters)
        {
            /*if (Debug.isDebugBuild)
            {
                Debug.Log("Attempting to compute the local position in " + localTransform.name);
            }*/
            // Computing local position
            Transform marker = null;
            currentParameters = null;
            // To get the proper position based on Camille's body, we need to get the corresponding anatomy parameters.
            var childNb = localTransform.childCount;
            if (childNb == 0)
                if (Debug.isDebugBuild)
                {
                    Debug.LogError("No child in collided object");
                    return Vector2.negativeInfinity;
                }

            for (var i = 0; i < localTransform.childCount; i++)
            {
                if (localTransform.GetChild(i).CompareTag("Marker"))
                {
                    marker = localTransform.GetChild(i);
                    currentParameters = marker.GetComponent<AnatomyParameters>();
                }
            }

            if (marker == null)
            {
                if (Debug.isDebugBuild) Debug.LogError("No Marker reference in child of collision");
                return Vector2.negativeInfinity;
            }


            var localCoordinates = marker.InverseTransformPoint(transform.position);
            // We reset the scale because setting the parent repeteadly can slightly modify the scale
            switch (currentParameters.type)
            {
                case AnatomyParameters.AnatomyType.Torso:
                    // Distance along the spine, 0 near the marker
                    var height = localCoordinates.z / (2 * currentParameters.length);
                    if (height < 0) height = 0;
                    else if (height > 1) height = 1; // Fallback if the parameter is approximated
                    // Distance across the spine, 0 near the marker, 1 on the left (along the marker y axis), -1 on the right		
                    var width = localCoordinates.y / currentParameters.width;
                    if (width < 0) width = 0;
                    else if (width > 1) width = 1; // Fallback if the parameter is approximated
                    return new Vector2(height, width);
                case AnatomyParameters.AnatomyType.Head:
                    // Rotation around the head, 0 on the face, 1 on the back of the head.
                    var face = Mathf.Acos(new Vector3(localCoordinates.x, localCoordinates.y, 0).normalized.y) /
                               Mathf.PI;
                    // Rotation around the head, 0 below the head, 1 on top.
                    var top = Mathf.Acos(-new Vector3(0, localCoordinates.y, localCoordinates.z).normalized.z) /
                              Mathf.PI;
                    return new Vector2(face, top);
                case AnatomyParameters.AnatomyType.Arm:
                case AnatomyParameters.AnatomyType.Forearm:
                case AnatomyParameters.AnatomyType.Hand:
                case AnatomyParameters.AnatomyType.Neck:
                    // Distance along the member, 0 near the marker, 1 at the end of the member
                    var dist = localCoordinates.z / (2 * currentParameters.length);
                    if (dist < 0) dist = 0;
                    else if (dist > 1) dist = 1; // Fallback if the parameter is approximated
                    // Rotation around the member, 0 on the inside of the member, 1 towards the outside.
                    var rot = Mathf.Acos(new Vector3(localCoordinates.x, localCoordinates.y, 0).normalized.y) /
                              Mathf.PI;
                    return new Vector2(dist, rot);
                default:
                    if (Debug.isDebugBuild)
                        Debug.LogError("Anatomy type of " + localTransform.name +
                                       " is not supported. Supported types are: member (0), torso (1) and head(2).");
                    return Vector2.negativeInfinity;
            }
        }

        #endregion
        
        // Start is called before the first frame update
        protected virtual void Start()
        {
            _touchManager = GetComponentInParent<TouchPerceptionManagerProto>();
            _rb = GetComponent<Rigidbody>();
            _previousPosition = transform.position;
            positionList = new List<Vector3>();
        }
        
        protected virtual void FixedUpdate()
        {
            // Updating the velocity of the hand part ( m/s )
            var rbVelocity = _rb.velocity.magnitude;
            var currentPosition = transform.position;
            transformVelocity = (currentPosition - _previousPosition).magnitude / Time.fixedDeltaTime;
            largestVelocity = Mathf.Max(rbVelocity, transformVelocity);
            _previousPosition = currentPosition;
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}

