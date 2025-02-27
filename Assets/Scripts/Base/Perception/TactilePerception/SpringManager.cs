using UnityEngine;

namespace TactilePerception
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(SpringJoint))]

    // This component deals with the spring object following the hand.
    public class SpringManager : MonoBehaviour
    {
        [SerializeField] [Tooltip("Reference to the HandTouchManager associated with this hand.")]
        private HandTouchManager _touchManager;

        [SerializeField]
        [Tooltip(
            "Distance between the Hand and the Spring where we teleport the Spring to the Hand to avoid oscillations.")]
        private float positionThreshold = 0.01f;

        private Transform _handTransform;

        public Collider SpringCollider { get; private set; }

        protected void Start()
        {
            SpringCollider = GetComponent<Collider>();
            _handTransform = _touchManager.handObject.transform;
        }

        protected void Update()
        {
            ForceRestPosition();
        }

        private void OnCollisionEnter(Collision collision)
        {
            // We could envision additional processing when the spring object collides with Camille
        }

        private void ForceRestPosition()
        {
            if ((transform.position - _handTransform.position).magnitude < positionThreshold)
                transform.position = _handTransform.position;
        }
    }
}