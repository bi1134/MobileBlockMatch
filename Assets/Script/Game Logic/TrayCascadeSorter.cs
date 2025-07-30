using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class TrayCascadeSorter
{
    private static int activeSwapCount = 0;

    public static IEnumerator StartBreadthFirstSort(Tray startTray, Action onComplete)
    {
        var visited = TrayController.Instance.GetSharedVisitedSet(); // auto-clears
        visited.Add(startTray);

        var currentLayer = new List<Tray> { startTray };

        while (currentLayer.Count > 0)
        {
            var nextLayer = new List<Tray>();
            var swapTasks = new List<IEnumerator>();

            foreach (var tray in currentLayer)
            {
                var neighbors = tray.GetAdjacentTrays()
                    .Where(t =>
                        !visited.Contains(t) &&
                        t.IsUnlocked() &&
                        !t.isSwapping &&
                        !t.isFinishing &&
                        t.gameObject.activeInHierarchy)
                    .OrderByDescending(t => tray.GetPriorityScore(t))
                    .ToList();

                foreach (var neighbor in neighbors)
                {
                    visited.Add(neighbor);

                    IEnumerator task = RunSwap(tray, neighbor);
                    swapTasks.Add(task);

                    nextLayer.Add(neighbor);
                }
            }

            foreach (var task in swapTasks)
                TrayController.Instance.StartCoroutine(task);

            yield return WaitUntilSwapsComplete();

            currentLayer = nextLayer;
        }

        onComplete?.Invoke();
    }

    private static IEnumerator RunSwap(Tray a, Tray b)
    {
        activeSwapCount++;
        bool done = false;

        a.TrySortWithNeighbor(b, () =>
        {
            done = true;
            activeSwapCount--;
        });

        yield return new WaitUntil(() => done);
    }

    private static IEnumerator WaitUntilSwapsComplete()
    {
        float timeout = 2f;
        float elapsed = 0f;

        while (activeSwapCount > 0 && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}
