using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "GridMapData", menuName = "Scriptable Objects/GridMapData")]
public class GridMapData : ScriptableObject
{
    [field: SerializeField] public Vector2Int GridSize { get; private set; } = new(5, 5);
    [field: SerializeField] public Vector3Int GridOrigin { get; private set; } = Vector3Int.zero;
    [field: SerializeField] public int MaxMoveCount { get; private set; } = 10;

    //blocked cells (walls, environment, etc)
    [field: SerializeField] public List<Vector3Int> BlockedCells { get; private set; } = new();

    //for trays placement
    [field: SerializeField] public List<TrayPlacementData> Trays { get; private set; } = new();

    [field: SerializeField] public List<DirectionalTrayData> DirectionalTrays { get; private set; } = new();
    [field: SerializeField] public List<BlockedTrayData> BlockedTrays { get; private set; } = new();

    //for tray spawners placement
    [field: SerializeField] public List<TraySpawnerPlacementData> Spawners { get; private set; } = new();

    //for food themes
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



