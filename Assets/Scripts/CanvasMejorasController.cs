using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class CanvasMejorasController : MonoBehaviour
{
    [Header("Panel Controls")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button botonMejorasButton;
    [SerializeField] private Button cerrarButton;

    [Header("Overlay")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private int mejorasCanvasSortingOrder = 120;
    [SerializeField, Range(0f, 1f)] private float overlayDimAlpha = 0.55f;

    [Header("Layout")]
    [SerializeField] private RectTransform panelRect;
    [SerializeField] private RectTransform itemsRoot;
    [SerializeField] private Vector2 itemSize = new Vector2(740f, 72f);
    [SerializeField] private float topPadding = 18f;
    [SerializeField] private float itemSpacing = 10f;

    [Header("Auto Click Upgrade")]
    [SerializeField] private Button autoClickUpgradeButton;
    [SerializeField] private TextMeshProUGUI autoClickPriceText;
    [SerializeField] private int autoClickInitialPrice = 100;
    [SerializeField] private float autoClickPriceMultiplier = 2f;
    [SerializeField] private int autoClickMaxPurchases = 10;
    [SerializeField] private float autoClickReductionPerPurchase = 1f;

    [Header("Auto Spawn Upgrade")]
    [SerializeField] private Button autoSpawnUpgradeButton;
    [SerializeField] private TextMeshProUGUI autoSpawnPriceText;
    [SerializeField] private int autoSpawnInitialPrice = 10000;
    [SerializeField] private float autoSpawnPriceMultiplier = 2f;
    [SerializeField] private float autoSpawnInitialInterval = 10f;
    [SerializeField] private float autoSpawnReductionPerPurchase = 1f;

    [Header("Coin Multiplier Upgrade")]
    [SerializeField] private Button coinMultiplierUpgradeButton;
    [SerializeField] private TextMeshProUGUI coinMultiplierPriceText;
    [SerializeField] private int coinMultiplierInitialPrice = 100;
    [SerializeField] private float coinMultiplierPriceMultiplier = 2f;
    [SerializeField] private float coinMultiplierIncreasePerPurchase = 0.2f;
    [SerializeField] private int coinMultiplierMaxPurchases = 25;

    private int autoClickPurchases;
    private int autoClickCurrentPrice;
    private int autoSpawnCurrentPrice;
    private int coinMultiplierCurrentPrice;
    private int coinMultiplierPurchases;

    private readonly List<UpgradeEntry> entries = new List<UpgradeEntry>();

    private Canvas hostCanvas;
    private int hostCanvasInitialSortingOrder;
    private Canvas overlayCanvas;

    private ScrollRect itemsScrollRect;
    private RectTransform itemsContent;

    private Color overlayBackgroundColor = FallbackOverlayColor;
    private Color panelBackgroundColor = FallbackPanelColor;
    private Color entryBackgroundColor = FallbackEntryColor;

    private ShopPanelController shopController;

    private static readonly Color FallbackOverlayColor = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color FallbackPanelColor = new Color(0.11f, 0.15f, 0.22f, 0.98f);
    private static readonly Color FallbackEntryColor = new Color(0.13f, 0.26f, 0.34f, 0.95f);

    private enum UpgradeId
    {
        AutoClick,
        AutoSpawn,
        CoinMultiplier
    }

    private sealed class UpgradeEntry
    {
        public UpgradeId id;
        public GameObject rootObject;
        public RectTransform rectTransform;
        public Image backgroundImage;
        public Button buyButton;
        public Image iconImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI priceText;
    }

    [Serializable]
    public struct UpgradeSaveData
    {
        public int autoClickPurchases;
        public int autoClickCurrentPrice;
        public int autoSpawnCurrentPrice;
        public int coinMultiplierCurrentPrice;
        public int coinMultiplierPurchases;
    }

    private void Awake()
    {
        EnsureUpgradesIcon();
        EnsureOverlay();
        AutoAssignReferences();
        EnsureOverlayHierarchy();
        CacheHostCanvas();
        EnsurePanelLayout();
        EnsureScrollSetup();
        ApplyShopStyle();
        CacheShopController();
        InitializeUpgradeState();
        RebuildEntries();
        BindButtons();
        DisableLegacyPanel();

        if (panel != null)
        {
            SetPanelState(false);
        }
        else
        {
            Debug.LogWarning("[CanvasMejorasController] Panel reference is missing. BotonMejoras cannot toggle it.");
        }

        if (botonMejorasButton == null)
        {
            Debug.LogWarning("[CanvasMejorasController] BotonMejoras button reference is missing.");
        }

        UpdateAutoClickPriceText();
        UpdateAutoSpawnPriceText();
        UpdateCoinMultiplierPriceText();

        GameManager.Instance?.ApplySavedUpgradesIfReady();
    }

    public Button GetCloseButton()
    {
        return cerrarButton;
    }

    public Button GetOpenButton()
    {
        return botonMejorasButton;
    }

    public UpgradeSaveData GetUpgradeSaveData()
    {
        return new UpgradeSaveData
        {
            autoClickPurchases = autoClickPurchases,
            autoClickCurrentPrice = autoClickCurrentPrice,
            autoSpawnCurrentPrice = autoSpawnCurrentPrice,
            coinMultiplierCurrentPrice = coinMultiplierCurrentPrice,
            coinMultiplierPurchases = coinMultiplierPurchases
        };
    }

    public void ApplyUpgradeSaveData(UpgradeSaveData data)
    {
        autoClickPurchases = Mathf.Clamp(data.autoClickPurchases, 0, autoClickMaxPurchases);
        autoClickCurrentPrice = Mathf.Max(1, data.autoClickCurrentPrice);
        autoSpawnCurrentPrice = Mathf.Max(1, data.autoSpawnCurrentPrice);
        coinMultiplierCurrentPrice = Mathf.Max(1, data.coinMultiplierCurrentPrice);
        coinMultiplierPurchases = Mathf.Max(0, data.coinMultiplierPurchases);
        if (coinMultiplierMaxPurchases > 0)
        {
            coinMultiplierPurchases = Mathf.Min(coinMultiplierPurchases, coinMultiplierMaxPurchases);
        }

        UpdateAutoClickPriceText();
        UpdateAutoSpawnPriceText();
        UpdateCoinMultiplierPriceText();
    }

    public bool IsOpen => panel != null && panel.activeSelf;

    public void OpenPanel()
    {
        SetPanelState(true);
    }

    public void ClosePanelPublic()
    {
        SetPanelState(false);
    }

    private void InitializeUpgradeState()
    {
        autoClickCurrentPrice = Mathf.Max(1, autoClickInitialPrice);
        autoSpawnCurrentPrice = Mathf.Max(1, autoSpawnInitialPrice);
        coinMultiplierCurrentPrice = Mathf.Max(1, coinMultiplierInitialPrice);
    }

    private void AutoAssignReferences()
    {
        if (panel == null)
        {
            Transform panelTransform = null;
            if (overlay != null)
            {
                panelTransform = overlay.transform.Find("Upgrades_Panel");
                if (panelTransform == null)
                {
                    panelTransform = overlay.transform.Find("Shop_Panel");
                }
            }
            if (panelTransform == null)
            {
                panelTransform = FindChildRecursive(transform, "Upgrades_Panel");
            }
            if (panelTransform == null)
            {
                panelTransform = FindChildRecursive(transform, "Panel Mejoras");
            }
            if (panelTransform == null)
            {
                panelTransform = FindChildRecursive(transform, "Panel");
            }
            if (panelTransform != null)
            {
                panel = panelTransform.gameObject;
            }
        }

        if (panelRect == null && panel != null)
        {
            panelRect = panel.GetComponent<RectTransform>();
        }

        if (overlay == null)
        {
            Transform overlayTransform = FindTransformByName("Upgrades_Overlay");
            if (overlayTransform == null)
            {
                overlayTransform = FindChildRecursive(transform, "Upgrades_Overlay");
            }
            if (overlayTransform == null)
            {
                overlayTransform = FindChildRecursive(transform, "Mejoras_Overlay");
            }
            if (overlayTransform != null)
            {
                overlay = overlayTransform.gameObject;
            }
        }

        if (itemsRoot == null && panel != null)
        {
            Transform found = panel.transform.Find("Upgrades_ItemsRoot");
            if (found == null)
            {
                found = panel.transform.Find("Shop_ItemsRoot");
            }
            if (found == null)
            {
                found = panel.transform.Find("Mejoras_ItemsRoot");
            }
            if (found != null)
            {
                itemsRoot = found as RectTransform;
            }
        }

        if (botonMejorasButton == null)
        {
            Transform botonMejorasTransform = FindChildRecursive(transform, "Upgrades_Icon");
            if (botonMejorasTransform == null)
            {
                botonMejorasTransform = FindChildRecursive(transform, "Mejoras");
            }
            if (botonMejorasTransform == null)
            {
                botonMejorasTransform = FindChildRecursive(transform, "BotonMejoras");
            }
            if (botonMejorasTransform != null)
            {
                botonMejorasButton = botonMejorasTransform.GetComponent<Button>() ??
                                     botonMejorasTransform.GetComponentInChildren<Button>(true);
            }
        }

        if (panel == null)
        {
            return;
        }

        if (cerrarButton == null)
        {
            Transform cerrarTransform = FindChildRecursive(panel.transform, "Cerrar");
            if (cerrarTransform == null)
            {
                cerrarTransform = FindChildRecursive(panel.transform, "Close_Button");
            }
            if (cerrarTransform != null)
            {
                cerrarButton = cerrarTransform.GetComponent<Button>() ??
                               cerrarTransform.GetComponentInChildren<Button>(true);
            }
        }

        if (autoClickUpgradeButton == null)
        {
            Transform upgradeButtonTransform = panel.transform.Find("Mejora-Tiempo-Auto-Click/Button");
            if (upgradeButtonTransform != null)
            {
                autoClickUpgradeButton = upgradeButtonTransform.GetComponent<Button>();
            }
        }

        if (autoClickPriceText == null)
        {
            Transform priceTransform = panel.transform.Find("Mejora-Tiempo-Auto-Click/Price");
            if (priceTransform != null)
            {
                autoClickPriceText = priceTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (autoSpawnUpgradeButton == null)
        {
            Transform autoSpawnButtonTransform = panel.transform.Find("Mejora Auto Spawn/Button");
            if (autoSpawnButtonTransform == null)
            {
                autoSpawnButtonTransform = panel.transform.Find("Mejora-Auto-Spawn/Button");
            }
            if (autoSpawnButtonTransform != null)
            {
                autoSpawnUpgradeButton = autoSpawnButtonTransform.GetComponent<Button>();
            }
        }

        if (autoSpawnPriceText == null)
        {
            Transform autoSpawnPriceTransform = panel.transform.Find("Mejora Auto Spawn/Price");
            if (autoSpawnPriceTransform == null)
            {
                autoSpawnPriceTransform = panel.transform.Find("Mejora-Auto-Spawn/Price");
            }
            if (autoSpawnPriceTransform != null)
            {
                autoSpawnPriceText = autoSpawnPriceTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        if (coinMultiplierUpgradeButton == null)
        {
            Transform coinMultiplierButtonTransform = panel.transform.Find("Mejora Multiplicar Moneda/Button");
            if (coinMultiplierButtonTransform == null)
            {
                coinMultiplierButtonTransform = panel.transform.Find("Mejora-Multiplicar-Moneda/Button");
            }
            if (coinMultiplierButtonTransform != null)
            {
                coinMultiplierUpgradeButton = coinMultiplierButtonTransform.GetComponent<Button>();
            }
        }

        if (coinMultiplierPriceText == null)
        {
            Transform coinMultiplierPriceTransform = panel.transform.Find("Mejora Multiplicar Moneda/Price");
            if (coinMultiplierPriceTransform == null)
            {
                coinMultiplierPriceTransform = panel.transform.Find("Mejora-Multiplicar-Moneda/Price");
            }
            if (coinMultiplierPriceTransform != null)
            {
                coinMultiplierPriceText = coinMultiplierPriceTransform.GetComponent<TextMeshProUGUI>();
            }
        }
    }

    private void BindButtons()
    {
        if (botonMejorasButton != null)
        {
            botonMejorasButton.onClick.RemoveListener(TogglePanel);
            botonMejorasButton.onClick.AddListener(TogglePanel);
        }

        if (cerrarButton != null)
        {
            cerrarButton.onClick.RemoveListener(ClosePanel);
            cerrarButton.onClick.AddListener(ClosePanel);
        }

        if (autoClickUpgradeButton != null)
        {
            autoClickUpgradeButton.onClick.RemoveListener(BuyAutoClickUpgrade);
            autoClickUpgradeButton.onClick.AddListener(BuyAutoClickUpgrade);
        }

        if (autoSpawnUpgradeButton != null)
        {
            autoSpawnUpgradeButton.onClick.RemoveListener(BuyAutoSpawnUpgrade);
            autoSpawnUpgradeButton.onClick.AddListener(BuyAutoSpawnUpgrade);
        }

        if (coinMultiplierUpgradeButton != null)
        {
            coinMultiplierUpgradeButton.onClick.RemoveListener(BuyCoinMultiplierUpgrade);
            coinMultiplierUpgradeButton.onClick.AddListener(BuyCoinMultiplierUpgrade);
        }
    }

    private void CacheShopController()
    {
        if (shopController != null) return;
        shopController = FindFirstObjectByType<ShopPanelController>();
    }


    private void OnDestroy()
    {
        if (botonMejorasButton != null)
        {
            botonMejorasButton.onClick.RemoveListener(TogglePanel);
        }

        if (cerrarButton != null)
        {
            cerrarButton.onClick.RemoveListener(ClosePanel);
        }

        if (autoClickUpgradeButton != null)
        {
            autoClickUpgradeButton.onClick.RemoveListener(BuyAutoClickUpgrade);
        }

        if (autoSpawnUpgradeButton != null)
        {
            autoSpawnUpgradeButton.onClick.RemoveListener(BuyAutoSpawnUpgrade);
        }

        if (coinMultiplierUpgradeButton != null)
        {
            coinMultiplierUpgradeButton.onClick.RemoveListener(BuyCoinMultiplierUpgrade);
        }
    }

    private void TogglePanel()
    {
        if (panel == null)
        {
            return;
        }

        SoundManager.Instance?.PlayClick();
        bool opening = !panel.activeSelf;
        SetPanelState(opening);
    }

    private void ClosePanel()
    {
        SoundManager.Instance?.PlayClick();
        SetPanelState(false);
    }

    private void BuyAutoClickUpgrade()
    {
        if (autoClickPurchases >= autoClickMaxPurchases)
        {
            UpdateAutoClickPriceText();
            return;
        }

        GameManager gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        if (!gameManager.TrySpendMoney(autoClickCurrentPrice))
        {
            return;
        }

        SoundManager.Instance?.PlayPurchase();
        autoClickPurchases++;
        ClickableObject.ApplyGlobalAutoClickUpgrade(autoClickReductionPerPurchase);

        if (autoClickPurchases < autoClickMaxPurchases)
        {
            autoClickCurrentPrice = MultiplyPrice(autoClickCurrentPrice, autoClickPriceMultiplier);
        }
        else if (autoClickUpgradeButton != null)
        {
            autoClickUpgradeButton.interactable = false;
        }

        UpdateAutoClickPriceText();
    }

    private void BuyAutoSpawnUpgrade()
    {
        GameManager gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        if (gameManager.IsAutoSpawnEnabled() && gameManager.GetSpawnInterval() <= 1f)
        {
            UpdateAutoSpawnPriceText();
            return;
        }

        if (!gameManager.TrySpendMoney(autoSpawnCurrentPrice))
        {
            return;
        }

        SoundManager.Instance?.PlayPurchase();
        if (!gameManager.IsAutoSpawnEnabled())
        {
            gameManager.EnableAutoSpawn(autoSpawnInitialInterval);
        }
        else
        {
            gameManager.ReduceSpawnInterval(autoSpawnReductionPerPurchase);
        }

        if (gameManager.GetSpawnInterval() > 1f)
        {
            autoSpawnCurrentPrice = MultiplyPrice(autoSpawnCurrentPrice, autoSpawnPriceMultiplier);
        }
        else if (autoSpawnUpgradeButton != null)
        {
            autoSpawnUpgradeButton.interactable = false;
        }

        UpdateAutoSpawnPriceText();
    }

    private void BuyCoinMultiplierUpgrade()
    {
        GameManager gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            return;
        }

        if (coinMultiplierMaxPurchases > 0 && coinMultiplierPurchases >= coinMultiplierMaxPurchases)
        {
            UpdateCoinMultiplierPriceText();
            return;
        }

        if (!gameManager.TrySpendMoney(coinMultiplierCurrentPrice))
        {
            return;
        }

        SoundManager.Instance?.PlayPurchase();
        float scaledIncrease = coinMultiplierIncreasePerPurchase / (1f + coinMultiplierPurchases * 0.2f);
        ClickableObject.IncreaseGlobalMoneyMultiplier(scaledIncrease);
        coinMultiplierPurchases++;
        if (coinMultiplierMaxPurchases <= 0 || coinMultiplierPurchases < coinMultiplierMaxPurchases)
        {
            coinMultiplierCurrentPrice = MultiplyPrice(coinMultiplierCurrentPrice, coinMultiplierPriceMultiplier);
        }
        UpdateCoinMultiplierPriceText();
    }

    private void UpdateAutoClickPriceText()
    {
        if (autoClickPriceText == null)
        {
            return;
        }

        if (autoClickPurchases >= autoClickMaxPurchases)
        {
            autoClickPriceText.text = "Precio: MAX";
            if (autoClickUpgradeButton != null)
            {
                autoClickUpgradeButton.interactable = false;
            }
            return;
        }

        if (autoClickUpgradeButton != null)
        {
            autoClickUpgradeButton.interactable = true;
        }
        autoClickPriceText.text = $"Precio: {autoClickCurrentPrice}$";
    }

    private void UpdateAutoSpawnPriceText()
    {
        if (autoSpawnPriceText == null)
        {
            return;
        }

        GameManager gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
        if (gameManager != null && gameManager.IsAutoSpawnEnabled() && gameManager.GetSpawnInterval() <= 1f)
        {
            autoSpawnPriceText.text = "Precio: MAX";
            if (autoSpawnUpgradeButton != null)
            {
                autoSpawnUpgradeButton.interactable = false;
            }
            return;
        }

        if (autoSpawnUpgradeButton != null)
        {
            autoSpawnUpgradeButton.interactable = true;
        }
        autoSpawnPriceText.text = $"Precio: {autoSpawnCurrentPrice}$";
    }

    private void UpdateCoinMultiplierPriceText()
    {
        if (coinMultiplierPriceText == null)
        {
            return;
        }

        if (coinMultiplierMaxPurchases > 0 && coinMultiplierPurchases >= coinMultiplierMaxPurchases)
        {
            coinMultiplierPriceText.text = "Precio: MAX";
            if (coinMultiplierUpgradeButton != null)
            {
                coinMultiplierUpgradeButton.interactable = false;
            }
            return;
        }

        if (coinMultiplierUpgradeButton != null)
        {
            coinMultiplierUpgradeButton.interactable = true;
        }
        coinMultiplierPriceText.text = $"Precio: {coinMultiplierCurrentPrice}$";
    }

    private int MultiplyPrice(int currentPrice, float multiplier)
    {
        float safeMultiplier = Mathf.Max(1f, multiplier);
        return Mathf.Max(1, Mathf.CeilToInt(currentPrice * safeMultiplier));
    }
}
