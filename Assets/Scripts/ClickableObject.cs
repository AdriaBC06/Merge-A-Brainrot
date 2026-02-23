using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    private FusionObject fusion;
    private Vector3 originalScale;
    private const float BaseAutoClickInterval = 10f;
    private const float MinAutoClickInterval = 1f;
    private static float globalAutoClickReduction;
    private static float globalMoneyMultiplier = 1f;
    private float autoClickInterval;
    private float autoClickTimer;

    private void Awake()
    {
        fusion = GetComponent<FusionObject>();
        originalScale = transform.localScale;
        RefreshAutoClickInterval();
        autoClickTimer = autoClickInterval;
    }

    private void Update()
    {
        DecreaseTimerInternal(Time.deltaTime);
    }

    private void OnMouseDown()
    {
        TriggerClickBehavior();
    }

    public void DecreaseAutoClickTimer(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        DecreaseTimerInternal(amount);
    }

    public static void ApplyGlobalAutoClickUpgrade(float reductionAmount)
    {
        if (reductionAmount <= 0f)
        {
            return;
        }

        globalAutoClickReduction += reductionAmount;

        ClickableObject[] clickableObjects = FindObjectsByType<ClickableObject>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (ClickableObject clickableObject in clickableObjects)
        {
            clickableObject.RefreshAutoClickInterval();
        }
    }

    public static void IncreaseGlobalMoneyMultiplier(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        globalMoneyMultiplier += amount;
    }

    private void TriggerClickBehavior()
    {
        int stage = fusion ? fusion.GetStage() : 1;
        float money = 10f * Mathf.Pow(2f, stage - 1) * globalMoneyMultiplier;
        GameManager gameManager = GameManager.Instance ?? FindFirstObjectByType<GameManager>();
    
        if (gameManager == null)
        {
            return;
        }

        gameManager.AddMoney(money);

        transform.localScale = originalScale * 1.15f;
        Invoke(nameof(ResetScale), 0.12f);

        if (gameManager.coinPrefab != null)
        {
            GameObject coin = Instantiate(
                gameManager.coinPrefab, 
                transform.position + Vector3.up * 0.5f, 
                Quaternion.identity
            );

            Debug.Log("Moneda creada en posición: " + coin.transform.position);

            Coin coinScript = coin.GetComponent<Coin>();
            if (coinScript != null)
            {
                coinScript.speed = 4f; 
            }
        }
    }

    private void DecreaseTimerInternal(float amount)
    {
        autoClickTimer -= amount;

        while (autoClickTimer <= 0f)
        {
            TriggerClickBehavior();
            autoClickTimer += autoClickInterval;
        }
    }

    private void RefreshAutoClickInterval()
    {
        autoClickInterval = Mathf.Max(MinAutoClickInterval, BaseAutoClickInterval - globalAutoClickReduction);

        if (autoClickTimer > autoClickInterval)
        {
            autoClickTimer = autoClickInterval;
        }
    }

    private void ResetScale()
    {
        transform.localScale = originalScale;
    }
}
