using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ScenarioManagerB : MonoBehaviour
{
    private GRETAnimationManager gretaManager;
    private TimerManagerB timerManager;
    private SimpleTetraminoSnap snapManager;
    private AnimationTrigger animTrigg;
    private ChessboardGenerator boardGenerator;
    private GretaCharacterAnimator gretaAnimator;
    private ArmContact armContact;
    public Animator animator;

    public bool isTouch = true;


    private GameObject puzzleDefault;
    private GameObject puzzleEmpathy;
    private GameObject puzzleAttention;
    private GameObject puzzleCelebration;
    private GameObject puzzleWelcome;

    private GameObject buttonStart;
    private GameObject buttonUndo;
    private GameObject buttonReset;
    private GameObject timerB;

    private bool fmlTriggered = false;

    public bool isTimerEnabled = false;
    private bool isTouchTimerRunning = false;
    private float touchTimer = 0f;
    private string currentTouchAnimation = "";
    public string fileName = "touch_timer_log.csv";


    public enum ScenarioType
    {
        Default,
        Empathy,
        Attention,
        Celebration,
        Welcome,
        Tutoriel
    }

    public ScenarioType currentScenario = ScenarioType.Default;

    // Structure pour stocker les paramètres PlayFml
    public class FmlParams
    {
        public string gesture;
        public string type;
        public FmlParams(string gesture, string type)
        {
            this.gesture = gesture;
            this.type = type;
        }
    }

    // Dictionnaire des paramètres par scénario et action
    private Dictionary<ScenarioType, Dictionary<string, FmlParams>> scenarioFmlParams;

    void Awake()
    {
        gretaManager = FindObjectOfType<GRETAnimationManager>();
        snapManager = FindObjectOfType<SimpleTetraminoSnap>();
        timerManager = FindObjectOfType<TimerManagerB>();
        animTrigg = FindObjectOfType<AnimationTrigger>();
        boardGenerator = FindObjectOfType<ChessboardGenerator>();
        gretaAnimator = FindObjectOfType<GretaCharacterAnimator>();
        armContact = FindObjectOfType<ArmContact>();
        buttonStart = GameObject.Find("ButtonStart");
        buttonUndo = GameObject.Find("ButtonUndo");
        buttonReset = GameObject.Find("ButtonReset");
        timerB = GameObject.Find("TimerB");

        puzzleDefault = GameObject.Find("PuzzleD");
        puzzleEmpathy = GameObject.Find("PuzzleE");
        puzzleAttention = GameObject.Find("PuzzleA");
        puzzleCelebration = GameObject.Find("PuzzleC");
        puzzleWelcome = GameObject.Find("PuzzleW");
        if (puzzleDefault != null) puzzleDefault.SetActive(false);
        if (puzzleEmpathy != null) puzzleEmpathy.SetActive(false);
        if (puzzleAttention != null) puzzleAttention.SetActive(false);
        if (puzzleCelebration != null) puzzleCelebration.SetActive(false);
        if (puzzleWelcome != null) puzzleWelcome.SetActive(false);

        // Initialisation des paramètres pour chaque scénario et chaque action
        scenarioFmlParams = new Dictionary<ScenarioType, Dictionary<string, FmlParams>>
        {
            {
                ScenarioType.Default, new Dictionary<string, FmlParams>
                {
                    { "OnStart", new FmlParams("welcomeTest", "Minor") },
                    { "OnStartGame", new FmlParams("EncourageGestureEarly", "Dialog") },
                    { "OnFinishGame", new FmlParams("CongratulateOfferPlayAgainGestureTimer", "Dialog") },
                    { "OnFinishGameWithTimer", new FmlParams("ComfortMistakeGestureEarly", "Dialog") },
                    { "OnMidTime", new FmlParams("CongratulateOfferPlayAgainGestureTimer", "Dialog") },
                    { "OnMistake", new FmlParams("ComfortMistakeGestureLate", "Dialog") },
                    { "OnEnd", new FmlParams("ThankYouFarewellGesture", "Dialog") }
                }
            },
            {
                ScenarioType.Empathy, new Dictionary<string, FmlParams>
                {
                    { "OnStart", new FmlParams("StartEmpathy", "Dialog") },
                    { "OnStartGame", new FmlParams("StartEmpathyGame", "Dialog") },
                    { "OnFinishGame", new FmlParams("FinishGameWinE", "Dialog") },
                    { "OnFinishGameWithTimer", new FmlParams("ComfortMistakeGesture", "Dialog") }
                }
            },
            {
                ScenarioType.Attention, new Dictionary<string, FmlParams>
                {
                    { "OnStart", new FmlParams("StartAttention", "Dialog") },
                    { "OnStartGame", new FmlParams("StartEmpathyGame", "Dialog") },
                    { "OnFinishGame", new FmlParams("FinishGameWinA", "Dialog") },
                    { "OnFinishGameWithTimer", new FmlParams("FinishLooseA", "Dialog") },
                    { "OnMidTime", new FmlParams("DirectAttentionTime", "Dialog") }
                }
            },
            {
                ScenarioType.Celebration, new Dictionary<string, FmlParams>
                {
                    { "OnStart", new FmlParams("StartC", "Dialog") },
                    { "OnStartGame", new FmlParams("StartEmpathyGame", "Dialog") },
                    { "OnFinishGame", new FmlParams("FinishGameWinC", "Dialog") },
                    { "OnFinishGameWithTimer", new FmlParams("FinishLooseC", "Dialog") },
                    { "OnMidTime", new FmlParams("DirectAttentionTimeWithTips", "Dialog") },
                    { "OnEnd", new FmlParams("End", "Dialog") }
                }
            },
            {
                ScenarioType.Welcome, new Dictionary<string, FmlParams>
                {
                    { "OnStart", new FmlParams("WelcomeSpeech", "Dialog") }
                }
            },
            {
                ScenarioType.Tutoriel, new Dictionary<string, FmlParams>
                {
                    { "OnStart", new FmlParams("TutorielStart", "Dialog") },
                    { "OnFinishGame", new FmlParams("TutorielFinishGame", "Dialog") },
                }
            }
        };
    }

    // Start() reste vide ou peut être supprimé si inutile
    void Start()
    {
        SetScenario(currentScenario);

        buttonStart.SetActive(false);
        buttonUndo.SetActive(false);
        buttonReset.SetActive(false);

        timerB.SetActive(false);


    }
    void Update()
    {
        if (isTouchTimerRunning)
        {
            touchTimer += Time.deltaTime;
            if (touchTimer >= 15f) 
            {
                StopTouchTimer();
            }
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            gretaManager.expFolder = "BenjExpe";

            gretaAnimator.useBapAnimation = true;
            gretaManager.PlayFml("ResetPose", "ResetPose");
        }
        else if (Input.GetKeyDown(KeyCode.F))
        {
            Debug.Log("Setting scenario to Empathy");

            gretaManager.expFolder = "BenjExpe";
            SetScenario(ScenarioType.Empathy);

            boardGenerator.GenerateBoard(4, 4);
            buttonReset.SetActive(true);
            buttonUndo.SetActive(true);
            buttonStart.SetActive(true);
            SetActivePuzzle(puzzleEmpathy);

            timerB.SetActive(true);
            timerManager.ResetTimer();
            timerManager.durationTimer = 90;
            timerManager.SetTimerDisplay(1, 30);

            OnStart();
        }
        else if (Input.GetKeyDown(KeyCode.G))
        {
            Debug.Log("Setting scenario to Attention");

            gretaManager.expFolder = "BenjExpe";
            SetScenario(ScenarioType.Attention);

            boardGenerator.GenerateBoard(7, 4);
            buttonReset.SetActive(true);
            buttonUndo.SetActive(true);
            buttonStart.SetActive(true);
            SetActivePuzzle(puzzleAttention);

            timerB.SetActive(true);
            timerManager.ResetTimer();
            timerManager.durationTimer = 210;
            timerManager.SetTimerDisplay(3, 30);

            OnStart();
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            Debug.Log("Setting scenario to Celebration");

            gretaManager.expFolder = "BenjExpe";
            SetScenario(ScenarioType.Celebration);

            boardGenerator.GenerateBoard(7, 4);
            buttonReset.SetActive(true);
            buttonUndo.SetActive(true);
            buttonStart.SetActive(true);
            SetActivePuzzle(puzzleCelebration);

            timerB.SetActive(true);
            timerManager.ResetTimer();
            timerManager.durationTimer = 210;
            timerManager.SetTimerDisplay(3, 30);

            OnStart();
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            Debug.Log("Welcome");

            gretaManager.expFolder = "BenjExpe";
            SetScenario(ScenarioType.Welcome);

            OnStart();
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            Debug.Log("Setting scenario to Tutoriel");

            gretaManager.expFolder = "BenjExpe";
            SetScenario(ScenarioType.Tutoriel);

            OnStart();
        }
    }

    public void StartTouchTimer(string animationName = "")
    {
        isTouchTimerRunning = true;
        touchTimer = 0f;
        currentTouchAnimation = animationName;
    }

    // Appelle cette méthode pour arrêter le timer
    public void StopTouchTimer()
    {
        if (isTouchTimerRunning)
        {
            isTouchTimerRunning = false;
            Debug.Log("Temps écoulé entre le toucher et le regard : " + touchTimer + " secondes pour l'animation : " + currentTouchAnimation);
            LogTouchTimerToCSV(currentTouchAnimation, touchTimer);
            currentTouchAnimation = "";
        }
    }

    private void LogTouchTimerToCSV(string animation, float time)
    {
        string filePath = "D:/Users/bdukatar/perso/STAGE_ANR_Match/i/Expe/" + fileName;
        bool fileExists = File.Exists(filePath);

        using (StreamWriter writer = new StreamWriter(filePath, true))
        {
            if (!fileExists)
                writer.WriteLine("Animation,Time"); // En-tête si nouveau fichier

            writer.WriteLine($"{animation},{time}");
        }
    }

    // Ajoute cette méthode utilitaire :
    private void SetActivePuzzle(GameObject toActivate)
    {
        // Désactive tous les puzzles
        if (puzzleDefault != null) puzzleDefault.SetActive(false);
        if (puzzleEmpathy != null) puzzleEmpathy.SetActive(false);
        if (puzzleAttention != null) puzzleAttention.SetActive(false);
        if (puzzleCelebration != null) puzzleCelebration.SetActive(false);
        if (puzzleWelcome != null) puzzleWelcome.SetActive(false);

        // Active seulement celui demandé
        if (toActivate != null) toActivate.SetActive(true);

        snapManager.initialPositions.Clear();
        foreach (GameObject tetramino in GameObject.FindGameObjectsWithTag("iii"))
        {
            snapManager.initialPositions[tetramino] = tetramino.transform.position;
        }
    }


    public void SetScenario(ScenarioType scenario)
    {
        // D'abord, retire tous les listeners pour éviter les doublons
        if (snapManager != null)
            snapManager.isFinishedEvent.RemoveListener(OnFinishGame);

        if (timerManager != null)
        {
            timerManager.startGame.RemoveListener(OnStartGame);
            timerManager.timerEnded.RemoveListener(OnFinishGameWithTimer);
            timerManager.midTimeReached.RemoveListener(OnMidTime);
        }

        // Ensuite, ajoute les listeners selon le scénario choisi
        switch (scenario)
        {
            case ScenarioType.Default:
                if (snapManager != null)
                    snapManager.isFinishedEvent.AddListener(OnFinishGame);
                if (timerManager != null)
                {
                    timerManager.startGame.AddListener(OnStartGame);
                    timerManager.timerEnded.AddListener(OnFinishGameWithTimer);
                    timerManager.midTimeReached.AddListener(OnMidTime);
                }
                break;

            case ScenarioType.Empathy:
                if (snapManager != null)
                    snapManager.isFinishedEvent.AddListener(OnFinishGame);
                if (timerManager != null)
                    timerManager.startGame.AddListener(OnStartGame);
                    timerManager.timerEnded.AddListener(OnFinishGameWithTimer);
                break;

            case ScenarioType.Attention:
                if (snapManager != null)
                    snapManager.isFinishedEvent.AddListener(OnFinishGame);
                if (timerManager != null)
                {
                    timerManager.startGame.AddListener(OnStartGame);
                    timerManager.timerEnded.AddListener(OnFinishGameWithTimer);
                    timerManager.midTimeReached.AddListener(OnMidTime);
                }
                break;

            case ScenarioType.Celebration:
                if (snapManager != null)
                    snapManager.isFinishedEvent.AddListener(OnFinishGame);
                if (timerManager != null)
                {
                    timerManager.startGame.AddListener(OnStartGame);
                    timerManager.timerEnded.AddListener(OnFinishGameWithTimer);
                    timerManager.midTimeReached.AddListener(OnMidTime);
                }
                break;
            case ScenarioType.Welcome:
                if (snapManager != null)
                    snapManager.isFinishedEvent.AddListener(OnFinishGame);
                break;
            case ScenarioType.Tutoriel:
                if (snapManager != null)
                    snapManager.isFinishedEvent.AddListener(OnFinishGame);
                break;    

        }

        currentScenario = scenario;
    }

    // Méthode utilitaire pour récupérer les bons paramètres selon le scénario et l'action
    private FmlParams GetFmlParams(string action)
    {
        if (scenarioFmlParams.TryGetValue(currentScenario, out var actions) && actions.TryGetValue(action, out var param))
            return param;
        return null;
    }

    private IEnumerator WaitAndPlayFml(string animName, FmlParams param, float triggerTime)
    {
        yield return null;

        fmlTriggered = false;
        while (!fmlTriggered)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(2);
            if (stateInfo.IsName(animName) && stateInfo.normalizedTime >= triggerTime)
            {
                if (gretaManager != null && param != null)
                    gretaManager.PlayFml(param.gesture, param.type);
                    if (currentScenario == ScenarioType.Celebration && (animName == "caresse" || animName == "hit"))
                    {
                        Debug.Log("Celebration scenario: waiting 10 seconds before ending");
                        yield return new WaitForSeconds(10f);
                        Debug.Log("Ending Celebration scenario");
                        OnEnd();
                    }
                    if (currentScenario == ScenarioType.Tutoriel && animName == "tap")
                    {
                        boardGenerator.GenerateBoard(4, 3);
                        SetActivePuzzle(puzzleWelcome);
                        buttonReset.SetActive(true);
                        buttonUndo.SetActive(true);
                    }
                fmlTriggered = true;


            }
            yield return null;
        }
    }

    public void OnWelcome()
    {
        gretaManager?.PlayFml("WelcomeSpeechGesture", "Dialog");

    }

    public void OnStart()
    {
        var param = GetFmlParams("OnStart");
        if (currentScenario == ScenarioType.Welcome)
        {
            gretaAnimator.useBapAnimation = false;
            gretaManager.PlayFml(param.gesture, param.type);
            animator.CrossFade("Welcome", 0.2f);

        }
        if (currentScenario == ScenarioType.Tutoriel)
        {
            if (isTouch)
            {
                gretaAnimator.useBapAnimation = false;
                armContact.SetSoundIDs("tap", 0);
                animTrigg.PlayAnimation("tap");
                StartCoroutine(WaitAndPlayFml("tap", param, 0.35f));
            }
            else
            {
                gretaAnimator.useBapAnimation = true;
                if (gretaManager != null && param != null)
                    gretaManager.PlayFml(param.gesture, param.type);
            }
        }
        if (currentScenario == ScenarioType.Empathy)
        {
            gretaAnimator.useBapAnimation = false;
            gretaManager.PlayFml(param.gesture, param.type);
            animator.CrossFade("Start1", 0.2f);
        }
        if (currentScenario == ScenarioType.Attention)
        {
            gretaAnimator.useBapAnimation = false;
            gretaManager.PlayFml(param.gesture, param.type);
            animator.CrossFade("Start2", 0.2f);
        }
        if (currentScenario == ScenarioType.Celebration)
        {
            gretaAnimator.useBapAnimation = false;
            gretaManager.PlayFml(param.gesture, param.type);
            animator.CrossFade("Start3", 0.2f);
        }
    }

    public void OnStartGame()
    {
        buttonStart.SetActive(false);

        var param = GetFmlParams("OnStartGame");
        
        gretaAnimator.useBapAnimation = true;
        gretaManager.PlayFml(param.gesture, param.type);

    }

    public void OnFinishGame()
    {
        Debug.Log("Finish game in ScenarioManagerB");

        var param = GetFmlParams("OnFinishGame");

        timerManager.ResetTimer();
        Debug.Log("Resetting timer in ScenarioManagerB : " + timerManager.gameStarted);

        if (currentScenario == ScenarioType.Tutoriel)
        {
            gretaAnimator.useBapAnimation = false;
            gretaManager.PlayFml(param.gesture, param.type);
            animator.CrossFade("Tuto", 0.2f);
        }
        if (currentScenario == ScenarioType.Empathy)
        {
            if (isTouch)
            {
                gretaAnimator.useBapAnimation = false;
                armContact.SetSoundIDs("frot",0);
                animTrigg.PlayAnimation("frot");
                StartCoroutine(WaitAndPlayFml("frot", param, 0.10f));
            }
            else
            {
                gretaAnimator.useBapAnimation = true;
                if (gretaManager != null && param != null)
                    gretaManager.PlayFml(param.gesture, param.type);
            }
        }
        if (currentScenario == ScenarioType.Attention)
        {
            if (isTouch)
            {
                gretaAnimator.useBapAnimation = false;
                armContact.SetSoundIDs("frot",1);
                animTrigg.PlayAnimation("frot");
                StartCoroutine(WaitAndPlayFml("frot", param, 0.10f));
            }
            else
            {
                gretaAnimator.useBapAnimation = true;
                if (gretaManager != null && param != null)
                    gretaManager.PlayFml(param.gesture, param.type);
            }
        }
        if (currentScenario == ScenarioType.Celebration)
        {
            if (isTouch)
            {
                gretaAnimator.useBapAnimation = false;
                animTrigg.PlayAnimation("hit");
                StartCoroutine(WaitAndPlayFml("hit", param, 0.10f));
            }
            else
            {
                gretaAnimator.useBapAnimation = true;
                if (gretaManager != null && param != null)
                    gretaManager.PlayFml(param.gesture, param.type);
            }
        }
    }

    public void OnFinishGameWithTimer()
    {
        snapManager.ResetAllTetraminos();
        var param = GetFmlParams("OnFinishGameWithTimer");
        if (currentScenario == ScenarioType.Empathy)
            armContact.SetSoundIDs("caresse", 0);
        if (currentScenario == ScenarioType.Attention)
            armContact.SetSoundIDs("caresse", 1);
        if (currentScenario == ScenarioType.Celebration)
            armContact.SetSoundIDs("caresse", 0);
        if (isTouch)
        {
            gretaAnimator.useBapAnimation = false;
            animTrigg.PlayAnimation("caresse");
            StartCoroutine(WaitAndPlayFml("caresse", param, 0.0f));

        }
    }

    public void OnMidTime()
    {
        Debug.Log("Mid time reached in ScenarioManagerB");
        var param = GetFmlParams("OnMidTime");
        if (currentScenario == ScenarioType.Attention)
            armContact.SetSoundIDs("tap", 0);
        if (currentScenario == ScenarioType.Celebration)
            armContact.SetSoundIDs("tap", 1);
        if (isTouch)
        {
            gretaAnimator.useBapAnimation = false;
            animTrigg.PlayAnimation("tap");
            StartCoroutine(WaitAndPlayFml("tap", param, 0.30f));
        }
        else
        {
            gretaAnimator.useBapAnimation = true;
            if (gretaManager != null && param != null)
                gretaManager.PlayFml(param.gesture, param.type);
        } 
    }

    public void OnEnd()
    {
        var param = GetFmlParams("OnEnd");
        if (gretaManager != null && param != null)
            
            gretaAnimator.useBapAnimation = false;
            gretaManager.PlayFml(param.gesture, param.type);
            animator.CrossFade("End", 0.2f);
    }
}
