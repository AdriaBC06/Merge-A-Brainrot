using UnityEngine;

public class Coin : MonoBehaviour
{
    [SerializeField] public float speed = 3f;
    [SerializeField] public float lifetime = 1f;
    [SerializeField] public AnimationCurve fadeCurve;

    private SpriteRenderer sr;
    private Vector3 startPos;
    private float spawnTime;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startPos = transform.position;
        spawnTime = Time.time;
    }

    void Update()
    {
        float t = Time.time - spawnTime;
        if (t > lifetime) 
        {
            Destroy(gameObject);
            return;
        }

        // Sube para arriba
        float height = speed * t;
        transform.position = startPos + Vector3.up * height;

        // Fade
        Color c = sr.color;
        c.a = fadeCurve.Evaluate(t / lifetime);
        sr.color = c;
    }
}