using UnityEngine;

public class ClickableObject : MonoBehaviour
{
    private FusionObject fusion;
    private Vector3 originalScale;

    private void Awake()
    {
        fusion = GetComponent<FusionObject>();
        originalScale = transform.localScale;
    }

    private void OnMouseDown()
    {
        int stage = fusion ? fusion.GetStage() : 1;
        float money = 10f * Mathf.Pow(2f, stage - 1);
    
        if (GameManager.Instance == null)
        {
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                gm.AddMoney(money);
            }
            else
            {
                return;
            }
        }
        else
        {
            GameManager.Instance.AddMoney(money);
        }

        transform.localScale = originalScale * 1.15f;
        Invoke(nameof(ResetScale), 0.12f);

        if (GameManager.Instance.coinPrefab != null)
        {
        GameObject coin = Instantiate(
            GameManager.Instance.coinPrefab, 
            transform.position + Vector3.up * 0.5f, 
            Quaternion.identity
        );

        Coin coinScript = coin.GetComponent<Coin>();
        if (coinScript != null)
        {
            coinScript.speed = 4f; 
        }
        }
    }

    private void ResetScale()
    {
        transform.localScale = originalScale;
    }
}