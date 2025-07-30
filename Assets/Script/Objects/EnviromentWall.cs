using UnityEngine;

public class EnviromentWall : GridObjects
{
    [SerializeField] public Transform visual;

    public void SetGridPosition(Vector3Int gridPos)
    {
        placementSystem = PlacementSystem.Instance;
        currentGridPos = gridPos;
        RegisterSelf();
    }

}
