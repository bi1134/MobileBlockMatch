using UnityEngine;
using DG.Tweening;
using System.Collections;
using System;

public class TrayVisual : MonoBehaviour
{
    [SerializeField] public Vector3 originalLocalOffset = new Vector3(0.5f, 0f, 0.5f);
    [SerializeField] private Transform trayVisual;
    private Vector3 startUpOffset => originalLocalOffset + pickUpOffset; // Offset for the initial position of the tray
    private Vector3 pickUpOffset = new Vector3(0f, 0.3f, 0f);
    private Tween moveTween;
    public bool IsDropFinished { get; private set; } = false;
    public bool IsDestroyFinished { get; private set; } = false;


    private void Awake()
    {
        originalLocalOffset = transform.localPosition;
    }

    private void Start()
    {
        ShakeScale();
        transform.localPosition = startUpOffset;
        StartCoroutine(DelayedDrop());
    }

    private IEnumerator DelayedDrop()
    {
        yield return new WaitForSeconds(0.1f);
        IsDropFinished = false;
        yield return this.transform.DOLocalMove(originalLocalOffset, 0.25f)
            .SetEase(Ease.InBounce)
            .WaitForCompletion();
        IsDropFinished = true;
    }

    public void DestroyGoesUp(Action onComplete = null)
    {
        ShakeScale();

        Sequence seq = DOTween.Sequence();

        Vector3 originalScale = transform.localScale;
        seq.timeScale = 1.5f;

        // main movement (go up)
        seq.Append(transform.DOMoveZ(transform.position.z + 1f, 1f)
            .SetEase(Ease.InOutBack));
        seq.Join(transform.DOMoveY(1.25f, 1f)
            .SetEase(Ease.InOutBack));

        //stretch up make the rubber-y feel
        seq.Insert(0.125f, transform.DOScaleZ(2f, 0.25f).SetEase(Ease.OutQuad, 1f, 0.2f));
        seq.Join(transform.DOScaleX(0.5f, 0.25f).SetEase(Ease.OutQuad, 1f, 0.2f));

        //snap back
        seq.Insert(0.3f, transform.DOScaleZ(originalScale.z, 0.25f).SetEase(Ease.InQuart));
        seq.Join(transform.DOScaleX(originalScale.x, 0.25f).SetEase(Ease.InQuart));

        //set to 0 before the seq end
        float disappearTime = 0.6f; //start disappearing at 60% of move duration
        seq.Insert(disappearTime, transform.DOScale(Vector3.zero, 0.25f)
            .SetEase(Ease.InBack));
        seq.OnComplete(() => onComplete?.Invoke());
    }

    public void SnapToOffset()
    {
        transform.localPosition = originalLocalOffset;
    }

    public void ShakeScale()
    {
        this.transform.DOShakeScale(.5f, 1, 10, 90).SetEase(Ease.InOutElastic);
    }

    // Reset drop flag if tray is picked up again
    public void PlayPickUpAnimation()
    {
        IsDropFinished = false;
        moveTween?.Kill();
        Vector3 lifted = originalLocalOffset + pickUpOffset;
        moveTween = transform.DOLocalMove(lifted, 0.25f).SetEase(Ease.OutQuad);
    }

    public Tween PlayDropAnimationSnap(Vector3 worldTarget)
    {
        moveTween?.Kill(); // kill any active animation
        Vector3 finalPos = worldTarget + originalLocalOffset;
        moveTween = transform.DOMove(finalPos, .2f).SetEase(Ease.OutBounce);
        return moveTween;
    }

    public void ResetVisual()
    {
        this.transform.position = Vector3.zero;
        this.transform.DOScale(Vector3.one, 1);
    }
}
