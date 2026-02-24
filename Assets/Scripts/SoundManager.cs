using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    private const string PrefMusicMuted = "Settings.MusicMuted";
    private const string PrefSfxMuted = "Settings.SfxMuted";
    private const string PrefMusicVolumeDb = "Settings.MusicVolumeDb";
    private const string PrefSfxVolumeDb = "Settings.SfxVolumeDb";

    [Header("Clips (Resources/Audio)")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip fusionClip;
    [SerializeField] private AudioClip purchaseClip;
    [SerializeField] private AudioClip musicClip;

    [Header("Volumes")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 1f;
    [Range(0f, 1f)][SerializeField] private float musicVolume = 0.7f;

    private AudioSource sfxSource;
    private AudioSource musicSource;
    private bool musicMuted;
    private bool sfxMuted;
    private float musicVolumeDb = 0f;
    private float sfxVolumeDb = 0f;

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
        sfxMuted = PlayerPrefs.GetInt(PrefSfxMuted, 0) == 1;
        musicVolumeDb = PlayerPrefs.HasKey(PrefMusicVolumeDb)
            ? PlayerPrefs.GetFloat(PrefMusicVolumeDb, 0f)
            : 0f;
        sfxVolumeDb = PlayerPrefs.HasKey(PrefSfxVolumeDb)
            ? PlayerPrefs.GetFloat(PrefSfxVolumeDb, 0f)
            : LinearToDb(sfxVolume);
        musicVolume = DbToLinear(musicVolumeDb);
        sfxVolume = DbToLinear(sfxVolumeDb);
    }

    private void ApplyVolumes()
    {
        if (sfxSource != null)
        {
            sfxSource.volume = sfxVolume;
        }

        if (musicSource != null)
        {
            musicSource.volume = musicVolume;
        }
    }

    private void ApplyMuteStates()
    {
        if (musicSource != null)
        {
            musicSource.mute = musicMuted;
        }

        if (sfxSource != null)
        {
            sfxSource.mute = sfxMuted;
        }
    }

    public void SetMusicVolumeDb(float db)
    {
        musicVolumeDb = Mathf.Clamp(db, -80f, 0f);
        PlayerPrefs.SetFloat(PrefMusicVolumeDb, musicVolumeDb);
        PlayerPrefs.Save();
        musicVolume = DbToLinear(musicVolumeDb);
        ApplyVolumes();
    }

    public void SetSfxVolumeDb(float db)
    {
        sfxVolumeDb = Mathf.Clamp(db, -80f, 0f);
        PlayerPrefs.SetFloat(PrefSfxVolumeDb, sfxVolumeDb);
        PlayerPrefs.Save();
        sfxVolume = DbToLinear(sfxVolumeDb);
        ApplyVolumes();
    }

    public float GetMusicVolumeDb()
    {
        return musicVolumeDb;
    }

    public float GetSfxVolumeDb()
    {
        return sfxVolumeDb;
    }

    public bool IsMusicMuted()
    {
        return musicMuted;
    }

    public bool IsSfxMuted()
    {
        return sfxMuted;
    }

    public void SetMusicMuted(bool muted)
    {
        musicMuted = muted;
        PlayerPrefs.SetInt(PrefMusicMuted, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMuteStates();
    }

    public void ToggleMusicMuted()
    {
        SetMusicMuted(!musicMuted);
    }

    public void SetSfxMuted(bool muted)
    {
        sfxMuted = muted;
        PlayerPrefs.SetInt(PrefSfxMuted, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMuteStates();
    }

    public void ToggleSfxMuted()
    {
        SetSfxMuted(!sfxMuted);
    }

    public void PlayClick()
    {
        if (sfxMuted)
        {
            return;
        }

        PlayOneShot(clickClip);
    }

    public void PlayFusion()
    {
        PlayOneShot(fusionClip);
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

    private float DbToLinear(float db)
    {
        return Mathf.Clamp01(Mathf.Pow(10f, db / 20f));
    }

    private float LinearToDb(float linear)
    {
        float clamped = Mathf.Clamp(linear, 0.0001f, 1f);
        return Mathf.Clamp(20f * Mathf.Log10(clamped), -80f, 0f);
    }
}
