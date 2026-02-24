using System;
using System.Collections.Generic;
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
    [SerializeField] private int changeWorldUnlockStage = 11;
    [Header("Change World Button Visuals")]
    [SerializeField] private Sprite changeWorldScreen1Icon;
    [SerializeField] private Sprite changeWorldScreen2Icon;
    private Transform screen1Container;
    private Transform screen2Container;
    private bool changeWorldUnlocked = false;
    private bool showingScreen1 = true;
    private Button changeWorldButtonComponent;
    private Image changeWorldButtonImage;

    private int highestStageReached = 1;
    private readonly Dictionary<int, int> shopPurchaseCounts = new Dictionary<int, int>();
    private bool initialBrainrotSpawned = false;
    public const string SaveKey = "MergeBrainrotSave";
    private SaveData pendingSave;

    [Serializable]
    private class SaveData
    {
        public float money;
        public int highestStage;
        public bool autoSpawnEnabled;
        public float spawnInterval;
        public bool showingScreen1;
        public float globalMoneyMultiplier;
        public float globalAutoClickReduction;
        public bool hasUpgrades;
        public CanvasMejorasController.UpgradeSaveData upgrades;
        public List<BrainrotSave> brainrots = new List<BrainrotSave>();
        public List<ShopPurchaseSave> shopPurchases = new List<ShopPurchaseSave>();
    }

    [Serializable]
    private struct BrainrotSave
    {
        public int stage;
        public float x;
        public float y;
        public float z;
        public bool screen1;
    }

    [Serializable]
    private struct ShopPurchaseSave
    {
        public int stage;
        public int count;
    }

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
        SceneManager.sceneUnloaded += HandleSceneUnloaded;

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
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
        }
    }

    private void OnApplicationQuit()
    {
        if (IsMainGameSceneActive())
        {
            SaveState();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && IsMainGameSceneActive())
        {
            SaveState();
        }
    }

    public void RequestSave()
    {
        if (!IsMainGameSceneActive())
        {
            return;
        }

        SaveState();
    }

    private void Update()
    {
        if (brainrotPrefab == null) return;
        if (!autoSpawnEnabled) return;
        if (!showingScreen1) return;

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
        changeWorldButtonComponent = null;
        changeWorldButtonImage = null;
        initialBrainrotSpawned = false;
        spawnTimer = 0f;

        if (IsMainGameSceneActive())
        {
            // Ensure newly spawned stage-1 brainrots are visible on scene entry.
            showingScreen1 = true;
        }

        EnsureContainers();
        InitChangeWorldButton();
        LoadState();

        if (IsMainGameSceneActive())
        {
            if (pendingSave != null && pendingSave.brainrots != null && pendingSave.brainrots.Count > 0)
            {
                RestoreBrainrots(pendingSave.brainrots);
            }
            else
            {
                TrySpawnInitialBrainrot();
            }
        }

        ApplySavedUpgradesIfReady();
        UIManager.Instance?.UpdateMoney(currentMoney);
    }

    private void HandleSceneUnloaded(Scene scene)
    {
        // Avoid overwriting saved data after the main scene unloads.
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
        int limit = 1;
        if (highestStageReached >= 10)
        {
            limit = highestStageReached - 8;
        }
        return Mathf.Clamp(limit, 1, maxStage);
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
        if (stage > GetVisibleShopStageLimit()) return false;
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

        RegisterShopPurchase(stage);
        return true;
    }

    public int GetShopPurchaseCount(int stage)
    {
        if (stage < 1)
        {
            return 0;
        }

        return shopPurchaseCounts.TryGetValue(stage, out int count) ? count : 0;
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
        TryUnlockChangeWorldForStage(brainrot.stage);

        bool alreadyParented = IsInScreenContainer(brainrot.transform);

        bool justUnlocked = false;
        if (brainrot.stage >= 11)
        {
            MoveToScreen2(brainrot);
            justUnlocked = TryUnlockChangeWorldForStage(brainrot.stage);
            if (justUnlocked)
            {
                AutoSwitchToScreen2();
            }
        }
        else
        {
            if (!alreadyParented)
            {
                Transform target = GetTargetContainerForStage(brainrot.stage);
                if (target != null)
                {
                    brainrot.transform.SetParent(target, true);
                }
            }
        }
    }

    public void OnBrainrotStageChanged(FusionObject brainrot, int newStage)
    {
        if (brainrot == null) return;
        EnsureContainers();
        TrackHighestStage(newStage);
        if (newStage >= 11)
        {
            MoveToScreen2(brainrot);
            bool justUnlocked = TryUnlockChangeWorldForStage(newStage);
            if (justUnlocked)
            {
                AutoSwitchToScreen2();
            }
        }
        else
        {
            TryUnlockChangeWorldForStage(newStage);
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

        if (screen1Container == null && screen2Container == null)
        {
            Debug.LogWarning("Screen1 Brainrots container not found in scene. Spawn aborted.");
            return false;
        }

        Vector3 spawnPos = FindSpawnPosition();
        Transform targetContainer = GetTargetContainerForStage(stage);
        if (targetContainer == null)
        {
            Debug.LogWarning("Brainrot containers not found. Spawn aborted.");
            return false;
        }
        GameObject spawned = Instantiate(brainrotPrefab, spawnPos, Quaternion.identity, targetContainer);

        FusionObject fusion = spawned.GetComponent<FusionObject>();
        if (fusion != null)
        {
            fusion.SetStage(stage);
        }

        int total = GetCurrentBrainrotCount();
        Debug.Log($"Brainrot stage {stage} spawneado en {spawnPos} | Total en escena: {total}");
        return true;
    }

    private Transform GetTargetContainerForStage(int stage)
    {
        EnsureContainers();
        bool goesToScreen2 = stage >= 11;
        if (goesToScreen2)
        {
            return screen2Container ?? screen1Container;
        }

        return screen1Container ?? screen2Container;
    }

    private void SaveState()
    {
        SaveData data = new SaveData
        {
            money = currentMoney,
            highestStage = highestStageReached,
            autoSpawnEnabled = autoSpawnEnabled,
            spawnInterval = spawnInterval,
            showingScreen1 = showingScreen1,
            globalMoneyMultiplier = ClickableObject.GetGlobalMoneyMultiplier(),
            globalAutoClickReduction = ClickableObject.GetGlobalAutoClickReduction(),
            brainrots = CollectBrainrots(),
            shopPurchases = CollectShopPurchases()
        };

        CanvasMejorasController upgrades = FindFirstObjectByType<CanvasMejorasController>(FindObjectsInactive.Include);
        if (upgrades != null)
        {
            data.upgrades = upgrades.GetUpgradeSaveData();
            data.hasUpgrades = true;
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    private void LoadState()
    {
        pendingSave = null;

        if (!PlayerPrefs.HasKey(SaveKey))
        {
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        if (string.IsNullOrEmpty(json))
        {
            return;
        }

        SaveData data = JsonUtility.FromJson<SaveData>(json);
        if (data == null)
        {
            return;
        }

        currentMoney = data.money;
        highestStageReached = Mathf.Max(1, data.highestStage);
        autoSpawnEnabled = data.autoSpawnEnabled;
        spawnInterval = Mathf.Max(1f, data.spawnInterval);
        showingScreen1 = data.showingScreen1;

        ClickableObject.SetGlobalMoneyMultiplier(data.globalMoneyMultiplier);
        ClickableObject.SetGlobalAutoClickReduction(data.globalAutoClickReduction);
        RestoreShopPurchases(data.shopPurchases);

        pendingSave = data;
        TryUnlockChangeWorldForStage(highestStageReached);
        ApplyWorldVisibility();
    }

    private void RestoreBrainrots(List<BrainrotSave> brainrots)
    {
        ClearExistingBrainrots();
        EnsureContainers();

        for (int i = 0; i < brainrots.Count; i++)
        {
            BrainrotSave data = brainrots[i];
            Transform parent = data.screen1 ? screen1Container : screen2Container;
            if (parent == null)
            {
                parent = screen1Container ?? screen2Container;
            }
            if (parent == null) continue;

            Vector3 pos = new Vector3(data.x, data.y, data.z);
            SpawnBrainrotAt(data.stage, pos, parent);
        }

        initialBrainrotSpawned = true;
    }

    private void RegisterShopPurchase(int stage)
    {
        if (stage < 1)
        {
            return;
        }

        if (shopPurchaseCounts.TryGetValue(stage, out int count))
        {
            shopPurchaseCounts[stage] = count + 1;
        }
        else
        {
            shopPurchaseCounts[stage] = 1;
        }
    }

    private List<ShopPurchaseSave> CollectShopPurchases()
    {
        List<ShopPurchaseSave> list = new List<ShopPurchaseSave>();
        foreach (KeyValuePair<int, int> entry in shopPurchaseCounts)
        {
            if (entry.Key < 1 || entry.Value < 1)
            {
                continue;
            }

            list.Add(new ShopPurchaseSave
            {
                stage = entry.Key,
                count = entry.Value
            });
        }

        return list;
    }

    private void RestoreShopPurchases(List<ShopPurchaseSave> purchases)
    {
        shopPurchaseCounts.Clear();
        if (purchases == null)
        {
            return;
        }

        for (int i = 0; i < purchases.Count; i++)
        {
            ShopPurchaseSave entry = purchases[i];
            if (entry.stage < 1 || entry.count < 1)
            {
                continue;
            }

            shopPurchaseCounts[entry.stage] = entry.count;
        }
    }

    private void SpawnBrainrotAt(int stage, Vector3 position, Transform parent)
    {
        if (brainrotPrefab == null || parent == null) return;

        GameObject spawned = Instantiate(brainrotPrefab, position, Quaternion.identity, parent);
        FusionObject fusion = spawned.GetComponent<FusionObject>();
        if (fusion != null)
        {
            fusion.SetStage(stage);
        }
    }

    private void ClearExistingBrainrots()
    {
        FusionObject[] existing = FindObjectsByType<FusionObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++)
        {
            Destroy(existing[i].gameObject);
        }
    }

    private List<BrainrotSave> CollectBrainrots()
    {
        List<BrainrotSave> list = new List<BrainrotSave>();
        FusionObject[] existing = FindObjectsByType<FusionObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < existing.Length; i++)
        {
            FusionObject brainrot = existing[i];
            if (brainrot == null) continue;

            bool inScreen1 = screen1Container != null && brainrot.transform.IsChildOf(screen1Container);
            bool inScreen2 = screen2Container != null && brainrot.transform.IsChildOf(screen2Container);
            if (!inScreen1 && !inScreen2)
            {
                inScreen1 = true;
            }

            Vector3 pos = brainrot.transform.position;
            list.Add(new BrainrotSave
            {
                stage = brainrot.stage,
                x = pos.x,
                y = pos.y,
                z = pos.z,
                screen1 = inScreen1
            });
        }

        return list;
    }

    private bool IsInScreenContainer(Transform target)
    {
        if (target == null) return false;
        if (screen1Container != null && target.IsChildOf(screen1Container)) return true;
        if (screen2Container != null && target.IsChildOf(screen2Container)) return true;
        return false;
    }

    public void ApplySavedUpgradesIfReady()
    {
        if (pendingSave == null || !pendingSave.hasUpgrades)
        {
            return;
        }

        CanvasMejorasController upgrades = FindFirstObjectByType<CanvasMejorasController>(FindObjectsInactive.Include);
        if (upgrades == null)
        {
            return;
        }

        upgrades.ApplyUpgradeSaveData(pendingSave.upgrades);
        pendingSave.hasUpgrades = false;
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
        return UnityEngine.Object.FindObjectsByType<FusionObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
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
            GameObject found = FindSceneObjectByName("Change World Button");
            if (found != null) changeWorldButton = found;
        }

        if (changeWorldButton != null)
        {
            changeWorldButtonComponent = changeWorldButton.GetComponent<Button>();
            if (changeWorldButtonComponent == null)
            {
                changeWorldButtonComponent = changeWorldButton.GetComponentInChildren<Button>();
            }

            if (changeWorldButtonComponent != null)
            {
                changeWorldButtonComponent.onClick.RemoveListener(ToggleWorld);
                changeWorldButtonComponent.onClick.AddListener(ToggleWorld);
            }
            else
            {
                Debug.LogWarning("Change World Button is missing a Button component.");
            }

            changeWorldButtonImage = null;
            if (changeWorldButtonComponent != null)
            {
                changeWorldButtonImage = changeWorldButtonComponent.targetGraphic as Image;
            }
            if (changeWorldButtonImage == null)
            {
                changeWorldButtonImage = changeWorldButton.GetComponent<Image>();
            }
            if (changeWorldButtonImage == null)
            {
                changeWorldButtonImage = changeWorldButton.GetComponentInChildren<Image>();
            }

            if (changeWorldButtonImage != null && changeWorldScreen1Icon == null)
            {
                changeWorldScreen1Icon = changeWorldButtonImage.sprite;
            }

            // Keep the button above other UI elements so it stays visible in both worlds.
            changeWorldButton.transform.SetAsLastSibling();
            changeWorldButton.SetActive(changeWorldUnlocked);
            UpdateChangeWorldButtonVisual();
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

    private bool TryUnlockChangeWorldForStage(int stage)
    {
        int unlockStage = Mathf.Max(1, changeWorldUnlockStage);
        if (stage > unlockStage && !changeWorldUnlocked)
        {
            UnlockChangeWorld();
            return true;
        }

        return false;
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
        UIManager.Instance?.SetWorldBackground(showingScreen1);
        UpdateChangeWorldButtonVisual();
        if (changeWorldButton != null)
        {
            changeWorldButton.transform.SetAsLastSibling();
            changeWorldButton.SetActive(changeWorldUnlocked);
        }
    }

    private void UpdateChangeWorldButtonVisual()
    {
        if (changeWorldButtonImage == null) return;

        Sprite target = showingScreen1 ? changeWorldScreen1Icon : changeWorldScreen2Icon;
        if (target == null) return;

        if (changeWorldButtonImage.sprite != target)
        {
            changeWorldButtonImage.sprite = target;
        }
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
