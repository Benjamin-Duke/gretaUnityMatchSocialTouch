using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class TimerManagerB : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerTextBox;

    public bool isExperiment = false;

    public float durationTimer = 240f;

    private float _gameTimer = 0f;

    public bool gameStarted = false;
    private SimpleTetraminoSnap snapManager;
    
    public UnityEvent startGame;
    public UnityEvent timerEnded;
    public UnityEvent midTimeReached;
    // Start is called before the first frame update
    void Start()
    {
        // if (isExperiment)
        //     {
        //         var minutes = Mathf.FloorToInt(durationTimer / 60);
        //         SetTimerDisplay(minutes, Mathf.FloorToInt(durationTimer - minutes * 60));
        //     }
        //     else
        //     {
        //         SetTimerDisplay(0, 0);
        //     }

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.S))
            StartStopTimer();

        //Debug.Log($"TimerManagerB: Update called, gameStarted: {gameStarted}");
        if (!gameStarted) return;
        
        if (isExperiment)
        {
            UpdateTimerFirstExpe();
            if (_gameTimer >= durationTimer)
                TimerEnd();
        }
        else
        {
            UpdateTimer();
        }

    }

    private void UpdateTimer()
    {
        _gameTimer += Time.deltaTime;

        var minutes = Mathf.FloorToInt(_gameTimer / 60);
        var seconds = Mathf.FloorToInt(_gameTimer - minutes * 60);

        SetTimerDisplay(minutes, seconds);
    }

    private void UpdateTimerFirstExpe()
    {
        _gameTimer += Time.deltaTime;
        var timerDisplay = durationTimer - _gameTimer;

        var minutes = Mathf.FloorToInt(timerDisplay / 60);
        var seconds = Mathf.FloorToInt(timerDisplay - minutes * 60);

        // Use Mathf.Approximately for float comparison, or check if timerDisplay crosses the half point
        float halfTime = durationTimer / 2f;
        // Check if we just crossed the half time mark this frame
        if (_gameTimer - Time.deltaTime < halfTime && _gameTimer >= halfTime)
        {
            midTimeReached?.Invoke();
        }

        SetTimerDisplay(minutes, seconds);
    }

    public void SetTimerDisplay(int minutes, int seconds)
    {
        var gameTimerDisplay = $"{minutes:0}:{seconds:00}";

        timerTextBox.text = gameTimerDisplay;
    }

    public float GetTimeResult()
    {
        return _gameTimer;
    }

    private void TimerEnd()
    {
        Debug.Log("Timer ended");
        StartStopTimer();
        SetTimerDisplay(0,0);
        timerEnded?.Invoke();
        
    }

    public void StartStopTimer()
    {
        bool wasStarted = gameStarted;
        gameStarted = !gameStarted;

        if (!wasStarted && gameStarted)
        {
            if (_gameTimer >= durationTimer || _gameTimer == 0f)
            {
                _gameTimer = 0f;
                if (isExperiment)
                {
                    var minutes = Mathf.FloorToInt(durationTimer / 60);
                    SetTimerDisplay(minutes, Mathf.FloorToInt(durationTimer - minutes * 60));
                }
            }
            startGame?.Invoke();
        }
    }

    public bool IsTimerStarted()
    {
        return gameStarted;
    }
    
    public void ResetTimer()
    {
        gameStarted = false;
        _gameTimer = 0f;
    }
}
