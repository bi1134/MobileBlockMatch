using System.Collections.Generic;
using UnityEngine;

public static class TrayFoodSpawner
{
    public static void SpawnFromColorList(Tray tray, List<ItemColorType> colors)
    {
        if (tray.foodGrid == null || tray.foodTheme == null || tray.GetShapeData() == null) return;

        var positions = GetFoodGridPositions(tray);
        int spawned = 0;

        foreach (var color in colors)
        {
            if (spawned >= positions.Count) break;
            var data = tray.foodTheme.Find(fd => fd.color == color);
            if (data == null) continue;

            var cell = positions[spawned];
            SpawnFood(tray, cell, data);
            spawned++;
        }
    }

    private static void SpawnFood(Tray tray, Vector3Int localCell, FoodData data)
    {
        var pos = tray.foodGrid.CellToWorld(localCell);
        var foodGO = Object.Instantiate(data.prefab, pos, Quaternion.identity, tray.foodGrid.transform);

        if (foodGO.TryGetComponent(out Food food))
        {
            food.Initialize(data);
            tray.activeFoods.Add(food);
        }
        else
        {
            Debug.LogWarning($"Spawned object {data.prefab.name} has no Food component!");
        }
    }

    private static List<Vector3Int> GetFoodGridPositions(Tray tray)
    {
        var result = new List<Vector3Int>();
        var size = tray.GetShapeData().Size * 2;

        for (int x = 0; x < size.x; x++)
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int trayOffset = new(x / 2, z / 2);
                if (!tray.HasExcludedOffset(trayOffset))
                    result.Add(new Vector3Int(x, 0, z));
            }

        return result;
    }
}
