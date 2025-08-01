using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlacementSystem : MonoBehaviour
{
    public static PlacementSystem Instance { get; private set; }

    [SerializeField] public GameObject mouseIndicator, cellIndicator;
    [SerializeField] public InputManager inputManager;
    [SerializeField] public Grid grid;

    [SerializeField] public GridMapData currentMap;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject cornerWallPrefab;
    [SerializeField] public Transform gridParent;

    private Dictionary<Vector3Int, GridObjects> occupiedCells = new();
    public Dictionary<TrayPlacementData, List<ItemColorType>> PlannedTrayFoodMap { get; set; } = new();

    private List<Tray> activeTrays = new();
    public List<Tray> AllPlannedTrays => activeTrays;

    private List<TraySpawner> traySpawners = new();
    public List<TraySpawner> TraySpawners => traySpawners;

    private List<TrayPlacementData> plannedTrayData = new();

    #region Startups
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        LoadMap();
        GenerateBoundaryWalls();
    }

    private void LoadMap()
    {
        GameManager.Instance.ResetState();
        GameManager.Instance.SetMoveLimit(currentMap.MaxMoveCount);

        ClearGrid();
        SpawnBlockedCells();

        plannedTrayData.Clear();
        traySpawners.Clear();
        activeTrays.Clear();

        CollectPlannedTrayData(); // Combine static + spawner trays

        var foodPlan = TrayFoodCalculator.CreateFoodPlan(plannedTrayData);
        GameManager.Instance.BlockedTrayFoodMap = foodPlan.BlockedTrayFood;
        GameManager.Instance.PlannedTrayFoodMap = foodPlan.NormalTrayFoodByData;

        SpawnTrays(foodPlan.NormalTrayFoodByData);
        SpawnTraySpawners();
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
        cellIndicator.transform.position = grid.CellToWorld(gridPosition);
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

    #endregion

    #region Generate statics walls and blocked cells
    private void GenerateBoundaryWalls()
    {
        Vector2Int size = currentMap.GridSize;
        Vector3Int origin = currentMap.GridOrigin;

        int halfX = size.x / 2;
        int halfZ = size.y / 2;

        int startX = origin.x - halfX - 1;
        int endX = origin.x + halfX + (size.x % 2 == 0 ? 0 : 1);

        int startZ = origin.z - halfZ - 1;
        int endZ = origin.z + halfZ + (size.y % 2 == 0 ? 0 : 1);

        // Top Wall (Z = endZ), skip corners
        for (int x = startX + 1; x < endX; x++)
        {
            Vector3Int cell = new(x, 0, endZ);
            if (currentMap.BlockedCells.Contains(cell)) continue;
            SpawnWall(cell, Quaternion.identity);
        }

        // Bottom Wall (Z = startZ), skip corners
        for (int x = startX + 1; x < endX; x++)
        {
            Vector3Int cell = new(x, 0, startZ);
            if (currentMap.BlockedCells.Contains(cell)) continue;
            SpawnWall(cell, Quaternion.identity);
        }

        // Left Wall (X = startX), skip corners
        for (int z = startZ + 1; z < endZ; z++)
        {
            Vector3Int cell = new(startX, 0, z);
            if (currentMap.BlockedCells.Contains(cell)) continue;
            SpawnWall(cell, Quaternion.Euler(0, -90, 0));
        }

        // Right Wall (X = endX), skip corners
        for (int z = startZ + 1; z < endZ; z++)
        {
            Vector3Int cell = new(endX, 0, z);
            if (currentMap.BlockedCells.Contains(cell)) continue;
            SpawnWall(cell, Quaternion.Euler(0, -90, 0));
        }

        // Now spawn corners
        SpawnCornerWalls(startX, endX, startZ, endZ);
    }

    private void SpawnWall(Vector3Int cell, Quaternion rotation)
    {
        Vector3 worldPos = grid.CellToWorld(cell);
        GameObject wall = Instantiate(wallPrefab, worldPos, Quaternion.identity, gridParent);

        if (wall.TryGetComponent(out EnviromentWall env))
        {
            env.SetGridPosition(cell);

            var pivot = env.visual;
            if (pivot != null)
            {
                // Default values
                pivot.localPosition = new Vector3(0.5f, 0f, 0.25f);
                pivot.localRotation = Quaternion.identity;

                bool isVertical = rotation == Quaternion.Euler(0, -90, 0);
                bool isBottom = cell.z < currentMap.GridOrigin.z;

                if (isVertical)
                {
                    pivot.localRotation = Quaternion.Euler(0, 90f, 0);

                    if (cell.x > currentMap.GridOrigin.x) // Right wall
                        pivot.localPosition = new Vector3(0.25f, 0f, 0.5f);
                    else // Left wall
                        pivot.localPosition = new Vector3(0.75f, 0f, 0.5f);
                }
                else if (isBottom)
                {
                    pivot.localPosition = new Vector3(0.5f, 0f, 0.75f); // Bottom wall
                }
            }
        }
    }

    private void SpawnCornerWalls(int startX, int endX, int startZ, int endZ)
    {
        if (cornerWallPrefab == null) return;

        var corners = new (Vector3Int cell, float yRot, Vector3 pivotPos)[]
        {
        // cell,         Y rot,      pivot localPosition
        (new Vector3Int(endX, 0, endZ),    -90f, new Vector3(0.25f, 0f, 0.25f)), // Top Right
        (new Vector3Int(endX, 0, startZ),    0f, new Vector3(0.25f, 0f, 0.75f)), // Bottom Right
        (new Vector3Int(startX, 0, startZ),  90f, new Vector3(0.75f, 0f, 0.75f)), // Bottom Left
        (new Vector3Int(startX, 0, endZ),   180f, new Vector3(0.75f, 0f, 0.25f)), // Top Left
        };

        foreach (var (cell, yRot, pivotOffset) in corners)
        {
            Vector3 worldPos = grid.CellToWorld(cell);
            GameObject corner = Instantiate(cornerWallPrefab, worldPos, Quaternion.identity, gridParent);

            if (corner.TryGetComponent(out EnviromentWall env))
            {
                env.SetGridPosition(cell);

                var pivot = env.visual;
                if (pivot != null)
                {
                    pivot.localRotation = Quaternion.Euler(0, yRot, 0);
                    pivot.localPosition = pivotOffset;
                }
            }
        }
    }

    private void SpawnBlockedCells()
    {
        foreach (Vector3Int blockedCell in currentMap.BlockedCells)
        {
            var wall = Instantiate(wallPrefab, grid.CellToWorld(blockedCell), Quaternion.identity, gridParent);
            var env = wall.GetComponent<EnviromentWall>();
            env.SetGridPosition(blockedCell);
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
    private void SpawnTrays(Dictionary<TrayPlacementData, List<ItemColorType>> foodMap)
    {
        foreach (var trayData in currentMap.Trays)
        {
            GameObject trayGO = Instantiate(trayData.trayPrefab, grid.CellToWorld(trayData.position), Quaternion.identity, gridParent);

            if (trayGO.TryGetComponent(out Tray tray))
            {
                tray.ApplyFoodSet(currentMap.ActiveFoodTheme);
                tray.OnTrayFinished += GameManager.Instance.HandleTrayFinished;
                activeTrays.Add(tray);
                Vector3Int gridPos = grid.WorldToCell(trayGO.transform.position);
                tray.currentGridPos = gridPos;
                tray.originalGridPos = gridPos;
                tray.lastValidGridPos = gridPos;

                if (foodMap.TryGetValue(trayData, out var foodList))
                {
                    tray.DelayedSpawnFromColorList(foodList);
                }
            }
        }
    }

    private void CollectPlannedTrayData()
    {
        plannedTrayData.Clear();
        int trayCounter = 0;

        // Static trays
        foreach (var tray in currentMap.Trays)
        {
            var data = tray;
            data.uniqueID = trayCounter++;
            plannedTrayData.Add(data);
        }

        // Spawner trays
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

    private void DistributeFoodToTrays(Dictionary<Tray, List<ItemColorType>> foodMap)
    {
        foreach (var kvp in foodMap)
        {
            Tray tray = kvp.Key;
            List<ItemColorType> foodList = kvp.Value;

            tray.SpawnFromColorList(foodList);
        }
    }

    public void RegisterTray(Tray tray)
    {
        if (!activeTrays.Contains(tray))
        {
            activeTrays.Add(tray);
        }
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