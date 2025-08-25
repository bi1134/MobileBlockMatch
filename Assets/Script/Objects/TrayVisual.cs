using UnityEngine;
using DG.Tweening;
using System.Collections;
using System;

public class TrayVisual : MonoBehaviour
{
    [SerializeField] public Vector3 originalLocalOffset = new Vector3(0.5f, 0f, 0.5f);
    [SerializeField] private Transform trayVisual;
    private Vector3 startUpOffset => originalLocalOffset + pickUpOffset; // Offset for the initial position of the tray
    private Vector3 pickUpOffset = new Vector3(0f, 1f, 0f);
    private Tween moveTween;
    public bool IsDropFinished { get; private set; } = false;
    public bool IsDestroyFinished { get; private set; } = false;

    public Vector3Int spawnDirection = Vector3Int.forward; // Default spawn direction, can be set externally
    private SpawnMode mode = SpawnMode.Normal;
    private string currentLayer = "Tray";
    private string outlineLayer = "Outline"; // Layer for outline effects
    private void Awake()
    {
        originalLocalOffset = transform.localPosition;
        transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }

    private void Start()
    {
        if (mode == SpawnMode.Normal)
        {
            transform.localPosition = startUpOffset;
        }
        else if (mode == SpawnMode.FromSpawner)
        {
            transform.localScale =new Vector3(1, 1, 0.1f) * 0.5f; //start up as flat z axis
            transform.localPosition = originalLocalOffset -(Vector3)spawnDirection * 0.65f + Vector3.up * 0.5f;
        }
        StartCoroutine(DelayedDrop());
    }

    public IEnumerator DelayedDrop()
    {
        yield return new WaitForSeconds(0.1f);
        IsDropFinished = false;

        Sequence seq = DOTween.Sequence();

        if (mode == SpawnMode.Normal)
        {
            seq.Append(transform.DOLocalMove(originalLocalOffset, 0.25f)
            .SetEase(Ease.InBounce));
            seq.Join(transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
        }
        else if (mode == SpawnMode.FromSpawner)
        {
            Vector3 aboveTarget = originalLocalOffset + Vector3.up * 0.5f;
            seq.Append(transform.DOLocalMove(originalLocalOffset, 0.15f).SetEase(Ease.OutExpo));
            seq.Append(transform.DOLocalMove(aboveTarget, 0.25f).SetEase(Ease.OutCubic));
            seq.Append(transform.DOLocalMove(originalLocalOffset, 0.25f)
            .SetEase(Ease.InBounce));
            seq.Join(transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack));
        }

        yield return seq.WaitForCompletion();
        IsDropFinished = true;
    }

    public void SetSpawnDirection(Vector3Int gridDirection)
    {
        spawnDirection = gridDirection;
        mode = SpawnMode.FromSpawner;
    }

    public void DestroyGoesUp(Action onComplete = null)
    {
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

        //calling event at idk 90% of the seq consider seq is 1 sec long
        seq.InsertCallback(0.9f, () =>
        {
            GameEventManager.OnTrayGoesOut?.Invoke(this, EventArgs.Empty);
        });

        seq.OnComplete(() => onComplete?.Invoke());
    }

    public void SnapToOffset()
    {
        transform.localPosition = originalLocalOffset;
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

    public void IsShowOutLine(bool show = false)
    {
        if(show)
        {
            trayVisual.gameObject.layer = LayerMask.NameToLayer(outlineLayer);
        }
        else
        {
            trayVisual.gameObject.layer = LayerMask.NameToLayer(currentLayer);
        }
    }

    public void ResetVisual()
    {
        this.transform.position = Vector3.zero;
        this.transform.DOScale(Vector3.one, 1);
    }
}

public enum SpawnMode
{
    Normal,
    FromSpawner
}
