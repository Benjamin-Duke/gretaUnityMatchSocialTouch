using UnityEngine;
using Leap;
using Leap.Unity;

public class HandGrabState : MonoBehaviour
{
    public LeapServiceProvider provider; // Assigne dans l'inspecteur
    public float grabThreshold = 0.8f;
    public float releaseThreshold = 0.3f;

    public bool IsGrabbingLeft { get; private set; }
    public bool IsGrabbingRight { get; private set; }

    void Update()
    {
        Frame frame = provider.CurrentFrame;

        foreach (var hand in frame.Hands)
        {
            if (hand.IsLeft)
            {
                IsGrabbingLeft = hand.GrabStrength > grabThreshold;
                if (hand.GrabStrength < releaseThreshold) IsGrabbingLeft = false;
            }
            else
            {
                IsGrabbingRight = hand.GrabStrength > grabThreshold;
                if (hand.GrabStrength < releaseThreshold) IsGrabbingRight = false;
                //Debug.Log($"Right Hand Grab Strength: {hand.GrabStrength}");
            }
        }
    }
}
