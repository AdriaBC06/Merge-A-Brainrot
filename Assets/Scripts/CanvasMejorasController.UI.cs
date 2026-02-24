using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class CanvasMejorasController
{
    [Header("Upgrade Icons")]
    [SerializeField] private Sprite autoClickIcon;
    [SerializeField] private Sprite autoSpawnIcon;
    [SerializeField] private Sprite coinMultiplierIcon;
    [SerializeField] private Sprite defaultUpgradeIcon;

    private void EnsurePanelLayout()
    {
        if (panel == null)
        {
            return;
        }

        if (panelRect == null)
        {
            panelRect = panel.GetComponent<RectTransform>();
        }

        if (panelRect != null)
        {
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(820f, 500f);
        }

        if (itemsRoot == null)
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

        if (itemsRoot == null)
        {
            GameObject rootObject = new GameObject("Upgrades_ItemsRoot", typeof(RectTransform));
            itemsRoot = rootObject.GetComponent<RectTransform>();
            itemsRoot.SetParent(panel.transform, false);
        }
        else if (itemsRoot.name != "Upgrades_ItemsRoot")
        {
            itemsRoot.name = "Upgrades_ItemsRoot";
        }

        if (itemsRoot != null)
        {
            itemsRoot.anchorMin = new Vector2(0.5f, 0.5f);
            itemsRoot.anchorMax = new Vector2(0.5f, 0.5f);
            itemsRoot.pivot = new Vector2(0.5f, 0.5f);
            itemsRoot.anchoredPosition = new Vector2(0f, -16f);
            itemsRoot.sizeDelta = new Vector2(760f, 390f);
        }

        ApplyLayoutFromShop();

        HideLegacyPanelChildren();
    }

    private void HideLegacyPanelChildren()
    {
        if (panel == null)
        {
            return;
        }

        if (panel.name != "Upgrades_Panel")
        {
            return;
        }

        foreach (Transform child in panel.transform)
        {
            if (itemsRoot != null && child == itemsRoot)
            {
                continue;
            }

            if (cerrarButton != null && child == cerrarButton.transform)
            {
                continue;
            }

            if (child.name == "Close_Button")
            {
                continue;
            }

            child.gameObject.SetActive(false);
        }
    }

    private void ApplyLayoutFromShop()
    {
        Transform shopOverlay = FindTransformByName("Shop_Overlay");
        if (shopOverlay == null)
        {
            return;
        }

        Transform shopPanelTransform = shopOverlay.Find("Shop_Panel");
        if (shopPanelTransform != null && panelRect != null)
        {
            RectTransform sourceRect = shopPanelTransform as RectTransform;
            if (sourceRect != null)
            {
                CopyRectTransform(sourceRect, panelRect);
            }
        }

        Transform shopItemsTransform = shopPanelTransform != null ? shopPanelTransform.Find("Shop_ItemsRoot") : null;
        if (shopItemsTransform != null && itemsRoot != null)
        {
            RectTransform sourceRect = shopItemsTransform as RectTransform;
            if (sourceRect != null)
            {
                CopyRectTransform(sourceRect, itemsRoot);
            }
        }

        if (cerrarButton != null && shopPanelTransform != null)
        {
            Transform shopClose = shopPanelTransform.Find("Close_Button");
            if (shopClose != null)
            {
                RectTransform sourceRect = shopClose as RectTransform;
                RectTransform targetRect = cerrarButton.GetComponent<RectTransform>();
                if (sourceRect != null && targetRect != null)
                {
                    CopyRectTransform(sourceRect, targetRect);
                }
            }
        }
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localScale = source.localScale;
    }

    private void EnsureScrollSetup()
    {
        if (itemsRoot == null)
        {
            return;
        }

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

    private void RebuildEntries()
    {
        EnsureScrollSetup();

        if (itemsContent == null)
        {
            return;
        }

        for (int i = itemsContent.childCount - 1; i >= 0; i--)
        {
            Destroy(itemsContent.GetChild(i).gameObject);
        }

        entries.Clear();

        entries.Add(CreateEntry(UpgradeId.AutoClick, "Auto Click"));
        entries.Add(CreateEntry(UpgradeId.AutoSpawn, "Auto Spawn"));
        entries.Add(CreateEntry(UpgradeId.CoinMultiplier, "Multiplicador de Monedas"));

        UpdateContentHeight(entries.Count);
        AssignUpgradeReferences();
    }

    private void AssignUpgradeReferences()
    {
        foreach (UpgradeEntry entry in entries)
        {
            if (entry == null) continue;

            switch (entry.id)
            {
                case UpgradeId.AutoClick:
                    autoClickUpgradeButton = entry.buyButton;
                    autoClickPriceText = entry.priceText;
                    break;
                case UpgradeId.AutoSpawn:
                    autoSpawnUpgradeButton = entry.buyButton;
                    autoSpawnPriceText = entry.priceText;
                    break;
                case UpgradeId.CoinMultiplier:
                    coinMultiplierUpgradeButton = entry.buyButton;
                    coinMultiplierPriceText = entry.priceText;
                    break;
            }
        }
    }

    private UpgradeEntry CreateEntry(UpgradeId id, string title)
    {
        UpgradeEntry entry = new UpgradeEntry();
        entry.id = id;

        entry.rootObject = new GameObject(
            $"Upgrade_Item_{id}",
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

        int index = entries.Count;
        entry.rectTransform.anchoredPosition = new Vector2(0f, -topPadding - index * (itemSize.y + itemSpacing));
        entry.rectTransform.sizeDelta = itemSize;

        entry.backgroundImage = entry.rootObject.GetComponent<Image>();
        entry.backgroundImage.color = entryBackgroundColor;

        entry.buyButton = entry.rootObject.GetComponent<Button>();
        entry.buyButton.targetGraphic = entry.backgroundImage;

        entry.iconImage = CreateIcon(entry.rectTransform, id);
        entry.titleText = CreateLabel(entry.rectTransform, "Title", new Vector2(130f, -16f), new Vector2(420f, 26f), 24f, FontStyles.Bold);
        entry.priceText = CreateLabel(entry.rectTransform, "Price", new Vector2(130f, -44f), new Vector2(320f, 22f), 20f, FontStyles.Normal);

        entry.titleText.text = title;

        return entry;
    }

    private Image CreateIcon(RectTransform parent, UpgradeId id)
    {
        GameObject iconObject = new GameObject(
            $"Icon_{id}",
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
        Sprite iconSprite = GetUpgradeIcon(id);
        if (iconSprite != null)
        {
            iconImage.sprite = iconSprite;
            iconImage.color = Color.white;
        }
        else
        {
            Color clear = Color.white;
            clear.a = 0f;
            iconImage.color = clear;
        }

        return iconImage;
    }

    private Sprite GetUpgradeIcon(UpgradeId id)
    {
        Sprite assigned = null;

        switch (id)
        {
            case UpgradeId.AutoClick:
                assigned = autoClickIcon;
                break;
            case UpgradeId.AutoSpawn:
                assigned = autoSpawnIcon;
                break;
            case UpgradeId.CoinMultiplier:
                assigned = coinMultiplierIcon;
                break;
        }

        if (assigned != null)
        {
            return assigned;
        }

        Sprite legacy = FindLegacyIcon(id);
        if (legacy != null)
        {
            return legacy;
        }

        return defaultUpgradeIcon;
    }

    private Sprite FindLegacyIcon(UpgradeId id)
    {
        Button sourceButton = null;
        switch (id)
        {
            case UpgradeId.AutoClick:
                sourceButton = autoClickUpgradeButton;
                break;
            case UpgradeId.AutoSpawn:
                sourceButton = autoSpawnUpgradeButton;
                break;
            case UpgradeId.CoinMultiplier:
                sourceButton = coinMultiplierUpgradeButton;
                break;
        }

        if (sourceButton == null)
        {
            return null;
        }

        Image target = sourceButton.targetGraphic as Image;
        Image[] images = sourceButton.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image img = images[i];
            if (img == null || img.sprite == null) continue;
            if (target != null && img == target) continue;
            return img.sprite;
        }

        return target != null ? target.sprite : null;
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

    private void UpdateContentHeight(int visibleCount)
    {
        if (itemsContent == null || itemsRoot == null) return;

        float contentHeight = topPadding + visibleCount * itemSize.y + Mathf.Max(0, visibleCount - 1) * itemSpacing + topPadding;
        contentHeight = Mathf.Max(contentHeight, itemsRoot.rect.height);

        itemsContent.sizeDelta = new Vector2(0f, contentHeight);
    }

    private TMP_FontAsset GetFontAsset()
    {
        if (UIManager.Instance != null && UIManager.Instance.moneyText != null && UIManager.Instance.moneyText.font != null)
        {
            return UIManager.Instance.moneyText.font;
        }

        return TMP_Settings.defaultFontAsset;
    }

    private Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), childName);
            if (found != null)
            {
                return found;
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


    private void SetPanelState(bool isOpen)
    {
        if (isOpen)
        {
            if (shopController != null && shopController.IsOpen)
            {
                shopController.CloseShop();
            }
        }

        if (panel != null)
        {
            panel.SetActive(isOpen);
        }

        if (overlay != null)
        {
            overlay.SetActive(isOpen);
        }

        if (isOpen)
        {
            ApplyOverlayAlpha();
            EnsurePanelLayout();
            EnsureScrollSetup();
            SetCanvasOrderForPanel();
            EnsureOverlayOrder();
            if (panel != null)
            {
                EnsureCloseButton(panel.transform);
            }
            if (shopController != null)
            {
                shopController.SetIconsDimmed(true, keepShopIconNormal: false, keepUpgradesIconNormal: true);
                shopController.BringIconsToFront();
            }

            if (itemsScrollRect != null)
            {
                itemsScrollRect.verticalNormalizedPosition = 1f;
            }
        }
        else
        {
            RestoreCanvasOrder();
            bool keepAbove = shopController != null && shopController.IsOpen;
            if (keepAbove)
            {
                shopController.SetIconsDimmed(true, keepShopIconNormal: true, keepUpgradesIconNormal: false);
                shopController.BringIconsToFront();
            }
            else if (shopController != null)
            {
                shopController.SetIconsDimmed(false, keepShopIconNormal: false, keepUpgradesIconNormal: false);
            }
        }
    }

    private void EnsureUpgradesIcon()
    {
        if (botonMejorasButton == null)
        {
            return;
        }

        Transform shopIcon = FindTransformByName("Shop_Icon");
        if (shopIcon == null)
        {
            return;
        }

        Transform targetParent = shopIcon.parent;
        if (targetParent == null)
        {
            return;
        }

        Transform current = botonMejorasButton.transform;
        if (current.parent != targetParent)
        {
            current.SetParent(targetParent, true);
        }

        if (current.name != "Upgrades_Icon")
        {
            current.name = "Upgrades_Icon";
        }
    }

    private void EnsureOverlayHierarchy()
    {
        if (overlay == null)
        {
            return;
        }
        Transform shopOverlay = FindTransformByName("Shop_Overlay");
        Transform targetParent = shopOverlay != null ? shopOverlay.parent : transform.parent;
        if (targetParent == null)
        {
            targetParent = transform;
        }

        if (overlay.transform.parent != targetParent)
        {
            overlay.transform.SetParent(targetParent, false);
        }

        if (overlay.name != "Upgrades_Overlay")
        {
            overlay.name = "Upgrades_Overlay";
        }

        if (panel != null && panel.transform.parent != overlay.transform)
        {
            panel.transform.SetParent(overlay.transform, false);
        }
    }

    private void EnsureOverlay()
    {
        if (overlay != null && overlay.name == "Shop_Overlay")
        {
            overlay = null;
        }

        if (overlay != null)
        {
            EnsureOverlayReferences();
            return;
        }

        Transform existingOverlay = FindTransformByName("Upgrades_Overlay");
        if (existingOverlay != null)
        {
            overlay = existingOverlay.gameObject;
            EnsureOverlayReferences();
            return;
        }

        Transform shopOverlay = FindTransformByName("Shop_Overlay");
        if (shopOverlay != null)
        {
            GameObject clone = Instantiate(shopOverlay.gameObject, shopOverlay.parent);
            clone.name = "Upgrades_Overlay";
            overlay = clone;
            EnsureOverlayReferences();
            return;
        }

        GameObject overlayObject = new GameObject("Upgrades_Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        overlayObject.transform.SetParent(transform, false);

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlayObject.GetComponent<Image>();
        overlayImage.color = FallbackOverlayColor;
        overlayImage.raycastTarget = true;

        overlayObject.SetActive(false);
        overlay = overlayObject;
        EnsureOverlayReferences();
    }

    private void EnsureOverlayReferences()
    {
        if (overlay == null)
        {
            return;
        }

        overlay.name = "Upgrades_Overlay";

        Transform panelTransform = overlay.transform.Find("Upgrades_Panel");
        if (panelTransform == null)
        {
            panelTransform = overlay.transform.Find("Shop_Panel");
            if (panelTransform != null)
            {
                panelTransform.name = "Upgrades_Panel";
            }
        }

        if (panelTransform == null)
        {
            panelTransform = CreateFallbackPanel(overlay.transform);
        }

        panel = panelTransform != null ? panelTransform.gameObject : null;
        panelRect = panelTransform as RectTransform;

        if (panelTransform != null)
        {
            Transform itemsTransform = panelTransform.Find("Upgrades_ItemsRoot");
            if (itemsTransform == null)
            {
                itemsTransform = panelTransform.Find("Shop_ItemsRoot");
                if (itemsTransform != null)
                {
                    itemsTransform.name = "Upgrades_ItemsRoot";
                }
            }

            if (itemsTransform == null)
            {
                itemsTransform = CreateFallbackItemsRoot(panelTransform);
            }

            itemsRoot = itemsTransform as RectTransform;

            EnsureCloseButton(panelTransform);
        }

        overlay.SetActive(false);
        if (panel != null)
        {
            panel.SetActive(false);
        }

        ApplyOverlayAlpha();
    }

    private void EnsureCloseButton(Transform panelTransform)
    {
        Transform closeTransform = panelTransform.Find("Close_Button");
        Transform shopOverlay = FindTransformByName("Shop_Overlay");
        Transform shopClose = shopOverlay != null ? shopOverlay.Find("Shop_Panel/Close_Button") : null;
        Image shopCloseImage = shopClose != null ? shopClose.GetComponent<Image>() : null;
        Image existingImage = closeTransform != null ? closeTransform.GetComponent<Image>() : null;

        bool needsReplace = closeTransform == null || shopCloseImage == null;
        if (!needsReplace && existingImage != null && shopCloseImage != null)
        {
            needsReplace = existingImage.sprite != shopCloseImage.sprite;
        }
        if (!needsReplace && closeTransform != null)
        {
            Transform existingIcon = closeTransform.Find("Close_Icon");
            needsReplace = existingIcon == null;
        }

        if (needsReplace)
        {
            if (closeTransform != null)
            {
                Destroy(closeTransform.gameObject);
                closeTransform = null;
            }

            if (shopClose != null)
            {
                GameObject clone = Instantiate(shopClose.gameObject, panelTransform);
                clone.name = "Close_Button";
                closeTransform = clone.transform;
            }
        }

        if (closeTransform == null)
        {
            return;
        }

        closeTransform.gameObject.SetActive(true);

        cerrarButton = closeTransform.GetComponent<Button>() ??
                       closeTransform.GetComponentInChildren<Button>(true);

        Transform iconTransform = closeTransform.Find("Close_Icon");
        if (iconTransform == null)
        {
            Transform shopOverlayForIcon = FindTransformByName("Shop_Overlay");
            if (shopOverlayForIcon != null)
            {
                Transform shopIcon = shopOverlayForIcon.Find("Shop_Panel/Close_Button/Close_Icon");
                if (shopIcon != null)
                {
                    GameObject iconClone = Instantiate(shopIcon.gameObject, closeTransform);
                    iconClone.name = "Close_Icon";
                }
            }
        }
    }

    private void ApplyOverlayAlpha()
    {
        if (overlay == null) return;

        Image overlayImage = overlay.GetComponent<Image>();
        if (overlayImage == null) return;

        float targetAlpha = Mathf.Clamp01(overlayDimAlpha);
        overlayImage.color = new Color(
            overlayImage.color.r,
            overlayImage.color.g,
            overlayImage.color.b,
            targetAlpha);
    }

    private RectTransform CreateFallbackPanel(Transform parent)
    {
        GameObject panelObject = new GameObject("Upgrades_Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform panelRectTransform = panelObject.GetComponent<RectTransform>();
        panelRectTransform.SetParent(parent, false);
        panelRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        panelRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        panelRectTransform.pivot = new Vector2(0.5f, 0.5f);
        panelRectTransform.anchoredPosition = Vector2.zero;
        panelRectTransform.sizeDelta = new Vector2(820f, 500f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = FallbackPanelColor;
        panelImage.raycastTarget = true;

        return panelRectTransform;
    }

    private RectTransform CreateFallbackItemsRoot(Transform panelTransform)
    {
        GameObject root = new GameObject("Upgrades_ItemsRoot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = root.GetComponent<RectTransform>();
        rect.SetParent(panelTransform, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, -16f);
        rect.sizeDelta = new Vector2(740f, 360f);

        Image image = root.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.01f);
        image.raycastTarget = true;

        return rect;
    }

    private void DisableLegacyPanel()
    {
        Transform legacyPanel = FindTransformByName("Panel Mejoras");
        if (legacyPanel == null)
        {
            return;
        }

        if (panel != null && legacyPanel.gameObject == panel)
        {
            return;
        }

        legacyPanel.gameObject.SetActive(false);
    }

    private void ApplyShopStyle()
    {
        overlayBackgroundColor = FallbackOverlayColor;
        panelBackgroundColor = FallbackPanelColor;
        entryBackgroundColor = FallbackEntryColor;

        Image shopOverlayImage = null;
        Image shopPanelImage = null;
        Image shopCloseButtonImage = null;
        Image shopCloseIconImage = null;

        GameObject shopOverlay = GameObject.Find("Shop_Overlay");
        if (shopOverlay != null)
        {
            shopOverlayImage = shopOverlay.GetComponent<Image>();
            Transform panelTransform = shopOverlay.transform.Find("Shop_Panel");
            if (panelTransform != null)
            {
                shopPanelImage = panelTransform.GetComponent<Image>();
                Transform closeButtonTransform = panelTransform.Find("Close_Button");
                if (closeButtonTransform != null)
                {
                    shopCloseButtonImage = closeButtonTransform.GetComponent<Image>();
                    Transform closeIconTransform = closeButtonTransform.Find("Close_Icon");
                    if (closeIconTransform != null)
                    {
                        shopCloseIconImage = closeIconTransform.GetComponent<Image>();
                    }
                }
            }
        }

        if (shopOverlayImage != null)
        {
            overlayBackgroundColor = shopOverlayImage.color;
        }

        if (shopPanelImage != null)
        {
            panelBackgroundColor = shopPanelImage.color;
        }

        if (overlay != null)
        {
            Image overlayImage = overlay.GetComponent<Image>();
            if (overlayImage == null)
            {
                overlayImage = overlay.AddComponent<Image>();
            }
            overlayImage.color = overlayBackgroundColor;
            overlayImage.raycastTarget = true;
            if (shopOverlayImage != null)
            {
                overlayImage.sprite = shopOverlayImage.sprite;
                overlayImage.type = shopOverlayImage.type;
            }
        }

        if (panel != null)
        {
            Image panelImage = panel.GetComponent<Image>();
            if (panelImage == null)
            {
                panelImage = panel.AddComponent<Image>();
            }
            panelImage.color = panelBackgroundColor;
            panelImage.raycastTarget = true;
            if (shopPanelImage != null)
            {
                panelImage.sprite = shopPanelImage.sprite;
                panelImage.type = shopPanelImage.type;
            }
        }

        if (cerrarButton != null)
        {
            cerrarButton.gameObject.SetActive(true);
            Image closeImage = cerrarButton.GetComponent<Image>();
            if (closeImage == null)
            {
                closeImage = cerrarButton.gameObject.AddComponent<Image>();
            }

            if (shopCloseButtonImage != null)
            {
                closeImage.sprite = shopCloseButtonImage.sprite;
                closeImage.type = shopCloseButtonImage.type;
                closeImage.color = shopCloseButtonImage.color;
            }
            else
            {
                closeImage.color = Color.white;
            }

            if (shopCloseIconImage != null && shopCloseIconImage.sprite != null)
            {
                Transform existingIcon = cerrarButton.transform.Find("Close_Icon");
                if (existingIcon == null)
                {
                    GameObject iconObject = new GameObject("Close_Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    iconObject.transform.SetParent(cerrarButton.transform, false);

                    RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                    iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRect.anchoredPosition = Vector2.zero;
                    iconRect.sizeDelta = new Vector2(42f, 42f);

                    Image iconImage = iconObject.GetComponent<Image>();
                    iconImage.sprite = shopCloseIconImage.sprite;
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false;
                }
                else
                {
                    Image iconImage = existingIcon.GetComponent<Image>();
                    if (iconImage != null)
                    {
                        iconImage.sprite = shopCloseIconImage.sprite;
                        iconImage.preserveAspect = true;
                        iconImage.raycastTarget = false;
                    }
                }

                Transform textChild = cerrarButton.transform.Find("Text (TMP)");
                if (textChild != null)
                {
                    textChild.gameObject.SetActive(false);
                }
            }
        }

        if (panel != null)
        {
            foreach (TextMeshProUGUI text in panel.GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                text.color = Color.white;
            }

            foreach (Transform child in panel.transform)
            {
                if (!child.name.Contains("Mejora"))
                {
                    continue;
                }

                Image entryImage = child.GetComponent<Image>();
                if (entryImage == null)
                {
                    entryImage = child.gameObject.AddComponent<Image>();
                }

                entryImage.color = entryBackgroundColor;
                entryImage.raycastTarget = false;
            }
        }
    }

    private void EnsureOverlayOrder()
    {
        if (overlay != null)
        {
            overlay.transform.SetAsLastSibling();
        }

        if (panel != null)
        {
            panel.transform.SetAsLastSibling();
        }

        if (botonMejorasButton != null)
        {
            botonMejorasButton.transform.SetAsLastSibling();
        }
    }

    private void CacheHostCanvas()
    {
        if (overlay != null)
        {
            hostCanvas = overlay.GetComponentInParent<Canvas>();
        }

        if (hostCanvas == null)
        {
            hostCanvas = GetComponent<Canvas>();
        }

        if (hostCanvas == null)
        {
            hostCanvas = GetComponentInParent<Canvas>();
        }

        if (hostCanvas != null)
        {
            hostCanvasInitialSortingOrder = hostCanvas.sortingOrder;
        }
    }

    private void SetCanvasOrderForPanel()
    {
        EnsureOverlayCanvas();
    }

    private void RestoreCanvasOrder()
    {
        // Keep overlay canvas settings; no main-canvas sorting changes.
    }

    private void EnsureOverlayCanvas()
    {
        if (overlay == null) return;

        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.GetComponent<Canvas>();
        }
        if (overlayCanvas == null)
        {
            overlayCanvas = overlay.AddComponent<Canvas>();
        }

        overlayCanvas.overrideSorting = true;
        overlayCanvas.sortingOrder = mejorasCanvasSortingOrder;
        if (hostCanvas != null)
        {
            overlayCanvas.sortingLayerID = hostCanvas.sortingLayerID;
        }

        overlayCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        overlayCanvas.worldCamera = hostCanvas != null ? hostCanvas.worldCamera : Camera.main;

        if (overlay.GetComponent<GraphicRaycaster>() == null)
        {
            overlay.AddComponent<GraphicRaycaster>();
        }
    }
}
