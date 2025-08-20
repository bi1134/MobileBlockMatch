using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TraySpawnPlan
{
    public Dictionary<TrayPlacementData, List<ItemColorType>> NormalTrayFoodByData = new();
    public Dictionary<Tray, List<ItemColorType>> BlockedTrayFood = new();
}

public struct BlockedTraySpec
{
    public TrayPlacementData data;
    public int requirement;
}

public static class TrayFoodCalculator
{
    public static TraySpawnPlan CreateFoodPlan(
        List<TrayPlacementData> allTrays,              // all planned trays (normal + dir + blocked + spawner)
        List<BlockedTraySpec> blockedSpecs             // blocked trays with requirement
        )
    {
        // startSet = everything that is NOT blocked (i.e., available at start, including spawner trays)
        var blockedSet = new HashSet<TrayPlacementData>(blockedSpecs.Select(b => b.data));
        var startSet = allTrays.Where(d => !blockedSet.Contains(d)).ToList();

        int minReq = blockedSpecs.Count == 0 ? 0 : blockedSpecs.Min(b => b.requirement);

        const int maxRetries = 30;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var plan = TryCreatePlanOnce(allTrays);
            if (plan == null) continue;

            // If first locked group needs ALL start-set finished -> enforce full solvable start-set
            bool ok = true;
            if (minReq >= startSet.Count)
            {
                ok = IsSubsetFullySolvable(plan.NormalTrayFoodByData, startSet);
            }

            if (ok)
            {
                // good plan
                Debug.Log($"Plan w/ blocked constraints succeeded on attempt {attempt}");
                return plan;
            }
        }

        Debug.LogError("Failed to generate a valid tray food plan (blocked constraints).");
        return null;
    }

    // --- Feasibility check: all start-set trays can self-finish (ignoring movement) ---
    private static bool IsSubsetFullySolvable(
        Dictionary<TrayPlacementData, List<ItemColorType>> assign,
        List<TrayPlacementData> subset)
    {
        // Need_X: how many of color X are still needed by X-colored trays.
        // Surplus_X: how many X we have sitting in non-X trays.
        var need = new Dictionary<ItemColorType, int>();
        var surplus = new Dictionary<ItemColorType, int>();

        foreach (var data in subset)
        {
            if (!data.trayPrefab || !data.trayPrefab.TryGetComponent(out Tray tray)) continue;
            var shape = tray.GetShapeData();
            if (shape == null) continue;

            int cap = CountValidSpawnPoints(shape);
            var homeColor = shape.trayColor;

            if (!assign.TryGetValue(data, out var items)) continue;

            int own = items.Count(c => c == homeColor);
            int needOwn = Mathf.Max(0, cap - own);
            need[homeColor] = need.GetValueOrDefault(homeColor) + needOwn;

            foreach (var c in items)
                if (!Equals(c, homeColor))
                    surplus[c] = surplus.GetValueOrDefault(c) + 1;
        }

        foreach (var kv in need)
        {
            var color = kv.Key;
            int needC = kv.Value;
            int supC = surplus.GetValueOrDefault(color);
            if (supC < needC) return false;
        }

        return true;
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