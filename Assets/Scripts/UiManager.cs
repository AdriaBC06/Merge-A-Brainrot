using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI moneyText;

    private void Awake() { Instance = this; }

    public void UpdateMoney(float money)
    {
        moneyText.text = $"${money:F0}";
    }
}