using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class DragPlacedPiece : MonoBehaviour
{
    public Camera worldCamera;
    public bool snapOnRelease = true;

    private Collider2D col;
    private Vector3 offset;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        if (worldCamera == null) worldCamera = Camera.main;
    }

    public void BeginDrag(PointerEventData eventData)
    {
        if (col) col.enabled = false;

        Vector3 mouseWorld = ScreenToWorld(eventData.position);

        // Put it exactly at cursor and don't keep a weird offset
        transform.position = mouseWorld;
        offset = Vector3.zero;
    }


    public void Drag(PointerEventData eventData)
    {
        Vector3 mouseWorld = ScreenToWorld(eventData.position);
        transform.position = mouseWorld + offset;
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (col) col.enabled = true;

        if (snapOnRelease)
        {
            var snap = GetComponent<SnapPiece>();
            if (snap != null) snap.TrySnap();
        }
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        if (worldCamera == null) worldCamera = Camera.main;
        Vector3 p = new Vector3(screenPos.x, screenPos.y, -worldCamera.transform.position.z);
        Vector3 w = worldCamera.ScreenToWorldPoint(p);
        w.z = transform.position.z;
        return w;
    }
}
