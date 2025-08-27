using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class PlacementSystem : MonoBehaviour
{
    public static PlacementSystem Instance { get; private set; }

    [SerializeField] public GameObject mouseIndicator, cellIndicator;
    [SerializeField] public InputManager inputManager;
    [SerializeField] public Grid grid;

    [SerializeField] public GridMapData[] currentMapGrid;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject cornerWallPrefab;
    [SerializeField] private GameObject halfWallPrefab;   
    [SerializeField] private GameObject fullWallPrefab;
    [SerializeField] public Transform gridParent;
    [SerializeField] private WallPrefabSet wallPrefabs;

    public GridMapData currentMap;
    public Dictionary<Vector3Int, GridObjects> occupiedCells = new();
    public Dictionary<TrayPlacementData, List<ItemColorType>> PlannedTrayFoodMap { get; set; } = new();

    private List<Tray> activeTrays = new();
    public List<Tray> AllPlannedTrays => activeTrays;

    private List<TraySpawner> traySpawners = new();
    public List<TraySpawner> TraySpawners => traySpawners;

    private List<TrayPlacementData> plannedTrayData = new();
 
    private int currentMapIndex = 0; // Current map index, can be used to switch maps or continue
    #region Startups
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadMap();
    }
    public void ContinueMap()
    {
        currentMapIndex++;
        if(currentMapIndex > currentMapGrid.Count() - 1 )
        {
            currentMapIndex = 0; // Reset to first map if out of bounds
        }
        print("current map num: " + currentMapIndex);
        //currentMap = currentMapGrid[currentMapIndex];
        LoadMap();
    }

    public void LoadMap()
    {
        GameManager.Instance.ResetState(currentMap.MaxMoveCount);
        ClearGrid();
        SnapCameraToCenter();
        plannedTrayData.Clear();
        traySpawners.Clear();
        activeTrays.Clear();

        CollectPlannedTrayData(); // Combine static + spawner trays

        var blockedSpecs = currentMap.BlockedTrays.Select(bt => new BlockedTraySpec
        {
            data = bt.ToPlacementData(),
            requirement = bt.requiredCompletedTrays
        }).ToList();

        var foodPlan = TrayFoodCalculator.CreateFoodPlan(plannedTrayData, blockedSpecs);

        if (foodPlan == null)
        {
            Debug.LogError("Failed to create food plan for this map.");
            return;
        }

        GameManager.Instance.BlockedTrayFoodMap = foodPlan.BlockedTrayFood;
        GameManager.Instance.PlannedTrayFoodMap = foodPlan.NormalTrayFoodByData;

        StartCoroutine(SpawnTrays(foodPlan.NormalTrayFoodByData, 0.1f));
        SpawnTraySpawners();

        SpawnMapFromMatrix();
    }

    private void ClearGrid()
    {
        foreach (Transform child in gridParent)
        {
            Destroy(child.gameObject);
        }
        occupiedCells.Clear();
    }

    #endregion

    #region Updates
    private void Update()
    {
        Vector3 mousePosition = inputManager.GetSelectedMapPosition();
        Vector3Int gridPosition = grid.WorldToCell(mousePosition);
        mouseIndicator.transform.position = mousePosition;
        cellIndicator.transform.position = grid.CellToLocal(gridPosition);
    }

    #endregion

    #region Grid and Cells management
    public void RegisterGridObject(Vector3Int gridPos, GridObjects obj)
    {
        occupiedCells[gridPos] = obj;
    }

    public void UnregisterGridObject(Vector3Int gridPos)
    {
        occupiedCells.Remove(gridPos);
        if (AllPlannedTrays.Count <= 0)
        {
            GameEventManager.OnLastTray?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool IsCellAvailable(Vector3Int cell, GridObjects requestingObj = null)
    {
        if (!occupiedCells.TryGetValue(cell, out var objAtCell))
            return true;
        return objAtCell == requestingObj;
    }


    public GridObjects GetGridObjectAt(Vector3Int cell)
    {
        occupiedCells.TryGetValue(cell, out var obj);
        return obj;
    }

    public bool IsCellInBounds(Vector3Int cell)
    {
        Vector2Int size = currentMap.GridSize;
        Vector3Int origin = currentMap.GridOrigin;

        int halfX = size.x / 2;
        int halfZ = size.y / 2;

        int minX = origin.x - halfX;
        int maxX = origin.x + halfX - 1 + (size.x % 2); // inclusive
        int minZ = origin.z - halfZ;
        int maxZ = origin.z + halfZ - 1 + (size.y % 2); // inclusive

        return cell.x >= minX && cell.x <= maxX &&
               cell.z >= minZ && cell.z <= maxZ;
    }

    private bool IsCellBlocked(Vector3Int cell)
    {
        return currentMap != null && currentMap.IsCellBlockedWorld(cell);
    }

    #endregion

    #region Generate statics walls and blocked cells
    private void SpawnMapFromMatrix()
    {
        int width = currentMap.cellMatrix.GetLength(0);
        int height = currentMap.cellMatrix.GetLength(1);

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                var type = currentMap.cellMatrix[x, z];
                if (type == CellType.Empty) continue;

                var worldCell = currentMap.MatrixToWorldCell(x, z);

                // Skip if trays/spawners already placed
                if (currentMap.Trays.Exists(t => t.position == worldCell) ||
                    currentMap.DirectionalTrays.Exists(t => t.position == worldCell) ||
                    currentMap.BlockedTrays.Exists(t => t.position == worldCell) ||
                    currentMap.Spawners.Exists(s => s.position == worldCell))
                    continue;

                var (prefab, rot, offset) = wallPrefabs.GetPrefab(type, worldCell, currentMap.GridOrigin);
                if (prefab == null) continue;

                var obj = Instantiate(prefab, grid.CellToWorld(worldCell), Quaternion.identity, gridParent);

                if (obj.TryGetComponent(out EnviromentWall env))
                {
                    env.SetGridPosition(worldCell);

                    if (env.visual != null)
                    {
                        env.visual.localRotation = rot;
                        env.visual.localPosition = offset;
                    }
                }
            }
        }
    }

    private void SpawnTraySpawners()
    {
        foreach (var spawnerData in currentMap.Spawners)
        {
            Vector3 worldPos = grid.CellToWorld(spawnerData.position);
            GameObject spawnerGO = Instantiate(
                spawnerData.traySpawnerPrefab,
                worldPos,
                Quaternion.identity,
                gridParent);

            if (spawnerGO.TryGetComponent(out TraySpawner traySpawner))
            {
                traySpawner.SetDirection(spawnerData.direction);
                traySpawner.SetTrayList(spawnerData.TraysToSpawn);
                traySpawners.Add(traySpawner);

                    var pivot = traySpawner.visual;
                if (pivot != null)
                {
                    switch (spawnerData.direction)
                    {
                        case SpawningDirection.Left:
                            pivot.localPosition = new Vector3(0.25f, 0f, 0.5f);
                            pivot.localRotation = Quaternion.Euler(0f, -90f, 0f);
                            break;

                        case SpawningDirection.Up:
                            pivot.localPosition = new Vector3(0.5f, 0f, 0.75f);
                            pivot.localRotation = Quaternion.Euler(0f, 0f, 0f);
                            break;

                        case SpawningDirection.Right:
                            pivot.localPosition = new Vector3(0.75f, 0f, 0.5f);
                            pivot.localRotation = Quaternion.Euler(0f, 90f, 0f);
                            break;

                        case SpawningDirection.Down:
                            pivot.localPosition = new Vector3(0.5f, 0f, 0.25f);
                            pivot.localRotation = Quaternion.Euler(0f, 180f, 0f);
                            break;
                    }
                }
            }
        }
    }

    public List<Vector3Int> GetCellsToCheck(Vector3Int spawnerPos, Vector2Int traySize, SpawningDirection direction)
    {
        List<Vector3Int> positions = new();

        Vector3Int basePos = direction switch
        {
            SpawningDirection.Up => spawnerPos + new Vector3Int(0, 0, 1),
            SpawningDirection.Down => spawnerPos + new Vector3Int(0, 0, -traySize.y),
            SpawningDirection.Left => spawnerPos + new Vector3Int(-traySize.x, 0, 0),
            SpawningDirection.Right => spawnerPos + new Vector3Int(1, 0, 0),
            _ => spawnerPos
        };

        for (int x = 0; x < traySize.x; x++)
            for (int z = 0; z < traySize.y; z++)
                positions.Add(basePos + new Vector3Int(x, 0, z));

        return positions;
    }

    #endregion

    #region Food and Tray Spawning
    private IEnumerator SpawnTrays(Dictionary<TrayPlacementData, List<ItemColorType>> foodMap, float delay)
    {
        List<(GameObject prefab, Vector3Int position, System.Action<Tray> setup)> traySpawnQueue = new();

        // Normal trays
        foreach (var trayData in currentMap.Trays)
        {
            traySpawnQueue.Add((trayData.trayPrefab, trayData.position, (tray) =>
            {
                tray.ApplyFoodSet(currentMap.ActiveFoodTheme);
                Vector3Int gridPos = grid.WorldToCell(tray.transform.position);
                tray.currentGridPos = gridPos;
                tray.originalGridPos = gridPos;
                tray.lastValidGridPos = gridPos;

                if (foodMap.TryGetValue(trayData, out var foodList))
                {
                    tray.DelayedSpawnFromColorList(foodList);
                }
            }
            ));
        }

        // Directional trays
        foreach (var dirTray in currentMap.DirectionalTrays)
        {
            var key = dirTray.ToPlacementData();
            traySpawnQueue.Add((dirTray.trayPrefab, dirTray.position, (tray) =>
            {
                if (tray is DirectionalTray dTray)
                    dTray.SetAxis(dirTray.movementAxis);

                tray.ApplyFoodSet(currentMap.ActiveFoodTheme);
                Vector3Int gridPos = grid.WorldToCell(tray.transform.position);
                tray.currentGridPos = gridPos;
                tray.originalGridPos = gridPos;
                tray.lastValidGridPos = gridPos;

                if (foodMap.TryGetValue(key, out var foodList))
                {
                    tray.DelayedSpawnFromColorList(foodList);
                }
            }
            ));
        }

        // Blocked trays
        foreach (var blockedTray in currentMap.BlockedTrays)
        {
            var key = blockedTray.ToPlacementData();
            traySpawnQueue.Add((blockedTray.trayPrefab, blockedTray.position, (tray) =>
            {
                if (tray is BlockedTray bTray)
                    bTray.SetUnlockRequirement(blockedTray.requiredCompletedTrays);

                tray.ApplyFoodSet(currentMap.ActiveFoodTheme);
                Vector3Int gridPos = grid.WorldToCell(tray.transform.position);
                tray.currentGridPos = gridPos;
                tray.originalGridPos = gridPos;
                tray.lastValidGridPos = gridPos;

                if (foodMap.TryGetValue(key, out var foodList))
                {
                    GameManager.Instance.BlockedTrayFoodMap[tray] = foodList;
                }
            }
            ));
        }

        // Now spawn them one-by-one
        foreach (var (prefab, pos, setup) in traySpawnQueue)
        {
            GameObject trayGO = Instantiate(prefab, grid.CellToWorld(pos), Quaternion.identity, gridParent);

            if (trayGO.TryGetComponent(out Tray tray))
            {
                activeTrays.Add(tray);
                setup(tray);
            }

            yield return Helpers.GetWaitForSecond(delay); // Wait between each spawn
        }
    }

    private void CollectPlannedTrayData()
    {
        plannedTrayData.Clear();
        int trayCounter = 0;

        // Normal Trays
        foreach (var tray in currentMap.Trays)
        {
            var data = tray;
            data.uniqueID = trayCounter++;
            plannedTrayData.Add(data);
        }

        // Directional Trays
        for (int i = 0; i < currentMap.DirectionalTrays.Count; i++)
        {
            var dirTray = currentMap.DirectionalTrays[i];
            dirTray.uniqueID = trayCounter++;
            currentMap.DirectionalTrays[i] = dirTray; // update in case it's reused

            plannedTrayData.Add(new TrayPlacementData
            {
                trayPrefab = dirTray.trayPrefab,
                position = dirTray.position,
                uniqueID = dirTray.uniqueID
            });
        }

        // Blocked Trays
        for (int i = 0; i < currentMap.BlockedTrays.Count; i++)
        {
            var blockedTray = currentMap.BlockedTrays[i];
            blockedTray.uniqueID = trayCounter++;
            currentMap.BlockedTrays[i] = blockedTray;

            plannedTrayData.Add(new TrayPlacementData
            {
                trayPrefab = blockedTray.trayPrefab,
                position = blockedTray.position,
                uniqueID = blockedTray.uniqueID
            });
        }

        // Spawner Trays
        foreach (var spawnerData in currentMap.Spawners)
        {
            foreach (var tray in spawnerData.TraysToSpawn)
            {
                TrayPlacementData data = tray;
                data.uniqueID = trayCounter++;
                plannedTrayData.Add(data);
            }
        }
    }

    public bool CanSpawnTrayAt(Vector3Int spawnerPos, Vector2Int traySize, SpawningDirection direction)
    {
        var checkPositions = GetCellsToCheck(spawnerPos, traySize, direction);
        return checkPositions.All(pos => IsCellAvailable(pos));
    }

    public Vector3Int GetTraySpawnPosition(Vector3Int spawnerPos, Vector2Int traySize, SpawningDirection direction)
    {
        return direction switch
        {
            SpawningDirection.Up => spawnerPos + new Vector3Int(0, 0, 1),
            SpawningDirection.Down => spawnerPos + new Vector3Int(0, 0, -traySize.y),
            SpawningDirection.Left => spawnerPos + new Vector3Int(-traySize.x, 0, 0),
            SpawningDirection.Right => spawnerPos + new Vector3Int(1, 0, 0),
            _ => spawnerPos
        };
    }

    public void RegisterTray(Tray tray)
    {
        if (!activeTrays.Contains(tray))
        {
            activeTrays.Add(tray);
        }
    }

    public bool CanTrayFitAtCell(Tray tray, Vector3Int cellPos)
    {
        // Bounds check for the tray anchor
        if (!IsCellInBounds(cellPos))
            return false;

        // Get all cells this tray would occupy at that position
        foreach (var cell in tray.GetOccupiedCells(cellPos))
        {
            if (!IsCellInBounds(cell))
                return false;

            // If this cell is occupied by something else, block it
            if (!IsCellAvailable(cell, tray))
                return false;
        }

        return true;
    }

    #endregion

    #region Camera Setting
    public void SnapCameraToCenter()
    {
        Camera cam = Camera.main;
        if (!cam) return;

        Vector2Int size = currentMap.GridSize;
        int width = size.x;
        int height = size.y; // if needed later, same rules apply

        //x center
        float centerX = (width % 2 == 0) ? 0f : 0.5f;

        //z center
        float centerZ = -(width * 0.5f);

        //y unchanged
        float centerY = cam.transform.position.y;

        cam.transform.position = new Vector3(centerX, centerY, centerZ);

        //orthographic size
        cam.orthographicSize = width + 0.75f;

        //camera rotation
        float rotationX = 90f - (width * 3.25f);
        cam.transform.rotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    #endregion

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);

        foreach (var kvp in occupiedCells)
        {
            Vector3 world = grid.CellToWorld(kvp.Key) + new Vector3(0.5f, 0.01f, 0.5f);
            Gizmos.DrawCube(world, new Vector3(0.9f, 0.05f, 0.9f));
        }
    }
#endif
}