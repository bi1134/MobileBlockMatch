using System;
using System.Collections.Generic;
using System.Collections;
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

    public GameState CurrentState { get; private set; }

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
        }
    }

    public void HandleTrayFinished(object sender, EventArgs e)
    {
        finishedTrayCount++;
        OnTrayFinished?.Invoke(this, EventArgs.Empty);
        print(totalTrayCount);
        // Check if win or lose
        StartCoroutine(CheckWinLoseCondition());
    }

    private IEnumerator CheckWinLoseCondition()
    {
        yield return new WaitForSeconds(1.5f); // Wait until everything else finishes

        if (totalTrayCount <= 0)
        {
            Debug.Log("All trays finished! WIN");
            SetGameState(GameState.Win);
            yield break;
        }

        if (currentMoveCount <= 0)
        {
            Debug.Log("Out of moves! LOSE");
            SetGameState(GameState.GameOver);
            yield break;
        }

        CheckLoseCondition(PlacementSystem.Instance.AllPlannedTrays);
    }

    public void CheckLoseCondition(List<Tray> allTrays)
    {
        var remaining = allTrays
            .Where(t => t != null && t.IsUnlocked())
            .OfType<DirectionalTray>()
            .ToList();

        if (remaining.Count < 2) return;

        foreach (var tray in remaining)
        {
            var trayPos = tray.GetGridPosition();
            var trayColor = tray.GetShapeData().trayColor;
            var dir = tray.allowedDirection;

            foreach (var other in remaining)
            {
                if (tray == other) continue;
                if (other.GetShapeData().trayColor == trayColor) continue; 

                var otherPos = other.GetGridPosition();

                bool aligned = dir switch
                {
                    DirectionalTray.MovementAxis.Horizontal => trayPos.z == otherPos.z,
                    DirectionalTray.MovementAxis.Vertical => trayPos.x == otherPos.x,
                    _ => false
                };

                bool near = dir switch
                {
                    DirectionalTray.MovementAxis.Horizontal =>
                        Mathf.Abs(trayPos.z - otherPos.z) <= 1 && Mathf.Abs(trayPos.x - otherPos.x) <= 5,
                    DirectionalTray.MovementAxis.Vertical =>
                        Mathf.Abs(trayPos.x - otherPos.x) <= 1 && Mathf.Abs(trayPos.z - otherPos.z) <= 5,
                    _ => false
                };

                if (aligned || near)
                    return; 
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
        StartCoroutine(CheckWinLoseCondition());
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