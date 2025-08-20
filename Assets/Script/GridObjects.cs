using System.Collections.Generic;
using UnityEngine;

public class GridObjects : MonoBehaviour
{
    [SerializeField] protected Vector2Int size = Vector2Int.one;
    [SerializeField] public Vector3Int currentGridPos;
    public Vector3Int GetGridPosition() => currentGridPos;
    protected PlacementSystem placementSystem;

    protected virtual void Start()
    {
        placementSystem = PlacementSystem.Instance;

        currentGridPos = placementSystem.grid.WorldToCell(transform.position);
        transform.position = placementSystem.grid.CellToWorld(currentGridPos);
        RegisterSelf();
    }

    public List<Vector3Int> GetOccupiedCells(Vector3Int origin)
    {
        List<Vector3Int> occupied = new();

        for (int x = 0; x < size.x; x++)
        {
            for (int z = 0; z < size.y; z++)
            {
                Vector2Int offset = new Vector2Int(x, z);

                //check if child is Tray and has excluded offsets
                if (this is Tray tray && tray.HasExcludedOffset(offset))
                    continue;

                occupied.Add(origin + new Vector3Int(x, 0, z));
            }
        }

        return occupied;
    }


    protected void RegisterSelf()
    {
        foreach (var cell in GetOccupiedCells(currentGridPos))
            placementSystem.RegisterGridObject(cell, this);
    }

    protected void UnregisterSelf()
    {
        foreach (var cell in GetOccupiedCells(currentGridPos))
            placementSystem.UnregisterGridObject(cell);
    }
}
