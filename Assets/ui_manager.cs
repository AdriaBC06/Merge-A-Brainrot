using UnityEngine;
using UnityEngine.SceneManagement;

public class ui_manager : MonoBehaviour
{
    public void OnStartPress()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
