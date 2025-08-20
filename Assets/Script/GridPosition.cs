
using System.Runtime.CompilerServices;
using UnityEngine;

public readonly record struct GridPosition(int RowIndex, int ColumnIndex)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator GridPosition(Vector2Int pos)
    {
        return new GridPosition(pos.y, pos.x);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector2Int(GridPosition pos)
    {
        return new Vector2Int(pos.ColumnIndex, pos.RowIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator Vector3Int(GridPosition pos)
    {
        return new Vector3Int(pos.ColumnIndex, 0, pos.RowIndex);
    }
}
