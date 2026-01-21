using UnityEngine;
using UnityEngine.SceneManagement;

public class Settings_manager : MonoBehaviour
{   
    static int index;
    public static void SettingsOpen()
    {
        index = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(1);
    }

    public void SettingsClose()
    {
        SceneManager.LoadScene(index);
    }
}
