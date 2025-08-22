using UnityEngine;

public class GameResultUI : MonoBehaviour
{
    [SerializeField] private GameObject WinPanel;
    [SerializeField] private GameObject LostPanel;

    private void OnEnable()
    {
        Show();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        if (GameManager.Instance.IsGameWin())
        {
            WinPanel.SetActive(true);
            LostPanel.SetActive(false);
        }
        else
        {
            WinPanel.SetActive(false);
            LostPanel.SetActive(true);
        }
    }

    public void ResetGameButton()
    {
        SoundEventManager.OnAnyButtonClicked?.Invoke(this, System.EventArgs.Empty);
        //SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        PlacementSystem.Instance.LoadMap();
    }

    public void ContinueButton()
    {
        SoundEventManager.OnAnyButtonClicked?.Invoke(this, System.EventArgs.Empty);
        PlacementSystem.Instance.ContinueMap();
    }

}
