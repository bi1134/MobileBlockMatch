using System.Collections.Generic;
using UnityEngine;

public class TrayMovementHelper
{
    private readonly Grid grid;

    public TrayMovementHelper(Grid grid)
    {
        this.grid = grid;
    }

    public (int left, int right, int up, int down) CalculateTrayMoveLimit(Tray tray, Vector3Int originCell)
    {
        int left = GetReachableCells(tray, originCell, Vector3Int.left).Count;
        int right = GetReachableCells(tray, originCell, Vector3Int.right).Count;
        int up = GetReachableCells(tray, originCell, Vector3Int.forward).Count;
        int down = GetReachableCells(tray, originCell, Vector3Int.back).Count;

        return (left, right, up, down);
    }

    private HashSet<Vector3Int> GetReachableCells(Tray tray, Vector3Int start, Vector3Int dir)
    {
        var reachable = new HashSet<Vector3Int>();
        Vector3Int next = start + dir;

        while (PlacementSystem.Instance.IsCellInBounds(next))
        {
            if (!PlacementSystem.Instance.CanTrayFitAtCell(tray, next))
                break;

            reachable.Add(next);
            next += dir;
        }
        return reachable;
    }

    public Vector3 ApplyAxisConstraint(Tray tray, Vector3 desired)
    {
        if (tray is DirectionalTray directional)
        {
            Vector3 current = tray.transform.position;
            if (directional.allowedDirection == MovementAxis.Horizontal)
                desired.z = current.z;
            else
                desired.x = current.x;
        }
        return desired;
    }

    public bool CanOccupyContinuous(Tray tray, Vector3 worldPos)
    {
        Vector3Int snapped = PlacementSystem.Instance.grid.WorldToCell(worldPos);

        foreach (var cell in tray.GetOccupiedCells(snapped))
        {
            if (!PlacementSystem.Instance.IsCellInBounds(cell))
                return false;

            if (!PlacementSystem.Instance.IsCellAvailable(cell, tray))
                return false;
        }

        return true;
    }
}
