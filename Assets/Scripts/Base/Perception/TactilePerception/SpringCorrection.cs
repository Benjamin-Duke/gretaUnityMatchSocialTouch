using UnityEngine;

[RequireComponent(typeof(SpringJoint))]
public class SpringCorrection : MonoBehaviour
{
    public GameObject target;
    public float deltaTime;
    public float maxDist;

    private Vector3 oldVelocity;

    //public float value1;
    private Rigidbody rb;

    // Use this for initialization
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        oldVelocity = transform.position;
    }

    // Update is called once per frame
    private void Update()
    {
        //value1 = (transform.position - oldVelocity).magnitude / Time.deltaTime;
        if ((target.transform.position - transform.position).magnitude > maxDist)
            // Debug.Log("Position Reset");
            transform.position = target.transform.position;
        /*
        if ((target.transform.position - oldVelocity).magnitude < directionTolerance)
            transform.position = target.transform.position;
            */
        /*
        if (Vector3.Dot(oldVelocity, rb.velocity) < 0)
        {
            Debug.Log("Position Reset when changing direction");
            transform.position = target.transform.position;
        }
        */
        //oldVelocity = target.transform.position;
    }
}