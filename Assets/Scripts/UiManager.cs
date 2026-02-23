using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public TextMeshProUGUI moneyText;

    [Header("World Background")]
    [SerializeField] private Sprite screen1BackgroundSprite;
    [SerializeField] private Sprite screen2BackgroundSprite;

    [Header("Canvas Ordering")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private int canvasSortingOrder = -10;

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private Image worldBackgroundImage;
    private Button homeButton;
    private GameObject homeNotificationDot;

    private void Awake()
    {
        Instance = this;
        EnsureCanvasRendersBehindSprites();
        EnsureBackgroundDefaults();
        EnsureHomeButtonBinding();
        HideHomeNotificationDot();
    }

    private void EnsureBackgroundDefaults()
    {
        if (worldBackgroundImage == null)
        {
            GameObject background = GameObject.Find("Background");
            if (background != null)
            {
                worldBackgroundImage = background.GetComponent<Image>();
            }
        }

        if (worldBackgroundImage != null && screen1BackgroundSprite == null)
        {
            screen1BackgroundSprite = worldBackgroundImage.sprite;
        }
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

        mainCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        mainCanvas.worldCamera = Camera.main;
        mainCanvas.overrideSorting = true;
        mainCanvas.sortingOrder = canvasSortingOrder;
    }

    private void EnsureHomeButtonBinding()
    {
        if (homeButton == null)
        {
            GameObject homeIcon = GameObject.Find("Home_Icon");
            if (homeIcon != null)
            {
                homeButton = homeIcon.GetComponent<Button>();
                if (homeButton == null)
                {
                    homeButton = homeIcon.GetComponentInChildren<Button>(true);
                }
            }
        }

        if (homeButton == null) return;
        homeButton.onClick.RemoveListener(OnHomePress);
        homeButton.onClick.AddListener(OnHomePress);
    }

    private void HideHomeNotificationDot()
    {
        if (homeNotificationDot == null)
        {
            homeNotificationDot = GameObject.Find("Notifcation_Symbol");
            if (homeNotificationDot == null)
            {
                homeNotificationDot = GameObject.Find("Notification_Symbol");
            }
        }

        if (homeNotificationDot != null && homeNotificationDot.activeSelf)
        {
            homeNotificationDot.SetActive(false);
        }
    }

    public void UpdateMoney(float money)
    {
        moneyText.text = $"${money:F0}";
    }

    public void SetWorldBackground(bool showingScreen1)
    {
        if (worldBackgroundImage == null)
        {
            EnsureBackgroundDefaults();
        }
        if (worldBackgroundImage == null) return;

        Sprite target = showingScreen1 ? screen1BackgroundSprite : screen2BackgroundSprite;
        if (target == null) return;

        worldBackgroundImage.sprite = target;
    }

    public void OnStartPress()
    {
        SceneManager.LoadScene(2);
    }

    public void OnSettingsPress()
    {
        SettingsManager.SettingsOpen();
    }

    public void OnHomePress()
    {
        if (Application.CanStreamedLevelBeLoaded(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
            return;
        }

        Debug.LogWarning($"Main menu scene '{mainMenuSceneName}' is not in Build Settings.");
    }
}
