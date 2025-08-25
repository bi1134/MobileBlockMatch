using DG.Tweening;
using UnityEngine;

public class GameStartingUI : MonoBehaviour
{
    [SerializeField] private GameObject startButton;

    private Vector3 originalScale;
    private Tween showTween;

    private void Awake()
    {
        originalScale = startButton.transform.localScale;
        startButton.transform.localScale = Vector3.zero; // start hidden
    }

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

        // Reset to smaller scale
        startButton.transform.localScale = originalScale * 0.75f;

        // Kill if something’s running
        showTween?.Kill();

        // Main panel pop
        showTween = startButton.transform
            .DOScale(originalScale, 0.4f)
            .SetEase(Ease.OutBack);
    }

}
