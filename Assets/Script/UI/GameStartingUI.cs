using UnityEngine;

public class GameStartingUI : MonoBehaviour
{
    private void OnEnable()
    {
        Show();
    }

    public void OnStartButtonClicked()
    {
        SoundEventManager.OnAnyButtonClicked?.Invoke(this, System.EventArgs.Empty);
        GameManager.Instance.SetGameState(GameState.Starting);
        Hide();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

}
