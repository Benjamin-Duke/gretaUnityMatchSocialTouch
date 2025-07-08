using UnityEngine;

public class GRETATestRunner : MonoBehaviour
{
    public GRETAnimationManager gretaManager;
    public Animator animator;

    [Header("Animator Settings")]
    public int armsLayerIndex = 2; // index du layer "ArmsTrunk" dans ton Animator

    [Header("Test File Names (without .xml)")]
    public string testFmlFile = "TestFml"; // nom de fichier sans .xml
    public string testActionName = "Dialog"; // ou "Touch", "MinorSuccess", etc.

    public float blendDuration = 0.5f; // durée du blending en secondes

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log(">> Playing GRETA FML and releasing arms from Animator");
            var gretaAnimator = FindObjectOfType<GretaCharacterAnimator>();
            if (gretaAnimator != null)
                gretaAnimator.useBapAnimation = true;

            if (gretaManager != null)
                gretaManager.PlayFml("ResetPose", "ResetPose");
            gretaManager.PlayFml(testFmlFile, testActionName);
        }
    }

    public void OnFinishGame()
    {
        if (gretaManager != null)
            gretaManager.PlayFml(testFmlFile, testActionName);

    }
}
