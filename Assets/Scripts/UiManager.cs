using TMPro;
using System.Globalization;
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
    [SerializeField] private string startButtonObjectName = "Start_Button";

    private Image worldBackgroundImage;
    private Button homeButton;
    private GameObject homeNotificationDot;
    private TextMeshProUGUI startButtonText;
    private Button quitButton;

    private void Awake()
    {
        Instance = this;
        EnsureCanvasRendersBehindSprites();
        EnsureBackgroundDefaults();
        EnsureHomeButtonBinding();
        EnsureQuitButtonBinding();
        HideHomeNotificationDot();
        UpdateStartButtonLabel();
        if (GameManager.Instance != null)
        {
            UpdateMoney(GameManager.Instance.currentMoney);
        }
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

    private void EnsureQuitButtonBinding()
    {
        if (quitButton == null)
        {
            GameObject quitObject = GameObject.Find("Quit_Button");
            if (quitObject != null)
            {
                quitButton = quitObject.GetComponent<Button>();
                if (quitButton == null)
                {
                    quitButton = quitObject.GetComponentInChildren<Button>(true);
                }
            }
        }

        if (quitButton == null) return;
        quitButton.onClick.RemoveListener(OnQuitPress);
        quitButton.onClick.AddListener(OnQuitPress);
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
        if (moneyText == null)
        {
            return;
        }

        moneyText.text = $"${FormatCompactMoney(money)}";
    }

    private static string FormatCompactMoney(float money)
    {
        double value = Mathf.Max(0f, money);
        if (value < 1000d)
        {
            return Mathf.FloorToInt((float)value).ToString(CultureInfo.InvariantCulture);
        }

        string[] suffixes = { "K", "M", "B", "T", "Qa", "Qi", "Sx", "Sp", "Oc", "No", "Dc" };
        int suffixIndex = -1;

        while (value >= 1000d && suffixIndex < suffixes.Length - 1)
        {
            value /= 1000d;
            suffixIndex++;
        }

        bool isWhole = Mathf.Approximately((float)(value % 1d), 0f);
        string numberPart = value >= 100d || isWhole
            ? value.ToString("0", CultureInfo.InvariantCulture)
            : value.ToString("0.00", CultureInfo.InvariantCulture);

        return $"{numberPart}{suffixes[suffixIndex]}";
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
        SoundManager.Instance?.PlayClick();
        SceneManager.LoadScene(2);
    }

    public void OnSettingsPress()
    {
        SoundManager.Instance?.PlayClick();
        GameManager.Instance?.RequestSave();
        SettingsManager.SettingsOpen();
    }

    public void OnHomePress()
    {
        SoundManager.Instance?.PlayClick();
        GameManager.Instance?.RequestSave();
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

    public void OnQuitPress()
    {
        SoundManager.Instance?.PlayClick();
        GameManager.Instance?.RequestSave();
        PlayerPrefs.Save();

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void UpdateStartButtonLabel()
    {
        if (startButtonText == null)
        {
            GameObject startButton = GameObject.Find(startButtonObjectName);
            if (startButton != null)
            {
                startButtonText = startButton.GetComponentInChildren<TextMeshProUGUI>(true);
            }
        }

        if (startButtonText == null)
        {
            return;
        }

        bool hasSave = PlayerPrefs.HasKey(GameManager.SaveKey) &&
            !string.IsNullOrEmpty(PlayerPrefs.GetString(GameManager.SaveKey));
        startButtonText.text = hasSave ? "CONTINUE" : "START";
    }
}
