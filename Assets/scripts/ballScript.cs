using UnityEngine;

public class ballScript : MonoBehaviour
{
     private Rigidbody2D rb;
    [SerializeField] private Vector2 startingVelocity = new Vector2(3000f, 0f);
    [SerializeField] private bool applyStartingVelocity = true;
    [SerializeField] private float startingVelocityDuration = 1f;
    [SerializeField] private float startingImpulse = 3000f;
    private bool hasStarted;
    private float applyStartingVelocityUntil;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.simulated = false; 
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartPenguin();
        }
    }

    private void FixedUpdate()
    {
        if (applyStartingVelocity && hasStarted && rb != null && rb.simulated && Time.fixedTime <= applyStartingVelocityUntil)
        {
            ApplyStartingVelocity();
        }
    }

    private void StartPenguin()
    {
        if (rb == null || hasStarted)
        {
            return;
        }

        hasStarted = true;
        rb.simulated = true;
        if (applyStartingVelocity)
        {
            applyStartingVelocityUntil = Time.fixedTime + startingVelocityDuration;
            rb.WakeUp();
            rb.AddForce(Vector2.right * startingImpulse, ForceMode2D.Impulse);
            ApplyStartingVelocity();
        }
    }

    private void ApplyStartingVelocity()
    {
        Vector2 velocity = rb.linearVelocity;
        velocity.x = Mathf.Max(velocity.x, startingVelocity.x);

        if (!Mathf.Approximately(startingVelocity.y, 0f))
        {
            velocity.y = startingVelocity.y;
        }

        rb.linearVelocity = velocity;
    }
}
