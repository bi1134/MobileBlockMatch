public static class TrayDataUtils
{
    public static TrayPlacementData ToPlacementData(this DirectionalTrayData data) => new TrayPlacementData
    {
        trayPrefab = data.trayPrefab,
        position = data.position,
        uniqueID = data.uniqueID
    };

    public static TrayPlacementData ToPlacementData(this BlockedTrayData data) => new TrayPlacementData
    {
        trayPrefab = data.trayPrefab,
        position = data.position,
        uniqueID = data.uniqueID
    };
}