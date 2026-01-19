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
            Debug.LogError("GameManager.Instance es NULL → No existe o Awake no se ejecutó");
            var gm = FindFirstObjectByType<GameManager>();
            if (gm != null)
            {
                Debug.LogWarning("Encontré GameManager con FindFirstObjectByType → usando fallback");
                gm.AddMoney(money);
            }
            else
            {
                Debug.LogError("¡NI SIGUIENTE ENCONTRADO! Crea un GameObject con GameManager.cs");
                return;
            }
        }
        else
        {
            GameManager.Instance.AddMoney(money);
        }

        transform.localScale = originalScale * 1.15f;
        Invoke(nameof(ResetScale), 0.12f);
    }

    private void ResetScale()
    {
        transform.localScale = originalScale;
    }
}