using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    public static ButtonVR lastPressedButton = null;

    // Délai entre les pressions de boutons pour éviter les doubles appuis accidentels
    private static float pressDelay = 0.5f;
    private static float lastPressTime = 0f;

    public static void RegisterPress(ButtonVR newButton)
    {
        // Protection contre les doubles clics trop rapides
        if (Time.time - lastPressTime < pressDelay)
        {
            Debug.Log($"[ButtonManager] Ignoré pression trop rapide sur {newButton.buttonID}");
            return;
        }

        lastPressTime = Time.time;

        if (lastPressedButton != null && lastPressedButton != newButton)
        {
            lastPressedButton.ResetVisual();
        }

        lastPressedButton = newButton;

        UIManager uiManager = FindObjectOfType<UIManager>();
        IntroManager introManager = FindObjectOfType<IntroManager>();

        if (uiManager == null)
        {
            Debug.LogError("[ButtonManager] UIManager non trouvé!");
            return;
        }

        if (introManager == null)
        {
            Debug.LogError("[ButtonManager] IntroManager non trouvé!");
            return;
        }

        // Si c'est un bouton de choix (ni Next, ni End, ni Pret)
        if (newButton.buttonID != "Next" && newButton.buttonID != "End" && newButton.buttonID != "Pret" && newButton.buttonID != "Test")
        {
            // Enregistre le choix sur le panel actuel
            uiManager.RegisterChoiceOnCurrentPanel(newButton.buttonID);
            Debug.Log($"[ButtonManager] Choix enregistré pour le panel {uiManager.GetCurrentPanelIndex()}: {newButton.buttonID}");
        }

        if (newButton.buttonID == "Test")
        {
            introManager.OnTestButtonPressed();
            Debug.Log("[ButtonManager] Bouton 'Test' pressé, séquence de test démarrée");
        }
        // Si c'est "Pret" (bouton initial)
        else if (newButton.buttonID == "Pret")
        {
            introManager.OnPlayerReady();
            Debug.Log("[ButtonManager] Démarrage de la séquence d'animations");
        }
        // Si c'est "Next" (passer au panel suivant)
        else if (newButton.buttonID == "Next")
        {
            if (uiManager.HasMadeChoiceOnCurrentPanel())
            {
                // Vérifier que la réponse est bien enregistrée avant de passer au suivant
                string[] responses = uiManager.GetAllResponses();
                int currentPanel = uiManager.GetCurrentPanelIndex();

                if (string.IsNullOrEmpty(responses[currentPanel]))
                {
                    Debug.LogWarning($"[ButtonManager] Réponse non enregistrée pour le panel {currentPanel}");
                    // Ne pas passer au suivant si la réponse n'est pas enregistrée
                    return;
                }

                uiManager.ShowNextPanel();
                Debug.Log("[ButtonManager] Passage au panel suivant");
            }
            else
            {
                Debug.LogWarning("[ButtonManager] Faites un choix avant d'appuyer sur 'Next'");
            }
        }
        // Si c'est "End" (fin de la séquence de panels, passer à l'animation suivante)
        else if (newButton.buttonID == "End")
        {
            if (uiManager.HasMadeChoiceOnCurrentPanel())
            {
                // Vérifier que la réponse du panel actuel est bien enregistrée
                string[] responses = uiManager.GetAllResponses();
                int currentPanel = uiManager.GetCurrentPanelIndex();

                if (string.IsNullOrEmpty(responses[currentPanel]))
                {
                    Debug.LogWarning($"[ButtonManager] Réponse non enregistrée pour le panel {currentPanel}");
                    return;
                }

                if (introManager.isTestSequence)
                {
                    // Si on est dans une séquence de test, revenir au panneau d'intro
                    uiManager.HideAllPanels();
                    introManager.introPanel.SetActive(true);
                    Debug.Log("[ButtonManager] Fin de la séquence de test, retour au panneau d'introduction");
                }
                else
                {
                    // Uniquement disponible sur le dernier panel
                    if (uiManager.IsOnLastPanel())
                    {
                        // Vérifier que toutes les réponses sont complètes
                        if (uiManager.ArePanelsComplete())
                        {
                            introManager.OnEndButtonPressed();
                            Debug.Log("[ButtonManager] Fin des panels, passage à l'animation suivante");
                            introManager.LogRemainingVariations();
                        }
                        else
                        {
                            Debug.LogWarning("[ButtonManager] Certaines réponses sont incomplètes, vérifiez les panels précédents");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[ButtonManager] Le bouton 'End' ne peut être utilisé que sur le dernier panel");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[ButtonManager] Faites un choix avant d'appuyer sur 'End'");
            }
        }

        Debug.Log($"[ButtonManager] Bouton pressé : {newButton.buttonID}");
    }

    public static bool CanPress(ButtonVR button)
    {
        // Empêcher les pressions trop rapides
        if (Time.time - lastPressTime < pressDelay)
        {
            return false;
        }

        // Pour le bouton "End", vérifier qu'on est bien sur le dernier panel
        if (button.buttonID == "End")
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (uiManager == null)
            {
                Debug.LogError("[ButtonManager] UIManager non trouvé lors de la vérification de CanPress!");
                return false;
            }

            if (!uiManager.IsOnLastPanel())
            {
                return false;
            }
        }

        return lastPressedButton != button;
    }
}