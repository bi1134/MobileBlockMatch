using UnityEngine;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEditor;
using System.Collections.Generic;

public enum CellType
{
    Empty,

    // Walls
    FullWallHorizontal,
    FullWallVertical,
    HalfWall,

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

    // L-shaped walls (3 sides filled, 1 side empty)
    LWall0,      // empty spot bottom-left (default orientation)
    LWall90,     // rotated +90 Y (empty spot top-left)
    LWall180,    // rotated +180 Y (empty spot top-right)
    LWall270,    // rotated +270 Y (empty spot bottom-right)
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
        return t != CellType.Empty;
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

        // Dropdown for selecting CellType
        activePaintType = (CellType)EditorGUILayout.EnumPopup("Active Paint Type", activePaintType);

        GUILayout.Space(5);

        // Extra buttons
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
        {
            ClearAll();
        }
        if (GUILayout.Button("Fill with Active Type"))
        {
            int width = cellMatrix.GetLength(0);
            int height = cellMatrix.GetLength(1);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    cellMatrix[x, y] = activePaintType;
        }
        GUILayout.EndHorizontal();
    }

    private static CellType DrawCell(Rect rect, CellType value)
    {
        float padding = rect.width * 0.2f;
        Rect padded = new Rect(rect.x + padding / 2, rect.y + padding / 2, rect.width - padding, rect.height - padding);

        Color c = value switch
        {
            CellType.Empty => Color.white,
            CellType.HalfWall => Color.red,
            CellType.FullWallHorizontal => new Color(0.2f, 0.2f, 1f),   // blue-ish
            CellType.FullWallVertical => new Color(0.4f, 0.4f, 1f),   // lighter blue
            CellType.LWall0 => new Color(1f, 0.6f, 0f),   // orange
            CellType.LWall90 => new Color(1f, 0.8f, 0.2f),
            CellType.LWall180 => new Color(1f, 1f, 0.4f),
            CellType.LWall270 => new Color(0.9f, 0.7f, 0.1f),
            CellType.CornerTopLeft => new Color(0f, 0.7f, 0f),
            CellType.CornerTopRight => new Color(0f, 0.9f, 0f),
            CellType.CornerBottomLeft => new Color(0f, 0.5f, 0f),
            CellType.CornerBottomRight => new Color(0f, 0.3f, 0f),
            CellType.BlendTop => Color.cyan,
            CellType.BlendBottom => Color.magenta,
            CellType.BlendLeft => Color.yellow,
            CellType.BlendRight => new Color(1f, 0.5f, 0f),
            _ => Color.gray
        };

        EditorGUI.DrawRect(padded, c);

        // Borders
        Handles.color = Color.black;
        Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.xMax, rect.y));
        Handles.DrawLine(new Vector3(rect.x, rect.yMax), new Vector3(rect.xMax, rect.yMax));
        Handles.DrawLine(new Vector3(rect.x, rect.y), new Vector3(rect.x, rect.yMax));
        Handles.DrawLine(new Vector3(rect.xMax, rect.y), new Vector3(rect.xMax, rect.yMax));

        // Paint interaction
        if ((Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag)
        && rect.Contains(Event.current.mousePosition))
        {
            if (Event.current.button == 0)
            {
                if (Event.current.shift)
                    value = CellType.Empty;
                else
                    value = activePaintType;

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



