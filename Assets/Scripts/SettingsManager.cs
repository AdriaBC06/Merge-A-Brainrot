using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    static int index;

    public AudioMixer audioMixer;

    private static SettingsManager instance;

    private Image musicIcon;
    private Image coinIcon;
    private SpaceGUI.Toggle musicToggle;

    public void SetVolume(float volume)
    {
        if (audioMixer != null)
        {
            audioMixer.SetFloat("volume", volume);
        }

        SoundManager.EnsureExists().SetMasterVolumeDb(volume);
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
        // Intentionally left blank (quality option removed from UI).
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
        ConfigureCoinSection();
        SyncToggleStates();
    }

    public static void SetMusicMuted(bool muted)
    {
        SoundManager.EnsureExists().SetMusicMuted(muted);
        instance?.UpdateIconStates();
    }

    public void ToggleCoinSound()
    {
        SoundManager.EnsureExists().ToggleCoinMuted();
        SoundManager.Instance?.PlayClick();
        UpdateIconStates();
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
                label.text = "Musica";
            }

            musicToggle = musicSection.GetComponentInChildren<SpaceGUI.Toggle>(true);
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

    private void ConfigureCoinSection()
    {
        GameObject coinIconObject = GameObject.Find("Sound_Icon");
        if (coinIconObject != null)
        {
            coinIcon = coinIconObject.GetComponent<Image>();
            if (coinIcon != null)
            {
                Button button = coinIcon.GetComponent<Button>();
                if (button == null)
                {
                    button = coinIcon.gameObject.AddComponent<Button>();
                }
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = coinIcon;
                button.onClick.RemoveListener(ToggleCoinSound);
                button.onClick.AddListener(ToggleCoinSound);
            }
        }

        GameObject soundTextObject = GameObject.Find("Sound_TXT");
        if (soundTextObject != null)
        {
            TMP_Text soundText = soundTextObject.GetComponent<TMP_Text>();
            if (soundText != null)
            {
                soundText.text = "Moneda";
            }
        }
    }

    private void SyncToggleStates()
    {
        if (musicToggle != null)
        {
            bool muted = SoundManager.Instance != null && SoundManager.Instance.IsMusicMuted();
            musicToggle.SetState(!muted, false);
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

        if (coinIcon != null)
        {
            bool muted = SoundManager.Instance != null && SoundManager.Instance.IsCoinMuted();
            coinIcon.color = SetMutedAlpha(coinIcon.color, muted);
        }
    }

    private Color SetMutedAlpha(Color color, bool muted)
    {
        color.a = muted ? 0.35f : 1f;
        return color;
    }
}
