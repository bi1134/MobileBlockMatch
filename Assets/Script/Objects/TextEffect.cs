using DG.Tweening;
using UnityEngine;

public class TextEffect : MonoBehaviour
{
    private Vector3 originalSetScale;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        originalSetScale = new Vector3(0.1f, 0.1f, 0.1f);
        
        ResetVisual();
        ShowsAndGoesUp();
    }

    private void ShowsAndGoesUp()
    {
        transform.DOScale(Vector3.one, .5f).SetEase(Ease.OutBack);

        transform.DOMoveY(1.5f, 0.6f)
            .SetEase(Ease.OutExpo);

        transform.DOMoveZ(transform.position.z + 2, 1)
            .SetEase(Ease.OutExpo).OnComplete(() => Destroy(transform.parent.gameObject));

        spriteRenderer.DOFade(0, 3)
            .SetDelay(1f);
    }

    private void ResetVisual()
    {
        transform.localScale = originalSetScale;
    }
}
