using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float currentMoney = 0f;

    [Header("Spawning")]
    [SerializeField] private GameObject brainrotPrefab;
    [SerializeField] public GameObject coinPrefab;  
    [SerializeField] private float spawnInterval = 10f; 
    [SerializeField] private bool autoSpawnEnabled = false;
    [SerializeField] private int maxObjects = 12; 
    [SerializeField] private float spawnZ = -1f;
    private float spawnTimer;
    private bool initialBrainrotSpawned;

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

    private void Start()
    {
        SpawnInitialBrainrot();
    }

    private void Update()
    {
        if (!autoSpawnEnabled || brainrotPrefab == null) return;

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
                spawnPos = new Vector3(x, y, spawnZ);
                Instantiate(brainrotPrefab, spawnPos, Quaternion.identity);
                
                int total = Object.FindObjectsByType<FusionObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                Debug.Log($"Brainrot spawneado en {spawnPos} | Total en escena: {total}");
                return;
            }
        }

        // Fallback si no encontró sitio libre
        float fallbackX = Random.Range(-6f, 6f);
        float fallbackY = Random.Range(-4f, 4f);
        spawnPos = new Vector3(fallbackX, fallbackY, spawnZ);
        Instantiate(brainrotPrefab, spawnPos, Quaternion.identity);
    }

    private void SpawnInitialBrainrot()
    {
        if (initialBrainrotSpawned || brainrotPrefab == null)
        {
            return;
        }

        initialBrainrotSpawned = true;
        SpawnBrainrot();
    }

    public void AddMoney(float amount)
    {
        currentMoney += amount;
        Debug.Log($"Dinero: {currentMoney}");
        UIManager.Instance?.UpdateMoney(currentMoney);
    }

    public bool TrySpendMoney(float amount)
    {
        if (amount <= 0f)
        {
            return true;
        }

        if (currentMoney < amount)
        {
            return false;
        }

        currentMoney -= amount;
        UIManager.Instance?.UpdateMoney(currentMoney);
        return true;
    }

    public bool IsAutoSpawnEnabled()
    {
        return autoSpawnEnabled;
    }

    public float GetSpawnInterval()
    {
        return spawnInterval;
    }

    public void EnableAutoSpawn(float intervalSeconds)
    {
        autoSpawnEnabled = true;
        spawnInterval = Mathf.Max(1f, intervalSeconds);
        spawnTimer = 0f;
    }

    public void ReduceSpawnInterval(float reductionAmount)
    {
        if (reductionAmount <= 0f)
        {
            return;
        }

        spawnInterval = Mathf.Max(1f, spawnInterval - reductionAmount);
    }

    public void RegisterBrainrot(FusionObject brainrot)
    {
        if (brainrot == null) return;
        EnsureContainers();

        if (brainrot.stage >= 11)
        {
            MoveToScreen2(brainrot);
            AutoSwitchToScreen2();
            UnlockChangeWorld();
        }
        else
        {
            brainrot.transform.SetParent(screen1Container, true);
        }
    }

    public void OnBrainrotStageChanged(FusionObject brainrot, int newStage)
    {
        if (brainrot == null) return;
        EnsureContainers();

        if (newStage >= 11)
        {
            UnlockChangeWorld();
            MoveToScreen2(brainrot);
            AutoSwitchToScreen2();
        }
    }

    public void ToggleWorld()
    {
        showingScreen1 = !showingScreen1;
        ApplyWorldVisibility();
    }

    private void EnsureContainers()
    {
        if (screen1Container == null)
        {
            GameObject existing = GameObject.Find(screen1Name);
            if (existing != null)
            {
                screen1Container = existing.transform;
            }
            else
            {
                Debug.LogWarning($"Scene object '{screen1Name}' not found. Please use MainGameScene > {screen1Name}.");
            }
        }

        if (screen2Container == null)
        {
            GameObject existing = GameObject.Find(screen2Name);
            if (existing != null)
            {
                screen2Container = existing.transform;
            }
            else
            {
                Debug.LogWarning($"Scene object '{screen2Name}' not found. Please use MainGameScene > {screen2Name}.");
            }
        }

        ApplyWorldVisibility();
    }

    private void InitChangeWorldButton()
    {
        if (changeWorldButton == null)
        {
            GameObject found = GameObject.Find("Change World Button");
            if (found != null) changeWorldButton = found;
        }

        if (changeWorldButton != null)
        {
            Button button = changeWorldButton.GetComponent<Button>();
            if (button == null)
            {
                button = changeWorldButton.GetComponentInChildren<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveListener(ToggleWorld);
                button.onClick.AddListener(ToggleWorld);
            }
            else
            {
                Debug.LogWarning("Change World Button is missing a Button component.");
            }

            changeWorldButton.SetActive(changeWorldUnlocked);
        }
    }

    private void UnlockChangeWorld()
    {
        if (changeWorldUnlocked) return;
        changeWorldUnlocked = true;
        if (changeWorldButton != null)
        {
            changeWorldButton.SetActive(true);
        }
    }

    private void AutoSwitchToScreen2()
    {
        showingScreen1 = false;
        ApplyWorldVisibility();
    }

    private void ApplyWorldVisibility()
    {
        if (screen1Container != null) screen1Container.gameObject.SetActive(showingScreen1);
        if (screen2Container != null) screen2Container.gameObject.SetActive(!showingScreen1);
    }

    private void MoveToScreen2(FusionObject brainrot)
    {
        if (screen2Container == null) return;
        brainrot.transform.SetParent(screen2Container, true);
    }
}
