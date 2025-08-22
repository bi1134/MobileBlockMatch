using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayProgressUI : MonoBehaviour
{
    [SerializeField] private Image moveCountImage;
    [SerializeField] private Image moveCountImageRed;
    [SerializeField] private TextMeshProUGUI moveCount;

    private void OnEnable()
    {
        AssignSignal();
    }

    private void OnDisable()
    {
        ResetSignal();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void OnProgressChanged(object sender, GameEventManager.OnProgressChangedEventArgs e)
    {
        moveCount.text = e.moveCount.ToString();
        moveCountImage.fillAmount = e.progressNormalized;
        moveCountImageRed.fillAmount = e.progressNormalized;
        if(moveCountImage.fillAmount == (float)25%1)
        {
            moveCountImage.color = new Color(0,0,0,0); //set alpha to 0
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
