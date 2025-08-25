using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayProgressUI : MonoBehaviour
{
    [SerializeField] private Image gameProgressImage;
    [SerializeField] private Image gameProgressImageRed;
    [SerializeField] private TextMeshProUGUI moveCount;

    private bool isVisible = true;
    private int displayedMoveCount = 0;
    private Vector3 moveCountOriginalScale = Vector3.one;
    private float shakeScale = 0.5f;

    //tween
    private Tween progressTween;
    private Tween progressFillTween;
    private Tween moveCountColorTween;

    private void OnEnable()
    {
        AssignSignal();

        var color = gameProgressImage.color;
        gameProgressImage.color = new Color(color.r, color.g, color.b, 0f);

        color = gameProgressImageRed.color;
        gameProgressImageRed.color = new Color(color.r, color.g, color.b, 0f);

        var textColor = moveCount.color;
        moveCount.color = new Color(textColor.r, textColor.g, textColor.b, 0f);

        // Kill any leftover tweens just in case
        gameProgressImage.DOKill();
        gameProgressImageRed.DOKill();
        moveCount.DOKill();

        // Fade them in
        gameProgressImage.DOFade(1f, 0.6f).SetEase(Ease.OutCubic);
        gameProgressImageRed.DOFade(1f, 0.6f).SetEase(Ease.OutCubic);
        moveCount.DOFade(1f, 0.6f).SetEase(Ease.OutCubic);

        GameEventManager.VisualProgressChanged(
            this,
            GameManager.Instance.currentMoveCount / (float)GameManager.Instance.maxMoveCount,
            GameManager.Instance.currentMoveCount
        );
    }

    private void OnDisable()
    {
        ResetSignal();
    }

    private void OnProgressChanged(object sender, GameEventManager.OnProgressChangedEventArgs e)
    {
        moveCount.text = e.moveCount.ToString();
        
        //kill prev tween if still running
        progressFillTween?.Kill();

        progressFillTween = DOTween.To(
            () => gameProgressImage.fillAmount,
            x => {
                gameProgressImage.fillAmount = x;
                gameProgressImageRed.fillAmount = x;
            },
            e.progressNormalized,
            0.35f // duration of smoothing
        ).SetEase(Ease.OutCubic);

        int target = e.moveCount;
        moveCount.DOCounter(
            displayedMoveCount,  // start value
            target,              // end value
            Mathf.Abs(target - displayedMoveCount) * 0.025f, // duration scales with difference
            true                 // whole numbers
        ).SetEase(Ease.OutExpo);
        displayedMoveCount = target;

        moveCount.transform.DOKill(); // kill any ongoing scale tweens

        moveCount.transform.localScale = moveCountOriginalScale;// reset in case it was distorted

        moveCount.transform
        .DOShakeScale(0.85f, shakeScale) // squash
        .SetEase(Ease.OutBounce).OnComplete(() => moveCount.transform.localScale = moveCountOriginalScale);

        if (e.progressNormalized <= 0.27f) //should be lower than 25% but i put 27 for fun for idk faster warning
        {
            moveCountColorTween = moveCount.DOColor(Color.red, 0.2f)
                .OnComplete(() =>
                {
                    moveCountColorTween = moveCount.DOColor(Color.white, 0.1f);
                });


            if (isVisible == true)
            {
                shakeScale = 1f;
                isVisible = false;
                progressTween = gameProgressImage
                .DOFade(0f, 0.3f) // fade out first
                .SetLoops(7, LoopType.Yoyo) // some how looping 7 making it look like blink 4 times so i'll go with it
                .OnComplete(() =>
                {
                    gameProgressImage.color = new Color(0, 0, 0, 0); // force invisible at the end
                });
            }
        }
        else
        {
            gameProgressImage.color = Color.white; //set to default white
            isVisible = true; //set to true so it can be shown again
            shakeScale = 0.5f; // reset shake scale to default
        }
    }

    private void ResetSignal()
    {
        GameEventManager.OnProgressChanged -= OnProgressChanged;
    }

    private void AssignSignal()
    {
        GameEventManager.OnProgressChanged += OnProgressChanged;
    }
}
