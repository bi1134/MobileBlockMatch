using UnityEngine;

public class GameResultUI : MonoBehaviour
{
    [SerializeField] private GameObject WinPanel;
    [SerializeField] private GameObject LostPanel;


    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        Hide();
    }

    private void GameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.IsGameEnd())
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
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
}
