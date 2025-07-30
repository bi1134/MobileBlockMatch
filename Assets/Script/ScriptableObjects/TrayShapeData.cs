using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ObjectData", menuName = "Scriptable Objects/ObjectData")]
public class TrayShapeData : ScriptableObject
{
    [field: SerializeField] public string Name { get; private set; }
    [field: SerializeField] public int ID { get; private set; }

    [field: SerializeField] public Vector2Int Size { get; private set; } = Vector2Int.one;

    [field: SerializeField] public List<Vector2Int> ExcludedOffsets { get; private set; } = new();

    [field: SerializeField] public ItemColorType trayColor { get; private set; } = ItemColorType.Red;
}

public enum ItemColorType
{
    Red, Blue, Green, Yellow, Purple, Orange, Pink
}

