using System.Collections;
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

        Debug.Log($"Spawning {prefab.name} at {spawnerGridPos} toward {spawnDirection}");

        Vector3Int spawnPos = PlacementSystem.Instance.GetTraySpawnPosition(spawnerGridPos, traySize, spawnDirection);
        GameObject trayGO = Instantiate(prefab, PlacementSystem.Instance.grid.CellToWorld(spawnPos), Quaternion.identity, PlacementSystem.Instance.gridParent);
        Vector3Int gridPos = PlacementSystem.Instance.grid.WorldToCell(trayGO.transform.position);

        if (trayGO.TryGetComponent(out Tray newTray))
        {
            newTray.ApplyFoodSet(PlacementSystem.Instance.currentMap.ActiveFoodTheme);
            newTray.OnTrayFinished += GameManager.Instance.HandleTrayFinished;
            PlacementSystem.Instance.RegisterTray(newTray);
            tray.currentGridPos = gridPos;
            tray.originalGridPos = gridPos;
            tray.lastValidGridPos = gridPos;
            //Use the exact TrayPlacementData that was planned
            var foodList = FindPlannedFood(trayData);
            if (foodList != null)
            {
                Debug.Log($"Found food for tray {prefab.name} with {foodList.Count} items.");
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

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (trayPrefabsToSpawn == null || spawnIndex >= trayPrefabsToSpawn.Count)
            return;

        GameObject nextPrefab = trayPrefabsToSpawn[spawnIndex];
        if (!nextPrefab.TryGetComponent(out Tray tray)) return;

        TrayShapeData shape = tray.GetShapeData();
        Vector2Int traySize = shape.Size * 2;

        Vector3Int spawnerGridPos = PlacementSystem.Instance.grid.WorldToCell(transform.position);
        var cells = PlacementSystem.Instance.GetCellsToCheck(spawnerGridPos, traySize, spawnDirection);

        bool canSpawn = true;
        foreach (var cell in cells)
        {
            if (!PlacementSystem.Instance.IsCellAvailable(cell))
            {
                canSpawn = false;
                break;
            }
        }

        Gizmos.color = canSpawn ? Color.green : Color.red;

        foreach (var cell in cells)
        {
            Vector3 worldPos = PlacementSystem.Instance.grid.CellToWorld(cell) + new Vector3(0.5f, 0.1f, 0.5f);
            Gizmos.DrawCube(worldPos, new Vector3(0.9f, 0.1f, 0.9f));
        }
    }
#endif
}

public enum SpawningDirection
{
    Up,
    Down,
    Left,
    Right
}
