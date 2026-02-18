using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI moneyText;
    [Header("Canvas Ordering")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private int canvasSortingOrder = 10;

    private void Awake()
    {
        Instance = this;
        EnsureCanvasRendersBehindSprites();
    }

    private void EnsureCanvasRendersBehindSprites()
    {
        if (!mainCanvas)
        {
            mainCanvas = GetComponentInParent<Canvas>();
        }
        if (!mainCanvas)
        {
            mainCanvas = FindFirstObjectByType<Canvas>();
        }
        if (!mainCanvas)
        {
            return;
        }

        // Keep UI visible by default and only assign camera mode if a main camera exists.
        if (Camera.main != null)
        {
            mainCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            mainCanvas.worldCamera = Camera.main;
        }
        else
        {
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }
        mainCanvas.overrideSorting = true;
        mainCanvas.sortingOrder = canvasSortingOrder;
    }

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
