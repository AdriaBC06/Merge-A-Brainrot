using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public float currentMoney = 0f;
    public int HighestStageReached => highestStageReached;
    public event Action<int> HighestStageReachedChanged;

    [Header("Spawning")]
    [SerializeField] private GameObject brainrotPrefab;
    [SerializeField] public GameObject coinPrefab;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private bool autoSpawnEnabled = true;
    [SerializeField] private int maxObjects = 12;
    [SerializeField] private float spawnZ = -1f;
    [SerializeField] private bool spawnInitialBrainrotOnSceneLoad = true;
    private float spawnTimer;

    [Header("World Containers")]
    [SerializeField] private string mainGameSceneName = "MainGameScene";
    [SerializeField] private string screen1Name = "Screen1 Brainrots";
    [SerializeField] private string screen2Name = "Screen2 Brainrots";
    [SerializeField] private string runtimeScreen1Name = "__Screen1BrainrotsRuntime";
    [SerializeField] private string runtimeScreen2Name = "__Screen2BrainrotsRuntime";
    [SerializeField] private GameObject changeWorldButton;
    private Transform screen1Container;
    private Transform screen2Container;
    private bool changeWorldUnlocked = false;
    private bool showingScreen1 = true;

    private int highestStageReached = 1;
    private bool initialBrainrotSpawned = false;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += HandleSceneLoaded;

        EnsureContainers();
        InitChangeWorldButton();
    }

    private void Start()
    {
        TrySpawnInitialBrainrot();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }
    }

    private void Update()
    {
        if (brainrotPrefab == null) return;
        if (!autoSpawnEnabled) return;

        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;

            if (GetCurrentBrainrotCount() < maxObjects)
            {
                SpawnBrainrot(1);
            }
        }
    }

    private void HandleSceneLoaded(Scene _, LoadSceneMode __)
    {
        screen1Container = null;
        screen2Container = null;
        changeWorldButton = null;
        initialBrainrotSpawned = false;
        spawnTimer = 0f;

        if (IsMainGameSceneActive())
        {
            // Ensure newly spawned stage-1 brainrots are visible on scene entry.
            showingScreen1 = true;
        }

        EnsureContainers();
        InitChangeWorldButton();
        TrySpawnInitialBrainrot();
    }

    public int GetMaxBrainrotStage()
    {
        FusionObject prefabFusion = GetBrainrotPrefabFusion();
        if (prefabFusion == null || prefabFusion.stageSprites == null || prefabFusion.stageSprites.Length == 0)
        {
            return 1;
        }

        return prefabFusion.stageSprites.Length;
    }

    public int GetVisibleShopStageLimit()
    {
        int maxStage = GetMaxBrainrotStage();
        return Mathf.Clamp(highestStageReached + 1, 1, maxStage);
    }

    public Sprite GetBrainrotSpriteForStage(int stage)
    {
        FusionObject prefabFusion = GetBrainrotPrefabFusion();
        if (prefabFusion == null || prefabFusion.stageSprites == null || prefabFusion.stageSprites.Length == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(stage - 1, 0, prefabFusion.stageSprites.Length - 1);
        return prefabFusion.stageSprites[index];
    }

    public bool TryBuyBrainrotFromShop(int stage, float price)
    {
        if (stage < 1) return false;
        if (stage > highestStageReached) return false;
        if (currentMoney < price) return false;
        if (GetCurrentBrainrotCount() >= maxObjects) return false;

        currentMoney -= price;
        UIManager.Instance?.UpdateMoney(currentMoney);

        bool spawned = SpawnBrainrot(stage);
        if (!spawned)
        {
            currentMoney += price;
            UIManager.Instance?.UpdateMoney(currentMoney);
            return false;
        }

        return true;
    }

    public void AddMoney(float amount)
    {
        currentMoney += amount;
        Debug.Log($"Dinero: {currentMoney}");
        UIManager.Instance?.UpdateMoney(currentMoney);
    }

    public bool TrySpendMoney(float amount)
    {
        if (amount <= 0f) return true;
        if (currentMoney < amount) return false;

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

    public void EnableAutoSpawn(float interval)
    {
        autoSpawnEnabled = true;
        spawnInterval = Mathf.Max(1f, interval);
    }

    public void ReduceSpawnInterval(float reduction)
    {
        if (reduction <= 0f) return;
        spawnInterval = Mathf.Max(1f, spawnInterval - reduction);
    }

    public void RegisterBrainrot(FusionObject brainrot)
    {
        if (brainrot == null) return;
        EnsureContainers();
        TrackHighestStage(brainrot.stage);

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
        TrackHighestStage(newStage);

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

    private bool SpawnBrainrot(int stage)
    {
        EnsureContainers();
        if (brainrotPrefab == null)
        {
            Debug.LogWarning("Brainrot prefab not assigned.");
            return false;
        }

        if (screen1Container == null)
        {
            Debug.LogWarning("Screen1 Brainrots container not found in scene. Spawn aborted.");
            return false;
        }

        Vector3 spawnPos = FindSpawnPosition();
        GameObject spawned = Instantiate(brainrotPrefab, spawnPos, Quaternion.identity, screen1Container);

        FusionObject fusion = spawned.GetComponent<FusionObject>();
        if (fusion != null)
        {
            fusion.SetStage(stage);
        }

        int total = GetCurrentBrainrotCount();
        Debug.Log($"Brainrot stage {stage} spawneado en {spawnPos} | Total en escena: {total}");
        return true;
    }

    private Vector3 FindSpawnPosition()
    {
        const int maxAttempts = 10;

        for (int i = 0; i < maxAttempts; i++)
        {
            float x = UnityEngine.Random.Range(-6f, 6f);
            float y = UnityEngine.Random.Range(-4f, 4f);
            Vector2 pos = new Vector2(x, y);

            if (Physics2D.OverlapCircle(pos, 1.2f) == null)
            {
                return new Vector3(x, y, spawnZ);
            }
        }

        float fallbackX = UnityEngine.Random.Range(-6f, 6f);
        float fallbackY = UnityEngine.Random.Range(-4f, 4f);
        return new Vector3(fallbackX, fallbackY, spawnZ);
    }

    private int GetCurrentBrainrotCount()
    {
        return UnityEngine.Object.FindObjectsByType<FusionObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
    }

    private FusionObject GetBrainrotPrefabFusion()
    {
        if (brainrotPrefab == null) return null;
        return brainrotPrefab.GetComponent<FusionObject>();
    }

    private void TrySpawnInitialBrainrot()
    {
        if (!spawnInitialBrainrotOnSceneLoad) return;
        if (initialBrainrotSpawned) return;

        EnsureContainers();
        if (screen1Container == null) return;

        if (GetCurrentBrainrotCount() > 0)
        {
            initialBrainrotSpawned = true;
            return;
        }

        initialBrainrotSpawned = SpawnBrainrot(1);
    }

    private void TrackHighestStage(int stage)
    {
        int clamped = Mathf.Max(1, stage);
        if (clamped <= highestStageReached) return;

        highestStageReached = clamped;
        HighestStageReachedChanged?.Invoke(highestStageReached);
    }

    private void EnsureContainers()
    {
        if (!IsMainGameSceneActive())
        {
            return;
        }

        if (screen1Container == null)
        {
            GameObject existing = FindSceneObjectByName(screen1Name);
            if (existing != null && !IsUiTransform(existing.transform))
            {
                screen1Container = existing.transform;
            }

            if (screen1Container == null)
            {
                screen1Container = GetOrCreateRuntimeContainer(runtimeScreen1Name);
            }

            if (screen1Container == null)
            {
                Debug.LogWarning($"Scene object '{screen1Name}' not found. Please use MainGameScene > {screen1Name}.");
            }
        }

        if (screen2Container == null)
        {
            GameObject existing = FindSceneObjectByName(screen2Name);
            if (existing != null && !IsUiTransform(existing.transform))
            {
                screen2Container = existing.transform;
            }

            if (screen2Container == null)
            {
                screen2Container = GetOrCreateRuntimeContainer(runtimeScreen2Name);
            }

            if (screen2Container == null)
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

    private bool IsMainGameSceneActive()
    {
        return SceneManager.GetActiveScene().name == mainGameSceneName;
    }

    private GameObject FindSceneObjectByName(string objectName)
    {
        GameObject found = GameObject.Find(objectName);
        if (found != null)
        {
            return found;
        }

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate == null) continue;
            if (candidate.name != objectName) continue;
            if (!candidate.gameObject.scene.IsValid() || !candidate.gameObject.scene.isLoaded) continue;

            return candidate.gameObject;
        }

        return null;
    }

    private bool IsUiTransform(Transform candidate)
    {
        if (candidate == null) return false;
        return candidate.GetComponent<RectTransform>() != null || candidate.GetComponentInParent<Canvas>() != null;
    }

    private Transform GetOrCreateRuntimeContainer(string objectName)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || !activeScene.isLoaded) return null;

        GameObject[] roots = activeScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == objectName)
            {
                return roots[i].transform;
            }
        }

        GameObject container = new GameObject(objectName);
        return container.transform;
    }
}
