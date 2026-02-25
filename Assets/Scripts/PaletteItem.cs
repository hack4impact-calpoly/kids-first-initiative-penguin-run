using UnityEngine;
using UnityEngine.EventSystems;

public class PaletteItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public GameObject piecePrefab;     // assign StraightTrackPieceBase prefab here
    public Camera worldCamera;         // usually Main Camera

    private GameObject spawned;
    private DragPlacedPiece drag;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (worldCamera == null) worldCamera = Camera.main;

        Vector3 p = new Vector3(eventData.position.x, eventData.position.y, -worldCamera.transform.position.z);
        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(p);

        // keep prefab depth and rotation
        mouseWorld.z = piecePrefab.transform.position.z;                 // e.g. -30
        Quaternion rot = piecePrefab.transform.rotation;                 // keeps ramp at -30, straight at 0

        spawned = Instantiate(piecePrefab, mouseWorld, rot);

        drag = spawned.GetComponent<DragPlacedPiece>();
        if (drag == null) drag = spawned.AddComponent<DragPlacedPiece>();

        drag.worldCamera = worldCamera;
        drag.BeginDrag(eventData);
    }



    public void OnDrag(PointerEventData eventData)
    {
        if (drag != null) drag.Drag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (drag != null) drag.EndDrag(eventData);
        spawned = null;
        drag = null;
    }
}
