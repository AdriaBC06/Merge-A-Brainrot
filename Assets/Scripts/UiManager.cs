using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI moneyText;

    private void Awake() { Instance = this; }

    public void UpdateMoney(float money)
    {
        moneyText.text = $"${money:F0}";
    }

    public void OnStartPress()
    {
        SceneManager.LoadScene(2);
    }

    public void OnSettingsPress()
    {
        SettingsManager.SettingsOpen();
    }
}