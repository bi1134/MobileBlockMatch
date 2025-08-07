using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public event EventHandler OnStateChanged;
    public event EventHandler<OnProgressChangedEventArgs> OnProgressChanged;
    public class OnProgressChangedEventArgs : EventArgs
    {
        public float progressNormalized;
        public int moveCount;
    }

    public static GameManager Instance { get; private set; }
    public Dictionary<Tray, List<ItemColorType>> BlockedTrayFoodMap { get; set; } = new();
    public Dictionary<TrayPlacementData, List<ItemColorType>> PlannedTrayFoodMap { get; set; } = new();

    public int totalTrayCount => PlacementSystem.Instance.AllPlannedTrays.Count; //for better reading
    private int finishedTrayCount;

    public int currentMoveCount = 0;
    private int maxMoveCount = 10; // default value, can be set from map config

    private float waitingToStartTimer = 1f;

    public event EventHandler OnTrayFinished;

    public int FinishedTrayCount => finishedTrayCount;

    [field: SerializeField]public GameState CurrentState { get; private set; }
    private float winLoseCheckDelay = 0.5f;
    [SerializeField] private float winLoseCheckTimer = 0f;
    [SerializeField]private bool shouldCheckWinLose = false;

    private readonly List<DirectionalTray> activeDirectionalTrays = new();

    private void Awake()
    {
        Instance = this;
        SetGameState(GameState.WaitingToStart);
    }

    private void Update()
    {
        switch(CurrentState)
        {
            case GameState.WaitingToStart:
                waitingToStartTimer -= Time.deltaTime;
                if (waitingToStartTimer <= 0f)
                {
                    SetGameState(GameState.Starting);
                }
                break;
            case GameState.Playing:
                HandleWinLoseCheckDelay();
                break;
        }
    }

    public void HandleTrayFinished(object sender, EventArgs e)
    {
        finishedTrayCount++;
        OnTrayFinished?.Invoke(this, EventArgs.Empty);
        print(totalTrayCount);
        TryTriggerAllSpawners();
        // Check if win or lose
        shouldCheckWinLose = true;
        winLoseCheckTimer = winLoseCheckDelay;
    }

    private void HandleWinLoseCheckDelay()
    {
        if (TrayController.Instance.track > 0)
        {
            shouldCheckWinLose = true;
            winLoseCheckTimer = winLoseCheckDelay;
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
        if (totalTrayCount <= 0)
        {
            Debug.Log("All trays finished! WIN");
            SetGameState(GameState.Win);
            return;
        }

        if (currentMoveCount <= 0)
        {
            Debug.Log("Out of moves! LOSE");
            SetGameState(GameState.GameOver);
            return;
        }

        CheckLoseCondition(PlacementSystem.Instance.AllPlannedTrays);
    }

    public void CheckLoseCondition(List<Tray> currentActiveTrays)
    {
        activeDirectionalTrays.Clear();

        foreach (var tray in currentActiveTrays)
        {
            if (tray is DirectionalTray dir && dir.IsUnlocked())
                activeDirectionalTrays.Add(dir);
        }

        if (currentActiveTrays.Count > activeDirectionalTrays.Count)
            return;

        foreach (var tray in activeDirectionalTrays)
        {
            var trayColor = tray.GetShapeData().trayColor;
            var trayAxis = tray.allowedDirection;
            var trayInner = tray.GetInnerRayCells();
            var trayOuter = tray.GetOuterRayCells();

            foreach (var other in activeDirectionalTrays)
            {
                if (tray == other) continue;
                if (!other.IsUnlocked()) continue;

                var otherColor = other.GetShapeData().trayColor;
                if (trayColor == otherColor) continue;

                var otherOccupied = other.GetOccupiedCells(other.GetGridPosition());

                if (trayAxis == other.allowedDirection)
                {
                    if (trayOuter.Overlaps(otherOccupied))
                        return;
                }
                else
                {
                    if (trayInner.Overlaps(otherOccupied))
                        return;

                    if (trayInner.Overlaps(other.GetInnerRayCells()))
                        return;
                }
            }
        }

        Debug.Log("Lose: No tray can reach another tray of a different color.");
        SetGameState(GameState.GameOver);
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
        OnProgressChanged?.Invoke(this, new OnProgressChangedEventArgs
        {
            progressNormalized = currentMoveCount / (float)maxMoveCount,
            moveCount = currentMoveCount
        });
        TryTriggerAllSpawners();
        shouldCheckWinLose = true;
        winLoseCheckTimer = winLoseCheckDelay;
        Debug.Log($"move Used: {currentMoveCount} / {maxMoveCount}");
        Debug.Log($"remaining trays: {totalTrayCount}");
    }

    public void SetMoveLimit(int maxMoveCountData)
    {
        maxMoveCount = maxMoveCountData;
        currentMoveCount = maxMoveCount;
        OnProgressChanged?.Invoke(this, new OnProgressChangedEventArgs
        {
            progressNormalized = currentMoveCount / (float)maxMoveCount,
            moveCount = currentMoveCount
        });
    }

    public void SetGameState(GameState gameState)
    {
        if (CurrentState == GameState.Win && gameState == GameState.GameOver)
        {
            Debug.Log("Ignoring GameOver because game already won.");
            return;
        }

        CurrentState = gameState;
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ResetState()
    {
        currentMoveCount = maxMoveCount;
        OnProgressChanged?.Invoke(this, new OnProgressChangedEventArgs
        {
            progressNormalized = currentMoveCount / (float)maxMoveCount,
            moveCount = currentMoveCount
        });
        finishedTrayCount = 0;
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
}

public enum GameState
{
    WaitingToStart,
    Starting,
    Playing,
    GameOver,
    Win
}