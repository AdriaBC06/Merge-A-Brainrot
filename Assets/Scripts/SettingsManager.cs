using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{   
    static int index;

    public AudioMixer audioMixer;
    
    public void SetVolume(float volume)
    {
        audioMixer.SetFloat("volume",volume);
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
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public static void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
    }


}
