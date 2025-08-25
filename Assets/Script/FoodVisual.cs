using DG.Tweening;
using System.Collections;
using UnityEngine;

public class FoodVisual : MonoBehaviour
{
    [SerializeField] private Vector3 originalLocalOffset = new Vector3(0.5f, 0f, 0.5f);
    private Vector3 startUpOffset => originalLocalOffset + pickUpOffset; // Offset for the initial position of the tray
    private Vector3 pickUpOffset = new Vector3(0f, 0.25f, 0f);
    private Tween moveTween;

    private void Awake()
    {
        originalLocalOffset = transform.localPosition;
        transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
    }

    private void Start()
    {
        transform.localPosition = startUpOffset;
        StartCoroutine(DelayedDrop());
    }

    private IEnumerator DelayedDrop()
    {
        yield return new WaitForSeconds(0.1f); // small delay to let the scene render
        Drop();
    }

    public void Drop()
    {
        Sequence seq = DOTween.Sequence();
        
        seq.Append(this.transform.DOScale(Vector3.one, 0.15f).SetEase(Ease.OutBack));
        seq.Append(transform.DOLocalMove(originalLocalOffset, 0.25f)
            .SetEase(Ease.OutBounce));
    }
}
