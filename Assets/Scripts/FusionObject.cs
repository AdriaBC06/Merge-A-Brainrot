using UnityEngine;
using System.Collections;

public class FusionObject : MonoBehaviour
{
    public int stage = 1;

    [Header("Sprites por Stage")]
    public Sprite[] stageSprites;

    private SpriteRenderer spriteRenderer;

    private FusionObject fusionCandidate;
    private bool fusionInProgress = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        UpdateSprite();
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        FusionObject other = collision.gameObject.GetComponent<FusionObject>();

        if (other == null) return;
        if (other.stage != stage) return;

        fusionCandidate = other;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        FusionObject other = collision.gameObject.GetComponent<FusionObject>();
        if (other == fusionCandidate)
            fusionCandidate = null;
    }

    public void TryFusionOnRelease()
    {
        if (fusionInProgress) return;
        if (fusionCandidate == null) return;

        fusionInProgress = true;

        fusionCandidate.ReceiveFusion();
        Destroy(gameObject);
    }

    public void ReceiveFusion()
    {
        stage++;
        UpdateSprite();
        StartCoroutine(FusionAnimation());
    }

    void UpdateSprite()
    {
        int index = stage - 1;

        if (stageSprites == null || stageSprites.Length == 0) return;

        if (index >= 0 && index < stageSprites.Length)
        {
            spriteRenderer.sprite = stageSprites[index];
        }
        else
        {
            Debug.LogWarning(
                $"{name}: No hay sprite para Stage {stage}"
            );
        }
    }

    IEnumerator FusionAnimation()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 bigScale = originalScale * 1.3f;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 8f;
            transform.localScale = Vector3.Lerp(originalScale, bigScale, t);
            yield return null;
        }

        t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * 6f;
            transform.localScale = Vector3.Lerp(bigScale, originalScale, t);
            yield return null;
        }

        fusionInProgress = false;
    }
}
