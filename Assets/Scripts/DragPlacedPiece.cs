using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider2D))]
public class DragPlacedPiece : MonoBehaviour
{
    public Camera worldCamera;
    public bool snapOnRelease = true;

    /// <summary>
    /// The piece currently under the player's finger, or null. Exposed so guidance can react to a
    /// drag in progress without every drag site having to notify it.
    /// </summary>
    public static DragPlacedPiece ActiveDrag { get; private set; }

    private Collider2D col;
    private Vector3 offset;

    void Awake()
    {
        if (DialogueManager.IsDialogueOpen){
            return;
        }
        
        col = GetComponent<Collider2D>();
        if (worldCamera == null) worldCamera = Camera.main;
    }

    public void BeginDrag(PointerEventData eventData)
    {
        if (DialogueManager.IsDialogueOpen){
            return;
        }
        if (col) col.enabled = false;
        ActiveDrag = this;

        Vector3 mouseWorld = ScreenToWorld(eventData.position);

        // Put it exactly at cursor and don't keep a weird offset
        transform.position = mouseWorld;
        offset = Vector3.zero;
    }


    public void Drag(PointerEventData eventData)
    {
        if (DialogueManager.IsDialogueOpen){
            return;
        }
        Vector3 mouseWorld = ScreenToWorld(eventData.position);
        transform.position = mouseWorld + offset;
    }

    public void EndDrag(PointerEventData eventData)
    {
        if (DialogueManager.IsDialogueOpen){
            return;
        }
        if (col) col.enabled = true;

        bool snapped = false;
        if (snapOnRelease)
        {
            var snap = GetComponent<SnapPiece>();
            if (snap != null) snapped = snap.TrySnap();
        }

        if (ActiveDrag == this) ActiveDrag = null;
        TrackPlacementGuidance.NotifyDragEnded(snapped);
    }

    private void OnDisable()
    {
        // A piece can be destroyed mid-drag; leaving a stale reference would freeze the guidance.
        if (ActiveDrag == this) ActiveDrag = null;
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
