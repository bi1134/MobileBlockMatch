using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayProgressUI : MonoBehaviour
{
    [SerializeField] private Image moveCountImage;
    [SerializeField] private TextMeshProUGUI moveCount;

    private void Awake()
    {
        GameManager.Instance.OnProgressChanged += OnProgressChanged;
    }

    private void OnProgressChanged(object sender, GameManager.OnProgressChangedEventArgs e)
    {
        moveCount.text = e.moveCount.ToString();
        moveCountImage.fillAmount = e.progressNormalized;
    }
}
