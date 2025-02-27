using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class TimerManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerTextBox;

    public string experiment = "First";

    public float durationTimer = 240f;

    private float _gameTimer = 0f;

    private bool _minigameStarted = false;
    
    public UnityEvent timerEnded;
    // Start is called before the first frame update
    void Start()
    {
        switch (experiment)
        {
            case "First":
                var minutes = Mathf.FloorToInt(durationTimer / 60);
                SetTimerDisplay(minutes, Mathf.FloorToInt(durationTimer - minutes * 60));
                break;
            case "Full":
                SetTimerDisplay(0, 0);
                break;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!_minigameStarted) return;
        switch (experiment)
        {
            case "First":
                UpdateTimerFirstExpe();
                if (_gameTimer >= durationTimer)
                    TimerEnd();
                break;
            case "Full":
                UpdateTimer();
                break;
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

        SetTimerDisplay(minutes, seconds);
    }

    private void SetTimerDisplay(int minutes, int seconds)
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
        StartStopTimer();
        SetTimerDisplay(0,0);
        timerEnded?.Invoke();
        
    }

    public void StartStopTimer()
    {
        _minigameStarted = _minigameStarted == false;
    }

    public bool IsTimerStarted()
    {
        return _minigameStarted;
    }
    
    public void ResetTimer()
    {
        _minigameStarted = false;
        _gameTimer = 0f;
    }
}
