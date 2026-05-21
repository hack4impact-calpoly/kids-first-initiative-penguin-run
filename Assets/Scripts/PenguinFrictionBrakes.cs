using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PenguinFrictionBrakes : MonoBehaviour
{
    [SerializeField] private LayerMask trackLayers = ~0;
    [SerializeField] private float rayDistance = 0.85f;

    private Rigidbody2D rb;

    public TrackTile CurrentTile { get; private set; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        RefreshCurrentTile();

        float a = CurrentTile != null ? CurrentTile.decel : 0f;
        Vector2 v = rb.linearVelocity;
        if (a > 0f && v.sqrMagnitude > 0.0001f)
        {
            float dv = a * Time.fixedDeltaTime;
            rb.linearVelocity = v.magnitude <= dv ? Vector2.zero : v - v.normalized * dv;
        }
    }

    public void Configure(LayerMask layers, float downwardRayDistance)
    {
        trackLayers = layers;
        rayDistance = downwardRayDistance;
    }

    private void RefreshCurrentTile()
    {
        CurrentTile = null;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, trackLayers);
        if (hit.collider != null)
        {
            CurrentTile = hit.collider.GetComponentInParent<TrackTile>();
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TrackTile tile = collision.collider.GetComponentInParent<TrackTile>();
        if (tile != null)
        {
            CurrentTile = tile;
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        TrackTile tile = collision.collider.GetComponentInParent<TrackTile>();
        if (tile != null && tile == CurrentTile)
        {
            CurrentTile = null;
        }
    }
}
