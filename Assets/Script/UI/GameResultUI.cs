using DG.Tweening;
using UnityEngine;

public class GameResultUI : MonoBehaviour
{
    [SerializeField] private GameObject gameResultPanel;

    [SerializeField] private GameObject WinPanel;
    [SerializeField] private GameObject LostPanel;

    private Vector3 originalScale;
    private Tween showTween;
    private Tween hideTween;

    private void Awake()
    {
        originalScale = gameResultPanel.transform.localScale;
        gameResultPanel.transform.localScale = Vector3.zero; // start hidden
    }

    public void Hide()
    {
        // Kill previous tweens
        hideTween?.Kill();

        // Reverse pop (slightly smaller before vanish)
        hideTween = gameResultPanel.transform
            .DOScale(originalScale * 0.75f, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                gameObject.SetActive(false); // finally hide
            });
    }

    public void Show()
    {
        gameObject.SetActive(true);

        // Reset to smaller scale
        gameResultPanel.transform.localScale = originalScale * 0.75f;

        // Kill if something’s running
        showTween?.Kill();

        // Main panel pop
        showTween = gameResultPanel.transform
            .DOScale(originalScale, 0.4f)
            .SetEase(Ease.OutBack);

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
