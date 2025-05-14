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
    public string fileName = "questionnaire_responses.csv";
    public string filePath = @"D:\Users\bdukatar\perso\STAGE_ANR_Match\i"; // <- votre chemin

    public bool isTestSequence = false;

    private Dictionary<string, int> animationSoundVariations = new Dictionary<string, int>
    {
        { "caresse", 8 },
        { "frot", 7 },
        { "tap", 7 },
        { "hit", 5 }
    };

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
    }

    void Start()
    {
        introPanel.SetActive(true);
        uiManager.HideAllPanels();
        InitializeRandomizedSequence();
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
        StartCoroutine(PlayTestSequence());
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

        uiManager.StartPanelSequence();

        yield return new WaitUntil(() => uiManager.ArePanelsComplete());
        yield return new WaitForSeconds(1f);

        introPanel.SetActive(true);
        uiManager.HideAllPanels();

        isTestSequence = false; // Désactiver le mode test après retour au panneau d'intro
    }

    public void OnPlayerReady()
    {
        introPanel.SetActive(false);
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

        uiManager.StartPanelSequence();
    }
    
    public void SaveResponsesToCSV()
    {
        string path = Path.Combine(filePath, fileName);

        // Créer le dossier s'il n'existe pas
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

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
            
            for (int i = 0; i < response.PanelResponses.Length; i++)
            {
                string cleanResponse = response.PanelResponses[i].Replace(",", " ");
                csvContent.Append($",{cleanResponse}");
            }
            
            csvContent.AppendLine();
        }

        File.WriteAllText(filePath, csvContent.ToString());
        Debug.Log($"[IntroManager] Réponses sauvegardées dans : {filePath}");
    }

    public void OnEndButtonPressed()
    {
        // Enregistrer les réponses actuelles avant de passer à l'animation suivante
        SaveCurrentResponses();
        
        uiManager.HideAllPanels();
        PlayNextAnimation();
    }
    
    private void SaveCurrentResponses()
    {
        string[] currentResponses = uiManager.GetAllResponses();
        ResponseData data = new ResponseData(currentAnimType, currentSoundIndex, currentResponses);
        allResponses.Add(data);
        
        // Log pour débogage
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
}