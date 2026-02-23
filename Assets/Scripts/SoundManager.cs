using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string PrefMusicMuted = "Settings.MusicMuted";
    private const string PrefCoinMuted = "Settings.CoinMuted";
    private const string PrefMasterVolume = "Settings.MasterVolume";

    [Header("Clips (Resources/Audio)")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip fusionClip;
    [SerializeField] private AudioClip coinClip;
    [SerializeField] private AudioClip purchaseClip;
    [SerializeField] private AudioClip musicClip;

    [Header("Volumes")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.7f;

    private AudioSource sfxSource;
    private AudioSource musicSource;
    private bool musicMuted;
    private bool coinMuted;
    private float masterVolume = 1f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject host = new GameObject("SoundManager");
        host.AddComponent<SoundManager>();
    }

    public static SoundManager EnsureExists()
    {
        if (Instance != null)
        {
            return Instance;
        }

        GameObject host = new GameObject("SoundManager");
        return host.AddComponent<SoundManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource = gameObject.AddComponent<AudioSource>();
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        LoadClipsIfNeeded();
        LoadPreferences();
        ApplyVolumes();
        ApplyMuteStates();
    }

    private void Start()
    {
        PlayMusic();
    }

    private void LoadClipsIfNeeded()
    {
        if (clickClip == null)
        {
            clickClip = Resources.Load<AudioClip>("Audio/Click");
        }

        if (fusionClip == null)
        {
            fusionClip = Resources.Load<AudioClip>("Audio/Fusion");
        }

        if (coinClip == null)
        {
            coinClip = Resources.Load<AudioClip>("Audio/Moneda");
        }

        if (purchaseClip == null)
        {
            purchaseClip = Resources.Load<AudioClip>("Audio/Mercado");
        }

        if (musicClip == null)
        {
            musicClip = Resources.Load<AudioClip>("Audio/MusicaFondo");
        }
    }

    private void LoadPreferences()
    {
        musicMuted = PlayerPrefs.GetInt(PrefMusicMuted, 0) == 1;
        coinMuted = PlayerPrefs.GetInt(PrefCoinMuted, 0) == 1;
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(PrefMasterVolume, 1f));
    }

    private void ApplyVolumes()
    {
        if (sfxSource != null)
        {
            sfxSource.volume = masterVolume * sfxVolume;
        }

        if (musicSource != null)
        {
            musicSource.volume = masterVolume * musicVolume;
        }
    }

    private void ApplyMuteStates()
    {
        if (musicSource != null)
        {
            musicSource.mute = musicMuted;
        }
    }

    public void SetMasterVolumeDb(float db)
    {
        float linear = Mathf.Pow(10f, db / 20f);
        SetMasterVolume(linear);
    }

    public void SetMasterVolume(float linear)
    {
        masterVolume = Mathf.Clamp01(linear);
        PlayerPrefs.SetFloat(PrefMasterVolume, masterVolume);
        ApplyVolumes();
    }

    public bool IsMusicMuted()
    {
        return musicMuted;
    }

    public bool IsCoinMuted()
    {
        return coinMuted;
    }

    public void SetMusicMuted(bool muted)
    {
        musicMuted = muted;
        PlayerPrefs.SetInt(PrefMusicMuted, muted ? 1 : 0);
        ApplyMuteStates();
    }

    public void ToggleMusicMuted()
    {
        SetMusicMuted(!musicMuted);
    }

    public void SetCoinMuted(bool muted)
    {
        coinMuted = muted;
        PlayerPrefs.SetInt(PrefCoinMuted, muted ? 1 : 0);
    }

    public void ToggleCoinMuted()
    {
        SetCoinMuted(!coinMuted);
    }

    public void PlayClick()
    {
        PlayOneShot(clickClip);
    }

    public void PlayFusion()
    {
        PlayOneShot(fusionClip);
    }

    public void PlayCoin()
    {
        if (coinMuted)
        {
            return;
        }

        PlayOneShot(coinClip);
    }

    public void PlayPurchase()
    {
        PlayOneShot(purchaseClip);
    }

    public void PlayMusic()
    {
        if (musicClip == null || musicSource == null)
        {
            return;
        }

        if (musicSource.clip != musicClip)
        {
            musicSource.clip = musicClip;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }
}
