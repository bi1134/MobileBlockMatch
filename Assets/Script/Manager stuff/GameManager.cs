using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public Dictionary<Tray, List<ItemColorType>> BlockedTrayFoodMap { get; set; } = new();
    public Dictionary<TrayPlacementData, List<ItemColorType>> PlannedTrayFoodMap { get; set; } = new();

    public int totalTrayCount => PlacementSystem.Instance.AllPlannedTrays.Count; //for better reading
    private int finishedTrayCount;

    public int currentMoveCount = 0;
    private int maxMoveCount = 10; // default value, can be set from map config

    private float waitingToStartTimer = 1f;

    public int FinishedTrayCount => finishedTrayCount;

    public GameState CurrentState { get; private set; }
    private GameState previousState;
    private float winLoseCheckDelayTime = 1f;
    [SerializeField] private float winLoseCheckTimer = 0f;
    [SerializeField]private bool shouldCheckWinLose = false;

    private readonly List<DirectionalTray> activeDirectionalTrays = new();


    // Logic Checkers
    private ComboChecker comboChecker;
    private WinLoseChecker winLoseChecker;

    public int comboCheck;

    //fps
    private int lastFrameIndex;
    private float[] frameDeltaTimeArray;

    [SerializeField] private TextMeshProUGUI fpsString;

    private void Awake()
    {
        Instance = this;
        GameEventManager.OnTrayFinished += HandleTrayFinished;
        winLoseChecker = new WinLoseChecker();
        comboChecker = new ComboChecker(2f); // 2 seconds combo time limit

        Application.targetFrameRate = 60; // Set target frame rate to 60 FPS

        frameDeltaTimeArray = new float[50]; // Array to store the last 50 frame delta times
    }

    private void Update()
    {
        frameDeltaTimeArray[lastFrameIndex] = Time.deltaTime;
        lastFrameIndex = (lastFrameIndex + 1) % frameDeltaTimeArray.Length;

        fpsString.text = Mathf.RoundToInt(CalculateFPS()).ToString();

        comboCheck = comboChecker.ComboCount;
        switch (CurrentState)
        {
            case GameState.Starting:
                waitingToStartTimer -= Time.deltaTime;
                if (waitingToStartTimer <= 0f)
                {
                    SetGameState(GameState.Playing);
                }
                break;
            case GameState.Playing:
                comboChecker.Update(Time.deltaTime);
                HandleWinLoseCheckDelay();
                break;
        }
    }

    private float CalculateFPS()
    {
        float total = 0f;
        foreach (float deltaTime in frameDeltaTimeArray)
        {
            total += deltaTime;
        }

        return frameDeltaTimeArray.Length / total;
    }

    public void HandleTrayFinished(object sender, EventArgs e)
    {
        finishedTrayCount++;
        print(totalTrayCount);
        TryTriggerAllSpawners();
        // Check if win or lose
        shouldCheckWinLose = true;
        winLoseCheckTimer = winLoseCheckDelayTime;
    }

    private void HandleWinLoseCheckDelay()
    {
        if (TrayController.Instance.track > 0)
        {
            shouldCheckWinLose = true;
            winLoseCheckTimer = winLoseCheckDelayTime;
            return;
        }

        if (shouldCheckWinLose)
        {
            winLoseCheckTimer -= Time.deltaTime;

            if (winLoseCheckTimer <= 0f)
            {
                winLoseCheckTimer = 0;
                shouldCheckWinLose = false;
                CheckWinLoseCondition();
            }
        }
    }

    private void CheckWinLoseCondition()
    {
        if (winLoseChecker.IsWin(totalTrayCount))
        {
            SetGameState(GameState.Win);
            return;
        }

        if (winLoseChecker.IsLose(currentMoveCount, PlacementSystem.Instance.AllPlannedTrays))
        {
            SetGameState(GameState.GameOver);
        }
    }

    private void TryTriggerAllSpawners()
    {
        foreach (var spawner in PlacementSystem.Instance.TraySpawners)
        {
            spawner.TrySpawnNext();
        }
    }

    public void RegisterMove()
    {
        currentMoveCount--;
        if (currentMoveCount < 0)
        {
            currentMoveCount = 0;
        }
        GameEventManager.VisualProgressChanged(this, currentMoveCount / (float)maxMoveCount, currentMoveCount);
        TryTriggerAllSpawners();
        shouldCheckWinLose = true;
        winLoseCheckTimer = winLoseCheckDelayTime;
        
        Debug.Log($"move Used: {currentMoveCount} / {maxMoveCount}");
        Debug.Log($"remaining trays: {totalTrayCount}");
    }

    public void SetGameState(GameState newState)
    {
        if (newState == GameState.Pausing)
        {
            previousState = CurrentState; // store where we came from
        }

        CurrentState = newState;
        GameEventManager.OnGameStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResetState(int maxMoveCountData)
    {
        maxMoveCount = maxMoveCountData;
        currentMoveCount = maxMoveCount;
        GameEventManager.VisualProgressChanged(this, currentMoveCount / (float)maxMoveCount, currentMoveCount);
        finishedTrayCount = 0;
        SetGameState(GameState.WaitingToStart);
        BlockedTrayFoodMap.Clear();
    }

    public void ResumeFromPause()
    {
        // Go back to where we were
        SetGameState(previousState);
    }

    public bool IsGamePlaying()
    {
        return CurrentState == GameState.Playing;
    }

    public bool IsGameEnd()
    {
        return CurrentState == GameState.GameOver || CurrentState == GameState.Win;
    }

    public bool IsGameWin()
    {
        return CurrentState == GameState.Win;
    }

    private void OnDestroy()
    {
        GameEventManager.OnTrayFinished -= HandleTrayFinished;
        comboChecker?.UnassignSignal();
    }
}

public enum GameState
{
    WaitingToStart,
    Starting,
    Pausing,
    Playing,
    GameOver,
    Win
}