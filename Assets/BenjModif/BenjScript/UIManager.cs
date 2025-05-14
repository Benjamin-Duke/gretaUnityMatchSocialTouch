using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject[] panels;
    private int currentPanelIndex = 0;
    private bool[] choicesMade;
    private string[] responses;

    void Start()
    {
        choicesMade = new bool[panels.Length];
        responses = new string[panels.Length];
        HideAllPanels();
    }

    public void StartPanelSequence()
    {
        // Réinitialiser les choix et réponses pour cette séquence de panels
        for (int i = 0; i < choicesMade.Length; i++)
        {
            choicesMade[i] = false;
            responses[i] = "";
        }

        currentPanelIndex = 0;
        ShowOnly(currentPanelIndex);
    }

    public void RegisterChoiceOnCurrentPanel(string responseText)
    {
        choicesMade[currentPanelIndex] = true;
        responses[currentPanelIndex] = responseText;
        Debug.Log($"[UIManager] Réponse enregistrée : {responseText} sur panel {currentPanelIndex}");
    }

    public bool HasMadeChoiceOnCurrentPanel()
    {
        return choicesMade[currentPanelIndex];
    }

    public void ShowNextPanel()
    {
        if (!choicesMade[currentPanelIndex])
        {
            Debug.LogWarning("[UIManager] Faites un choix avant de continuer.");
            return;
        }

        if (currentPanelIndex < panels.Length - 1)
        {
            currentPanelIndex++;
            ShowOnly(currentPanelIndex);
        }
        else
        {
            Debug.LogWarning("[UIManager] Dernier panel déjà atteint");
        }
    }

    public void HideAllPanels()
    {
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }
    }

    public void ShowOnly(int indexToShow)
    {
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == indexToShow);
        }
    }

    public string[] GetAllResponses()
    {
        // Retourner une copie des réponses pour éviter des modifications accidentelles
        string[] responsesCopy = new string[responses.Length];
        for (int i = 0; i < responses.Length; i++)
        {
            responsesCopy[i] = responses[i];
        }
        return responsesCopy;
    }

    public bool IsOnLastPanel()
    {
        return currentPanelIndex == panels.Length - 1;
    }

    public int GetCurrentPanelIndex()
    {
        return currentPanelIndex;
    }

    public bool ArePanelsComplete()
    {
        // Vérifie si tous les panels ont reçu une réponse
        foreach (var choice in choicesMade)
        {
            if (!choice)
            {
                return false;
            }
        }
        return true;
    }
}