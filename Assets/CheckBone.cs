using UnityEngine;
using System.Collections.Generic;

public class GretaBoneChecker : MonoBehaviour
{
    // Liste des noms d'os attendus (à adapter si besoin)
    private static readonly string[] RequiredBones = new string[]
    {
        // FAP (visage)
        "LeftEye", "RightEye", "Nostrils", "LipCornerL", "LipCornerR",
        "UpperLidL", "LowerLidL", "UpperLidR", "LowerLidR",
        "BrowInnerL", "BrowOuterL", "BrowInnerR", "BrowOuterR",
        "CheekL", "CheekR", "LipLowerL", "LipLowerR", "LipUpperL", "LipUpperR", "Jaw",

        // BAP (corps)
        "Hips", "Head", "Neck1", "Neck", "Spine4", "Spine3", "Spine2", "Spine1", "Spine", "Spine2V", "Spine1V", "SpineV",
        "LeftUpLeg", "LeftUpLegRoll", "LeftLeg", "LeftFoot", "LeftLegRoll", "LeftToeBase",
        "RightUpLeg", "RightUpLegRoll", "RightLeg", "RightFoot", "RightLegRoll", "RightToeBase",
        "RightShoulder", "RightArm", "RightArmRoll", "RightForeArm", "RightHand", "RightForeArmRoll",
        "RightHandThumb1", "RightHandThumb2", "RightHandThumb3",
        "RightHandIndex0", "RightHandIndex1", "RightHandIndex2", "RightHandIndex3",
        "RightHandMiddle1", "RightHandMiddle2", "RightHandMiddle3",
        "RightHandRing1", "RightHandRing2", "RightHandRing3",
        "RightHandPinky0", "RightHandPinky1", "RightHandPinky2", "RightHandPinky3",
        "LeftShoulder", "LeftArm", "LeftArmRoll", "LeftForeArm", "LeftHand", "LeftForeArmRoll",
        "LeftHandThumb1", "LeftHandThumb2", "LeftHandThumb3",
        "LeftHandIndex0", "LeftHandIndex1", "LeftHandIndex2", "LeftHandIndex3",
        "LeftHandMiddle1", "LeftHandMiddle2", "LeftHandMiddle3",
        "LeftHandRing1", "LeftHandRing2", "LeftHandRing3",
        "LeftHandPinky0", "LeftHandPinky1", "LeftHandPinky2", "LeftHandPinky3"
    };

    [ContextMenu("Check Greta Bones")]
    public void CheckBones()
    {
        var allTransforms = GetComponentsInChildren<Transform>(true);
        var boneNames = new HashSet<string>();
        foreach (var t in allTransforms)
            boneNames.Add(t.name);

        List<string> missing = new List<string>();
        foreach (var req in RequiredBones)
            if (!boneNames.Contains(req))
                missing.Add(req);

        if (missing.Count == 0)
            Debug.Log("<color=green>[GretaBoneChecker]</color> Tous les os nécessaires sont présents !");
        else
        {
            Debug.LogWarning("<color=red>[GretaBoneChecker]</color> Os manquants ou mal nommés :\n" + string.Join(", ", missing));
        }
    }
}