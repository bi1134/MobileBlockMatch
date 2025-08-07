using System.Collections.Generic;
using UnityEngine;

public class DirectionalTray : Tray
{
    [SerializeField] private Transform verticalArrows;
    [SerializeField] private Transform horizontalArrows;
    [SerializeField] public MovementAxis allowedDirection;

    protected override void Start()
    {
        base.Start();
    }

    public void SetAxis(MovementAxis axis)
    {
        allowedDirection = axis;

        if (verticalArrows != null)
            verticalArrows.gameObject.SetActive(axis == MovementAxis.Vertical);

        if (horizontalArrows != null)
            horizontalArrows.gameObject.SetActive(axis == MovementAxis.Horizontal);
    }

    public HashSet<Vector3Int> GetInnerRayCells(int maxDistance = 5)
    {
        HashSet<Vector3Int> inner = new();

        var basePos = GetGridPosition();
        var shape = GetShapeData();
        Vector2Int size = shape.Size;

        if (allowedDirection == MovementAxis.Horizontal)
        {
            int centerZ = basePos.z + size.y / 2;

            for (int dx = -maxDistance; dx <= maxDistance; dx++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Vector3Int pos = new(basePos.x + dx + x, 0, centerZ);
                    inner.Add(pos);
                }
            }
        }
        else if (allowedDirection == MovementAxis.Vertical)
        {
            int centerX = basePos.x + size.x / 2;

            for (int dz = -maxDistance; dz <= maxDistance; dz++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    Vector3Int pos = new(centerX, 0, basePos.z + dz + z);
                    inner.Add(pos);
                }
            }
        }

        return inner;
    }

    public HashSet<Vector3Int> GetOuterRayCells(int maxDistance = 5)
    {
        HashSet<Vector3Int> outer = new();

        var basePos = GetGridPosition();
        var shape = GetShapeData();
        Vector2Int size = shape.Size;

        if (allowedDirection == MovementAxis.Horizontal)
        {
            // Top and bottom lines (offset ±1)
            int topZ = basePos.z + size.y + 0; // top row
            int bottomZ = basePos.z - 1;       // bottom row

            for (int dx = -maxDistance; dx <= maxDistance; dx++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    outer.Add(new Vector3Int(basePos.x + dx + x, 0, topZ));
                    outer.Add(new Vector3Int(basePos.x + dx + x, 0, bottomZ));
                }
            }
        }
        else if (allowedDirection == MovementAxis.Vertical)
        {
            // Left and right lines (offset ±1)
            int leftX = basePos.x - 1;
            int rightX = basePos.x + size.x;

            for (int dz = -maxDistance; dz <= maxDistance; dz++)
            {
                for (int z = 0; z < size.y; z++)
                {
                    outer.Add(new Vector3Int(leftX, 0, basePos.z + dz + z));
                    outer.Add(new Vector3Int(rightX, 0, basePos.z + dz + z));
                }
            }
        }

        return outer;
    }

    public override bool CanMoveInDirection(Vector3Int direction)
    {
        if (allowedDirection == MovementAxis.Horizontal && direction.z != 0)
            return false;
        if (allowedDirection == MovementAxis.Vertical && direction.x != 0)
            return false;

        return base.CanMoveInDirection(direction);
    }
}

public enum MovementAxis { Vertical, Horizontal }
