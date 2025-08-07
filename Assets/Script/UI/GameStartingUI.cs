using UnityEngine;
using UnityEngine.UI;

public class GameStartingUI : MonoBehaviour
{

    private void Start()
    {
        Show();
    }

    public void OnStartButtonClicked()
    {
        GameManager.Instance.SetGameState(GameState.Playing);
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
