using UnityEngine;

public class DirectionalTray : Tray
{
    public enum MovementAxis { Vertical, Horizontal}
    [SerializeField] private Transform verticalArrows;
    [SerializeField] private Transform horizontalArrows;

    [SerializeField] public MovementAxis allowedDirection;

    protected override void Start()
    {
        base.Start();
        // Set the visibility of the arrows based on the allowed direction
        verticalArrows.gameObject.SetActive(allowedDirection == MovementAxis.Vertical);
        horizontalArrows.gameObject.SetActive(allowedDirection == MovementAxis.Horizontal);
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
