using UnityEngine;

public class Drag2D : MonoBehaviour
{
    private Vector3 offset;
    public bool isDragging { get; private set; }

    private FusionObject fusion;

    void Start()
    {
        fusion = GetComponent<FusionObject>();
    }

    void OnMouseDown()
    {
        isDragging = true;
        offset = transform.position - GetMouseWorldPos();
    }

    void OnMouseDrag()
    {
        transform.position = GetMouseWorldPos() + offset;
    }

    void OnMouseUp()
    {
        isDragging = false;
        fusion.TryFusionOnRelease();
    }

    Vector3 GetMouseWorldPos()
    {
        Vector3 mousePoint = Input.mousePosition;
        mousePoint.z = Camera.main.nearClipPlane;
        return Camera.main.ScreenToWorldPoint(mousePoint);
    }
}
