using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TraySpawnPlan
{
    public Dictionary<TrayPlacementData, List<ItemColorType>> NormalTrayFoodByData = new();
    public Dictionary<Tray, List<ItemColorType>> BlockedTrayFood = new();
}

public static class TrayFoodCalculator
{
    public static TraySpawnPlan CreateFoodPlan(List<TrayPlacementData> trayDataList)
    {
        var plan = new TraySpawnPlan();

        var trayCapacities = new Dictionary<TrayPlacementData, int>();
        var totalColorCounts = new Dictionary<ItemColorType, int>();

        // Step 1: Calculate individual tray capacities and total color demand
        foreach (var data in trayDataList)
        {
            if (data.trayPrefab == null) continue;
            if (!data.trayPrefab.TryGetComponent(out Tray tray)) continue;

            var shape = tray.GetShapeData();
            int cap = CountValidSpawnPoints(shape);
            trayCapacities[data] = cap;

            var color = shape.trayColor;
            if (!totalColorCounts.ContainsKey(color))
                totalColorCounts[color] = 0;

            totalColorCounts[color] += cap;
        }

        // Step 2: Build full food pool
        List<ItemColorType> foodPool = new();
        foreach (var kvp in totalColorCounts)
        {
            for (int i = 0; i < kvp.Value; i++)
                foodPool.Add(kvp.Key);
        }

        // Step 3: Shuffle once
        foodPool = foodPool.OrderBy(_ => Random.value).ToList();

        // Step 4: Fill trays from shuffled pool
        var usedCount = new Dictionary<ItemColorType, int>();

        foreach (var data in trayDataList)
        {
            if (!data.trayPrefab.TryGetComponent(out Tray tray)) continue;

            var shape = tray.GetShapeData();
            int cap = trayCapacities[data];
            var trayColor = shape.trayColor;

            var foodList = new List<ItemColorType>();
            var uniqueColors = new HashSet<ItemColorType>();

            int safety = 0;
            const int maxSafety = 500;

            int maxMatches = Mathf.FloorToInt(cap * 0.75f); // Enforce no more than 3 matching colors

            // Fallback fill
            while (foodList.Count < cap && foodPool.Count > 0)
            {
                int currentMatchCount = foodList.Count(f => f == trayColor);

                // Try to find a fallback that doesn't break the 3-match rule
                int fallbackIndex = foodPool.FindIndex(c => !(c == trayColor && currentMatchCount >= maxMatches));

                if (fallbackIndex == -1)
                {
                    Debug.LogWarning($"Could not find suitable fallback for tray {trayColor} (pos: {data.position})");
                    break; // No valid fallback left
                }

                var fallback = foodPool[fallbackIndex];
                foodPool.RemoveAt(fallbackIndex);
                foodList.Add(fallback);
                usedCount[fallback] = usedCount.GetValueOrDefault(fallback) + 1;
            }

            while (foodList.Count < cap && safety++ < maxSafety)
            {
                int currentMatchCount = foodList.Count(f => f == trayColor);

                var candidates = foodPool
                    .Where(c =>
                        usedCount.GetValueOrDefault(c) < totalColorCounts[c] &&
                        (uniqueColors.Contains(c) || uniqueColors.Count < 3) &&
                        !(c == trayColor && currentMatchCount >= maxMatches)
                    ).ToList();

                if (candidates.Count == 0) break;

                var chosen = candidates[Random.Range(0, candidates.Count)];
                foodList.Add(chosen);

                usedCount[chosen] = usedCount.GetValueOrDefault(chosen) + 1;
                uniqueColors.Add(chosen);

                // Remove first instance of chosen from pool
                int idx = foodPool.FindIndex(c => c == chosen);
                if (idx >= 0)
                    foodPool.RemoveAt(idx);
            }

           

            plan.NormalTrayFoodByData[data] = foodList;
        }

        // Optional debug logs
        Debug.Log("<color=cyan>--- Food Pool Plan ---</color>");
        foreach (var kvp in totalColorCounts)
            Debug.Log($"Need {kvp.Value} of {kvp.Key}");

        Debug.Log("<color=yellow>--- Food Used ---</color>");
        foreach (var kvp in usedCount)
            Debug.Log($"Used {kvp.Value} of {kvp.Key}");

        return plan;
    }

    private static int CountValidSpawnPoints(TrayShapeData shape)
    {
        int count = 0;
        Vector2Int size = shape.Size * 2;

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int offset = new(x / 2, z / 2);
                if (!shape.ExcludedOffsets.Contains(offset))
                    count++;
            }
        }

        return count;
    }
}