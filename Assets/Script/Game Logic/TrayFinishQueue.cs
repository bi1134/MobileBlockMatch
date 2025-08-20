using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public static class TrayFinishQueue 
{
    private static readonly Queue<(Tray tray, Action action)> queue = new();
    private static bool isProcessing = false;

    public static IEnumerator DelayedCompletionCheck(Tray tray)
    {
        yield return new WaitForEndOfFrame();

        if (tray.isSwapping || tray.isFinishing) yield break;

        int total = tray.activeFoods.Count;
        int matching = tray.activeFoods.Count(f => f.Color == tray.GetShapeData().trayColor);

        if (total > 0 && total == matching)
        {
            tray.isFinishing = true;
            tray.enabled = false;

            yield return Helpers.GetWaitForSecond(0.2f);
            tray.KillAllTweens();
            tray.placementSystem.AllPlannedTrays.Remove(tray);
            tray.UnregisterSelf();

            Enqueue(tray, () =>
            {
                tray.visual.DestroyGoesUp(() => tray.gameObject.SetActive(false));
                GameEventManager.OnTrayFinished?.Invoke(tray, EventArgs.Empty);
            });
        }
    }

    private static void Enqueue(Tray tray, Action action)
    {
        queue.Enqueue((tray, action));
        if (!isProcessing)
            GameManager.Instance.StartCoroutine(ProcessQueue());
    }

    private static IEnumerator ProcessQueue()
    {
        isProcessing = true;
        while (queue.Count > 0)
        {
            var (tray, action) = queue.Dequeue();
            action?.Invoke();
            yield return new WaitForSeconds(0.3f); // spacing between trays finishing
        }
        isProcessing = false;
    }
}
