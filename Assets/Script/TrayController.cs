using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class TrayController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private LayerMask trayLayer;
    [SerializeField] private LayerMask trayCollision;
    [SerializeField, Range(0f, 0.5f)]
    private float trayCollisionMargin = 0.15f;

    [SerializeField] private float trayCollisionDirection = 0.15f;

    [SerializeField] private float moveSpeed = 20f;
    public static TrayController Instance { get; private set; }

    private readonly Queue<Tray> removeQueue = new();
    private bool isProcessingRemove = false;
    private readonly HashSet<Tray> activeRecursiveTrays = new();

    public bool IsInputBlocked => activeRecursiveTrays.Count > 0;

    public int track;

    private Tray selectedTray;
    public bool isDragging;
    private Vector3 dragOffset;
    public float moveCheckCooldown = 0.1f;
    public float lastMoveCheckTime = 0f;

    private Vector3 lastBoxCastOrigin;
    private Vector3 lastBoxCastHalfExtents;
    private Vector3 lastBoxCastDirection;
    private float lastBoxCastDistance;
    private RaycastHit[] trayCastHits = new RaycastHit[10];

    [SerializeField] public static HashSet<Food> SwappingFoods = new();

    private void Awake() => Instance = this;

    private void Update()
    {
        track = activeRecursiveTrays.Count;
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
            if (tray != null && tray.IsUnlocked())
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
        if (!isDragging)
            isDragging = true;

        Vector3 mouseWorldPos = inputManager.GetSelectedMapPosition();
        Vector3 desired = mouseWorldPos - dragOffset;
        desired = ApplyDragAxisConstraint(selectedTray, desired);

        Vector3 current = selectedTray.transform.position;
        Vector3 moveDelta = desired - current;

        if (moveDelta == Vector3.zero)
            return;

        Vector3 moveDir = moveDelta.normalized;
        float moveDist = moveDelta.magnitude;
        Vector3 step = moveDir * Mathf.Min(moveDist, moveSpeed * Time.deltaTime);

        // Directional checks
        bool blockX = IsDirectionBlocked(selectedTray, new Vector3(Mathf.Sign(moveDelta.x), 0, 0), step.magnitude);
        bool blockZ = IsDirectionBlocked(selectedTray, new Vector3(0, 0, Mathf.Sign(moveDelta.z)), step.magnitude);
        bool blockDiagonal = IsDirectionBlocked(selectedTray, new Vector3(Mathf.Sign(moveDelta.x), 0, Mathf.Sign(moveDelta.z)), step.magnitude);

        if (blockDiagonal)
        {
            if (Mathf.Abs(moveDelta.x) > Mathf.Abs(moveDelta.z) && !blockX)
                desired.z = current.z; // favor X
            else if (!blockZ)
                desired.x = current.x; // favor Z
            else if (!blockX)
                desired.z = current.z;
            else
                desired = current;
        }
        else
        {
            if (blockX && !blockZ)
                desired.x = current.x;
            else if (!blockX && blockZ)
                desired.z = current.z;
            else if (blockX && blockZ)
                desired = current;
        }

        moveDir = desired - current;
        moveDist = moveDir.magnitude;

        if (moveDist > 0.01f)
        {
            step = moveDir.normalized * Mathf.Min(moveDist, moveSpeed * Time.deltaTime);
            Vector3 newPos = current + step;

            selectedTray.transform.position = newPos;
            selectedTray.currentGridPos = PlacementSystem.Instance.grid.WorldToCell(newPos);
        }
    }

    private bool IsDirectionBlocked(Tray tray, Vector3 direction, float moveStepDistance)
    {
        if (direction == Vector3.zero)
            return false;

        var shape = tray.GetShapeData();
        var excludedOffsets = shape.ExcludedOffsets;
        Vector2Int size = shape.Size;

        Vector3 basePos = tray.transform.position;
        float castHeight = 0.25f;

        // Scale cast distance to match movement magnitude
        float checkDistance = Mathf.Max(trayCollisionDirection, moveStepDistance);

        Vector3 dir = direction.normalized;

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                Vector2Int offset = new(x, y);
                if (excludedOffsets.Contains(offset))
                    continue;

                Vector3 cellCenter = basePos + new Vector3(x + 0.5f, castHeight, y + 0.5f);
                Vector3 castOrigin = cellCenter + dir * trayCollisionMargin * 0.5f;

                Vector3 halfExtents = new Vector3(
                    0.5f - trayCollisionMargin * 0.5f,
                    castHeight,
                    0.5f - trayCollisionMargin * 0.5f
                );

                int hitCount = Physics.BoxCastNonAlloc(
                    castOrigin,
                    halfExtents,
                    dir,
                    trayCastHits,
                    Quaternion.identity,
                    checkDistance,
                    trayCollision
                );

                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = trayCastHits[i];
                    Tray hitTray = hit.collider.GetComponentInParent<Tray>();
                    if (hitTray == tray) continue;

                    if (hitTray != null)
                    {
                        Vector3 local = hit.point + hitTray.GetVisualOffSet();
                        int hitX = Mathf.FloorToInt(local.x);
                        int hitZ = Mathf.FloorToInt(local.z);
                        Vector2Int hitOffset = new(hitX, hitZ);
                        Vector3 hitOffsets = new(hitX, 0.25f ,hitZ);
                        Debug.DrawLine(castOrigin, hitOffsets, Color.red, checkDistance);

                        if (!hitTray.GetShapeData().ExcludedOffsets.Contains(hitOffset))
                            return true;
                    }
                    else
                    {
                        return true; // Hit wall or environment
                    }
                }
            }
        }

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
        if (a != null) yield return a.CheckAndStartSwap(null);
        if (b != null) yield return b.CheckAndStartSwap(null);
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
            yield return Helpers.GetWaitForSecond(0.1f);
        }

        isProcessingRemove = false;
    }

}