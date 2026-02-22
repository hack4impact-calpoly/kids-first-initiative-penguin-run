using UnityEngine;

[ExecuteAlways]
public class SnapPiece : MonoBehaviour
{
    [Tooltip("Assign child snap points here (local positions).")]
    public Transform[] snapPoints;

    [Tooltip("How close (world units) two snap points must be to snap.")]
    public float snapRadius = 0.6f;

    [Tooltip("Layer containing track pieces to snap to.")]
    public LayerMask snapLayer;

    [Tooltip("Snap rotation to match the target snap point rotation.")]
    public bool snapRotation = true;

    // Call this after drag finishes (e.g. OnMouseUp / pointer up)
    public bool TrySnap()
    {
        if (snapPoints == null || snapPoints.Length == 0) return false;

        float bestDist = float.MaxValue;
        Vector3 bestDelta = Vector3.zero;
        Quaternion bestRotation = Quaternion.identity;
        bool found = false;

        // search each of my snap points
        foreach (var myPoint in snapPoints)
        {
            // find colliders near this point
            Collider2D[] hits = Physics2D.OverlapCircleAll(myPoint.position, snapRadius, snapLayer);

            foreach (var hit in hits)
            {
                // if the hit is my own collider, skip
                if (hit.transform.IsChildOf(transform)) continue;

                // find the SnapPiece on the hit object (in parent chain)
                SnapPiece other = hit.GetComponentInParent<SnapPiece>();
                if (other == null) continue;

                // compare all of that other piece's snap points
                foreach (var otherPoint in other.snapPoints)
                {
                    float d = Vector2.Distance(myPoint.position, otherPoint.position);
                    if (d <= snapRadius && d < bestDist)
                    {
                        bestDist = d;
                        bestDelta = otherPoint.position - myPoint.position;
                        found = true;

                        if (snapRotation)
                        {
                            // We want myPoint rotation to match otherPoint rotation.
                            // Compute the required delta rotation in world space.
                            bestRotation = otherPoint.rotation * Quaternion.Inverse(myPoint.rotation);
                        }
                    }
                }
            }
        }

        if (found)
        {
            // Apply snapping: move the whole piece by the delta
            transform.position += bestDelta;

            if (snapRotation)
            {
                // Apply rotation around the piece's pivot
                transform.rotation = bestRotation * transform.rotation;
            }

            Debug.Log($"Snapped {name} by {bestDelta} (dist {bestDist:F3})");
            return true;
        }

        // nothing to snap to
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (snapPoints == null) return;
        Gizmos.color = Color.yellow;
        foreach (var p in snapPoints)
        {
            if (p == null) continue;
            Gizmos.DrawWireSphere(p.position, snapRadius);
            Gizmos.DrawIcon(p.position, "sv_label_0", true); // small icon for visibility
        }
    }
    void OnMouseUp()
    {
        GetComponent<SnapPiece>().TrySnap();
    }
}

public class DragPiece : MonoBehaviour
{
    private Vector3 offset;

    void OnMouseDown()
    {
        var mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        offset = transform.position - mouseWorld;
    }

    void OnMouseDrag()
    {
        var mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = transform.position.z;
        transform.position = mouseWorld + offset;
    }

    void OnMouseUp()
    {
        GetComponent<SnapPiece>().TrySnap();
    }
}
