using System;
using System.Collections.Generic;
using UnityEngine;

public class TraySpawner : GridObjects
{
    [SerializeField] private SpawningDirection spawnDirection;
    [SerializeField] private List<GameObject> trayPrefabsToSpawn = new();
    [SerializeField] public Transform visual;

    private List<TrayPlacementData> trayDataList = new(); // Keep original data for proper food mapping
    private int spawnIndex = 0;

    public void TrySpawnNext()
    {
        if (spawnIndex >= trayPrefabsToSpawn.Count || spawnIndex >= trayDataList.Count)
            return;

        TrayPlacementData trayData = trayDataList[spawnIndex];
        GameObject prefab = trayData.trayPrefab;

        if (!prefab.TryGetComponent(out Tray tray)) return;

        TrayShapeData shape = tray.GetShapeData();
        Vector2Int traySize = shape.Size;

        Vector3Int spawnerGridPos = PlacementSystem.Instance.grid.WorldToCell(transform.position);
        bool canSpawn = PlacementSystem.Instance.CanSpawnTrayAt(spawnerGridPos, traySize, spawnDirection);

        if (!canSpawn)
        {
            Debug.Log($"Cannot spawn {prefab.name} at {spawnerGridPos}");
            return;
        }

        SoundEventManager.OnAnyTraySpawner?.Invoke(this, EventArgs.Empty);
        Debug.Log($"Spawning {prefab.name} at {spawnerGridPos} toward {spawnDirection}");

        Vector3Int spawnPos = PlacementSystem.Instance.GetTraySpawnPosition(spawnerGridPos, traySize, spawnDirection);
        GameObject trayGO = Instantiate(prefab, PlacementSystem.Instance.grid.CellToWorld(spawnPos), Quaternion.identity, PlacementSystem.Instance.gridParent);
        Vector3Int gridPos = PlacementSystem.Instance.grid.WorldToCell(trayGO.transform.position);

        if (trayGO.TryGetComponent(out Tray newTray))
        {
            newTray.visual.SetSpawnDirection(ToVector3Int(spawnDirection));
            newTray.ApplyFoodSet(PlacementSystem.Instance.currentMap.ActiveFoodTheme);
            PlacementSystem.Instance.RegisterTray(newTray);
            newTray.currentGridPos = gridPos;
            newTray.originalGridPos = gridPos;
            newTray.lastValidGridPos = gridPos;


            //Use the exact TrayPlacementData that was planned
            var foodList = FindPlannedFood(trayData);
            if (foodList != null)
            {
                newTray.DelayedSpawnFromColorList(foodList);
            }
            else
            {
                Debug.LogWarning($"No food found for tray prefab {prefab.name} at spawnIndex {spawnIndex}.\nTrayData: prefab={trayData.trayPrefab?.name}, pos={trayData.position}");

                // Optional: print available keys to help debug
                foreach (var key in GameManager.Instance.PlannedTrayFoodMap.Keys)
                {
                    Debug.Log($"Available key: {key.trayPrefab?.name}, pos={key.position}");
                }
            }
        }
        spawnIndex++;
    }

    List<ItemColorType> FindPlannedFood(TrayPlacementData trayData)
    {
        foreach (var kvp in GameManager.Instance.PlannedTrayFoodMap)
        {
            if (kvp.Key.trayPrefab == trayData.trayPrefab &&
                kvp.Key.uniqueID == trayData.uniqueID)
            {
                return kvp.Value;
            }
        }

        return null;
    }

    public List<Tray> GetAllTraysToBeSpawned()
    {
        List<Tray> result = new();
        foreach (var prefab in trayPrefabsToSpawn)
        {
            if (prefab.TryGetComponent(out Tray tray))
            {
                result.Add(tray);
            }
        }
        return result;
    }

    public void SetDirection(SpawningDirection dir)
    {
        spawnDirection = dir;
    }

    public void SetTrayList(List<TrayPlacementData> dataList)
    {
        trayDataList = dataList;
        trayPrefabsToSpawn.Clear();

        foreach (var data in trayDataList)
        {
            trayPrefabsToSpawn.Add(data.trayPrefab);
        }
    }

    public Vector3Int ToVector3Int(SpawningDirection dir)
    {
        switch (dir)
        {
            case SpawningDirection.Left: return new Vector3Int(-1, 0, 0);
            case SpawningDirection.Right: return new Vector3Int(1, 0, 0);
            case SpawningDirection.Up: return new Vector3Int(0, 0, 1);
            case SpawningDirection.Down: return new Vector3Int(0, 0, -1);
            default: return Vector3Int.zero;
        }
    }
}

public enum SpawningDirection
{
    Up,
    Down,
    Left,
    Right
}
