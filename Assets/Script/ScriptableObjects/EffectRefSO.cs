using UnityEngine;

[CreateAssetMenu(fileName = "EffectRefSO", menuName = "Scriptable Objects/EffectRefSO")]
public class EffectRefSO : ScriptableObject
{
    [Sirenix.OdinInspector.Title("Combo arrays")]
    public GameObject[] combo;

    [Sirenix.OdinInspector.Title("Box Destroy")]
    public GameObject[] boxDestroy;

    [Sirenix.OdinInspector.Title("Tray Finised")]
    public GameObject[] trayFinised;
}
