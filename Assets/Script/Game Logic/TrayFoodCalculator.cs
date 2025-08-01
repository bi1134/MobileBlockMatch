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
        const int maxRetries = 10;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var plan = TryCreatePlanOnce(trayDataList);
            if (plan != null)
            {
                Debug.Log($"Plan succeeded on attempt {attempt}");
                return plan;
            }
        }

        Debug.LogError("Failed to generate a valid tray food plan after retries.");
        return null;
    }

    private static TraySpawnPlan TryCreatePlanOnce(List<TrayPlacementData> trayDataList)
    {
        var plan = new TraySpawnPlan();
        var trayCapacities = new Dictionary<TrayPlacementData, int>();
        var totalColorCounts = new Dictionary<ItemColorType, int>();

        foreach (var data in trayDataList)
        {
            if (!data.trayPrefab || !data.trayPrefab.TryGetComponent(out Tray tray)) continue;
            int cap = CountValidSpawnPoints(tray.GetShapeData());
            trayCapacities[data] = cap;

            var color = tray.GetShapeData().trayColor;
            totalColorCounts[color] = totalColorCounts.GetValueOrDefault(color) + cap;
        }

        List<ItemColorType> foodPool = totalColorCounts
            .SelectMany(kvp => Enumerable.Repeat(kvp.Key, kvp.Value))
            .OrderBy(_ => Random.value)
            .ToList();

        var usedCount = new Dictionary<ItemColorType, int>();

        foreach (var data in trayDataList)
        {
            if (!data.trayPrefab.TryGetComponent(out Tray tray)) continue;

            var shape = tray.GetShapeData();
            int cap = trayCapacities[data];
            var trayColor = shape.trayColor;

            var foodList = new List<ItemColorType>();
            var uniqueColors = new HashSet<ItemColorType>();

            int maxMatches = Mathf.FloorToInt(cap * 0.75f);
            int safety = 0;

            while (foodList.Count < cap && safety++ < 500)
            {
                int matchCount = foodList.Count(f => f == trayColor);

                var candidates = foodPool
                    .Where(c =>
                        usedCount.GetValueOrDefault(c) < totalColorCounts[c] &&
                        (uniqueColors.Contains(c) || uniqueColors.Count < 3) &&
                        !(c == trayColor && matchCount >= maxMatches))
                    .ToList();

                if (candidates.Count == 0)
                    return null; //unable to fulfill this tray — let caller retry

                var chosen = candidates[Random.Range(0, candidates.Count)];
                foodList.Add(chosen);

                usedCount[chosen] = usedCount.GetValueOrDefault(chosen) + 1;
                uniqueColors.Add(chosen);
                foodPool.Remove(chosen); // removes one instance
            }

            plan.NormalTrayFoodByData[data] = foodList;
        }

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