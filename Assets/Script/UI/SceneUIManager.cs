using System;
using System.Collections;
using UnityEngine;

public class SceneUIManager : MonoBehaviour
{
    [SerializeField] private GameObject gameStartingUI;
    [SerializeField] private GameObject gamePlayingProcessUI;
    [SerializeField] private GameResultUI gameResultUI;
    [SerializeField] private GameSettingUI gameSettingUI;
    [SerializeField] private GameObject particleUI;

    private void Awake()
    {
        GameEventManager.OnGameStateChanged += OnGameStateChanged;
        GameEventManager.OnLastTray += ShowParticle;
    }

    private void OnGameStateChanged(object sender, EventArgs e)
    {
        particleUI.SetActive(false);
        switch (GameManager.Instance.CurrentState)
        {
            case GameState.WaitingToStart:
                gameStartingUI.SetActive(true);
                gameResultUI.gameObject.SetActive(false);
                gameSettingUI.gameObject.SetActive(false);
                // gamePlayingProcessUI stays active, but maybe dim it if needed
                break;
            case GameState.Starting:
                gamePlayingProcessUI.SetActive(true); // stays visible
                break;
            case GameState.Playing:
                gameStartingUI.SetActive(false);
                gameResultUI.Hide();
                gameSettingUI.gameObject.SetActive(false);
                break;

            case GameState.Win:
            case GameState.GameOver:
                SoundEventManager.OnGameResultShow?.Invoke(this, EventArgs.Empty);
                gameResultUI.Show();
                // gamePlayingProcessUI still stays active here
                break;

            case GameState.Pausing:
                gameSettingUI.Show();
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
        gameResultUI.gameObject.SetActive(false);
        gameSettingUI.gameObject.SetActive(false);
    }

    private void ShowParticle(object sender, EventArgs e)
    {
        particleUI.SetActive(true);
    }

    public void ShowSettingUI()
    {
        SoundEventManager.OnAnyButtonClicked?.Invoke(this, EventArgs.Empty);
        GameManager.Instance.SetGameState(GameState.Pausing);
    }

    public void HideSettingUI()
    {
        SoundEventManager.OnAnyButtonClicked?.Invoke(this, EventArgs.Empty);
        GameManager.Instance.ResumeFromPause();
        gameSettingUI.Hide();
    }

    private void OnDestroy()
    {
        GameEventManager.OnGameStateChanged -= OnGameStateChanged;
        GameEventManager.OnLastTray -= ShowParticle;
    }
}
