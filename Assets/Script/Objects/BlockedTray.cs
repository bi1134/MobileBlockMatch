using System;
using UnityEngine;

public class BlockedTray : Tray
{
    [SerializeField] private float requiredCompletedTrays = 0;
    [SerializeField] private bool unlocked = false;
    public override bool IsUnlocked() => unlocked;

    [Header("Blocked Visual")]
    [SerializeField] private GameObject normalTrayMesh;
    [SerializeField] private GameObject blockedTrayMesh;

    protected override void Start()
    {
        base.Start();
        UpdateVisual();

        GameEventManager.OnTrayFinished += TryUnlock;
    }

    public void SetUnlockRequirement(int requirement)
    {
        requiredCompletedTrays = requirement;
    }

    private void TryUnlock(object sender, EventArgs e)
    {
        if (unlocked) return;

        if (GameManager.Instance.FinishedTrayCount >= requiredCompletedTrays)
        {
            unlocked = true;
            Debug.Log($"{gameObject.name} is now unlocked!");
            UpdateVisual();

            TrySpawnRemainingFood();
            CheckAndStartSwap(null);
        }
    }


    private void UpdateVisual()
    {
        if (visual == null) return;

        // Show/hide based on unlock status
        if (normalTrayMesh) normalTrayMesh.SetActive(unlocked);
        if (blockedTrayMesh) blockedTrayMesh.SetActive(!unlocked);

        // Also hide grid (so food is invisible)
        var grid = visual.GetComponentInChildren<Grid>(includeInactive: true);
        if (grid != null)
            grid.gameObject.SetActive(unlocked);
    }


    private void TrySpawnRemainingFood()
    {
        if (GameManager.Instance.BlockedTrayFoodMap.TryGetValue(this, out var foodList))
        {
            SpawnFromColorList(foodList);
        }
    }

    private void OnDestroy()
    {
        GameEventManager.OnTrayFinished -= TryUnlock;
    }
}


