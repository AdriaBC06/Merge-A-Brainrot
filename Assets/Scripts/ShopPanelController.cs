using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ShopPanelController : MonoBehaviour
{
    [SerializeField] private Button shopButton;
    [SerializeField] private GameObject shopOverlay;
    [SerializeField] private Button closeButton;
    [SerializeField] private RectTransform shopPanel;
    [SerializeField] private RectTransform itemsRoot;
    [SerializeField] private bool hideOverlayOnAwake = true;

    [Header("Visual Layer")]
    [SerializeField] private int shopCanvasSortingOrder = 120;

    [Header("Pricing")]
    [SerializeField] private int basePrice = 25;
    [SerializeField] private float priceMultiplier = 2f;

    [Header("Layout")]
    [SerializeField] private Vector2 itemSize = new Vector2(740f, 72f);
    [SerializeField] private float topPadding = 18f;
    [SerializeField] private float itemSpacing = 10f;

    private readonly List<ShopEntry> entries = new List<ShopEntry>();
    private GameManager gameManager;
    private TMP_FontAsset cachedFont;

    private Canvas hostCanvas;
    private int hostCanvasInitialSortingOrder;
    private bool hostCanvasCached;

    private ScrollRect itemsScrollRect;
    private RectTransform itemsContent;

    private sealed class ShopEntry
    {
        public int stage;
        public GameObject rootObject;
        public RectTransform rectTransform;
        public Image backgroundImage;
        public Button buyButton;
        public Image iconImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI priceText;
        public GameObject lockOverlay;
    }

    private void Awake()
    {
        AutoAssignReferences();
        CacheHostCanvas();
        EnsureScrollSetup();
        BindButtons();

        if (hideOverlayOnAwake && shopOverlay != null)
        {
            shopOverlay.SetActive(false);
        }
    }

    private void Start()
    {
        ConnectGameManager();
        RebuildEntries();
        RefreshEntries();
    }

    private void OnDestroy()
    {
        if (shopButton != null)
        {
            shopButton.onClick.RemoveListener(OpenShop);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseShop);
        }

        if (gameManager != null)
        {
            gameManager.HighestStageReachedChanged -= HandleHighestStageChanged;
        }

        RestoreCanvasOrder();
    }

    private void OnValidate()
    {
        AutoAssignReferences();
    }

    public void OpenShop()
    {
        if (shopOverlay == null) return;

        SetCanvasOrderForShop();
        shopOverlay.SetActive(true);
        shopOverlay.transform.SetAsLastSibling();

        EnsureScrollSetup();
        ConnectGameManager();
        RebuildEntries();
        RefreshEntries();

        if (itemsScrollRect != null)
        {
            itemsScrollRect.verticalNormalizedPosition = 1f;
        }
    }

    public void CloseShop()
    {
        if (shopOverlay == null) return;
        shopOverlay.SetActive(false);
        RestoreCanvasOrder();
    }

    private void BindButtons()
    {
        if (shopButton != null)
        {
            shopButton.onClick.RemoveListener(OpenShop);
            shopButton.onClick.AddListener(OpenShop);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseShop);
            closeButton.onClick.AddListener(CloseShop);
        }
    }

    private void ConnectGameManager()
    {
        GameManager resolved = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();
        if (resolved == gameManager) return;

        if (gameManager != null)
        {
            gameManager.HighestStageReachedChanged -= HandleHighestStageChanged;
        }

        gameManager = resolved;

        if (gameManager != null)
        {
            gameManager.HighestStageReachedChanged += HandleHighestStageChanged;
        }
    }

    private void HandleHighestStageChanged(int _)
    {
        RefreshEntries();
    }

    private void RebuildEntries()
    {
        if (itemsContent == null) return;

        int maxStage = gameManager != null ? gameManager.GetMaxBrainrotStage() : 1;
        maxStage = Mathf.Max(1, maxStage);

        if (entries.Count == maxStage)
        {
            return;
        }

        for (int i = itemsContent.childCount - 1; i >= 0; i--)
        {
            Transform child = itemsContent.GetChild(i);
            Destroy(child.gameObject);
        }

        entries.Clear();

        for (int stage = 1; stage <= maxStage; stage++)
        {
            entries.Add(CreateEntry(stage));
        }

        UpdateContentHeight(entries.Count);
    }

    private void RefreshEntries()
    {
        if (entries.Count == 0) return;

        ConnectGameManager();

        int highestReached = gameManager != null ? gameManager.HighestStageReached : 1;
        int visibleLimit = gameManager != null ? gameManager.GetVisibleShopStageLimit() : 1;

        int visibleIndex = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            ShopEntry entry = entries[i];
            if (entry == null || entry.rootObject == null) continue;

            int stage = entry.stage;
            bool visible = stage <= visibleLimit;
            entry.rootObject.SetActive(visible);
            if (!visible) continue;

            entry.rectTransform.anchoredPosition = new Vector2(0f, -topPadding - visibleIndex * (itemSize.y + itemSpacing));
            visibleIndex++;

            bool unlocked = stage <= highestReached;
            entry.buyButton.interactable = unlocked;
            entry.lockOverlay.SetActive(!unlocked);

            entry.titleText.text = $"Brainrot Stage {stage}";

            Sprite stageSprite = gameManager != null ? gameManager.GetBrainrotSpriteForStage(stage) : null;
            entry.iconImage.sprite = stageSprite;

            if (unlocked)
            {
                entry.priceText.text = $"Comprar: {GetStagePrice(stage)}";
                entry.backgroundImage.color = new Color(0.13f, 0.26f, 0.34f, 0.95f);
            }
            else
            {
                entry.priceText.text = "Bloqueado";
                entry.backgroundImage.color = new Color(0.15f, 0.15f, 0.15f, 0.9f);
            }
        }

        UpdateContentHeight(visibleIndex);
    }

    private void TryBuyStage(int stage)
    {
        if (gameManager == null) return;

        int price = GetStagePrice(stage);
        bool bought = gameManager.TryBuyBrainrotFromShop(stage, price);

        if (!bought)
        {
            Debug.Log($"No se pudo comprar stage {stage}. Dinero o desbloqueo insuficiente.");
        }
    }

    private int GetStagePrice(int stage)
    {
        float price = basePrice * Mathf.Pow(priceMultiplier, stage - 1);
        return Mathf.RoundToInt(price);
    }

    private ShopEntry CreateEntry(int stage)
    {
        ShopEntry entry = new ShopEntry();
        entry.stage = stage;

        entry.rootObject = new GameObject(
            $"Shop_Item_{stage}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button)
        );

        entry.rectTransform = entry.rootObject.GetComponent<RectTransform>();
        entry.rectTransform.SetParent(itemsContent, false);
        entry.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        entry.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        entry.rectTransform.pivot = new Vector2(0.5f, 1f);
        entry.rectTransform.anchoredPosition = new Vector2(0f, -topPadding - (stage - 1) * (itemSize.y + itemSpacing));
        entry.rectTransform.sizeDelta = itemSize;

        entry.backgroundImage = entry.rootObject.GetComponent<Image>();
        entry.backgroundImage.color = new Color(0.13f, 0.26f, 0.34f, 0.95f);

        entry.buyButton = entry.rootObject.GetComponent<Button>();
        entry.buyButton.targetGraphic = entry.backgroundImage;

        int stageForButton = stage;
        entry.buyButton.onClick.AddListener(() => TryBuyStage(stageForButton));

        entry.iconImage = CreateIcon(entry.rectTransform, stage);
        entry.titleText = CreateLabel(entry.rectTransform, "Title", new Vector2(130f, -16f), new Vector2(300f, 26f), 24f, FontStyles.Bold);
        entry.priceText = CreateLabel(entry.rectTransform, "Price", new Vector2(130f, -44f), new Vector2(320f, 22f), 20f, FontStyles.Normal);

        entry.lockOverlay = CreateLockOverlay(entry.rectTransform);

        return entry;
    }

    private Image CreateIcon(RectTransform parent, int stage)
    {
        GameObject iconObject = new GameObject(
            $"Icon_{stage}",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.SetParent(parent, false);
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(12f, -36f);
        iconRect.sizeDelta = new Vector2(52f, 52f);

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        return iconImage;
    }

    private TextMeshProUGUI CreateLabel(
        RectTransform parent,
        string name,
        Vector2 anchoredPosition,
        Vector2 size,
        float fontSize,
        FontStyles fontStyle)
    {
        GameObject textObject = new GameObject(
            name,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = new Vector2(0f, 1f);
        textRect.anchorMax = new Vector2(0f, 1f);
        textRect.pivot = new Vector2(0f, 1f);
        textRect.anchoredPosition = anchoredPosition;
        textRect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = GetFontAsset();
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        text.raycastTarget = false;

        return text;
    }

    private GameObject CreateLockOverlay(RectTransform parent)
    {
        GameObject overlay = new GameObject(
            "Lock_Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.SetParent(parent, false);
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.pivot = new Vector2(0.5f, 0.5f);
        overlayRect.anchoredPosition = Vector2.zero;
        overlayRect.sizeDelta = Vector2.zero;

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = new Color(0f, 0f, 0f, 0.55f);
        overlayImage.raycastTarget = false;

        TextMeshProUGUI lockText = CreateLabel(overlayRect, "Lock_Text", new Vector2(0f, -20f), new Vector2(itemSize.x, 32f), 24f, FontStyles.Bold);
        lockText.alignment = TextAlignmentOptions.Center;
        lockText.text = "BLOQUEADO";

        return overlay;
    }

    private TMP_FontAsset GetFontAsset()
    {
        if (cachedFont != null) return cachedFont;

        if (UIManager.Instance != null && UIManager.Instance.moneyText != null && UIManager.Instance.moneyText.font != null)
        {
            cachedFont = UIManager.Instance.moneyText.font;
        }
        else
        {
            cachedFont = TMP_Settings.defaultFontAsset;
        }

        return cachedFont;
    }

    private void UpdateContentHeight(int visibleCount)
    {
        if (itemsContent == null || itemsRoot == null) return;

        float contentHeight = topPadding + visibleCount * itemSize.y + Mathf.Max(0, visibleCount - 1) * itemSpacing + topPadding;
        contentHeight = Mathf.Max(contentHeight, itemsRoot.rect.height);

        itemsContent.sizeDelta = new Vector2(0f, contentHeight);
    }

    private void EnsureScrollSetup()
    {
        if (itemsRoot == null) return;

        Image viewportImage = itemsRoot.GetComponent<Image>();
        if (viewportImage == null)
        {
            viewportImage = itemsRoot.gameObject.AddComponent<Image>();
        }
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

        if (itemsRoot.GetComponent<RectMask2D>() == null)
        {
            itemsRoot.gameObject.AddComponent<RectMask2D>();
        }

        itemsScrollRect = itemsRoot.GetComponent<ScrollRect>();
        if (itemsScrollRect == null)
        {
            itemsScrollRect = itemsRoot.gameObject.AddComponent<ScrollRect>();
        }

        itemsScrollRect.horizontal = false;
        itemsScrollRect.vertical = true;
        itemsScrollRect.movementType = ScrollRect.MovementType.Clamped;
        itemsScrollRect.scrollSensitivity = 30f;
        itemsScrollRect.viewport = itemsRoot;

        if (itemsContent == null)
        {
            Transform existing = itemsRoot.Find("Scroll_Content");
            if (existing != null)
            {
                itemsContent = existing as RectTransform;
            }
        }

        if (itemsContent == null)
        {
            GameObject content = new GameObject("Scroll_Content", typeof(RectTransform));
            itemsContent = content.GetComponent<RectTransform>();
            itemsContent.SetParent(itemsRoot, false);
            itemsContent.anchorMin = new Vector2(0f, 1f);
            itemsContent.anchorMax = new Vector2(1f, 1f);
            itemsContent.pivot = new Vector2(0.5f, 1f);
            itemsContent.anchoredPosition = Vector2.zero;
            itemsContent.sizeDelta = new Vector2(0f, 0f);
        }

        itemsScrollRect.content = itemsContent;
    }

    private void CacheHostCanvas()
    {
        hostCanvas = GetComponent<Canvas>();
        if (hostCanvas == null)
        {
            hostCanvas = GetComponentInParent<Canvas>();
        }

        if (hostCanvas != null)
        {
            hostCanvasInitialSortingOrder = hostCanvas.sortingOrder;
            hostCanvasCached = true;
        }
    }

    private void SetCanvasOrderForShop()
    {
        if (hostCanvas == null)
        {
            CacheHostCanvas();
        }

        if (hostCanvas == null) return;
        if (!hostCanvasCached)
        {
            hostCanvasInitialSortingOrder = hostCanvas.sortingOrder;
            hostCanvasCached = true;
        }

        hostCanvas.overrideSorting = true;
        hostCanvas.sortingOrder = shopCanvasSortingOrder;
    }

    private void RestoreCanvasOrder()
    {
        if (hostCanvas == null || !hostCanvasCached) return;
        hostCanvas.overrideSorting = true;
        hostCanvas.sortingOrder = hostCanvasInitialSortingOrder;
    }

    private void AutoAssignReferences()
    {
        if (shopButton == null)
        {
            Transform found = transform.Find("Shop_Icon");
            if (found != null)
            {
                shopButton = found.GetComponent<Button>();
            }
        }

        if (shopOverlay == null)
        {
            Transform found = transform.Find("Shop_Overlay");
            if (found != null)
            {
                shopOverlay = found.gameObject;
            }
        }

        if (shopPanel == null && shopOverlay != null)
        {
            Transform found = shopOverlay.transform.Find("Shop_Panel");
            if (found != null)
            {
                shopPanel = found as RectTransform;
            }
        }

        if (itemsRoot == null && shopPanel != null)
        {
            Transform found = shopPanel.Find("Shop_ItemsRoot");
            if (found != null)
            {
                itemsRoot = found as RectTransform;
            }
        }

        if (closeButton == null && shopOverlay != null)
        {
            Transform found = shopOverlay.transform.Find("Shop_Panel/Close_Button");
            if (found != null)
            {
                closeButton = found.GetComponent<Button>();
            }
        }
    }
}
