using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float currentMoney = 0f;

    [Header("Spawning")]
    [SerializeField] private GameObject brainrotPrefab;
    [SerializeField] public GameObject coinPrefab;  
    [SerializeField] private float spawnInterval = 10f; 
    [SerializeField] private int maxObjects = 12; 
    private float spawnTimer;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        if (brainrotPrefab == null) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            int currentCount = Object.FindObjectsByType<FusionObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
            if (currentCount < maxObjects)
            {
                SpawnBrainrot();
            }
        }
    }

    private void SpawnBrainrot()
    {
        const int maxAttempts = 10;
        Vector3 spawnPos = Vector3.zero;

        // busca sitio libre para spawnear
        for (int i = 0; i < maxAttempts; i++)
        {
            float x = Random.Range(-6f, 6f);
            float y = Random.Range(-4f, 4f);
            Vector2 pos = new Vector2(x, y);

            if (Physics2D.OverlapCircle(pos, 1.2f) == null)
            {
                spawnPos = new Vector3(x, y, 0);
                Instantiate(brainrotPrefab, spawnPos, Quaternion.identity);
                
                int total = Object.FindObjectsByType<FusionObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                Debug.Log($"Brainrot spawneado en {spawnPos} | Total en escena: {total}");
                return;
            }
        }

        // Fallback si no encontró sitio libre
        float fallbackX = Random.Range(-6f, 6f);
        float fallbackY = Random.Range(-4f, 4f);
        spawnPos = new Vector3(fallbackX, fallbackY, 0);
        Instantiate(brainrotPrefab, spawnPos, Quaternion.identity);
    }

    public void AddMoney(float amount)
    {
        currentMoney += amount;
        Debug.Log($"Dinero: {currentMoney}");
        UIManager.Instance?.UpdateMoney(currentMoney);
    }
}