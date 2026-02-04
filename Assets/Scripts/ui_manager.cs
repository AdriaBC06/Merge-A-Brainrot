using UnityEngine;
using UnityEngine.SceneManagement;

public class Ui_manager : MonoBehaviour
{
    public void OnStartPress()
    {
        SceneManager.LoadScene(2);
    }

    public void OnSettingsPress()
    {
        Settings_manager.SettingsOpen();
    }
}
