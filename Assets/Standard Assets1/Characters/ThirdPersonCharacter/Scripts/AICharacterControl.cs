using System;
using UnityEngine;
using UnityEngine.AI;

namespace UnityStandardAssets.Characters.ThirdPerson
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(ThirdPersonCharacter))]
    public class AICharacterControl : MonoBehaviour
    {
        public Transform target; // target to aim for
        public Transform initialPos;
        private Animator m_Anim;
        private bool mustTurn;
        public NavMeshAgent agent { get; private set; } // the navmesh agent required for the path finding
        public ThirdPersonCharacter character { get; private set; } // the character we are controlling


        private void Start()
        {
            // get the components on the object we need ( should not be null due to require component so no need to check )
            agent = GetComponentInChildren<NavMeshAgent>();
            character = GetComponent<ThirdPersonCharacter>();
            m_Anim = GetComponent<Animator>();

            agent.updateRotation = false;
            agent.updatePosition = true;
        }


        private void FixedUpdate()
        {
            if (target != null) agent.SetDestination(target.position);

            if (agent.remainingDistance > agent.stoppingDistance)
            {
                character.Move(agent.desiredVelocity*0.5f, false, false);
            }
            else if (target != null)
            {
                var updatedPos = target.position;
                updatedPos.y = 0.0f;
                var updatedAgentPos = agent.transform.position;
                updatedAgentPos.y = 0.0f;
                var direction = (updatedPos - agent.transform.position).normalized;
                var test = AngleAroundAxis(agent.transform.forward,
                    (target.position - agent.transform.position).normalized, new Vector3(0, 1, 0));
                //Debug.Log(test);
                //if (Vector3.Distance(transform.position, updatedPos) < 0.35f)
                //if ((agent.stoppingDistance - agent.remainingDistance) > 1.2f)
                //{
                    //    character.Move(agent.transform.position - initialPos.position, false, false);
                //    m_Anim.SetFloat("Forward", -0.9f, 0.1f, Time.deltaTime);
                    //    agent.Move(agent.transform.position - initialPos.position);
                //}
                

                //if (Vector3.Dot (direction, agent.transform.forward) < 0.25f && !mustTurn)
                //Updated on 23/05/23 to 60 from 70
                if ((test > 60f || test < -60f) && !mustTurn)
                    mustTurn = true;

                if (mustTurn)
                {
                    //if (Vector3.Dot (direction, agent.transform.forward) < 0.94f) {
                    if (test > 4f || test < -4f)
                    {
                        //Debug.Log ("We must turn ! " + Vector3.Dot (direction, agent.transform.forward));
                        //var lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                        var lookRotation = Quaternion.LookRotation(direction);
                        agent.transform.rotation = Quaternion.RotateTowards(agent.transform.rotation, lookRotation,
                            Time.deltaTime * Mathf.Lerp(180, 360, 0));
                        //float rotateDirection = (((target.rotation.eulerAngles.y - agent.transform.rotation.eulerAngles.y) + 360f) % 360f) > 180.0f ? -1 : 1;
                        float rotateDirection = test > 0f ? 1 : -1;
                        m_Anim.SetFloat("Turn", rotateDirection, 0.1f, Time.deltaTime);
                    }
                    else
                    {
                        //Debug.Log ("Stopping the turn-around");
                        mustTurn = false;
                        character.Move(Vector3.zero, false, false);
                    }
                }
                else
                {
                    character.Move(Vector3.zero, false, false);
                }
            }
            else
            {
                character.Move(Vector3.zero, false, false);
            }
        }

        private void Update()
        {
            if (target == null) return;
            /*
            if ((agent.stoppingDistance - agent.remainingDistance) > 1.2f && Vector3.Distance(transform.position, initialPos.position) > 0.2f)
            {
                transform.position = Vector3.MoveTowards(transform.position, initialPos.position, Time.deltaTime);
                m_Anim.SetFloat("Forward", -0.9f, 0.1f, Time.deltaTime);
            }
            else
            {
                m_Anim.SetFloat("Forward", 0f);
            }*/
        }


        public void SetTarget(Transform target)
        {
            this.target = target;
        }

        public static float AngleAroundAxis(Vector3 dirA, Vector3 dirB, Vector3 axis)
        {
            // Project A and B onto the plane orthogonal target axis
            dirA = dirA - Vector3.Project(dirA, axis);
            dirB = dirB - Vector3.Project(dirB, axis);

            // Find (positive) angle between A and B
            var angle = Vector3.Angle(dirA, dirB);

            // Return angle multiplied with 1 or -1
            return angle * (Vector3.Dot(axis, Vector3.Cross(dirA, dirB)) < 0 ? -1 : 1);
        }
    }
}