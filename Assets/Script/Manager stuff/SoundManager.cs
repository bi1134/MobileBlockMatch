using Unity.Burst.Intrinsics;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioRefClip audioClipRefsSO;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioSource audioSourceFixed;

    private void OnEnable()
    {
        AssignSignal();
    }

    #region Tray sound
    private void Tray_OnAnyTrayPickup(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trayPickup);
    }
    
    private void Tray_OnAnyTrayDrop(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trayDrop);
    }

    private void TraySpawner_OnSpawnTray(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.traySpawn);
    }

    private void Tray_OnAnyTrayFinished(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trayFinished);
    }

    private void Tray_OnAnyTrayGoesOut(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trayGoesOut);
    }

    private void Tray_OnTryPickupBlocked(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trayBlocked);
    }

    #endregion

    #region Food Sound
    private void Tray_OnAnyFoodSwap(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.itemMove);
    }
    #endregion

    #region UI sound
    private void Button_OnAnyButtonClicked(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.buttonClick);
    }
    #endregion

    #region Game Sound
    private void Game_OnResultSound(object sender, System.EventArgs e)
    {
        if(GameManager.Instance.IsGameEnd())
        {
            if(GameManager.Instance.IsGameWin())
            {
                PlaySoundFixed(audioClipRefsSO.gameResultWin[0]);
            }
            else
            {
                PlaySoundFixed(audioClipRefsSO.gameResultLose[0]);
            }
        }
    }

    private void Game_OnComboChanged(object sender, int combo)
    {
        if (combo <= 1) return; // no sound for single trays

        // Example: choose clip by combo level
        int index = Mathf.Clamp(combo - 2, 0, audioClipRefsSO.gameCombo.Length - 1);
        PlaySoundFixed(audioClipRefsSO.gameCombo[index]);
    }

    #endregion

    private void PlaySound(AudioClip[] audioClipArray, float volume = 1f)
    {
        PlaySound(audioClipArray[Random.Range(0, audioClipArray.Length)], volume);
    }

    private void PlaySound(AudioClip audioClip, float volume = 1f, bool randomizePitch = true, float minRange = 0.95f, float maxRange = 1.08f)
    {
        if (audioClip == null) return;

        audioSource.pitch = randomizePitch ? Random.Range(minRange, maxRange) : 1f;
        audioSource.PlayOneShot(audioClip, volume);
    }

 
    private void PlaySoundFixed(AudioClip[] audioClipArray, int index, float volume = 1f)
    {
        if (audioClipArray == null || audioClipArray.Length == 0) return;
        index = Mathf.Clamp(index, 0, audioClipArray.Length - 1);
        PlaySoundFixed(audioClipArray[index], volume);
    }

    private void PlaySoundFixed(AudioClip audioClip, float volume = 1f)
    {
        if (audioClip == null) return;
        audioSourceFixed.pitch = 1f;
        audioSourceFixed.PlayOneShot(audioClip, volume);
    }


    //prob not gonna happened
    private void OnDisable()
    {
        ResetSignal();
    }

    private void AssignSignal()
    {
        //tray
        SoundEventManager.OnAnyTrayPickupPlay += Tray_OnAnyTrayPickup;
        SoundEventManager.OnAnyTrayDropPlay += Tray_OnAnyTrayDrop;
        SoundEventManager.OnAnyTraySpawner += TraySpawner_OnSpawnTray;
        GameEventManager.OnTrayFinished += Tray_OnAnyTrayFinished;
        GameEventManager.OnTrayGoesOut += Tray_OnAnyTrayGoesOut;
        SoundEventManager.OnTryPickupBlocked += Tray_OnTryPickupBlocked;

        //food
        SoundEventManager.OnAnyFoodSwap += Tray_OnAnyFoodSwap;

        //button
        SoundEventManager.OnAnyButtonClicked += Button_OnAnyButtonClicked;

        //Game ChangeState
        GameEventManager.OnGameStateChanged += Game_OnResultSound;

        //Game Combo
        GameEventManager.OnComboChanged += Game_OnComboChanged;
    }

    private void ResetSignal()
    {
        //tray 
        SoundEventManager.OnAnyTrayPickupPlay -= Tray_OnAnyTrayPickup;
        SoundEventManager.OnAnyTrayDropPlay -= Tray_OnAnyTrayDrop;
        SoundEventManager.OnAnyTraySpawner -= TraySpawner_OnSpawnTray;
        GameEventManager.OnTrayFinished -= Tray_OnAnyTrayFinished;
        GameEventManager.OnTrayGoesOut -= Tray_OnAnyTrayGoesOut;
        SoundEventManager.OnTryPickupBlocked -= Tray_OnTryPickupBlocked;
        
        //food
        SoundEventManager.OnAnyFoodSwap -= Tray_OnAnyFoodSwap;
        
        //ui button
        SoundEventManager.OnAnyButtonClicked -= Button_OnAnyButtonClicked;
        
        //game
        GameEventManager.OnGameStateChanged -= Game_OnResultSound;
        GameEventManager.OnComboChanged -= Game_OnComboChanged;
    }
}
