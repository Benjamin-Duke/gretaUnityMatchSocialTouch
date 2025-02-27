using UnityEngine;


// Simple component to get the velocity of an object.

[RequireComponent(typeof(Rigidbody))]
public class GetVelocity : MonoBehaviour
{
    public float velocity;
    public float rbVelocity; //Velocity computed from the object rigid body
    public float cpVelocity; //Volocity computed from the object position
    private Vector3 oldPosition;
    private Rigidbody rb;

    // Use this for initialization
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        oldPosition = transform.position;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        rbVelocity = rb.velocity.magnitude;
        cpVelocity = (transform.position - oldPosition).magnitude / Time.fixedDeltaTime;
        oldPosition = transform.position;
        velocity = Mathf.Max(rbVelocity, cpVelocity);
    }
}