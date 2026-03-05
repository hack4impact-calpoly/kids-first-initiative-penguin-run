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

    float r = EffectiveSnapRadius();

    float bestDist = float.MaxValue;
    Vector3 bestDelta = Vector3.zero;
    Quaternion bestRotation = Quaternion.identity;
    bool found = false;

    foreach (var myPoint in snapPoints)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(myPoint.position, r, snapLayer);

        foreach (var hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;

            SnapPiece other = hit.GetComponentInParent<SnapPiece>();
            if (other == null) continue;

            foreach (var otherPoint in other.snapPoints)
            {
                float d = Vector2.Distance(myPoint.position, otherPoint.position);
                if (d <= r && d < bestDist)
                {
                    bestDist = d;
                    bestDelta = otherPoint.position - myPoint.position;
                    found = true;

                    if (snapRotation)
                        bestRotation = otherPoint.rotation * Quaternion.Inverse(myPoint.rotation);
                }
            }
        }
    }

    if (found)
    {
        transform.position += bestDelta;
        if (snapRotation) transform.rotation = bestRotation * transform.rotation;
        return true;
    }

    return false;
}

    private void OnDrawGizmosSelected()
    {
        if (snapPoints == null) return;
        Gizmos.color = Color.yellow;
        foreach (var p in snapPoints)
        {
            if (p == null) continue;
            Gizmos.DrawWireSphere(p.position, EffectiveSnapRadius());
            Gizmos.DrawIcon(p.position, "sv_label_0", true); // small icon for visibility
        }
    }
    float EffectiveSnapRadius()
    {
    return snapRadius * transform.lossyScale.x;
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
