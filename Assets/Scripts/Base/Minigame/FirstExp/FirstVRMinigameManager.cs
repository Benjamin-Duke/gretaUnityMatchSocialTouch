using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Minigame;
using UnityEngine;

//To setup the tetros prefab from Unity Editor
[Serializable]
public struct TetroPrefab
{
    public GameObject prefab;
    public Tetromino tetrotype;
}

//Class dedicated to managing the state of the Minigame so as to make the scenario managers easier to maintain and use
public class FirstVRMinigameManager : MonoBehaviour
{
    [SerializeField] public TetroPrefab[] tetrosPrefab;

    private Dictionary<Tetromino, GameObject> _currentTetros = new Dictionary<Tetromino, GameObject>();

    private HashSet<Tetromino> _tetroCounter = new HashSet<Tetromino>();
    
    [Tooltip("GameObject with the TimerManager to start, stop and reset the timer when we start the task.")]
    public GameObject timer;
    private TimerManager _timerManager;

    public FirstVRExperimentManager scenarioManager;
    
    // Start is called before the first frame update
    void Start()
    {
        _timerManager = timer.GetComponent<TimerManager>();
        if (_timerManager == null)
        {
            Debug.LogError("No TimerManager found : timer will not be updated during the game.");
        }
        
        _timerManager.timerEnded.AddListener(EndMinigame);

    }

    // Update is called once per frame
    void Update()
    {
        if (!_timerManager.IsTimerStarted()) return;
        if (_tetroCounter.Count == 9)
        {
            EndMinigame();
        }
    }

    public void SetupTetros()
    {
        foreach (var tetro in tetrosPrefab)
        {
            if (_currentTetros.ContainsKey(tetro.tetrotype))
            {
                Destroy(_currentTetros[tetro.tetrotype]);
                _currentTetros[tetro.tetrotype] = Instantiate(tetrosPrefab.First(p => p.tetrotype == tetro.tetrotype).prefab);
            }
            else
                _currentTetros.Add(tetro.tetrotype, Instantiate(tetro.prefab));
        }
    }

    public void StartMinigame()
    {
        SetupTetros();
        _timerManager.ResetTimer();
        _tetroCounter.Clear();
        _timerManager.experiment = "First";
        _timerManager.StartStopTimer();
    }

    public bool IsMinigameStarted()
    {
        return _timerManager.IsTimerStarted();
    }

    public void EndMinigame()
    {
        var result = 0;
        if (_timerManager.IsTimerStarted())
        {
            _timerManager.StartStopTimer();
            result = (int) _timerManager.GetTimeResult();
        }
        scenarioManager.MinigameEnded(result);
    }
    
    public void ResetTetro(Tetromino tetrotype)
    {
        if (!_timerManager.IsTimerStarted()) return;
        if (!_currentTetros.ContainsKey(tetrotype)) return;
        Destroy(_currentTetros[tetrotype]);
        _currentTetros[tetrotype] = Instantiate(tetrosPrefab.First(p => p.tetrotype == tetrotype).prefab);
        scenarioManager.TetroFell();
    }

    public void TetroEntered(Tetromino tetrotype)
    {
        if (!IsMinigameStarted()) return;
        var prevCount = _tetroCounter.Count;
        _tetroCounter.Add(tetrotype);
        if (_tetroCounter.Count > prevCount)
            scenarioManager.UpdateGridStatus(_tetroCounter.Count);
    }
    
    public void TetroExited(Tetromino tetrotype)
    {
        if (!IsMinigameStarted()) return;
        _tetroCounter.Remove(tetrotype);
        scenarioManager.UpdateGridStatus(_tetroCounter.Count);
    }

    public int GetTimer()
    {
        return Mathf.FloorToInt(_timerManager.GetTimeResult());
    }
}
