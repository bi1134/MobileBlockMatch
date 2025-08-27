using UnityEngine;

[CreateAssetMenu(fileName = "WallPrefabSet", menuName = "Scriptable Objects/WallPrefabSet")]
public class WallPrefabSet : ScriptableObject
{
    public GameObject halfWallPrefab;
    public GameObject fullWallPrefabHorizontal;
    public GameObject fullWallPrefabVertical;
    public GameObject cornerWallPrefab;
    public GameObject blendTopPrefab;
    public GameObject blendBottomPrefab;
    public GameObject blendLeftPrefab;
    public GameObject blendRightPrefab;
    public GameObject lWallPrefab;

    public (GameObject prefab, Quaternion rot, Vector3 offset) GetPrefab(CellType type, Vector3Int worldCell, Vector3Int gridOrigin)
    {
        switch (type)
        {
            case CellType.HalfWall:
            case CellType.HalfWallTop:
                return (halfWallPrefab, Quaternion.identity, new Vector3(0.5f, 0f, 0.25f)); // top/north

            case CellType.HalfWallBottom:
                return (halfWallPrefab, Quaternion.identity, new Vector3(0.5f, 0f, 0.75f)); // bottom/south

            case CellType.HalfWallLeft:
                return (halfWallPrefab, Quaternion.Euler(0, 90f, 0), new Vector3(0.75f, 0f, 0.5f)); // west

            case CellType.HalfWallRight:
                return (halfWallPrefab, Quaternion.Euler(0, 90f, 0), new Vector3(0.25f, 0f, 0.5f)); // east

            case CellType.FullWallHorizontal:
                return (fullWallPrefabHorizontal, Quaternion.identity, Vector3.zero);

            case CellType.FullWallVertical:
                return (fullWallPrefabVertical, Quaternion.identity, Vector3.zero);

            case CellType.LWall0:
                return (lWallPrefab, Quaternion.identity, new Vector3(0.5f, 0, 0.5f));
            case CellType.LWall90:
                return (lWallPrefab, Quaternion.Euler(0, 90f, 0), new Vector3(0.5f, 0, 0.5f));
            case CellType.LWall180:
                return (lWallPrefab, Quaternion.Euler(0, 180f, 0), new Vector3(0.5f, 0, 0.5f));
            case CellType.LWall270:
                return (lWallPrefab, Quaternion.Euler(0, 270f, 0), new Vector3(0.5f, 0, 0.5f));

            case CellType.CornerTopLeft:
                return (cornerWallPrefab, Quaternion.Euler(0, 180f, 0), new Vector3(0.75f, 0f, 0.25f));
            case CellType.CornerTopRight:
                return (cornerWallPrefab, Quaternion.Euler(0, -90f, 0), new Vector3(0.25f, 0f, 0.25f));
            case CellType.CornerBottomLeft:
                return (cornerWallPrefab, Quaternion.Euler(0, 90f, 0), new Vector3(0.75f, 0f, 0.75f));
            case CellType.CornerBottomRight:
                return (cornerWallPrefab, Quaternion.identity, new Vector3(0.25f, 0f, 0.75f));

            case CellType.BlendTop: return (blendTopPrefab, Quaternion.identity, Vector3.zero);
            case CellType.BlendBottom: return (blendBottomPrefab, Quaternion.identity, Vector3.zero);
            case CellType.BlendLeft: return (blendLeftPrefab, Quaternion.identity, Vector3.zero);
            case CellType.BlendRight: return (blendRightPrefab, Quaternion.identity, Vector3.zero);

            default: return (null, Quaternion.identity, Vector3.zero);
        }
    }

}
