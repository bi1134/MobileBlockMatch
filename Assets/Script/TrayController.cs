using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrayController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private LayerMask trayLayer;
    [SerializeField] private float dragThreshold = 0.5f;
    [SerializeField, Range(0f, 0.5f)]
    private float trayCollisionMargin = 0.15f;

    public static TrayController Instance { get; private set; }

    private readonly Queue<Tray> removeQueue = new();
    private bool isProcessingRemove = false;
    private readonly HashSet<Tray> activeRecursiveTrays = new();
    private readonly HashSet<Tray> sharedVisited = new();

    public bool IsTraySwaping => sharedVisited.Count > 0;
    public bool IsInputBlocked => activeRecursiveTrays.Count > 0;

    public int track;

    private Tray selectedTray;
    public bool isDragging;
    private Vector3 dragOffset;
    public float moveCheckCooldown = 0.1f;
    public float lastMoveCheckTime = 0f;

    [SerializeField] public static HashSet<Food> SwappingFoods = new();

    private void Awake() => Instance = this;

    private void Update()
    {
        track = sharedVisited.Count;
        if (IsInputBlocked || GameManager.Instance.IsGameEnd()) return;

        if (inputManager.WasTouchPressedThisFrame())
            TrySelectTray();

        if (inputManager.IsTouchPressed() && selectedTray != null)
            HandleTrayDrag();

        if (inputManager.WasTouchReleasedThisFrame() && isDragging)
            ReleaseTray();
    }

    private void TrySelectTray()
    {
        if (Time.time < lastMoveCheckTime + moveCheckCooldown)
            return;

        lastMoveCheckTime = Time.time;

        Vector3 clickWorldPos = inputManager.GetSelectedMapPosition();
        Ray ray = new Ray(clickWorldPos + Vector3.up * 5f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, trayLayer))
        {
            Tray tray = hit.collider.GetComponent<Tray>();
            if (tray != null)
            {
                selectedTray = tray;
                selectedTray.OnPickUp();
                selectedTray.visual.PlayPickUpAnimation();
                dragOffset = clickWorldPos - selectedTray.transform.position;
            }
        }
    }

    private void HandleTrayDrag()
    {
        if (!isDragging) isDragging = true;

        Vector3 mouseWorldPos = inputManager.GetSelectedMapPosition();
        Vector3 desired = mouseWorldPos - dragOffset;
        desired = ApplyDragAxisConstraint(selectedTray, desired);

        Vector3 current = selectedTray.transform.position;
        Vector3 moveDelta = desired - current;

        if (moveDelta == Vector3.zero)
            return;

        // Check X and Z separately
        bool blockX = IsDirectionBlocked(selectedTray, new Vector3(Mathf.Sign(moveDelta.x), 0, 0));
        bool blockZ = IsDirectionBlocked(selectedTray, new Vector3(0, 0, Mathf.Sign(moveDelta.z)));

        // Prioritize free axis
        if (blockX && !blockZ)
            desired.x = current.x; // Lock X, move Z
        else if (!blockX && blockZ)
            desired.z = current.z; // Lock Z, move X
        else if (blockX && blockZ)
            desired = current; // Both blocked, stop
                               // else: both free, move as-is (or prioritize dominant axis if you prefer)

        // Move gently toward resolved target
        Vector3 moveDir = desired - current;
        float moveSpeed = 15f;
        float moveDist = moveDir.magnitude;

        if (moveDist > 0.01f)
        {
            Vector3 step = moveDir.normalized * Mathf.Min(moveDist, moveSpeed * Time.deltaTime);
            Vector3 newPos = current + step;

            selectedTray.transform.position = newPos;
            selectedTray.currentGridPos = PlacementSystem.Instance.grid.WorldToCell(newPos);
        }
    }

    private bool IsDirectionBlocked(Tray tray, Vector3 direction)
    {
        Vector3 basePos = tray.transform.position + tray.GetVisualOffSet(); 
        Vector2 size = tray.GetShapeData().Size;

        float extent = (Mathf.Abs(direction.x) > 0) ? size.x * 0.25f : size.y * 0.25f;
        extent += 0.05f;

        Vector3 checkPos = basePos + direction * extent;
        Vector3Int cell = PlacementSystem.Instance.grid.WorldToCell(checkPos);

        if (!tray.CanMoveTo(cell)) return true;
        if (IsOverlappingOtherTrays(tray, checkPos)) return true;

        return false;
    }

    private Vector3 ApplyDragAxisConstraint(Tray tray, Vector3 desired)
    {
        if (tray is DirectionalTray directional)
        {
            Vector3 current = tray.transform.position;
            if (directional.allowedDirection == DirectionalTray.MovementAxis.Horizontal)
                desired.z = current.z; // lock Z
            else
                desired.x = current.x; // lock X
        }
        return desired;
    }

    private bool IsOverlappingOtherTrays(Tray movingTray, Vector3 testPosition)
    {
        Bounds bounds = GetTrayBoundsAt(movingTray, testPosition, trayCollisionMargin);

        foreach (Tray other in PlacementSystem.Instance.AllPlannedTrays)
        {
            if (other == movingTray) continue;
            Vector3 otherCenter = other.transform.position + other.GetVisualOffSet();
            Bounds otherBounds = GetTrayBoundsAt(other, otherCenter, trayCollisionMargin);
            if (bounds.Intersects(otherBounds))
                return true;
        }

        return false;
    }

    private Bounds GetTrayBoundsAt(Tray tray, Vector3 basePosition, float margin)
    {
        Vector3 center = basePosition + tray.GetVisualOffSet() + new Vector3(0f, 0.2f, 0f);
        Vector3 size = new Vector3(tray.GetShapeData().Size.x, 0.4f, tray.GetShapeData().Size.y) * (1f - margin);
        return new Bounds(center, size);
    }

    private void ReleaseTray()
    {
        selectedTray.OnDrop();
        lastMoveCheckTime = Time.time;
        selectedTray = null;
        isDragging = false;
    }

    public void RunDeferredCompletion(Tray a, Tray b, Action callback)
    {
        lastMoveCheckTime = Time.time;
        StartCoroutine(DeferredCompletionRoutine(a, b, callback));
    }

    private IEnumerator DeferredCompletionRoutine(Tray a, Tray b, Action callback)
    {
        yield return null;
        if (a != null) a.CheckIfCompleted();
        if (b != null) b.CheckIfCompleted();
        if (a != null) yield return a.CheckAndStartRecursiveSwap(null);
        if (b != null) yield return b.CheckAndStartRecursiveSwap(null);
        callback?.Invoke();
    }

    public void NotifyTrayRecursiveStarted(Tray tray)
    {
        if (activeRecursiveTrays.Add(tray))
        {
            lastMoveCheckTime = Time.time;
            moveCheckCooldown = 0.5f;
        }
    }

    public void NotifyTrayRecursiveEnded(Tray tray)
    {
        if (!removeQueue.Contains(tray) && activeRecursiveTrays.Contains(tray))
        {
            removeQueue.Enqueue(tray);
            if (!isProcessingRemove)
                StartCoroutine(ProcessRemoveQueue());
        }
    }

    private IEnumerator ProcessRemoveQueue()
    {
        isProcessingRemove = true;
        while (removeQueue.Count > 0)
        {
            var tray = removeQueue.Dequeue();
            activeRecursiveTrays.Remove(tray);
            lastMoveCheckTime = Time.time;
            yield return new WaitForSeconds(0.1f);
        }
        isProcessingRemove = false;
    }

    public HashSet<Tray> GetSharedVisitedSet()
    {
        sharedVisited.Clear();
        return sharedVisited;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || PlacementSystem.Instance == null) return;

        foreach (Tray tray in PlacementSystem.Instance.AllPlannedTrays)
        {
            Vector3 trayWorldPos = tray.transform.position;
            Vector3 offsetPos = trayWorldPos + tray.GetVisualOffSet();
            Vector2 size = tray.GetShapeData().Size;

            // Draw bounds in cyan
            Gizmos.color = (tray == selectedTray) ? Color.cyan : new Color(0, 1, 1, 0.25f); // brighter if selected
            Bounds bounds = GetTrayBoundsAt(tray, trayWorldPos, trayCollisionMargin);
            Gizmos.DrawWireCube(bounds.center, bounds.size);

            // Draw check positions (red spheres + yellow checked grid)
            Vector3[] directions = { Vector3.right, Vector3.left, Vector3.forward, Vector3.back };

            foreach (Vector3 dir in directions)
            {
                float extent = (Mathf.Abs(dir.x) > 0) ? size.x * 0.5f : size.y * 0.5f;
                extent += 0.05f;

                Vector3 checkPos = offsetPos + dir * extent;

                // Draw red sphere at check position
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(checkPos + Vector3.up * 0.1f, 0.075f);

                // Draw the grid cell being checked (yellow)
                Vector3Int cell = PlacementSystem.Instance.grid.WorldToCell(checkPos);
                Vector3 cellWorld = PlacementSystem.Instance.grid.CellToWorld(cell) + new Vector3(0.5f, 0f, 0.5f);
                Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
                Gizmos.DrawCube(cellWorld + Vector3.up * 0.05f, new Vector3(1f, 0.05f, 1f));
            }
        }
    }

}