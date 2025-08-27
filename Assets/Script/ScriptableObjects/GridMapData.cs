using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using System.Collections.Generic;

public enum CellType
{
    Empty,
    Floor,

    // Walls
    FullWallHorizontal,
    FullWallVertical,

    HalfWall,          // tool / legacy alias
    HalfWallTop,       // faces "top" (north)
    HalfWallRight,     // east
    HalfWallBottom,    // south
    HalfWallLeft,      // west

    // Corners
    CornerTopLeft,
    CornerTopRight,
    CornerBottomLeft,
    CornerBottomRight,

    // Blend edges
    BlendTop,
    BlendBottom,
    BlendLeft,
    BlendRight,

    // L-walls
    LWall0,
    LWall90,
    LWall180,
    LWall270,
}


[CreateAssetMenu(fileName = "GridMapData", menuName = "Scriptable Objects/GridMapData")]
public class GridMapData : SerializedScriptableObject
{
    [Sirenix.OdinInspector.Title("Grid Settings")]
    [Sirenix.OdinInspector.OnValueChanged(nameof(ResizeMatrix))]
    public Vector2Int GridSize = new(5, 5);

    [OdinSerialize, TableMatrix(DrawElementMethod = nameof(DrawCell),
 SquareCells = true, ResizableColumns = false, Transpose = false, IsReadOnly = false)]
    public CellType[,] cellMatrix = new CellType[5, 5];

    [Button(ButtonSizes.Large), GUIColor(1f, 0.3f, 0.3f)]
    private void ClearAll()
    {
        int width = cellMatrix.GetLength(0);
        int height = cellMatrix.GetLength(1);

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                cellMatrix[x, y] = CellType.Empty;
    }

    public CellType GetCell(int x, int y)
    {
        if (cellMatrix == null || cellMatrix.GetLength(0) != GridSize.x || cellMatrix.GetLength(1) != GridSize.y)
            ResizeMatrix();
        return cellMatrix[x, y];
    }

    public bool TryWorldToMatrix(Vector3Int worldCell, out int x, out int z)
    {
        // same minX/minZ math your PlacementSystem uses
        int halfX = GridSize.x / 2;
        int halfZ = GridSize.y / 2;

        int minX = GridOrigin.x - halfX;
        int minZ = GridOrigin.z - halfZ;

        x = worldCell.x - minX;
        z = worldCell.z - minZ;

        return x >= 0 && x < GridSize.x && z >= 0 && z < GridSize.y;
    }

    public Vector3Int MatrixToWorldCell(int x, int z)
    {
        int matrixHeight = cellMatrix.GetLength(1);

        // flip z (vertical axis)
        int flippedZ = (matrixHeight - 1) - z;

        int halfX = GridSize.x / 2;
        int halfZ = GridSize.y / 2;

        int minX = GridOrigin.x - halfX - 1; // -1 for boundary
        int minZ = GridOrigin.z - halfZ - 1;

        return new Vector3Int(minX + x, 0, minZ + flippedZ);
    }

    public bool IsCellBlockedWorld(Vector3Int worldCell)
    {
        if (!TryWorldToMatrix(worldCell, out int x, out int z))
            return false;

        var t = cellMatrix[x, z];
        return t != CellType.Empty && t != CellType.Floor;
    }

    // Legacy-style enumerable so your old loops still work
    public IEnumerable<Vector3Int> BlockedCells
    {
        get
        {
            for (int x = 0; x < cellMatrix.GetLength(0); x++)
                for (int z = 0; z < cellMatrix.GetLength(1); z++)
                    if (cellMatrix[x, z] != CellType.Empty)
                        yield return MatrixToWorldCell(x, z);
        }
    }

    private void ResizeMatrix()
    {
        if (GridSize.x <= 0 || GridSize.y <= 0) return;

        int matrixX = GridSize.x + 2; // +2 for boundary
        int matrixY = GridSize.y + 2;

        var newMatrix = new CellType[matrixX, matrixY];

        // Copy existing values
        for (int x = 0; x < Mathf.Min(matrixX, cellMatrix.GetLength(0)); x++)
        {
            for (int y = 0; y < Mathf.Min(matrixY, cellMatrix.GetLength(1)); y++)
            {
                newMatrix[x, y] = cellMatrix[x, y];
            }
        }

        cellMatrix = newMatrix;
    }

#if UNITY_EDITOR
    private static CellType activePaintType = CellType.HalfWall; // default

    [OnInspectorGUI]
    private void DrawPaintToolbar()
    {
        GUILayout.Space(5);
        GUILayout.Label("Paint Tool", EditorStyles.boldLabel);

        // Dropdown for selecting CellType (will list the new HalfWall* too, you can ignore them)
        activePaintType = (CellType)EditorGUILayout.EnumPopup("Active Paint Type", activePaintType);

        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
        {
            ClearAll();
        }
        if (GUILayout.Button("Fill with Active Type"))
        {
            int w = cellMatrix.GetLength(0);
            int h = cellMatrix.GetLength(1);
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    cellMatrix[x, y] = activePaintType == CellType.HalfWall
                        ? CellType.HalfWallTop // default orientation if filling with HalfWall tool
                        : activePaintType;
        }
        GUILayout.EndHorizontal();
    }

    private static bool IsHalfWall(CellType t) =>
        t == CellType.HalfWallTop || t == CellType.HalfWallRight ||
        t == CellType.HalfWallBottom || t == CellType.HalfWallLeft;

    private static CellType NextHalfWall(CellType t) => t switch
    {
        CellType.HalfWallTop => CellType.HalfWallRight,
        CellType.HalfWallRight => CellType.HalfWallBottom,
        CellType.HalfWallBottom => CellType.HalfWallLeft,
        CellType.HalfWallLeft => CellType.HalfWallTop,
        _ => CellType.HalfWallTop
    };

    private static bool IsCorner(CellType t) =>
        t == CellType.CornerTopLeft || t == CellType.CornerTopRight ||
        t == CellType.CornerBottomLeft || t == CellType.CornerBottomRight;

    private static CellType NextCorner(CellType t) => t switch
    {
        CellType.CornerTopLeft => CellType.CornerTopRight,
        CellType.CornerTopRight => CellType.CornerBottomRight,
        CellType.CornerBottomRight => CellType.CornerBottomLeft,
        CellType.CornerBottomLeft => CellType.CornerTopLeft,
        _ => CellType.CornerTopLeft
    };

    // NOTE: Odin supports this signature to also receive matrix indices.
    private static CellType DrawCell(Rect rect, CellType value, int x, int y)
    {
        float padding = rect.width * 0.2f;
        Rect padded = new Rect(rect.x + padding / 2, rect.y + padding / 2,
                               rect.width - padding, rect.height - padding);

        // Base color
        Color c = value switch
        {
            CellType.Empty => Color.white,
            CellType.Floor => new Color(0.92f, 0.92f, 0.92f),

            CellType.FullWallHorizontal => new Color(0.2f, 0.2f, 1f),
            CellType.FullWallVertical => new Color(0.4f, 0.4f, 1f),

            CellType.HalfWallTop => new Color(1f, 0.3f, 0.3f),
            CellType.HalfWallRight => new Color(1f, 0.35f, 0.35f),
            CellType.HalfWallBottom => new Color(1f, 0.4f, 0.4f),
            CellType.HalfWallLeft => new Color(1f, 0.45f, 0.45f),
            CellType.HalfWall => new Color(1f, 0.3f, 0.3f),

            CellType.CornerTopLeft => new Color(0f, 0.7f, 0f),
            CellType.CornerTopRight => new Color(0f, 0.9f, 0f),
            CellType.CornerBottomLeft => new Color(0f, 0.5f, 0f),
            CellType.CornerBottomRight => new Color(0f, 0.3f, 0f),

            _ => Color.gray
        };

        EditorGUI.DrawRect(padded, c);

        // Borders
        Handles.color = Color.black;
        Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.xMax, rect.y));
        Handles.DrawLine(new Vector3(rect.x, rect.yMax), new Vector3(rect.xMax, rect.yMax));
        Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x, rect.yMax));
        Handles.DrawLine(new Vector3(rect.xMax, rect.y), new Vector3(rect.xMax, rect.yMax));

        // HalfWall arrows
        if (value == CellType.HalfWall || IsHalfWall(value))
        {
            Vector2 center = rect.center;
            float len = rect.width * 0.28f;

            Vector2 dir = value switch
            {
                CellType.HalfWallTop => new Vector2(0f, -1f),
                CellType.HalfWallRight => new Vector2(1f, 0f),
                CellType.HalfWallBottom => new Vector2(0f, 1f),
                CellType.HalfWallLeft => new Vector2(-1f, 0f),
                _ => new Vector2(0f, -1f)
            };

            Vector2 tip = center + dir.normalized * len;
            Handles.color = Color.black;
            Handles.DrawLine(center, tip);

            float head = rect.width * 0.14f;
            Vector2 perp = new Vector2(-dir.y, dir.x).normalized;
            Vector3 p0 = new Vector3(tip.x, tip.y, 0f);
            Vector3 p1 = new Vector3(tip.x - dir.x * head + perp.x * head * 0.6f,
                                     tip.y - dir.y * head + perp.y * head * 0.6f, 0f);
            Vector3 p2 = new Vector3(tip.x - dir.x * head - perp.x * head * 0.6f,
                                     tip.y - dir.y * head - perp.y * head * 0.6f, 0f);
            Handles.DrawAAConvexPolygon(p0, p1, p2);
        }

        // Corner "L-shape" indicator
        if (IsCorner(value))
        {
            Handles.color = Color.black;
            float inset = rect.width * 0.25f;

            switch (value)
            {
                case CellType.CornerTopLeft:
                    Handles.DrawLine(new Vector2(rect.x + inset, rect.y), new Vector2(rect.x + inset, rect.y + inset));
                    Handles.DrawLine(new Vector2(rect.x, rect.y + inset), new Vector2(rect.x + inset, rect.y + inset));
                    break;

                case CellType.CornerTopRight:
                    Handles.DrawLine(new Vector2(rect.xMax - inset, rect.y), new Vector2(rect.xMax - inset, rect.y + inset));
                    Handles.DrawLine(new Vector2(rect.xMax - inset, rect.y + inset), new Vector2(rect.xMax, rect.y + inset));
                    break;

                case CellType.CornerBottomLeft:
                    Handles.DrawLine(new Vector2(rect.x + inset, rect.yMax - inset), new Vector2(rect.x, rect.yMax - inset));
                    Handles.DrawLine(new Vector2(rect.x + inset, rect.yMax - inset), new Vector2(rect.x + inset, rect.yMax));
                    break;

                case CellType.CornerBottomRight:
                    Handles.DrawLine(new Vector2(rect.xMax - inset, rect.yMax - inset), new Vector2(rect.xMax, rect.yMax - inset));
                    Handles.DrawLine(new Vector2(rect.xMax - inset, rect.yMax - inset), new Vector2(rect.xMax - inset, rect.yMax));
                    break;
            }
        }

        // Mouse interaction (rotation only on mouse down, not drag)
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
        {
            if (Event.current.button == 0)
            {
                if (Event.current.shift)
                {
                    value = CellType.Empty;
                }
                else
                {
                    if (activePaintType == CellType.HalfWall)
                    {
                        value = IsHalfWall(value) ? NextHalfWall(value) : CellType.HalfWallTop;
                    }
                    else if (activePaintType == CellType.CornerTopLeft)
                    {
                        value = IsCorner(value) ? NextCorner(value) : CellType.CornerTopLeft;
                    }
                    else
                    {
                        value = activePaintType;
                    }
                }

                GUI.changed = true;
                Event.current.Use();
            }
        }

        return value;
    }
#endif

    [Sirenix.OdinInspector.Title("Gameplay Data")]
    [field: SerializeField] public Vector3Int GridOrigin { get; private set; } = Vector3Int.zero;
    [field: SerializeField] public int MaxMoveCount { get; private set; } = 10;

    [field: SerializeField] public List<TrayPlacementData> Trays { get; private set; } = new();
    [field: SerializeField] public List<DirectionalTrayData> DirectionalTrays { get; private set; } = new();
    [field: SerializeField] public List<BlockedTrayData> BlockedTrays { get; private set; } = new();
    [field: SerializeField] public List<TraySpawnerPlacementData> Spawners { get; private set; } = new();

    [field: SerializeField] public List<FoodData> ActiveFoodTheme { get; private set; }
}

[System.Serializable]
public class TrayPlacementData
{
    public GameObject trayPrefab;
    public Vector3Int position;

    public int uniqueID;

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + (trayPrefab ? trayPrefab.GetInstanceID() : 0);
            hash = hash * 23 + position.GetHashCode();
            hash = hash * 23 + uniqueID;
            return hash;
        }
    }

    public override bool Equals(object obj)
    {
        return obj is TrayPlacementData other &&
               trayPrefab == other.trayPrefab &&
               position == other.position &&
               uniqueID == other.uniqueID;
    }
}

[System.Serializable]
public struct TraySpawnerPlacementData
{
    public GameObject traySpawnerPrefab;
    public List<TrayPlacementData> TraysToSpawn;
    public Vector3Int position;
    public SpawningDirection direction;
}


[System.Serializable]
public struct DirectionalTrayData
{
    public GameObject trayPrefab;
    public Vector3Int position;
    public MovementAxis movementAxis;
    public int uniqueID;
}

[System.Serializable]
public struct BlockedTrayData
{
    public GameObject trayPrefab;           
    public Vector3Int position;
    public int requiredCompletedTrays;
    public int uniqueID;
}



