using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    public static ButtonVR lastPressedButton = null;

    public static void RegisterPress(ButtonVR newButton)
    {
        if (lastPressedButton != null && lastPressedButton != newButton)
        {
            lastPressedButton.ResetVisual();
        }

        lastPressedButton = newButton;

        UIManager uiManager = FindObjectOfType<UIManager>();
        IntroManager introManager = FindObjectOfType<IntroManager>();

        // Si c'est un bouton de choix (ni Next, ni End, ni Pret)
        if (newButton.buttonID != "Next" && newButton.buttonID != "End" && newButton.buttonID != "Pret")
        {
            // Enregistre le choix sur le panel actuel
            uiManager.RegisterChoiceOnCurrentPanel(newButton.buttonID);

        }

        if (newButton.buttonID == "Test")
        {
            introManager.OnTestButtonPressed();
            Debug.Log("[ButtonManager] Bouton 'Test' pressé, séquence de test démarrée");
        }
        // Si c'est "Pret" (bouton initial)
        if (newButton.buttonID == "Pret")
        {
            introManager.OnPlayerReady();
            Debug.Log("[ButtonManager] Démarrage de la séquence d'animations");
        }

        // Si c'est "Next" (passer au panel suivant)
        if (newButton.buttonID == "Next")
        {
            if (uiManager.HasMadeChoiceOnCurrentPanel())
            {
                uiManager.ShowNextPanel();
                Debug.Log("[ButtonManager] Passage au panel suivant");
            }
            else
            {
                Debug.LogWarning("[ButtonManager] Faites un choix avant d'appuyer sur 'Next'");
            }
        }

        // Si c'est "End" (fin de la séquence de panels, passer à l'animation suivante)
        if (newButton.buttonID == "End")
        {
            if (uiManager.HasMadeChoiceOnCurrentPanel())
            {
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
                        introManager.OnEndButtonPressed();
                        Debug.Log("[ButtonManager] Fin des panels, passage à l'animation suivante");
                        introManager.LogRemainingVariations();
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
        // Pour le bouton "End", vérifier qu'on est bien sur le dernier panel
        if (button.buttonID == "End")
        {
            UIManager uiManager = FindObjectOfType<UIManager>();
            if (!uiManager.IsOnLastPanel())
            {
                return false;
            }
        }
        
        return lastPressedButton != button;
    }
}