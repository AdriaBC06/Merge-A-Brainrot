using UnityEngine;
using UnityEngine.SceneManagement;

public class Ui_manager : MonoBehaviour
{
    public void OnStartPress()
    {
        
    }

    public void OnSettingsPress()
    {
        Settings_manager.SettingsOpen();
    }
}
