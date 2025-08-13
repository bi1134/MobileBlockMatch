using System.Collections.Generic;
using UnityEngine;


public class WinLoseChecker
{
    private readonly List<DirectionalTray> activeDirectionalTrays = new List<DirectionalTray>();

    public bool IsLose(int currentMovecount, List<Tray> currentActiveTrays)
    {
        //if no move left = lose
        if(currentMovecount <= 0)
        {
            Debug.Log("out of move");
            return true;
        }

        return NonReachablePairs(currentActiveTrays);
    }

    public bool IsWin(int totalTrayCounts)
    {
        return totalTrayCounts <= 0;
    }

    private bool NonReachablePairs(List<Tray> currentActiveTrays)
    {
        activeDirectionalTrays.Clear();

        foreach (var tray in currentActiveTrays)
        {
            if (tray is DirectionalTray dir && dir.IsUnlocked())
                activeDirectionalTrays.Add(dir);
        }

        // If there are non-directional trays still active, it's not a loss yet
        if (currentActiveTrays.Count > activeDirectionalTrays.Count)
            return false;

        foreach (var tray in activeDirectionalTrays)
        {
            var trayColor = tray.GetShapeData().trayColor;
            var trayAxis = tray.allowedDirection;
            var trayInner = tray.GetInnerRayCells();
            var trayOuter = tray.GetOuterRayCells();

            foreach (var other in activeDirectionalTrays)
            {
                if (tray == other) continue;
                if (!other.IsUnlocked()) continue;

                var otherColor = other.GetShapeData().trayColor;
                if (trayColor == otherColor) continue;

                var otherOccupied = other.GetOccupiedCells(other.GetGridPosition());

                if (trayAxis == other.allowedDirection)
                {
                    if (trayOuter.Overlaps(otherOccupied))
                        return false;
                }
                else
                {
                    if (trayInner.Overlaps(otherOccupied))
                        return false;

                    if (trayInner.Overlaps(other.GetInnerRayCells()))
                        return false;
                }
            }
        }

        Debug.Log("Lose: No tray can reach another tray of a different color.");
        return true;
    }
}
