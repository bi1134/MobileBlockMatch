using DG.Tweening;
using UnityEngine;

public class ResultPanel : MonoBehaviour
{
    [SerializeField] private GameObject ribbonResult;
    [SerializeField] private GameObject resultText;
    [SerializeField] private GameObject reactEmoji;
    [SerializeField] private GameObject resultButton;

    private Vector3 originalScale;

    private void Awake()
    {
        originalScale = Vector3.one;

        // Start hidden
        ribbonResult.transform.localScale = Vector3.zero;
        resultText.transform.localScale = Vector3.zero;
        reactEmoji.transform.localScale = Vector3.zero;
        resultButton.transform.localScale = Vector3.zero; 
    }

    private void OnEnable()
    {
        Show(); 
    }

    private void Show()
    {
        Sequence seq = DOTween.Sequence();

        //ribbon and button
        seq.Insert(0f, PopJump(ribbonResult));
        seq.Insert(0f, PopShow(resultButton));

        //emoji and text jump 0.2f after ribbon
        seq.Insert(0.2f, PopShow(reactEmoji));
        seq.Insert(0.2f, PopShow(resultText));

        seq.OnComplete(() =>
        {
            // Start idle animation for emoji
            StartEmojiIdle();
        });
    }

    private Tween PopShow(GameObject go)
    {
        // Reset to smaller scale
        go.transform.localScale = originalScale * 0.75f;

        // Start tween for THIS object only (don’t overwrite a global tween)
        return go.transform
            .DOScale(originalScale, 0.4f)
            .SetEase(Ease.OutBack);
    }

    private Tween PopJump(GameObject go)
    {
        go.transform.localScale = originalScale * 0.75f;

        // Scale + Jump combined
        return go.transform.DOScale(originalScale, 0.4f)
            .SetEase(Ease.OutBack)
            .OnStart(() =>
            {
                // small upward movement (UI anchored movement)
                var rect = go.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.DOAnchorPosY(rect.anchoredPosition.y + 20f, 0.25f)
                        .SetLoops(2, LoopType.Yoyo) // up then back down
                        .SetEase(Ease.OutQuad);
                }
            });
    }

    private void StartEmojiIdle()
    {
        reactEmoji.transform.DOLocalMoveY(
            reactEmoji.transform.localPosition.y + 10f,
            1.5f
        )
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo)  // infinite up/down
        .SetDelay(3f); // wait 3 sec before first cycle
    }
}
