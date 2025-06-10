using UnityEngine;
using Leap;
using Leap.Unity;

public class LeapGrabObject : MonoBehaviour
{
    public LeapServiceProvider leapProvider;
    private GameObject grabbedObject = null;
    public GameObject GrabbedObject => grabbedObject; // Pour le système de snap

    private Vector3 initialObjectPosition;
    private Quaternion initialObjectRotation;
    private Quaternion initialHandRotation;
    private Vector3 grabOffset;
    private int grabbingHandId = -1;
    
    private SimpleTetraminoSnap snapSystem;

    void Start()
    {
        snapSystem = FindObjectOfType<SimpleTetraminoSnap>();
        //initialObjectRotation = grabbedObject.transform.rotation;
    }

    void Update()
    {
        Frame frame = leapProvider.CurrentFrame;

        foreach (Hand hand in frame.Hands)
        {
            if (grabbedObject == null)
            {
                if (hand.GrabStrength > 0.8f)
                {
                    TryGrabObject(hand);
                }
            }
            else
            {
                if (hand.Id == grabbingHandId)
                {
                    if (hand.GrabStrength > 0.8f)
                    {
                        MoveObjectWithHand(hand);
                    }
                    else
                    {
                        ReleaseObject();
                    }
                }
            }
        }

        
    }

    void TryGrabObject(Hand hand)
    {
        Vector3 handPosition = new Vector3(hand.PalmPosition.x, hand.PalmPosition.y, hand.PalmPosition.z);

        Collider[] colliders = Physics.OverlapSphere(handPosition, 0.01f);

        foreach (var collider in colliders)
        {
            if (collider.CompareTag("iii"))
            {
                Rigidbody rb = collider.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    grabbedObject = rb.gameObject;
                    grabbingHandId = hand.Id;

                    rb.isKinematic = true;

                    grabOffset = grabbedObject.transform.position - handPosition;
                    initialObjectPosition = grabbedObject.transform.position;
                    initialObjectRotation = grabbedObject.transform.rotation;
                    initialHandRotation = GetHandRotation(hand);
                    break;
                }
            }
        }
    }

    void MoveObjectWithHand(Hand hand)
    {
        Vector3 handPosition = new Vector3(hand.PalmPosition.x, hand.PalmPosition.y, hand.PalmPosition.z);
        Quaternion handRotation = GetHandRotation(hand);

        // Position
        grabbedObject.transform.position = handPosition;
        //grabbedObject.transform.rotation = Quaternion.Euler(0, 0, 90);

        // // Rotation pour les tétraminos - simplifiée
        // if (grabbedObject.CompareTag("iii"))
        // {
        //     // Rotation basée sur l'orientation de la main, snappée aux 90°
        //     Vector3 handForward = new Vector3(hand.Direction.x, 0, hand.Direction.z).normalized;
        //     float angle = Mathf.Atan2(handForward.x, handForward.z) * Mathf.Rad2Deg;
        //     float snappedAngle = Mathf.Round(angle / 90f) * 90f;

        //     grabbedObject.transform.rotation = Quaternion.Euler(0, snappedAngle, 0);
        // }
        // else
        // {
        //     // Rotation normale pour les autres objets
        //     Quaternion deltaRotation = handRotation * Quaternion.Inverse(initialHandRotation);
        //     grabbedObject.transform.rotation = deltaRotation * initialObjectRotation;
        // }
    }

    void ReleaseObject()
    {
        if (grabbedObject != null)
        {
            if (grabbedObject.CompareTag("iii") && snapSystem != null)
            {
                // Vérification de la validité du placement
                Vector3 snapPos = snapSystem.GetSnapPosition(grabbedObject.transform.position);
                var blockPositions = snapSystem.GetTetraminoBlockPositions(grabbedObject, snapPos);
                bool isValid = snapSystem.IsPlacementValid(blockPositions, grabbedObject);

                if (isValid)
                {
                    snapSystem.SnapToGrid(grabbedObject);

                    Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true; 
                    }
                }
                else
                {
                    // Retour à la position/rotation initiale
                    grabbedObject.transform.position = initialObjectPosition;
                    grabbedObject.transform.rotation = initialObjectRotation;

                    Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.isKinematic = true;
                    }
                }
            }
            else
            {
                grabbedObject.transform.position -= new Vector3(0, 0.10f, 0);
                grabbedObject.transform.rotation = initialObjectRotation;

                Rigidbody rb = grabbedObject.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.isKinematic = false;
                }
            }
        }

        grabbedObject = null;
        grabbingHandId = -1;
    }

    Quaternion GetHandRotation(Hand hand)
    {
        Vector3 handX = new Vector3(hand.Basis.xBasis.x, hand.Basis.xBasis.y, hand.Basis.xBasis.z);
        Vector3 handY = new Vector3(hand.Basis.yBasis.x, hand.Basis.yBasis.y, hand.Basis.yBasis.z);
        Vector3 handZ = new Vector3(hand.Basis.zBasis.x, hand.Basis.zBasis.y, hand.Basis.zBasis.z);

        Matrix4x4 rotationMatrix = new Matrix4x4();
        rotationMatrix.SetColumn(0, handX);
        rotationMatrix.SetColumn(1, handY);
        rotationMatrix.SetColumn(2, handZ);
        rotationMatrix.SetColumn(3, new Vector4(0, 0, 0, 1));

        return rotationMatrix.rotation;
    }
}