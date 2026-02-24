using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    static int index;

    private static SettingsManager instance;

    private Image musicIcon;
    private Image popIcon;
    private SpaceGUI.Toggle musicToggle;
    private SpaceGUI.Toggle popToggle;
    private Slider musicSlider;
    private Slider sfxSlider;

    [Header("Mute Buttons Layout")]
    [SerializeField] private bool overrideMuteLayout = true;
    [SerializeField] private float muteMusicToggleX = -220f;
    [SerializeField] private float mutePopToggleX = 220f;
    [SerializeField] private float muteMusicLabelX = -220f;
    [SerializeField] private float mutePopLabelX = 220f;

    public void SetVolume(float volume)
    {
        SoundManager.EnsureExists().SetSfxVolumeDb(volume);
    }

    public void SetMusicVolume(float volume)
    {
        SoundManager.EnsureExists().SetMusicVolumeDb(volume);
    }

    public static void SettingsOpen()
    {
        index = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(1);
    }

    public void SettingsClose()
    {
        SceneManager.LoadScene(index);
    }

    public void SetQuality(int qualityIndex)
    {
        // No-op: kept for legacy UI bindings in the settings scene.
    }

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        SoundManager.EnsureExists();
        HideLegacyOptions();
        ConfigureMusicSection();
        ConfigureSoundSection();
        ConfigureMuteLabels();
        ConfigureSliders();
        SyncToggleStates();
    }

    public static void SetMusicMuted(bool muted)
    {
        SoundManager.EnsureExists().SetMusicMuted(muted);
        instance?.UpdateIconStates();
    }

    public static void SetPopMuted(bool muted)
    {
        SoundManager.EnsureExists().SetSfxMuted(muted);
        instance?.UpdateIconStates();
    }

    private void HideLegacyOptions()
    {
        GameObject dropdown = GameObject.Find("Dropdown_Large");
        if (dropdown != null)
        {
            dropdown.SetActive(false);
        }

        GameObject qualityLabel = GameObject.Find("Language");
        if (qualityLabel != null)
        {
            qualityLabel.SetActive(false);
        }
    }

    private void ConfigureMusicSection()
    {
        GameObject musicSection = GameObject.Find("Fullscreen");
        if (musicSection != null)
        {
            TMP_Text label = musicSection.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.text = "Mute Music";
                label.alignment = TextAlignmentOptions.Center;
            }

            musicToggle = musicSection.GetComponentInChildren<SpaceGUI.Toggle>(true);
            RectTransform musicRect = musicSection.GetComponent<RectTransform>();
            if (musicRect != null && overrideMuteLayout)
            {
                musicRect.anchoredPosition = new Vector2(muteMusicToggleX, musicRect.anchoredPosition.y);
            }
        }

        GameObject musicIconObject = GameObject.Find("Music_Icon");
        if (musicIconObject != null)
        {
            musicIcon = musicIconObject.GetComponent<Image>();
        }

        GameObject musicTextObject = GameObject.Find("Music_TXT");
        if (musicTextObject != null)
        {
            TMP_Text musicText = musicTextObject.GetComponent<TMP_Text>();
            if (musicText != null)
            {
                musicText.text = "Musica";
            }
        }
    }

    private void ConfigureSoundSection()
    {
        GameObject popIconObject = GameObject.Find("Sound_Icon");
        if (popIconObject != null)
        {
            popIcon = popIconObject.GetComponent<Image>();
        }

        GameObject soundTextObject = GameObject.Find("Sound_TXT");
        if (soundTextObject != null)
        {
            TMP_Text soundText = soundTextObject.GetComponent<TMP_Text>();
            if (soundText != null)
            {
                soundText.text = "Pop";
            }
        }

        ConfigurePopToggle();
    }

    private void ConfigureMuteLabels()
    {
        TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        TMP_Text musicLabel = null;

        foreach (TMP_Text text in texts)
        {
            if (text == null) continue;
            if (string.Equals(text.text?.Trim(), "Fullscreen", StringComparison.OrdinalIgnoreCase))
            {
                musicLabel = text;
                break;
            }
        }

        if (musicLabel != null)
        {
            musicLabel.text = "Mute Music";
            musicLabel.alignment = TextAlignmentOptions.Center;
            if (overrideMuteLayout)
            {
                musicLabel.rectTransform.anchoredPosition = new Vector2(
                    muteMusicLabelX,
                    musicLabel.rectTransform.anchoredPosition.y
                );
            }
        }

        if (musicLabel == null)
        {
            return;
        }

        Transform parent = musicLabel.transform.parent;
        if (parent == null)
        {
            return;
        }

        TMP_Text popLabel = null;
        Transform existing = parent.Find("Mute_Pop_Label");
        if (existing != null)
        {
            popLabel = existing.GetComponent<TMP_Text>();
        }
        else
        {
            GameObject clone = Instantiate(musicLabel.gameObject, parent);
            clone.name = "Mute_Pop_Label";
            popLabel = clone.GetComponent<TMP_Text>();
        }

        if (popLabel == null)
        {
            return;
        }

        popLabel.text = "Mute Pop";
        popLabel.alignment = TextAlignmentOptions.Center;

        RectTransform musicRect = musicLabel.rectTransform;
        RectTransform popRect = popLabel.rectTransform;
        if (musicRect != null && popRect != null)
        {
            if (overrideMuteLayout || existing == null)
            {
                popRect.anchoredPosition = new Vector2(mutePopLabelX, popRect.anchoredPosition.y);
            }
        }
    }

    private void ConfigurePopToggle()
    {
        GameObject musicSection = GameObject.Find("Fullscreen");
        if (musicSection == null)
        {
            return;
        }

        Transform parent = musicSection.transform.parent;
        if (parent == null)
        {
            return;
        }

        Transform existing = parent.Find("Mute_Pop");
        GameObject popObject = existing != null ? existing.gameObject : Instantiate(musicSection, parent);
        popObject.name = "Mute_Pop";

        RectTransform popRect = popObject.GetComponent<RectTransform>();
        RectTransform musicRect = musicSection.GetComponent<RectTransform>();
        if (popRect != null && musicRect != null)
        {
            if (overrideMuteLayout || existing == null)
            {
                popRect.anchoredPosition = new Vector2(mutePopToggleX, popRect.anchoredPosition.y);
            }
        }

        TMP_Text label = popObject.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = "Mute Pop";
            label.alignment = TextAlignmentOptions.Center;
        }

        popToggle = popObject.GetComponentInChildren<SpaceGUI.Toggle>(true);
        if (popToggle != null)
        {
            popToggle.toggleTarget = SpaceGUI.Toggle.ToggleTarget.Pop;
        }
    }

    private void SyncToggleStates()
    {
        if (musicToggle != null)
        {
            bool muted = SoundManager.Instance != null && SoundManager.Instance.IsMusicMuted();
            musicToggle.SetState(!muted, false);
        }

        if (popToggle != null)
        {
            bool muted = SoundManager.Instance != null && SoundManager.Instance.IsSfxMuted();
            popToggle.SetState(!muted, false);
        }

        UpdateIconStates();
    }

    private void UpdateIconStates()
    {
        if (musicIcon != null)
        {
            bool muted = SoundManager.Instance != null && SoundManager.Instance.IsMusicMuted();
            musicIcon.color = SetMutedAlpha(musicIcon.color, muted);
        }

        if (popIcon != null)
        {
            bool muted = SoundManager.Instance != null && SoundManager.Instance.IsSfxMuted();
            popIcon.color = SetMutedAlpha(popIcon.color, muted);
        }

    }

    private Color SetMutedAlpha(Color color, bool muted)
    {
        color.a = muted ? 0.35f : 1f;
        return color;
    }

    private void ConfigureSliders()
    {
        GameObject musicRoot = GameObject.Find("Music_Slider");
        if (musicRoot != null)
        {
            musicSlider = musicRoot.GetComponentInChildren<Slider>(true);
        }

        GameObject soundRoot = GameObject.Find("Sound_Slider");
        if (soundRoot != null)
        {
            sfxSlider = soundRoot.GetComponentInChildren<Slider>(true);
        }

        if (musicSlider != null)
        {
            musicSlider.onValueChanged.RemoveAllListeners();
            musicSlider.minValue = -80f;
            musicSlider.maxValue = 0f;
            musicSlider.wholeNumbers = true;
            musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveAllListeners();
            sfxSlider.minValue = -80f;
            sfxSlider.maxValue = 0f;
            sfxSlider.wholeNumbers = true;
            sfxSlider.onValueChanged.AddListener(SetVolume);
        }

        if (SoundManager.Instance != null)
        {
            if (musicSlider != null)
            {
                musicSlider.SetValueWithoutNotify(SoundManager.Instance.GetMusicVolumeDb());
            }

            if (sfxSlider != null)
            {
                sfxSlider.SetValueWithoutNotify(SoundManager.Instance.GetSfxVolumeDb());
            }
        }
    }
}
