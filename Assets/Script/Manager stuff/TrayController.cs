using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrayController : MonoBehaviour
{
    [SerializeField] private InputManager inputManager;
    [SerializeField] public LayerMask trayLayer;
    [SerializeField] public LayerMask trayCollision;
    [SerializeField] public float trayDragCooldown = 0.5f; // tweak as needed
    [SerializeField] private bool isTrayDragLocked = false;
    [SerializeField] private float moveSpeed = 20f;

    [PropertyRange(0, 1)]
    [SerializeField] private float straightThreshold = 0.5f;
    [PropertyRange(0, 1)]
    [SerializeField] private float diagonalThreshold = 0.25f;

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


    //moving
    private Vector3Int limitOriginCell;
    private int maxMoveLeft, maxMoveRight, maxMoveUp, maxMoveDown;
    private TrayMovementHelper trayMovementHelper;

    [SerializeField] public static HashSet<Food> SwappingFoods = new();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        trayMovementHelper = new TrayMovementHelper(PlacementSystem.Instance.grid);
    }

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
                trayDragTimer = 0f;
                isTrayDragLocked = false;
            }
        }
    }

    public void ForcePickupDelay()
    {
        isTrayDragLocked = true;
        trayDragTimer = 0.16f;
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
            if (tray != null && !tray.IsBusy())
            {
                if(tray.IsUnlocked())
                {
                    BeginDrag(tray);
                }
                else
                {
                    SoundEventManager.OnTryPickupBlocked?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    private void BeginDrag(Tray tray)
    {
        selectedTray = tray;
        isDragging = true;
        dragOffset = inputManager.GetSelectedMapPosition() - selectedTray.transform.position;
        selectedTray.visual.IsShowOutLine(true);


        limitOriginCell = tray.currentGridPos;
        (maxMoveLeft, maxMoveRight, maxMoveUp, maxMoveDown) =
            trayMovementHelper.CalculateTrayMoveLimit(tray, limitOriginCell);

        tray.OnPickUp();
        tray.visual.PlayPickUpAnimation();
    }

    private void HandleTrayDrag()
    {
        Vector3 mouseWorldPos = inputManager.GetSelectedMapPosition();
        Vector3 desired = mouseWorldPos - dragOffset;
        desired = trayMovementHelper.ApplyAxisConstraint(selectedTray, desired);

        // clamp inside world bounds
        Vector3Int minCell = limitOriginCell + new Vector3Int(-maxMoveLeft, 0, -maxMoveDown);
        Vector3Int maxCell = limitOriginCell + new Vector3Int(maxMoveRight, 0, maxMoveUp);

        Vector3 minWorld = PlacementSystem.Instance.grid.CellToWorld(minCell);
        Vector3 maxWorld = PlacementSystem.Instance.grid.CellToWorld(maxCell);

        float clampedX = Mathf.Clamp(desired.x, minWorld.x, maxWorld.x);
        float clampedZ = Mathf.Clamp(desired.z, minWorld.z, maxWorld.z);

        Vector3 clampedWorldPos = new Vector3(clampedX, desired.y, clampedZ);

        // smooth tray movement
        selectedTray.transform.position = Vector3.MoveTowards(
            selectedTray.transform.position,
            clampedWorldPos,
            moveSpeed * Time.deltaTime
        );

        // --- custom snapping with threshold ---
        Vector3 worldPos = selectedTray.transform.position;
        Vector3 cellSize = PlacementSystem.Instance.grid.cellSize;

        float gridX = worldPos.x / cellSize.x;
        float gridZ = worldPos.z / cellSize.z;

        int x = SnapWithThreshold(gridX, straightThreshold);
        int z = SnapWithThreshold(gridZ, diagonalThreshold);

        Vector3Int candidateCell = new Vector3Int(x, 0, z);

        // step-by-step check to avoid diagonal tunneling
        if (candidateCell != limitOriginCell)
        {
            Vector3Int oldCell = limitOriginCell;

            // try horizontal step
            if (candidateCell.x != oldCell.x)
            {
                Vector3Int step = new Vector3Int(candidateCell.x, 0, oldCell.z);
                if (PlacementSystem.Instance.CanTrayFitAtCell(selectedTray, step))
                    oldCell = step;
            }

            // try vertical step
            if (candidateCell.z != oldCell.z)
            {
                Vector3Int step = new Vector3Int(oldCell.x, 0, candidateCell.z);
                if (PlacementSystem.Instance.CanTrayFitAtCell(selectedTray, step))
                    oldCell = step;
            }

            if (oldCell != limitOriginCell)
            {
                limitOriginCell = oldCell;
                selectedTray.currentGridPos = oldCell;

                (maxMoveLeft, maxMoveRight, maxMoveUp, maxMoveDown) =
                    trayMovementHelper.CalculateTrayMoveLimit(selectedTray, oldCell);
            }
        }
    }

    private int SnapWithThreshold(float value, float threshold)
    {
        int floor = Mathf.FloorToInt(value);
        float frac = value - floor;

        if (frac > threshold)
            return floor + 1;
        return floor;
    }

    private void EndDrag()
    {
        if (selectedTray != null)
        {
            selectedTray.OnDrop();
            selectedTray.visual.IsShowOutLine(false);
            selectedTray = null;
        }
        isDragging = false;
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