using UnityEngine;

namespace ZMD
{
    /// Follow a target by applying physical forces.  
    /// Allows for more reliable rotational collision detection than Configurable Joints
    /// and more reliable translational collision detection than Articulation Bodies.
    public class PhysicsFollowMono : MonoBehaviour
    {
        [SerializeField] PhysicsFollow process;
        private Transform _lover;
        void OnValidate() { process.OnValidate(); }

        void Start()
        {
            process.Start();
        }
        void FixedUpdate() { process.FixedUpdate(); }

        public void SetTarget(Transform value)
        {
            process.SetOnlyTarget(value);
            process.OnValidate();
        }
        
        public void SetTargetLover()
        {
            transform.position = _lover.position;
            transform.rotation = _lover.rotation;
            process.self.velocity = Vector3.zero;
            process.self.angularVelocity = Vector3.zero;
            process.SetTarget(_lover);
            process.OnValidate();
        }
        public Transform GetTarget(){ return process.GetTarget();}

        public void SetLover(Transform value)
        {
            _lover = value;
        }
        
        public Transform GetLover(){ return _lover;}
        
    }
}
