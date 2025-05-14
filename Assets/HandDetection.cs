using UnityEngine;
using Leap;
using Leap.Unity;
using UnityEngine.InputSystem;
public class HandDetection : MonoBehaviour
{
    public LeapServiceProvider leapProvider;
    public Transform leftHandTarget;
    public Transform rightHandTarget;
    public Transform leftRestPosition;
    public Transform rightRestPosition;

    void Update()
    {
        Frame frame = leapProvider.CurrentFrame;

        bool leftDetected = false;
        bool rightDetected = false;

        foreach (Hand hand in frame.Hands)
        {
            Debug.Log("Hand detected: " + hand.IsLeft + " " + hand.IsRight);
            if (hand.IsLeft) leftDetected = true;
            if (hand.IsRight) rightDetected = true;
        }

        if (!leftDetected)
        {
            Debug.Log("Left hand not detected, resetting position.");
            leftHandTarget.position = leftRestPosition.position;
            leftHandTarget.rotation = leftRestPosition.rotation;
        }

        if (!rightDetected)
        {
            Debug.Log("Right hand not detected, resetting position.");
            rightHandTarget.position = rightRestPosition.position;
            rightHandTarget.rotation = rightRestPosition.rotation;
        }
    }
}
