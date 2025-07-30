using UnityEngine;
using DG.Tweening;
using System.Collections;

public class TrayVisual : MonoBehaviour
{
    [SerializeField] public Vector3 originalLocalOffset = new Vector3(0.5f, 0f, 0.5f);
    [SerializeField] private Transform trayVisual;
    private Vector3 startUpOffset => originalLocalOffset + pickUpOffset; // Offset for the initial position of the tray
    private Vector3 pickUpOffset = new Vector3(0f, 0.5f, 0f);
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

    private IEnumerator DelayedDestroy()
    {
        IsDestroyFinished = false;
        yield return this.transform.DOShakeScale(.5f, 3, 10, 90).SetEase(Ease.InOutElastic)
            .WaitForCompletion();
        IsDestroyFinished = true;
    }

    public void FollowMouse(Vector3 worldPos)
    {
        transform.position = worldPos;
    }

    public void Drop()
    {
        this.transform.DOLocalMove(originalLocalOffset, 0.25f)
            .SetEase(Ease.InBounce);
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

    public void PlayDropAnimation()
    {
        moveTween?.Kill(); // kill any active animation
        moveTween = transform.DOLocalMove(originalLocalOffset, 0.25f).SetEase(Ease.OutBounce);
    }
}
