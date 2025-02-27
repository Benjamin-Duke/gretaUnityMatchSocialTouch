using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisibleHandLovers : MonoBehaviour
{
    public Transform _lover;

    //public float rotspeed = 5.0f;

    private float speed = 17.0f;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = Vector3.Lerp(transform.position ,_lover.position, Time.deltaTime * speed);
        transform.rotation = Quaternion.Slerp(transform.rotation ,_lover.rotation, Time.deltaTime * speed);
    }

    public void SetLover(Transform value)
    {
        _lover = value;
    }

    public Transform GetLover()
    {
        return _lover;
    }
}
