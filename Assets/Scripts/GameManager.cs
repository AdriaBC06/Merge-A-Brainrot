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

    [Header("World Containers")]
    [SerializeField] private string screen1Name = "Screen1 Brainrots";
    [SerializeField] private string screen2Name = "Screen2 Brainrots";
    [SerializeField] private GameObject changeWorldButton;
    private Transform screen1Container;
    private Transform screen2Container;
    private bool changeWorldUnlocked = false;
    private bool showingScreen1 = true;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureContainers();
        InitChangeWorldButton();
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
                Instantiate(brainrotPrefab, spawnPos, Quaternion.identity, screen1Container);
                
                int total = Object.FindObjectsByType<FusionObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
                Debug.Log($"Brainrot spawneado en {spawnPos} | Total en escena: {total}");
                return;
            }
        }

        // Fallback si no encontró sitio libre
        float fallbackX = Random.Range(-6f, 6f);
        float fallbackY = Random.Range(-4f, 4f);
        spawnPos = new Vector3(fallbackX, fallbackY, 0);
        Instantiate(brainrotPrefab, spawnPos, Quaternion.identity, screen1Container);
    }

    public void AddMoney(float amount)
    {
        currentMoney += amount;
        Debug.Log($"Dinero: {currentMoney}");
        UIManager.Instance?.UpdateMoney(currentMoney);
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
            if (brainrot.stage >= 10)
            {
                UnlockChangeWorld();
            }
        }
    }

    public void OnBrainrotStageChanged(FusionObject brainrot, int newStage)
    {
        if (brainrot == null) return;
        EnsureContainers();

        if (newStage >= 10)
        {
            UnlockChangeWorld();
        }

        if (newStage >= 11)
        {
            MoveToScreen2(brainrot);
            AutoSwitchToScreen2();
        }
    }

    public void ToggleWorld()
    {
        if (!changeWorldUnlocked) return;
        showingScreen1 = !showingScreen1;
        ApplyWorldVisibility();
    }

    private void EnsureContainers()
    {
        if (screen1Container == null)
        {
            GameObject existing = GameObject.Find(screen1Name);
            if (existing == null)
            {
                existing = new GameObject(screen1Name);
                existing.transform.SetParent(transform, false);
            }
            screen1Container = existing.transform;
        }

        if (screen2Container == null)
        {
            GameObject existing = GameObject.Find(screen2Name);
            if (existing == null)
            {
                existing = new GameObject(screen2Name);
                existing.transform.SetParent(transform, false);
            }
            screen2Container = existing.transform;
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

        if (!changeWorldUnlocked && changeWorldButton != null)
        {
            changeWorldButton.SetActive(false);
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
