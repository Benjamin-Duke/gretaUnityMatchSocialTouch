using UnityEngine;

public class GRETATestRunner : MonoBehaviour
{
    public GRETAnimationManager gretaManager;
    
    [Header("Test File Names (without .xml)")]
    public string testFmlFile = "TestFml"; // nom de fichier sans .xml
    public string testActionName = "Dialog"; // ou "Touch", "MinorSuccess", etc.

    void Update()
    {
        // Appuie sur la touche T pour tester une animation/audio
        if (Input.GetKeyDown(KeyCode.T))
        {
            Debug.Log(">> Playing test FML...");
            gretaManager.PlayFml(testFmlFile, testActionName);
        }
    }
}
