using DG.Tweening;
using UnityEngine;

public class AnimationTest : MonoBehaviour
{
    private Vector3 originalPos;
    private Vector3 originalSize;

    private void Awake()
    {
        originalPos = this.transform.position;
        originalSize = new Vector3(0.1f, .1f, .1f);
    }


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            //ShakeScale();
            GoesUpDissapear();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetVisual();
        }
    }

    public void ShakeScale()
    {
        this.transform.DOShakeScale(.5f, 1, 10, 90).SetEase(Ease.InOutElastic);
    }

    public void GoesUpDissapear()
    {
        transform.DOScale(Vector3.one, .5f).SetEase(Ease.OutBack);

        transform.DOMoveY(2, 5)
            .SetEase(Ease.Linear);

        transform.DOMoveZ(2, 5)
            .SetEase(Ease.Linear);
    }

    public void ResetVisual()
    {
        this.transform.position = originalPos;
        this.transform.localScale = originalSize;
    }

}
