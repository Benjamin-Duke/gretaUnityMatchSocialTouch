using UnityEngine;
using UnityEngine.Serialization;

public class AnatomyParameters : MonoBehaviour
{
    public enum AnatomyType
    {
        Hand = 0,
        Torso = 1,
        Head = 2,
        Forearm = 3,
        Arm = 4,
        Neck = 5
    }

    [SerializeField]
    [Tooltip(
        "Type of the body part. \n-Torso is for the back. \n-Special treatement is applied to head to torso; others are just to discriminate.")]
    public AnatomyType type;

    [FormerlySerializedAs("length1")] [SerializeField] [Tooltip("If Torso: height of the body part.\nIf head : 0\nIf other : length of the member")]
    public float length;

    [FormerlySerializedAs("length2")] [SerializeField] [Tooltip("f Torso: width of the body part.\nIf head or other : 0 (not used)")]
    public float width;
}