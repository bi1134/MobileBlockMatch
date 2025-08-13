using System;
using System.Collections.Generic;
using UnityEngine;

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

    [field: SerializeField]public GameState CurrentState { get; private set; }
    private float winLoseCheckDelayTime = 1f;
    [SerializeField] private float winLoseCheckTimer = 0f;
    [SerializeField]private bool shouldCheckWinLose = false;

    private readonly List<DirectionalTray> activeDirectionalTrays = new();


    // Logic Checkers
    private ComboChecker comboChecker;
    private WinLoseChecker winLoseChecker;

    public int comboCheck;

    private void Awake()
    {
        Instance = this;
        GameEventManager.OnTrayFinished += HandleTrayFinished;
        winLoseChecker = new WinLoseChecker();
        comboChecker = new ComboChecker(1f); // 0.5 seconds combo time limit
        
    }

    private void Update()
    {
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
            Debug.Log("All trays finished! WIN");
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

    public void SetGameState(GameState gameState)
    {
        if (CurrentState == GameState.Win && gameState == GameState.GameOver)
        {
            Debug.Log("Ignoring GameOver because game already won.");
            return;
        }

        CurrentState = gameState;
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
    Playing,
    GameOver,
    Win
}