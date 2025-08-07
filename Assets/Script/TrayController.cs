using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrayController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] private LayerMask trayLayer;
    [SerializeField] private LayerMask trayCollision;
    [SerializeField, Range(0f, 0.5f)]
    private float trayCollisionMargin = 0.15f;
    [SerializeField] private float trayCollisionDirection = 0.15f;
    [SerializeField] private float trayDragCooldown = 0.5f; // tweak as needed
    [SerializeField] private bool isTrayDragLocked = false;
    [SerializeField] private float moveSpeed = 20f;

    public int track;
    public float trayDragTimer = 0f;

    public static TrayController Instance { get; private set; }

    private readonly Queue<Tray> removeQueue = new();
    private bool isProcessingRemove = false;
    private readonly HashSet<Tray> activeRecursiveTrays = new();

    public bool IsInputBlocked => activeRecursiveTrays.Count > 0;

    public bool isDragging;
    private Vector3 dragOffset;
    private RaycastHit[] trayCastHits = new RaycastHit[10];
    private Tray selectedTray;
    private bool isWaitingForTrayClear = false;
    private float postDropBlockTime = 0.1f;
    public float lastDropTime = -10f;

    [SerializeField] public static HashSet<Food> SwappingFoods = new();

    private void Awake() => Instance = this;

    private void Update()
    {
        track = activeRecursiveTrays.Count;

        DelayPickup();
        
        if (GameManager.Instance.IsGameEnd() || !GameManager.Instance.IsGamePlaying() || isTrayDragLocked) return;


        if (inputManager.WasTouchPressedThisFrame())
            TrySelectTray();

        if (inputManager.IsTouchPressed() && selectedTray != null)
            HandleTrayDrag();

        if (inputManager.WasTouchReleasedThisFrame() && isDragging)
            EndDrag();
    }

    private void DelayPickup()
    {
        //if theres any in queue then locked = true and return
        if (IsInputBlocked)
        {
            isTrayDragLocked = true;
            trayDragTimer = trayDragCooldown;
            isWaitingForTrayClear = false;
            return;
        }

        if (isWaitingForTrayClear)
        {
            isWaitingForTrayClear = false; // only once
            isTrayDragLocked = true;
            trayDragTimer = trayDragCooldown;
            return;
        }

        if (isTrayDragLocked)
        {
            trayDragTimer -= Time.deltaTime;

            if (trayDragTimer <= 0f)
            {
                trayDragTimer = 0;
                isTrayDragLocked = false;
            }
        }
    }

    public void ForcePickupDelay()
    {
        isTrayDragLocked = true;
        trayDragTimer = 0.2f;
        isWaitingForTrayClear = false;
        lastDropTime = Time.time;
    }

    private void TrySelectTray()
    {
        if (isTrayDragLocked || Time.time < lastDropTime + postDropBlockTime)
            return;

        Vector3 clickWorldPos = inputManager.GetSelectedMapPosition();
        Ray ray = new Ray(clickWorldPos + Vector3.up * 5f, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 100f, trayLayer))
        {
            Tray tray = hit.collider.GetComponent<Tray>();
            if (tray != null && tray.IsUnlocked())
            {
                BeginDrag(tray);
            }
        }
    }

    private void BeginDrag(Tray tray)
    {
        selectedTray = tray;
        isDragging = true;
        dragOffset = inputManager.GetSelectedMapPosition() - tray.transform.position;

        tray.OnPickUp();
        tray.visual.PlayPickUpAnimation();
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

        DirectionalChecks(moveDelta, step.magnitude, out bool blockX, out bool blockZ, out bool blockDiagonal);

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

    private void EndDrag()
    {
        if (selectedTray != null)
        {
            selectedTray.OnDrop();
            selectedTray = null;
        }
        isDragging = false;
    }

    private void DirectionalChecks(Vector3 moveDelta, float stepMag, out bool blockX, out bool blockZ, out bool blockDiagonal)
    {
        blockX = IsDirectionBlocked(selectedTray, new Vector3(Mathf.Sign(moveDelta.x), 0, 0), stepMag);
        blockZ = IsDirectionBlocked(selectedTray, new Vector3(0, 0, Mathf.Sign(moveDelta.z)), stepMag);
        blockDiagonal = IsDirectionBlocked(selectedTray, new Vector3(Mathf.Sign(moveDelta.x), 0, Mathf.Sign(moveDelta.z)), stepMag);
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
            if (directional.allowedDirection == MovementAxis.Horizontal)
                desired.z = current.z; // lock Z
            else
                desired.x = current.x; // lock X
        }
        return desired;
    }

    public void RunDeferredCompletion(Tray a, Tray b, Action callback)
    {
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
            isTrayDragLocked = true;
            trayDragTimer = trayDragCooldown;
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
            yield return Helpers.GetWaitForSecond(0.1f);
        }

        isProcessingRemove = false;

        if (activeRecursiveTrays.Count == 0)
        {
            isWaitingForTrayClear = true; // mark to start delay next frame
            isTrayDragLocked = true;
            trayDragTimer = trayDragCooldown;
        }
    }

}