using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System.Collections;

public class Tray : GridObjects
{
    public Vector3 GetVisualOffSet() => visual.originalLocalOffset;
    public TrayShapeData GetShapeData() => shapeData;
    public virtual bool IsUnlocked() => true;

    #region References and variables
    [Header("References")]
    [SerializeField] public TrayVisual visual;
    [SerializeField] private TrayShapeData shapeData;
    [SerializeField] private Grid foodGrid;
    [SerializeField] private List<FoodData> foodTheme;

    public List<Food> activeFoods = new();
    public Vector3Int localPosition;

    public Vector3Int originalGridPos;
    public Vector3Int lastValidGridPos;
    private const int MaxBagSize = 20;
    private readonly HashSet<Vector3Int> occupiedCells = new();
    public bool isFinishing, isSwapping, isRunningRecursiveSort = false;

    private bool isBeingDestroyed = false;
    #endregion

    #region Startup
    private void Awake() => size = shapeData.Size;
    protected override void Start()
    {
        base.Start();
        visual.SnapToOffset();
    }

    public void DelayedSpawnFromColorList(List<ItemColorType> colors)
    {
        StartCoroutine(SpawnFoodAfterDrop(colors));
    }

    private IEnumerator SpawnFoodAfterDrop(List<ItemColorType> colors)
    {
        // Wait for visual to finish bouncing down
        yield return new WaitUntil(() => visual != null && visual.IsDropFinished);

        // Spawn food after visual is settled
        SpawnFromColorList(colors);

        yield return Helpers.GetWaitForSecond(0.2f);

        if (IsUnlocked()) 
        {
            yield return CheckAndStartSwap(() =>
            {
            });
        }
    }

    private void DestroyAfterVisual()
    {
        visual.ResetVisual();
        gameObject.SetActive(false);
    }

    #endregion

    #region Interaction Functions
    public void OnPickUp()
    {
        if (!IsUnlocked() || IsBusy()) return;
        SoundEventManager.OnAnyTrayPickupPlay?.Invoke(this, EventArgs.Empty);
        originalGridPos = currentGridPos;
        lastValidGridPos = currentGridPos;
        UnregisterSelf();
    }

    public void OnDrop()
    {
        if (!IsUnlocked() || IsBusy()) return;
        TrayController.Instance.ForcePickupDelay();
        Vector3 worldPos = transform.position;
        Vector3 cellSize = placementSystem.grid.cellSize;

        int x = Mathf.RoundToInt(worldPos.x / cellSize.x);
        int z = Mathf.RoundToInt(worldPos.z / cellSize.z);

        Vector3Int snappedCell = new Vector3Int(x, 0, z);

        if (CanMoveTo(snappedCell))
            currentGridPos = snappedCell;
        else
            currentGridPos = originalGridPos;

        Vector3 snappedWorld = PlacementSystem.Instance.grid.CellToWorld(currentGridPos);
        transform.position = snappedWorld;

        RegisterSelf();
        SoundEventManager.OnAnyTrayDropPlay?.Invoke(this, EventArgs.Empty);

        //start coroutine for drop animation + neighbor check
        if (currentGridPos != null)
            StartCoroutine(HandleDropAndCheck(currentGridPos));

        if (currentGridPos != originalGridPos)
        {
            GameManager.Instance.RegisterMove();
        }
    }

    public virtual bool CanMoveInDirection(Vector3Int direction)
    {
        if (direction == Vector3Int.zero)
            return false;

        Vector3Int target = currentGridPos + direction;

        // Diagonal blocking logic
        if (direction.x != 0 && direction.z != 0)
        {
            if (!CanMoveTo(currentGridPos + new Vector3Int(direction.x, 0, 0)) ||
                !CanMoveTo(currentGridPos + new Vector3Int(0, 0, direction.z)))
                return false;
        }

        return CanMoveTo(target);
    }

    public bool CanMoveTo(Vector3Int origin)
    {
        foreach (var cell in GetOccupiedCells(origin))
            if (!placementSystem.IsCellAvailable(cell, this))
                return false;

        return true;
    }

    private IEnumerator HandleDropAndCheck(Vector3Int snapCell)
    {
        Tween dropTween = visual.PlayDropAnimationSnap(placementSystem.grid.CellToWorld(snapCell));
        yield return dropTween.WaitForCompletion();

        yield return CheckAndStartSwap(null);
    }

    #endregion

    #region Food Item handling
    public void SpawnFromColorList(List<ItemColorType> colors)
    {
        if (foodGrid == null || foodTheme == null || shapeData == null) return;

        var positions = GetFoodGridPositions();
        int spawned = 0;

        foreach (var color in colors)
        {
            if (spawned >= positions.Count) break;

            var data = foodTheme.Find(fd => fd.color == color);
            if (data == null) continue;

            var cell = positions[spawned];
            SpawnFood(cell, data);
            spawned++;
        }
    }

    private void SpawnFood(Vector3Int localCell, FoodData data)
    {
        var pos = foodGrid.CellToWorld(localCell);
        var foodGO = Instantiate(data.prefab, pos, Quaternion.identity, foodGrid.transform);

        if (foodGO.TryGetComponent(out Food food))
        {
            food.Initialize(data);
            activeFoods.Add(food);
        }
        else
        {
            Debug.LogWarning($"Spawned object {data.prefab.name} has no Food component!");
        }
    }

    private List<Vector3Int> GetFoodGridPositions()
    {
        var result = new List<Vector3Int>();
        var size = shapeData.Size * 2;

        for (int x = 0; x < size.x; x++)
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int trayOffset = new(x / 2, z / 2);
                if (!HasExcludedOffset(trayOffset))
                    result.Add(new Vector3Int(x, 0, z));
            }

        return result;
    }

    public List<Food> GetAllFoods()
    {
        var result = new List<Food>();
        foreach (Transform child in foodGrid.transform)
            if (child.TryGetComponent(out Food food))
                result.Add(food);

        return result;
    }

    private void SyncActiveFoods()
    {
        activeFoods.Clear();
        activeFoods.AddRange(GetAllFoods());
    }
    #endregion

    #region Tray Handling
    public IEnumerator CheckAndStartSwap(Action onFinished)
    {
        if (!IsUnlocked() || isFinishing || isSwapping || isRunningRecursiveSort)
        {
            onFinished?.Invoke();
            yield break;
        }

        isRunningRecursiveSort = true;


        yield return TrayCascadeSorter.StartBreadthFirstSort(this, () =>
        {
            isRunningRecursiveSort = false;
            onFinished?.Invoke();
        });
    }

    public int GetPriorityScore(Tray neighbor)
    {
        int score = 0;

        var myColor = shapeData.trayColor;
        var theirColor = neighbor.shapeData.trayColor;

        var myWrong = activeFoods.Where(f => f.Color != myColor).ToList();
        var theirWrong = neighbor.activeFoods.Where(f => f.Color != theirColor).ToList();

        bool canMutuallySwap = myWrong.Any(f => f.Color == theirColor) &&
                               theirWrong.Any(f => f.Color == myColor);
        if (canMutuallySwap) score += 1000;

        if (myWrong.Any(f => f.Color == theirColor) || theirWrong.Any(f => f.Color == myColor))
            score += 500;

        int myMissing = activeFoods.Count(f => f.Color != myColor);
        int theirMissing = neighbor.activeFoods.Count(f => f.Color != theirColor);
        score += (20 - Mathf.Min(myMissing, theirMissing)) * 10;

        score += (activeFoods.Count + neighbor.activeFoods.Count);
        score += GetAdjacentTrayCount(neighbor) * 2;

        return score;
    }

    public List<Tray> GetAdjacentTrays()
    {
        Vector3Int[] directions = {
     Vector3Int.left, Vector3Int.right,
     Vector3Int.forward, Vector3Int.back
     };

        var neighbors = new List<Tray>();

        foreach (var cell in GetOccupiedCells(currentGridPos))
        {
            foreach (var dir in directions)
            {
                var neighborCell = cell + dir;
                if (placementSystem.GetGridObjectAt(neighborCell) is Tray neighbor &&
                    neighbor != this)
                {
                    neighbors.Add(neighbor);
                }
            }
        }

        return neighbors;
    }
    private int GetAdjacentTrayCount(Tray tray)
    {
        Vector3Int[] directions = {
     Vector3Int.left, Vector3Int.right,
     Vector3Int.forward, Vector3Int.back
     };

        var neighbors = new HashSet<Tray>();

        foreach (var cell in tray.GetOccupiedCells(tray.currentGridPos))
        {
            foreach (var dir in directions)
            {
                var neighborCell = cell + dir;
                if (placementSystem.GetGridObjectAt(neighborCell) is Tray other &&
                    other != tray)
                {
                    neighbors.Add(other);
                }
            }
        }

        return neighbors.Count;
    }

    public void TrySortWithNeighbor(Tray other, Action onComplete = null)
    {
        if (this.isSwapping || other.isSwapping || this.isFinishing || other.isFinishing || !this.gameObject.activeInHierarchy || !other.gameObject.activeInHierarchy || !this.IsUnlocked() || !other.IsUnlocked())
        {
            onComplete?.Invoke();
            return;
        }

        this.isSwapping = true;
        other.isSwapping = true;

        this.SyncActiveFoods();
        other.SyncActiveFoods();

        var myBag = GetWrongFoods(this).Take(MaxBagSize).ToList();
        var theirBag = GetWrongFoods(other).Take(MaxBagSize).ToList();

        var iWant = theirBag.Where(f => f != null && f.Color == shapeData.trayColor).ToList();
        var theyWant = myBag.Where(f => f != null && f.Color == other.shapeData.trayColor).ToList();

        int pairCount = Mathf.Min(iWant.Count, theyWant.Count);
        var swapSequence = DOTween.Sequence();

        float delayStep = 0.1f;
        int swapIndex = 0;

        //mutual swap
        for (int i = 0; i < pairCount; i++)
        {
            if (theyWant[i] != null && iWant[i] != null)
            {
                var swap = PerformSwap(theyWant[i], iWant[i], this, other);
                float startTime = swapIndex * delayStep;
                swapSequence.Insert(startTime, swap);
                swapIndex++;
            }
        }

        //extra give from this tray to other
        var myGivable = myBag.Except(theyWant).ToList();
        foreach (var desired in iWant.Skip(pairCount))
        {
            if (myGivable.Count == 0) break;
            var give = myGivable[0];
            myGivable.RemoveAt(0);
            if (give != null && desired != null)
            {
                var swap = PerformSwap(give, desired, this, other);
                float startTime = swapIndex * delayStep;
                swapSequence.Insert(startTime, swap);
                swapIndex++;
            }
        }

        //extra give from other to this stray
        var theirGivable = theirBag.Except(iWant).ToList();
        foreach (var desired in theyWant.Skip(pairCount))
        {
            if (theirGivable.Count == 0) break;
            var give = theirGivable[0];
            theirGivable.RemoveAt(0);
            if (give != null && desired != null)
            {
                var swap = PerformSwap(give, desired, other, this);
                float startTime = swapIndex * delayStep;
                swapSequence.Insert(startTime, swap);
                swapIndex++;
            }
        }

        if (swapSequence.Duration() <= 0)
        {
            this.isSwapping = false;
            other.isSwapping = false;
            onComplete?.Invoke();
            return;
        }

        float earlyCallbackTime = Mathf.Min(swapSequence.Duration() * 0.75f, 0.1f);
        DOVirtual.DelayedCall(earlyCallbackTime, () =>
        {
            if (!this || !other) return;
            if (this.isFinishing || other.isFinishing) return;

            // Early logic trigger so the next tray in the chain can start
            onComplete?.Invoke();
        });

        // Final cleanup still happens after animation
        swapSequence.OnComplete(() =>
        {
            this.isSwapping = false;
            other.isSwapping = false;

            if (!this || !other) return;

            this.SyncActiveFoods();
            other.SyncActiveFoods();

            TrayController.Instance.RunDeferredCompletion(this, other, null); // onComplete already called
        });
    }

    public void CheckIfCompleted()
    {
        if (!IsUnlocked() || foodGrid == null || visual == null || !gameObject.activeInHierarchy || isFinishing || isSwapping)
            return;

        // Delay completion until end of frame to avoid finishing during swap handling
        StartCoroutine(DelayedCompletionCheck());
    }

    private IEnumerator DelayedCompletionCheck()
    {
        yield return new WaitForEndOfFrame(); // ensure any perfor swap has completed

        if (isSwapping || isFinishing) yield break;

        int total = activeFoods.Count;
        int matching = activeFoods.Count(f => f != null && f.Color == shapeData.trayColor);

        if (total > 0 && total == matching)
        {
            isFinishing = true;
            isBeingDestroyed = true;
            this.enabled = false;
            // no tween kill until food positions have fully settled
            yield return new WaitForSeconds(0.2f);
            KillAllTweens();
            placementSystem.AllPlannedTrays.Remove(this);
            UnregisterSelf();
            visual.DestroyGoesUp(() => DestroyAfterVisual());
            GameEventManager.OnTrayFinished?.Invoke(this, EventArgs.Empty);
        }
    }

    private List<Food> GetWrongFoods(Tray tray)
    {
        var targetColor = tray.shapeData.trayColor;
        return tray.activeFoods.Where(f => f.Color != targetColor).ToList();
    }

    private Sequence PerformSwap(Food fromMe, Food fromThem, Tray me, Tray other)
    {
        if (me == null || other == null || me.isFinishing || other.isFinishing || fromMe == null || fromThem == null)
            return DOTween.Sequence();

        me.activeFoods.Remove(fromMe);
        other.activeFoods.Remove(fromThem);

        var t1 = fromMe.transform;
        var t2 = fromThem.transform;
        if (t1 == null || t2 == null) return DOTween.Sequence();

        // Start & target positions
        Vector3 t1Start = t1.position;
        Vector3 t2Start = t2.position;
        Vector3 t1Target = t2Start;
        Vector3 t2Target = t1Start;

        float totalDuration = 0.6f;
        float jumpHeight = 0.6f;

        var seq = DOTween.Sequence();

        // Animate both foods
        var t1Seq = AnimateFoodJump(t1, t1Start, t1Target, totalDuration, jumpHeight);
        var t2Seq = AnimateFoodJump(t2, t2Start, t2Target, totalDuration, jumpHeight);

        seq.Join(t1Seq).Join(t2Seq);

        float eventTime = totalDuration * 0.15f;
        seq.InsertCallback(eventTime, () =>
        {
            if (me == null || other == null || me.isFinishing || other.isFinishing)
                return;
            SoundEventManager.OnAnyFoodSwap?.Invoke(this, EventArgs.Empty);
        });

        // On complete, snap positions and re-parent
        seq.OnComplete(() =>
        {
            if (me == null || other == null || me.isFinishing || other.isFinishing)
                return;

            t1.SetParent(other.foodGrid.transform, true);
            t2.SetParent(me.foodGrid.transform, true);

            // snap to nearest cell in each tray's grid
            Vector3Int t1Cell = other.foodGrid.WorldToCell(t1Target);
            Vector3Int t2Cell = me.foodGrid.WorldToCell(t2Target);

            t1.position = other.foodGrid.CellToWorld(t1Cell);
            t2.position = me.foodGrid.CellToWorld(t2Cell);

            other.activeFoods.Add(fromMe);
            me.activeFoods.Add(fromThem);
        });

        return seq;
    }

    private Sequence AnimateFoodJump(Transform t, Vector3 start, Vector3 target, float totalDuration, float jumpHeight)
    {
        var seq = DOTween.Sequence();
        float dipTime = totalDuration * 0.15f;
        float jumpTime = totalDuration * 0.55f;
        float landTime = totalDuration * 0.3f;

        // Step 1: Dip back a little in Z
        Vector3 dipPos = start + new Vector3(0f, 0f, -0.2f);
        seq.Append(t.DOMove(dipPos, dipTime).SetEase(Ease.OutQuad));

        // Step 2 & 3: Parabolic arc (Y + Z bump)
        Vector3 mid = (start + target) / 2f;
        mid.y += jumpHeight;
        mid.z += jumpHeight * 2; // push forward on Z axis during jump

        seq.Append(DOTween.To(
            () => 0f,
            x =>
            {
                // quadratic bezier: start -> mid -> target
                Vector3 pos = Mathf.Pow(1 - x, 2) * start +
                              2 * (1 - x) * x * mid +
                              Mathf.Pow(x, 2) * target;
                t.position = pos;
            },
            1f, jumpTime
        ).SetEase(Ease.OutBack));


        return seq;
    }

    #endregion
    public bool HasExcludedOffset(Vector2Int offset) =>
       shapeData != null && shapeData.ExcludedOffsets.Contains(offset);

    public void ApplyFoodSet(List<FoodData> foodSet)
    {
        if (shapeData == null) return;

        var myColor = shapeData.trayColor;
        foodTheme = foodSet
            .Where(f => f != null && f.prefab != null)
            .Where(f => f.color == myColor || foodSet.Any(fd => fd.color == f.color))
            .ToList();
    }

    private void KillAllTweens()
    {
        if (isSwapping) return;
        foreach (var food in activeFoods)
        {
            if (food != null)
            {
                DOTween.Kill(food.transform, complete: true);
            }
        }

        if (visual != null)
        {
            DOTween.Kill(visual.transform, complete: true);
        }

        DOTween.Kill(this.transform, complete: true);
    }

    public bool IsBusy()
    {
        return isSwapping || isRunningRecursiveSort || isBeingDestroyed || isFinishing;
    }

    private void OnDrawGizmos()
    {
        if (PlacementSystem.Instance == null) return;

        Gizmos.color = Color.green;
        List<Vector3Int> occupied = GetOccupiedCells(currentGridPos);

        foreach (var cell in occupied)
        {
            Vector3 world = PlacementSystem.Instance.grid.CellToWorld(cell);
            Vector3 center = world + PlacementSystem.Instance.grid.cellSize / 2f;
            Vector3 size = PlacementSystem.Instance.grid.cellSize * 0.9f;

            Gizmos.DrawWireCube(center, size);
        }
    }
}