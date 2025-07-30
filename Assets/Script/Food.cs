using UnityEngine;

public class Food : MonoBehaviour
{
    #region References and Variables
    [SerializeField] private FoodData foodData;
    public ItemColorType Color => foodData.color;

    private Vector3 initialLocalOffset;

    #endregion

    #region Startups
    private void Awake()
    {
    }

    public void Initialize(FoodData data)
    {
        foodData = data;
    }
    #endregion

    #region Food Logic

    #endregion
}
