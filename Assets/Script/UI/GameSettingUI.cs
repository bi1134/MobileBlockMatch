using DG.Tweening;
using UnityEngine;

public class GameSettingUI : MonoBehaviour
{
    [SerializeField] private GameObject settingPanel;

    [SerializeField] private GameObject musicSetting;
    [SerializeField] private GameObject soundSetting;
    [SerializeField] private GameObject hapticSetting;

    private Vector3 originalScale;
    private Tween showTween;
    private Tween hideTween;

    private void Awake()
    {
        originalScale = settingPanel.transform.localScale;
        settingPanel.transform.localScale = Vector3.zero; // start hidden
    }

    public void Show()
    {
        gameObject.SetActive(true);

        // Reset to smaller scale
        settingPanel.transform.localScale = originalScale * 0.75f;

        // Kill if something’s running
        showTween?.Kill();

        // Main panel pop
        showTween = settingPanel.transform
            .DOScale(originalScale, 0.4f)
            .SetEase(Ease.OutBack);

        // Children pop with overlap (staggered)
        float childDelay = 0.1f;
        PopChild(musicSetting.transform, 0f);
        PopChild(soundSetting.transform, childDelay);
        PopChild(hapticSetting.transform, childDelay * 2);
    }

    public void Hide()
    {
        // Kill previous tweens
        hideTween?.Kill();

        // Reverse pop (slightly smaller before vanish)
        hideTween = settingPanel.transform
            .DOScale(originalScale * 0.75f, 0.25f)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                gameObject.SetActive(false); // finally hide
            });
    }

    private void PopChild(Transform child, float delay)
    {
        child.localScale = Vector3.zero;
        child.DOScale(Vector3.one, 0.35f)
             .SetEase(Ease.OutBack)
             .SetDelay(delay);
    }
    
}
