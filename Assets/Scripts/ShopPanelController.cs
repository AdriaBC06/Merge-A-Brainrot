using System;
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
    [SerializeField, Range(0f, 1f)] private float overlayDimAlpha = 0.55f;

    [Header("Visual Layer")]
    [SerializeField] private int shopCanvasSortingOrder = 120;

    [Header("Pricing")]
    [SerializeField] private int basePrice = 25;
    [SerializeField] private int priceIncrement = 10;
    [SerializeField] private float purchasePriceGrowthFactor = 1.2f;

    [Header("Layout")]
    [SerializeField] private Vector2 itemSize = new Vector2(740f, 72f);
    [SerializeField] private float topPadding = 18f;
    [SerializeField] private float itemSpacing = 10f;

    private readonly List<ShopEntry> entries = new List<ShopEntry>();
    private GameManager gameManager;
    private TMP_FontAsset cachedFont;

    private Canvas hostCanvas;
    private int hostCanvasInitialSortingOrder;
    private Canvas overlayCanvas;

    private ScrollRect itemsScrollRect;
    private RectTransform itemsContent;

    public bool IsOpen
    {
        get
        {
            if (shopPanel != null)
            {
                return shopPanel.gameObject.activeSelf;
            }

            return shopOverlay != null && shopOverlay.activeSelf;
        }
    }

    [Header("Mejoras Layer")]
    [SerializeField] private Canvas mejorasCanvas;

    private int mejorasCanvasInitialSortingOrder;

    private Button mejorasOpenButton;
    private Transform shopIconTransform;
    private Transform mejorasIconTransform;

    private readonly List<Button> homeButtons = new List<Button>();
    private readonly Dictionary<Image, Color> originalIconColors = new Dictionary<Image, Color>();

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
        public TextMeshProUGUI countText;
        public GameObject lockOverlay;
    }

    private void Awake()
    {
        AutoAssignReferences();
        CacheHostCanvas();
        CacheMejorasCanvas();
        CacheMejorasOpenButton();
        CacheHomeButtons();
        CacheIconTransforms();
        EnsureScrollSetup();
        BindButtons();
        if (hideOverlayOnAwake && shopOverlay != null)
        {
            shopOverlay.SetActive(false);
        }

        if (hideOverlayOnAwake && shopPanel != null)
        {
            shopPanel.gameObject.SetActive(false);
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
            shopButton.onClick.RemoveListener(OnShopButtonPressed);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonPressed);
        }

        if (homeButtons.Count > 0)
        {
            foreach (Button button in homeButtons)
            {
                if (button == null) continue;
                button.onClick.RemoveListener(OnCloseButtonPressed);
            }
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

    private void OnShopButtonPressed()
    {
        SoundManager.Instance?.PlayClick();
        OpenShop();
    }

    private void OnCloseButtonPressed()
    {
        SoundManager.Instance?.PlayClick();
        CloseShop();
    }

    public void OpenShop()
    {
        if (shopOverlay == null) return;

        if (IsOpen)
        {
            CloseShop();
            return;
        }

        CanvasMejorasController mejorasController = FindMejorasController();
        if (mejorasController != null && mejorasController.IsOpen)
        {
            mejorasController.ClosePanelPublic();
        }

        ApplyMejorasCloseStyle();
        SetMejorasCanvasBelowShop();
        SetCanvasOrderForShop();
        ApplyOverlayAlpha();
        shopOverlay.SetActive(true);
        if (shopPanel != null)
        {
            shopPanel.gameObject.SetActive(true);
        }
        EnsureIconAboveOverlay();
        SetIconsDimmed(true, keepShopIconNormal: true, keepUpgradesIconNormal: false);
        BringIconsToFront();

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
        if (shopPanel != null)
        {
            shopPanel.gameObject.SetActive(false);
        }
        bool keepOverlay = false;
        CanvasMejorasController mejorasController = FindMejorasController();
        if (mejorasController != null && mejorasController.IsOpen)
        {
            keepOverlay = true;
        }
        shopOverlay.SetActive(keepOverlay);
        RestoreCanvasOrder();
        RestoreMejorasCanvasOrder();

        bool keepAbove = mejorasController != null && mejorasController.IsOpen;
        if (keepAbove)
        {
            SetIconsDimmed(true, keepShopIconNormal: false, keepUpgradesIconNormal: true);
            BringIconsToFront();
        }
        else
        {
            SetIconsDimmed(false, keepShopIconNormal: false, keepUpgradesIconNormal: false);
        }
    }

    private void BindButtons()
    {
        if (shopButton != null)
        {
            shopButton.onClick.RemoveListener(OnShopButtonPressed);
            shopButton.onClick.AddListener(OnShopButtonPressed);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(OnCloseButtonPressed);
            closeButton.onClick.AddListener(OnCloseButtonPressed);
        }

        if (homeButtons.Count > 0)
        {
            foreach (Button button in homeButtons)
            {
                if (button == null) continue;
                button.onClick.RemoveListener(OnCloseButtonPressed);
                button.onClick.AddListener(OnCloseButtonPressed);
            }
        }
    }

    private void OnDisable()
    {
        // no-op
    }

    private void EnsureIconAboveOverlay()
    {
        if (shopOverlay == null)
        {
            return;
        }

        shopOverlay.transform.SetAsLastSibling();
        BringIconsToFront();
    }

    private void CacheMejorasCanvas()
    {
        if (mejorasCanvas != null) return;

        CanvasMejorasController controller = FindMejorasController();
        if (controller == null) return;

        Canvas found = controller.GetComponent<Canvas>();
        if (found == null)
        {
            found = controller.GetComponentInParent<Canvas>();
        }

        if (found == null) return;

        mejorasCanvas = found;
        mejorasCanvasInitialSortingOrder = found.sortingOrder;
    }

    private void CacheMejorasOpenButton()
    {
        if (mejorasOpenButton != null) return;

        CanvasMejorasController controller = FindMejorasController();
        if (controller == null) return;

        Button openButton = controller.GetOpenButton();
        if (openButton == null) return;

        mejorasOpenButton = openButton;
    }

    private void CacheIconTransforms()
    {
        if (shopButton != null)
        {
            shopIconTransform = shopButton.transform;
        }

        if (mejorasOpenButton != null)
        {
            mejorasIconTransform = mejorasOpenButton.transform;
        }
    }

    public void BringIconsToFront()
    {
        CacheIconTransforms();

        if (shopIconTransform != null)
        {
            shopIconTransform.SetAsLastSibling();
        }

        if (mejorasIconTransform != null)
        {
            mejorasIconTransform.SetAsLastSibling();
        }

        if (homeButtons.Count > 0)
        {
            foreach (Button button in homeButtons)
            {
                if (button == null) continue;
                button.transform.SetAsLastSibling();
            }
        }
    }

    public void SetIconsDimmed(bool dim, bool keepShopIconNormal, bool keepUpgradesIconNormal)
    {
        CacheIconTransforms();
        CacheHomeButtons();
        EnsureIconCanvasOrder(shopCanvasSortingOrder + 1);

        float dimFactor = 0.35f;
        if (shopOverlay != null)
        {
            Image overlayImage = shopOverlay.GetComponent<Image>();
            if (overlayImage != null)
            {
                dimFactor = Mathf.Clamp01(1f - overlayImage.color.a);
            }
        }

        if (shopIconTransform != null)
        {
            SetImageDim(shopIconTransform.GetComponent<Image>(), dim && !keepShopIconNormal, dimFactor);
        }

        if (mejorasIconTransform != null)
        {
            SetImageDim(mejorasIconTransform.GetComponent<Image>(), dim && !keepUpgradesIconNormal, dimFactor);
        }

        if (homeButtons.Count > 0)
        {
            foreach (Button button in homeButtons)
            {
                if (button == null) continue;
                SetImageDim(button.GetComponent<Image>(), dim, dimFactor);
            }
        }
    }

    private void EnsureIconCanvasOrder(int sortingOrder)
    {
        EnsureOverlayCanvas();

        EnsureCanvasForTransform(shopIconTransform, sortingOrder);
        EnsureCanvasForTransform(mejorasIconTransform, sortingOrder);

        if (homeButtons.Count > 0)
        {
            for (int i = 0; i < homeButtons.Count; i++)
            {
                Button button = homeButtons[i];
                if (button == null) continue;
                EnsureCanvasForTransform(button.transform, sortingOrder);
            }
        }
    }

    private void EnsureCanvasForTransform(Transform target, int sortingOrder)
    {
        if (target == null) return;

        Canvas canvas = target.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = target.gameObject.AddComponent<Canvas>();
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = sortingOrder;
        if (overlayCanvas != null)
        {
            canvas.sortingLayerID = overlayCanvas.sortingLayerID;
        }
        else if (hostCanvas != null)
        {
            canvas.sortingLayerID = hostCanvas.sortingLayerID;
        }

        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = overlayCanvas != null ? overlayCanvas.worldCamera : (hostCanvas != null ? hostCanvas.worldCamera : Camera.main);

        if (target.GetComponent<GraphicRaycaster>() == null)
        {
            target.gameObject.AddComponent<GraphicRaycaster>();
        }
    }

    private void ApplyOverlayAlpha()
    {
        if (shopOverlay == null) return;

        Image overlayImage = shopOverlay.GetComponent<Image>();
        if (overlayImage == null) return;

        float targetAlpha = Mathf.Clamp01(overlayDimAlpha);
        overlayImage.color = new Color(
            overlayImage.color.r,
            overlayImage.color.g,
            overlayImage.color.b,
            targetAlpha);
    }

    private void SetImageDim(Image image, bool dim, float dimFactor)
    {
        if (image == null) return;

        if (!originalIconColors.TryGetValue(image, out Color original))
        {
            originalIconColors[image] = image.color;
            original = image.color;
        }

        if (dim)
        {
            image.color = new Color(original.r * dimFactor, original.g * dimFactor, original.b * dimFactor, original.a);
        }
        else
        {
            image.color = original;
        }
    }

    private void CacheHomeButtons()
    {
        homeButtons.Clear();

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform current = allTransforms[i];
            if (current == null) continue;

            if (!current.name.StartsWith("Home_Icon"))
            {
                continue;
            }

            Button button = current.GetComponent<Button>();
            if (button != null && !homeButtons.Contains(button))
            {
                homeButtons.Add(button);
            }
        }
    }

    private void CacheMejorasCanvasByName()
    {
        if (mejorasCanvas != null) return;

        Transform mejorasRoot = FindTransformByName("Canvas Mejoras");
        if (mejorasRoot == null) return;

        Canvas found = mejorasRoot.GetComponent<Canvas>();
        if (found == null)
        {
            found = mejorasRoot.GetComponentInParent<Canvas>();
        }

        if (found == null) return;

        mejorasCanvas = found;
        mejorasCanvasInitialSortingOrder = found.sortingOrder;
    }

    private void SetMejorasCanvasBelowShop()
    {
        // No-op: avoid changing the main UI canvas sorting order (keeps brainrots visible).
    }

    private void RestoreMejorasCanvasOrder()
    {
        // No-op: avoid changing the main UI canvas sorting order.
    }

    private void ApplyMejorasCloseStyle()
    {
        if (closeButton == null) return;

        CanvasMejorasController mejorasController = FindMejorasController();
        if (mejorasController == null) return;

        Button sourceButton = mejorasController.GetCloseButton();
        if (sourceButton == null) return;

        ApplyCloseButtonStyle(sourceButton, closeButton);
    }

    private void ApplyCloseButtonStyle(Button sourceButton, Button targetButton)
    {
        if (sourceButton == null || targetButton == null) return;

        RectTransform sourceRect = sourceButton.GetComponent<RectTransform>();
        RectTransform targetRect = targetButton.GetComponent<RectTransform>();
        if (sourceRect != null && targetRect != null)
        {
            targetRect.anchorMin = sourceRect.anchorMin;
            targetRect.anchorMax = sourceRect.anchorMax;
            targetRect.pivot = sourceRect.pivot;
            targetRect.anchoredPosition = sourceRect.anchoredPosition;
            targetRect.sizeDelta = sourceRect.sizeDelta;
            targetRect.localScale = sourceRect.localScale;
        }

        Image sourceImage = sourceButton.GetComponent<Image>();
        Image targetImage = targetButton.GetComponent<Image>();
        if (targetImage == null)
        {
            targetImage = targetButton.gameObject.AddComponent<Image>();
        }

        if (sourceImage != null)
        {
            targetImage.sprite = sourceImage.sprite;
            targetImage.type = sourceImage.type;
            targetImage.color = sourceImage.color;
            targetImage.preserveAspect = sourceImage.preserveAspect;
        }

        targetButton.transition = sourceButton.transition;
        targetButton.colors = sourceButton.colors;
        targetButton.spriteState = sourceButton.spriteState;
        targetButton.animationTriggers = sourceButton.animationTriggers;

        for (int i = targetButton.transform.childCount - 1; i >= 0; i--)
        {
            Destroy(targetButton.transform.GetChild(i).gameObject);
        }

        for (int i = 0; i < sourceButton.transform.childCount; i++)
        {
            Transform child = sourceButton.transform.GetChild(i);
            GameObject clone = Instantiate(child.gameObject, targetButton.transform);
            clone.name = child.name;
            clone.SetActive(child.gameObject.activeSelf);
        }

        targetButton.onClick.RemoveListener(OnCloseButtonPressed);
        targetButton.onClick.AddListener(OnCloseButtonPressed);
    }

    private static CanvasMejorasController FindMejorasController()
    {
        CanvasMejorasController[] allControllers = Resources.FindObjectsOfTypeAll<CanvasMejorasController>();
        for (int i = 0; i < allControllers.Length; i++)
        {
            CanvasMejorasController controller = allControllers[i];
            if (controller != null && controller.gameObject.scene.IsValid())
            {
                return controller;
            }
        }

        return null;
    }

    private static Transform FindTransformByName(string name)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform current = allTransforms[i];
            if (current != null && current.name == name)
            {
                return current;
            }
        }

        return null;
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
        int previewLimit = Mathf.Min(visibleLimit + 1, entries.Count);

        int visibleIndex = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            ShopEntry entry = entries[i];
            if (entry == null || entry.rootObject == null) continue;

            int stage = entry.stage;
            bool visible = stage <= previewLimit;
            entry.rootObject.SetActive(visible);
            if (!visible) continue;

            entry.rectTransform.anchoredPosition = new Vector2(0f, -topPadding - visibleIndex * (itemSize.y + itemSpacing));
            visibleIndex++;

            bool unlocked = stage <= visibleLimit;
            entry.buyButton.interactable = unlocked;
            entry.lockOverlay.SetActive(!unlocked);

            Sprite stageSprite = gameManager != null ? gameManager.GetBrainrotSpriteForStage(stage) : null;
            entry.iconImage.sprite = stageSprite;
            entry.titleText.text = GetBrainrotDisplayName(stage, stageSprite);
            if (entry.countText != null)
            {
                int count = gameManager != null ? gameManager.GetShopPurchaseCount(stage) : 0;
                entry.countText.text = count > 0 ? $"x{count}" : string.Empty;
            }

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
            // Debug.Log($"No se pudo comprar stage {stage}. Dinero o desbloqueo insuficiente.");
            return;
        }

        SoundManager.Instance?.PlayPurchase();
        RefreshEntries();
    }

    private int GetStagePrice(int stage)
    {
        int n = Mathf.Max(0, stage - 1);
        double incremental = n * (n + 1) * 0.5;
        double price = basePrice + incremental * priceIncrement;

        int purchaseCount = gameManager != null ? gameManager.GetShopPurchaseCount(stage) : 0;
        float growth = Mathf.Max(1.01f, purchasePriceGrowthFactor);
        double scaled = price * Math.Pow(growth, purchaseCount);
        if (scaled > int.MaxValue)
        {
            return int.MaxValue;
        }

        return Mathf.RoundToInt((float)scaled);
    }

    private string GetBrainrotDisplayName(int stage, Sprite stageSprite)
    {
        if (stageSprite == null)
        {
            return $"Brainrot Stage {stage}";
        }

        string raw = stageSprite.name ?? string.Empty;
        raw = raw.Replace('_', ' ').Replace('-', ' ').Trim();
        raw = TrimTrailingDigits(raw);
        if (string.IsNullOrEmpty(raw))
        {
            return $"Brainrot Stage {stage}";
        }

        return CapitalizeFirst(raw);
    }

    private string TrimTrailingDigits(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        int end = value.Length;
        while (end > 0 && char.IsDigit(value[end - 1]))
        {
            end--;
        }

        if (end == value.Length)
        {
            return value;
        }

        return value.Substring(0, end).TrimEnd();
    }

    private string CapitalizeFirst(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        char first = char.ToUpperInvariant(value[0]);
        if (value.Length == 1)
        {
            return first.ToString();
        }

        return first + value.Substring(1);
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
        entry.countText = CreateCountLabel(entry.rectTransform);

        entry.lockOverlay = CreateLockOverlay(entry.rectTransform);

        return entry;
    }

    private TextMeshProUGUI CreateCountLabel(RectTransform parent)
    {
        GameObject textObject = new GameObject(
            "Count",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(parent, false);
        textRect.anchorMin = new Vector2(1f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(1f, 1f);
        textRect.anchoredPosition = new Vector2(-16f, -16f);
        textRect.sizeDelta = new Vector2(100f, 26f);

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = GetFontAsset();
        text.fontSize = 22f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Right;
        text.raycastTarget = false;
        text.text = "x0";

        return text;
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
        iconRect.anchoredPosition = new Vector2(12f, 0f);
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
        }
    }

    private void SetCanvasOrderForShop()
    {
        EnsureOverlayCanvas();
    }

    private void RestoreCanvasOrder()
    {
        // Keep overlay canvas settings; no main-canvas sorting changes.
    }

    private void EnsureOverlayCanvas()
    {
        if (shopOverlay == null) return;

        if (overlayCanvas == null)
        {
            overlayCanvas = shopOverlay.GetComponent<Canvas>();
        }
        if (overlayCanvas == null)
        {
            overlayCanvas = shopOverlay.AddComponent<Canvas>();
        }

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = shopCanvasSortingOrder;
        if (hostCanvas != null)
        {
            overlayCanvas.sortingLayerID = hostCanvas.sortingLayerID;
        }

        overlayCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        overlayCanvas.worldCamera = hostCanvas != null ? hostCanvas.worldCamera : Camera.main;

        if (shopOverlay.GetComponent<GraphicRaycaster>() == null)
        {
            shopOverlay.AddComponent<GraphicRaycaster>();
        }
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
