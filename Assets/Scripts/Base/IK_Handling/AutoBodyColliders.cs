using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoBodyColliders : MonoBehaviour
{
    //public GameObject root;
    // Start is called before the first frame update
    void Start()
    {

        RecursiveBodyCollider(this.gameObject);

    }

    public void RecursiveBodyCollider(GameObject actual, GameObject parent =null)
    {
        int i = 0;
        Debug.Log("[INFO NUMBER]:" + actual.transform.childCount + "   " + actual.name);
        bool flag = false;
        if (actual.transform.childCount == 0)
        {
            CapsuleCollider capsuleCollider = actual.AddComponent<CapsuleCollider>();
            //ArticulationBody articulationBody= parent.AddComponent<ArticulationBody>();
            //if (parent != null)
            //{
            //    FixedJoint articulationBody = actual.AddComponent<FixedJoint>();
            //    actual.GetComponent<FixedJoint>().connectedBody = parent.GetComponent<Rigidbody>();
            //}
            //parent.GetComponent<ArticulationBody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            //parent.GetComponent<ArticulationBody>().useGravity = false;
            actual.GetComponent<CapsuleCollider>().isTrigger = false;
            actual.GetComponent<CapsuleCollider>().radius = 0.015f;
            actual.GetComponent<CapsuleCollider>().direction = 0;
            actual.GetComponent<CapsuleCollider>().height = 0.03f;
            flag = true;
        }
        else
        {
            while (i < actual.transform.childCount)
            {
                Debug.Log("[INFO NUMBER 2]: " + actual.transform.childCount + "   " + actual.name);
                if (actual.transform.childCount <= 0)
                {
                    Debug.Log("End nb child");
                }
                else
                {
                    Debug.Log("[I]: " + actual.name + "   " + actual.transform.childCount + "   " + i+actual.name.Contains("meta"));
                    Debug.Log("[INFO]:" + actual.transform.GetChild(i).gameObject.name);
                    if (!flag)
                    {
                        CapsuleCollider capsuleCollider = actual.AddComponent<CapsuleCollider>();
                        //ArticulationBody articulationBody= parent.AddComponent<ArticulationBody>();
                        //if (parent != null)
                        //{
                        //    FixedJoint articulationBody = actual.AddComponent<FixedJoint>();
                        //    actual.GetComponent<FixedJoint>().connectedBody = parent.GetComponent<Rigidbody>();
                        //}
                        //parent.GetComponent<ArticulationBody>().collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                        //parent.GetComponent<ArticulationBody>().useGravity = false;
                        actual.GetComponent<CapsuleCollider>().isTrigger = false;
                        if (actual.name.Contains("meta") || actual.name.Contains("Palm"))
                        {
                            Debug.Log("META");
                            actual.GetComponent<CapsuleCollider>().radius = 0.03f;
                            actual.GetComponent<CapsuleCollider>().height = 0.00f;
                        }
                        else
                        {
                            actual.GetComponent<CapsuleCollider>().radius = 0.015f;
                            actual.GetComponent<CapsuleCollider>().direction = 0;
                            actual.GetComponent<CapsuleCollider>().height = 0.03f;
                        }
                        flag = true;
                    }
                    RecursiveBodyCollider(actual.transform.GetChild(i).gameObject, actual);
                    i++;
                }
            }
        }
    }
};
