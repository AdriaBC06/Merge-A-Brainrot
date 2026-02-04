using UnityEngine;

public class ChangeWorld : MonoBehaviour
{
    public void OnChangeWorldPressed()
    {
        GameManager.Instance?.ToggleWorld();
    }

    public void ToggleWorld()
    {
        GameManager.Instance?.ToggleWorld();
    }
}
