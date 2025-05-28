using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject[] panels;
    private int currentPanelIndex = 0;
    private bool[] choicesMade;
    private string[] responses;
    
    // Ajout d'une sauvegarde de secours pour les réponses
    private string[] backupResponses;

    void Start()
    {
        choicesMade = new bool[panels.Length];
        responses = new string[panels.Length];
        backupResponses = new string[panels.Length];
        HideAllPanels();
    }

    public void StartPanelSequence()
    {
        // Réinitialiser les choix et réponses pour cette séquence de panels
        for (int i = 0; i < choicesMade.Length; i++)
        {
            choicesMade[i] = false;
            responses[i] = "";
            backupResponses[i] = "";
        }

        currentPanelIndex = 0;
        ShowOnly(currentPanelIndex);
    }

    public void RegisterChoiceOnCurrentPanel(string responseText)
    {
        if (currentPanelIndex < 0 || currentPanelIndex >= choicesMade.Length)
        {
            Debug.LogError($"[UIManager] Index de panel invalide: {currentPanelIndex}");
            return;
        }
        
        choicesMade[currentPanelIndex] = true;
        responses[currentPanelIndex] = responseText;
        backupResponses[currentPanelIndex] = responseText; // Sauvegarde de secours
        
        Debug.Log($"[UIManager] Réponse enregistrée: '{responseText}' sur panel {currentPanelIndex}");
        
        // Vérification immédiate que la réponse a bien été enregistrée
        if (responses[currentPanelIndex] != responseText)
        {
            Debug.LogError($"[UIManager] ERREUR: Réponse non enregistrée correctement pour panel {currentPanelIndex}!");
            // Nouvelle tentative
            responses[currentPanelIndex] = responseText;
        }
    }

    public bool HasMadeChoiceOnCurrentPanel()
    {
        if (currentPanelIndex < 0 || currentPanelIndex >= choicesMade.Length)
        {
            Debug.LogError($"[UIManager] Index de panel invalide lors de la vérification: {currentPanelIndex}");
            return false;
        }
        return choicesMade[currentPanelIndex];
    }

    public void ShowNextPanel()
    {
        if (currentPanelIndex < 0 || currentPanelIndex >= choicesMade.Length)
        {
            Debug.LogError($"[UIManager] Index de panel invalide lors du passage au suivant: {currentPanelIndex}");
            return;
        }
        
        if (!choicesMade[currentPanelIndex])
        {
            Debug.LogWarning("[UIManager] Faites un choix avant de continuer.");
            return;
        }
        
        // Vérification supplémentaire que la réponse est bien enregistrée
        if (string.IsNullOrEmpty(responses[currentPanelIndex]))
        {
            Debug.LogWarning($"[UIManager] Réponse manquante pour panel {currentPanelIndex} avant de passer au suivant!");
            
            // Si la réponse est vide mais qu'on a fait un choix, essayer d'utiliser la sauvegarde
            if (!string.IsNullOrEmpty(backupResponses[currentPanelIndex]))
            {
                Debug.Log($"[UIManager] Utilisation de la sauvegarde de réponse pour panel {currentPanelIndex}: {backupResponses[currentPanelIndex]}");
                responses[currentPanelIndex] = backupResponses[currentPanelIndex];
            }
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
        if (indexToShow < 0 || indexToShow >= panels.Length)
        {
            Debug.LogError($"[UIManager] Tentative d'afficher un panel invalide: {indexToShow}");
            return;
        }
        
        for (int i = 0; i < panels.Length; i++)
        {
            panels[i].SetActive(i == indexToShow);
        }
    }

    public string[] GetAllResponses()
    {
        // Vérification supplémentaire pour les réponses manquantes
        for (int i = 0; i < responses.Length; i++)
        {
            if (choicesMade[i] && string.IsNullOrEmpty(responses[i]))
            {
                Debug.LogWarning($"[UIManager] Réponse manquante pour panel {i} malgré choix effectué!");
                
                // Utiliser la sauvegarde si disponible
                if (!string.IsNullOrEmpty(backupResponses[i]))
                {
                    Debug.Log($"[UIManager] Utilisation de la sauvegarde pour panel {i}: {backupResponses[i]}");
                    responses[i] = backupResponses[i];
                }
            }
        }
        
        // Retourner une copie des réponses pour éviter des modifications accidentelles
        string[] responsesCopy = new string[responses.Length];
        for (int i = 0; i < responses.Length; i++)
        {
            responsesCopy[i] = responses[i];
            Debug.Log($"[UIManager] Réponse pour panel {i}: '{responses[i]}'");
        }
        return responsesCopy;
    }
    
    // Nouvelle méthode pour récupérer la réponse d'un panel spécifique
    public string GetResponseForPanel(int panelIndex)
    {
        if (panelIndex < 0 || panelIndex >= responses.Length)
        {
            Debug.LogError($"[UIManager] Tentative de récupérer la réponse d'un panel invalide: {panelIndex}");
            return null;
        }
        
        // Si la réponse principale est vide mais que nous avons une sauvegarde
        if (string.IsNullOrEmpty(responses[panelIndex]) && !string.IsNullOrEmpty(backupResponses[panelIndex]))
        {
            Debug.Log($"[UIManager] Récupération de la sauvegarde pour panel {panelIndex}: {backupResponses[panelIndex]}");
            responses[panelIndex] = backupResponses[panelIndex];
        }
        
        return responses[panelIndex];
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
        for (int i = 0; i < choicesMade.Length; i++)
        {
            if (!choicesMade[i])
            {
                Debug.LogWarning($"[UIManager] Panel {i} incomplet");
                return false;
            }
            
            // Vérifier également que la réponse existe
            if (string.IsNullOrEmpty(responses[i]))
            {
                Debug.LogWarning($"[UIManager] Réponse manquante pour panel {i}");
                return false;
            }
        }
        return true;
    }
}