using UnityEngine;

[CreateAssetMenu(fileName = "EffectRefSO", menuName = "Scriptable Objects/EffectRefSO")]
public class EffectRefSO : ScriptableObject
{
    [Sirenix.OdinInspector.Title("Combo arrays")]
    public GameObject[] combo;
}
