using System;
using UnityEngine;

public class SceneUIManager : MonoBehaviour
{
    [SerializeField] private GameObject gameStartingUI;
    [SerializeField] private GameObject gamePlayingProcessUI;
    [SerializeField] private GameObject gameResultUI;
    [SerializeField] private GameObject gameSettingUI;

    private void Awake()
    {
        GameEventManager.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnGameStateChanged(object sender, EventArgs e)
    {
        switch (GameManager.Instance.CurrentState)
        {
            case GameState.WaitingToStart:
                gameStartingUI.SetActive(true);
                gameResultUI.SetActive(false);
                // gamePlayingProcessUI stays active, but maybe dim it if needed
                break;
            case GameState.Playing:
                gameStartingUI.SetActive(false);
                gameResultUI.SetActive(false);
                gamePlayingProcessUI.SetActive(true); // stays visible
                break;

            case GameState.GameOver:
            case GameState.Win:
                gameResultUI.SetActive(true);
                // gamePlayingProcessUI still stays active here
                break;

            default:
                HideAll();
                break;
        }

        print("change State " + GameManager.Instance.CurrentState);
    }

    private void HideAll()
    {
        gameStartingUI.SetActive(false);
        gameResultUI.SetActive(false);
    }

    public void ShowSettingUI()
    {
        gameSettingUI.SetActive(true);
    }

    public void HideSettingUI()
    {
        gameSettingUI.SetActive(false);
    }

    private void OnDestroy()
    {
        GameEventManager.OnGameStateChanged -= OnGameStateChanged;
    }
}
