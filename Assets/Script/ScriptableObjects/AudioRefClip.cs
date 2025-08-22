using UnityEngine;

[CreateAssetMenu(fileName = "AudioRefClip", menuName = "Scriptable Objects/AudioRefClip")]
public class AudioRefClip : ScriptableObject
{
    [Sirenix.OdinInspector.Title("Tray Sounds")]
    public AudioClip[] trayPickup;
    public AudioClip[] trayDrop;
    public AudioClip[] traySpawn;
    public AudioClip[] trayFinished;
    public AudioClip[] trayGoesOut;
    public AudioClip[] trayBlocked;


    [Sirenix.OdinInspector.Title("Items sounds")]
    public AudioClip[] itemMove;

    [Sirenix.OdinInspector.Title("UI sounds")]
    public AudioClip[] buttonClick;

    [Sirenix.OdinInspector.Title("Game Result")]
    public AudioClip[] gameResultWin;
    public AudioClip[] gameResultLose;
    public AudioClip[] gameCombo;

}
