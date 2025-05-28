using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
    
public class IntroManager : MonoBehaviour
{
    public GameObject introPanel;
    public AnimationTrigger animationTrigger;
    public UIManager uiManager;
    public ArmContact armContact;
    public GameObject GRETA; 
    private Vector3 basePosition;
    public Vector3 targetPosition = new Vector3(0, 2, 0); // Modifie selon ta cible
    public string fileName = "questionnaire_responses.csv";
    public string filePath = @"D:\Users\bdukatar\perso\STAGE_ANR_Match\i"; // <- votre chemin
    
    // Option pour sauvegarder après chaque animation (recommandé)
    public bool saveAfterEachAnimation = true;
    
    // Chemin pour les sauvegardes temporaires
    private string tempSavePath;

    public bool isTestSequence = false;

    private Dictionary<string, int> animationSoundVariations = new Dictionary<string, int>
    {
        { "caresse", 8 },
        { "frot", 7 },
        { "tap", 7 },
        { "hit", 5 }
    };
    // private Dictionary<string, int> animationSoundVariations = new Dictionary<string, int>
    // {
    //     { "caresse", 1 },
    //     { "frot", 1 },
    // };

    private Queue<(string, int)> randomizedAnimationQueue = new Queue<(string, int)>();
    private string currentAnimType = "";
    private int currentSoundIndex = 0;
    private bool sequenceComplete = false;
    
    // Structure pour stocker les réponses avec leur combinaison correspondante
    private List<ResponseData> allResponses = new List<ResponseData>();

    // Classe pour stocker les données de réponse
    private class ResponseData
    {
        public string AnimationType;
        public int SoundVariation;
        public string[] PanelResponses;
        
        public ResponseData(string animType, int soundVar, string[] responses)
        {
            AnimationType = animType;
            SoundVariation = soundVar;
            PanelResponses = responses;
        }
        
        // Méthode pour vérifier si toutes les réponses sont remplies
        public bool HasValidResponses(int expectedCount)
        {
            if (PanelResponses == null || PanelResponses.Length != expectedCount)
                return false;
                
            for (int i = 0; i < PanelResponses.Length; i++)
            {
                if (string.IsNullOrEmpty(PanelResponses[i]))
                {
                    Debug.LogWarning($"[ResponseData] Réponse manquante pour {AnimationType}_{SoundVariation} au panel {i}");
                    return false;
                }
            }
            return true;
        }
    }

    void Start()
    {
        basePosition = GRETA.transform.position;
        MoveObjectToTarget();
        introPanel.SetActive(true);
        uiManager.HideAllPanels();
        InitializeRandomizedSequence();
        
        // Initialiser le chemin de sauvegarde temporaire
        tempSavePath = Path.Combine(filePath, "temp_" + fileName);
        
        // Créer le dossier si nécessaire
        EnsureDirectoryExists();
    }
    
    private void EnsureDirectoryExists()
    {
        string directory = Path.GetDirectoryName(Path.Combine(filePath, fileName));
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private void InitializeRandomizedSequence()
    {
        List<(string, int)> allPairs = new List<(string, int)>();

        foreach (var entry in animationSoundVariations)
        {
            string animType = entry.Key;
            int count = entry.Value;
            for (int i = 0; i < count; i++)
            {
                allPairs.Add((animType, i));
            }
        }

        ShuffleList(allPairs);
        randomizedAnimationQueue = new Queue<(string, int)>(allPairs);
    }

    private void ShuffleList<T>(List<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }

    public void OnTestButtonPressed()
    {
        isTestSequence = true; // Activer le mode test
        MoveObjectToBase();
        StartCoroutine(PlayTestSequence());
    }

    private void MoveObjectToTarget()
    {
        if (GRETA != null)
            GRETA.transform.position = targetPosition;
    }

    private void MoveObjectToBase()
    {
        if (GRETA != null)
            GRETA.transform.position = basePosition;
    }

    private IEnumerator PlayTestSequence()
    {
        introPanel.SetActive(false);
        string testAnimType = "caresse"; // Exemple d'animation
        int testSoundIndex = 8; // Exemple d'ID sonore

        armContact.SetSoundIDs(testAnimType, testSoundIndex);
        animationTrigger.PlayAnimation(testAnimType);

        yield return new WaitUntil(() => animationTrigger.IsIdle());
        yield return new WaitForSeconds(0.5f);

        MoveObjectToTarget();
        uiManager.StartPanelSequence();

        yield return new WaitUntil(() => uiManager.ArePanelsComplete());
        yield return new WaitForSeconds(1f);

        //MoveObjectToBase();
        // yield return new WaitForSeconds(1f);
        // introPanel.SetActive(true);
        // uiManager.HideAllPanels();
        
        isTestSequence = false; // Désactiver le mode test après retour au panneau d'intro
    }

    public void OnPlayerReady()
    {
        introPanel.SetActive(false);
        MoveObjectToBase();
        sequenceComplete = false;
        allResponses.Clear(); // Réinitialiser les réponses au début
        PlayNextAnimation();
    }

    private void PlayNextAnimation()
    {
        if (randomizedAnimationQueue.Count == 0)
        {
            Debug.Log("Toutes les animations ont été jouées. Séquence terminée.");
            sequenceComplete = true;
            SaveResponsesToCSV();
            return;
        }

        var next = randomizedAnimationQueue.Dequeue();
        currentAnimType = next.Item1;
        currentSoundIndex = next.Item2;

        Debug.Log($"Animation: {currentAnimType}, Variation sonore: {currentSoundIndex}");
        StartCoroutine(PlayAnimationWithSound(currentAnimType, currentSoundIndex));
    }

    private IEnumerator PlayAnimationWithSound(string animName, int soundChoice)
    {
        armContact.SetSoundIDs(animName, soundChoice);
        animationTrigger.PlayAnimation(animName);

        yield return new WaitUntil(() => animationTrigger.IsIdle());
        yield return new WaitForSeconds(0.5f);

        MoveObjectToTarget();
        uiManager.StartPanelSequence();
    }
    
    public void SaveResponsesToCSV()
    {
        string path = Path.Combine(filePath, fileName);
        
        StringBuilder csvContent = new StringBuilder();
        
        // En-tête du CSV avec les questions pour tous les panels
        csvContent.Append("AnimationType,SoundVariation");
        for (int i = 0; i < uiManager.panels.Length; i++)
        {
            csvContent.Append($",Panel{i+1}Response");
        }
        csvContent.AppendLine();

        // Nombre de réponses incomplètes
        int incompleteResponses = 0;
        
        // Ajouter toutes les réponses collectées
        foreach (var response in allResponses)
        {
            // Vérifier que cette réponse est complète (tous les panels ont une réponse)
            if (!response.HasValidResponses(uiManager.panels.Length))
            {
                incompleteResponses++;
                Debug.LogWarning($"[IntroManager] Réponse incomplète détectée pour {response.AnimationType}_{response.SoundVariation}");
                
                // Si on a des réponses manquantes, on les remplace par "NON_ENREGISTRE"
                csvContent.Append($"{response.AnimationType},{response.SoundVariation}");
                for (int i = 0; i < uiManager.panels.Length; i++)
                {
                    string panelResponse = (i < response.PanelResponses.Length && !string.IsNullOrEmpty(response.PanelResponses[i])) 
                        ? response.PanelResponses[i].Replace(",", " ") 
                        : "NON_ENREGISTRE";
                    csvContent.Append($",{panelResponse}");
                }
                csvContent.AppendLine();
            }
            else
            {
                // Réponse complète
                csvContent.Append($"{response.AnimationType},{response.SoundVariation}");
                for (int i = 0; i < response.PanelResponses.Length; i++)
                {
                    string cleanResponse = response.PanelResponses[i].Replace(",", " ");
                    csvContent.Append($",{cleanResponse}");
                }
                csvContent.AppendLine();
            }
        }

        // Enregistrer le fichier CSV
        File.WriteAllText(path, csvContent.ToString());
        Debug.Log($"[IntroManager] Réponses sauvegardées dans : {path}");
        
        // Si on a détecté des réponses incomplètes, afficher un avertissement
        if (incompleteResponses > 0)
        {
            Debug.LogWarning($"[IntroManager] {incompleteResponses} réponses incomplètes détectées et marquées 'NON_ENREGISTRE' dans le CSV");
        }
    }
    
    // Sauvegarde temporaire des réponses (protection contre les crashs)
    private void SaveTemporaryResponses()
    {
        if (allResponses.Count == 0) return;
        
        StringBuilder csvContent = new StringBuilder();
        
        // En-tête du CSV avec les questions pour tous les panels
        csvContent.Append("AnimationType,SoundVariation");
        for (int i = 0; i < uiManager.panels.Length; i++)
        {
            csvContent.Append($",Panel{i+1}Response");
        }
        csvContent.AppendLine();

        // Ajouter toutes les réponses collectées
        foreach (var response in allResponses)
        {
            csvContent.Append($"{response.AnimationType},{response.SoundVariation}");
            
            for (int i = 0; i < uiManager.panels.Length; i++)
            {
                string panelResponse = (i < response.PanelResponses.Length && !string.IsNullOrEmpty(response.PanelResponses[i])) 
                    ? response.PanelResponses[i].Replace(",", " ") 
                    : "EN_ATTENTE";
                csvContent.Append($",{panelResponse}");
            }
            csvContent.AppendLine();
        }

        File.WriteAllText(tempSavePath, csvContent.ToString());
        Debug.Log($"[IntroManager] Sauvegarde temporaire dans : {tempSavePath}");
    }

    public void OnEndButtonPressed()
    {
        // Double vérification que toutes les réponses ont été enregistrées
        string[] currentResponses = uiManager.GetAllResponses();
        bool allValid = true;
        
        for (int i = 0; i < currentResponses.Length; i++)
        {
            if (string.IsNullOrEmpty(currentResponses[i]))
            {
                Debug.LogWarning($"[IntroManager] Panel {i} n'a pas de réponse enregistrée!");
                allValid = false;
                
                // Essayer de récupérer la réponse directement depuis UIManager
                currentResponses[i] = uiManager.GetResponseForPanel(i);
                
                if (string.IsNullOrEmpty(currentResponses[i]))
                {
                    Debug.LogError($"[IntroManager] Impossible de récupérer la réponse pour le panel {i}");
                    currentResponses[i] = "ERREUR_RECUPERATION";
                }
                else
                {
                    Debug.Log($"[IntroManager] Réponse récupérée pour panel {i}: {currentResponses[i]}");
                    allValid = true;
                }
            }
        }
        
        if (!allValid)
        {
            Debug.LogWarning("[IntroManager] Réponses incomplètes détectées!");
        }
        
        // Enregistrer les réponses actuelles
        SaveCurrentResponses();
        
        // Sauvegarder après chaque animation si l'option est activée
        if (saveAfterEachAnimation)
        {
            SaveTemporaryResponses();
        }
        
        uiManager.HideAllPanels();
        MoveObjectToBase();
        PlayNextAnimation();
    }
    
    private void SaveCurrentResponses()
    {
        string[] currentResponses = uiManager.GetAllResponses();
        
        // Vérification supplémentaire des réponses
        for (int i = 0; i < currentResponses.Length; i++)
        {
            if (string.IsNullOrEmpty(currentResponses[i]))
            {
                Debug.LogWarning($"[IntroManager] Réponse manquante pour le panel {i}!");
                
                // Si la réponse du panel est vide, essayer une dernière fois de la récupérer
                currentResponses[i] = uiManager.GetResponseForPanel(i) ?? "MANQUANT";
            }
        }
        
        ResponseData data = new ResponseData(currentAnimType, currentSoundIndex, currentResponses);
        allResponses.Add(data);
        
        // Log détaillé pour débogage
        StringBuilder logMessage = new StringBuilder();
        logMessage.AppendLine($"[IntroManager] Réponses enregistrées pour {currentAnimType}_{currentSoundIndex}:");
        for (int i = 0; i < currentResponses.Length; i++)
        {
            logMessage.AppendLine($"Panel {i}: {currentResponses[i]}");
        }
        Debug.Log(logMessage.ToString());
    }

    public void LogRemainingVariations()
    {
        Debug.Log($"Variations restantes : {randomizedAnimationQueue.Count}");
    }
    
    // Protection supplémentaire - sauvegarder en cas d'arrêt de l'application
    private void OnApplicationQuit()
    {
        if (!sequenceComplete && allResponses.Count > 0)
        {
            Debug.Log("[IntroManager] Sauvegarde d'urgence des réponses avant fermeture de l'application");
            SaveResponsesToCSV();
        }
    }
}