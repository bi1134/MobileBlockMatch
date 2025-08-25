using System;
using UnityEngine;

public class EffectManager : MonoBehaviour
{
    [SerializeField] private EffectRefSO effectRefs;

    private void OnEnable()
    {
        AssignSignal();
    }

    private void OnDisable()
    {
        ResetSignal();
    }


    private void SpawnEffectOnTrayFinished(object sender, EventArgs e)
    {
        if(sender is TrayVisual tray)
        {
            SpawnEffect(effectRefs.trayFinised[0], tray.transform.position);
        }
    }

    private void SpawnEffectOnBlockedTray(object sender, EventArgs e)
    {
        if (sender is BlockedTray tray)
        {
            SpawnEffect(effectRefs.boxDestroy[0], tray.transform.position + tray.GetVisualOffSet() + Vector3.up);
        }
    }


    private void SpawnComboEffect(object sender, int combo)
    {
        if (combo <= 1 || effectRefs?.combo == null || effectRefs.combo.Length == 0) return;

        if (sender is Tray tray)
        {
            int index = Mathf.Clamp(combo - 2, 0, effectRefs.combo.Length - 1);
            GameObject prefab = effectRefs.combo[index];
            if (prefab != null)
            {
                Vector3 yLayer = new Vector3(0, combo, 0);
                Vector3 spawnPos = tray.transform.position + tray.GetVisualOffSet() + Vector3.up + yLayer * 0.5f;
                SpawnEffect(prefab, spawnPos);
            }
        }
    }

    private void SpawnEffect(GameObject prefab, Vector3 position)
    {
        if (prefab != null)
        {
            GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        }
    }


    private void AssignSignal()
    {
        GameEventManager.OnTrayGoesOut += SpawnEffectOnTrayFinished;
        GameEventManager.OnBlockedTrayFinished += SpawnEffectOnBlockedTray;
        GameEventManager.OnComboChanged += SpawnComboEffect;
    }

    private void ResetSignal()
    {
        GameEventManager.OnTrayGoesOut -= SpawnEffectOnTrayFinished;
        GameEventManager.OnBlockedTrayFinished -= SpawnEffectOnBlockedTray;
        GameEventManager.OnComboChanged -= SpawnComboEffect;
    }
}
