using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField] private AudioRefClip audioClipRefsSO;
    [SerializeField] private AudioSource audioSource;


    private void OnEnable()
    {
        AssignSignal();
    }

    #region tray sound
    private void Tray_OnAnyTrayPickup(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trayPickup);
    }
    
    private void Tray_OnAnyTrayDrop(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trayDrop);
    }

    private void Tray_OnAnyTrayFinished(object sender, System.EventArgs e)
    {
        PlaySound(audioClipRefsSO.trayFinished);
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

    private void PlaySound(AudioClip[] audioClipArray, float volume = 1f)
    {
        PlaySound(audioClipArray[Random.Range(0, audioClipArray.Length)], volume);
    }
  
    private void PlaySound(AudioClip audioClip, float volume = 1f)
    {
        audioSource.pitch = Random.Range(0.95f, 1.08f);
        audioSource.PlayOneShot(audioClip, volume);
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
        GameEventManager.OnTrayFinished += Tray_OnAnyTrayFinished;

        //food
        SoundEventManager.OnAnyFoodSwap += Tray_OnAnyFoodSwap;

        //button
        SoundEventManager.OnAnyButtonClicked += Button_OnAnyButtonClicked;
    }


    private void ResetSignal()
    {
        SoundEventManager.OnAnyTrayPickupPlay -= Tray_OnAnyTrayPickup;
        SoundEventManager.OnAnyTrayDropPlay -= Tray_OnAnyTrayDrop;
        GameEventManager.OnTrayFinished -= Tray_OnAnyTrayFinished;
        SoundEventManager.OnAnyFoodSwap -= Tray_OnAnyFoodSwap;
        SoundEventManager.OnAnyButtonClicked -= Button_OnAnyButtonClicked;
    }


}
