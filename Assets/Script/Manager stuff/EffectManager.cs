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


    private void SpawnEffect(GameObject prefab, Vector3 position)
    {
        if (prefab != null)
        {
            GameObject effect = Instantiate(prefab, position, Quaternion.identity);
        }
    }

    private void SpawnEffectOnTrayFinished(object sender, EventArgs e)
    {
        print("dsfjkfsdkjl");
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
                Vector3 testVEc = new Vector3(0, combo, 0);
                Vector3 spawnPos = tray.transform.position + tray.GetVisualOffSet() + Vector3.up + testVEc * 0.5f;
                SpawnEffect(prefab, spawnPos);
            }
        }
    }

    private void AssignSignal()
    {
        GameEventManager.OnTrayFinished += SpawnEffectOnTrayFinished;
        GameEventManager.OnComboChanged += SpawnComboEffect;
    }

    private void ResetSignal()
    {
        GameEventManager.OnTrayFinished -= SpawnEffectOnTrayFinished;
        GameEventManager.OnComboChanged -= SpawnComboEffect;
    }
}
