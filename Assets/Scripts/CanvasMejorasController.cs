using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CanvasMejorasController : MonoBehaviour
{
    [Header("Panel Controls")]
    [SerializeField] private GameObject panel;
    [SerializeField] private Button botonMejorasButton;
    [SerializeField] private Button cerrarButton;

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
    [SerializeField] private float coinMultiplierIncreasePerPurchase = 0.5f;

    private int autoClickPurchases;
    private int autoClickCurrentPrice;
    private int autoSpawnCurrentPrice;
    private int coinMultiplierCurrentPrice;

    private void Awake()
    {
        AutoAssignReferences();
        InitializeUpgradeState();

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

        if (panel != null)
        {
            panel.SetActive(false);
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
            Transform panelTransform = FindChildRecursive(transform, "Panel");
            if (panelTransform != null)
            {
                panel = panelTransform.gameObject;
            }
        }

        if (botonMejorasButton == null)
        {
            Transform botonMejorasTransform = FindChildRecursive(transform, "BotonMejoras");
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

        panel.SetActive(!panel.activeSelf);
    }

    private void ClosePanel()
    {
        if (panel == null)
        {
            return;
        }

        panel.SetActive(false);
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

        if (!gameManager.TrySpendMoney(coinMultiplierCurrentPrice))
        {
            return;
        }

        ClickableObject.IncreaseGlobalMoneyMultiplier(coinMultiplierIncreasePerPurchase);
        coinMultiplierCurrentPrice = MultiplyPrice(coinMultiplierCurrentPrice, coinMultiplierPriceMultiplier);
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

        coinMultiplierPriceText.text = $"Precio: {coinMultiplierCurrentPrice}$";
    }

    private int MultiplyPrice(int currentPrice, float multiplier)
    {
        float safeMultiplier = Mathf.Max(1f, multiplier);
        return Mathf.Max(1, Mathf.CeilToInt(currentPrice * safeMultiplier));
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
}
