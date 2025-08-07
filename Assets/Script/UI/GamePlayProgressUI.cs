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
        GameManager.Instance.OnStateChanged += OnStateChanged;
    }

    private void Start()
    {
        Hide();
    }

    private void OnStateChanged(object sender, System.EventArgs e)
    {
        if(GameManager.Instance.CurrentState == GameState.Playing)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

   

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }


    private void OnProgressChanged(object sender, GameManager.OnProgressChangedEventArgs e)
    {
        moveCount.text = e.moveCount.ToString();
        moveCountImage.fillAmount = e.progressNormalized;
    }
}
