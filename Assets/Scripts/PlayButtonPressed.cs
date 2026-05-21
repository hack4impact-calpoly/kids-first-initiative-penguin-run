using UnityEngine;

public class PlayButtonPressed : MonoBehaviour
{
    public ResetLevel resetLevel;
    [SerializeField] private Vector2 startingVelocity = new Vector2(3000f, 0f);
    [SerializeField] private bool applyStartingVelocity = true;
    [SerializeField] private float startingVelocityDuration = 1f;
    [SerializeField] private float startingImpulse = 3000f;

    private float camWidth;
    private float camHeight;
    private Camera cam;

    public Rigidbody2D penguinRb;
    public GameObject penguin;
    private bool hasStarted;
    private float applyStartingVelocityUntil;

    private void Start()
    {
        // Session ID should be set by web backend/authentication before game loads
        string sessionId = PlayerPrefs.GetString("sessionId", "");
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogWarning("[PlayButtonPressed] Session ID not set. Game events won't be saved.");
        }

        penguin = GameObject.FindGameObjectWithTag("Player");
        penguinRb = penguin.GetComponent<Rigidbody2D>();

        penguinRb.simulated = false;

        // Get the camera information
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        camHeight = 2f * cam.orthographicSize;
        camWidth = camHeight * cam.aspect;

        resetLevel = FindFirstObjectByType<ResetLevel>();
    }

    void Update()
    {   
        // These lines are to debug
        // Vector3 penguinPos = penguin.transform.position;
        // Debug.Log("Penguin position: " + penguinPos);

        // If the penguin's position is outside of the camera position
        // cam.transform.position gets the midpoint of the camera
        if (DialogueManager.IsDialogueOpen){
            return;
        }
        if (penguin.transform.position.x > (cam.transform.position.x + camWidth / 2f) ||
            penguin.transform.position.y > (cam.transform.position.y + camHeight / 2f) ||
            penguin.transform.position.x < (cam.transform.position.x - camWidth / 2f) ||
            penguin.transform.position.y < (cam.transform.position.y - camHeight / 2f))
        {
            resetLevel.ResetGame();
        }
    }

    public void PlayButtonClicked()
    {
        if (DialogueManager.IsDialogueOpen){
            return;
        }
        StartPenguin();
    }

    private void FixedUpdate()
    {
        if (applyStartingVelocity && hasStarted && penguinRb != null && penguinRb.simulated && Time.fixedTime <= applyStartingVelocityUntil)
        {
            ApplyStartingVelocity();
        }
    }

    private void StartPenguin()
    {
        if (penguinRb == null || hasStarted)
        {
            return;
        }

        hasStarted = true;
        penguinRb.simulated = true;
        if (applyStartingVelocity)
        {
            applyStartingVelocityUntil = Time.fixedTime + startingVelocityDuration;
            penguinRb.WakeUp();
            penguinRb.AddForce(Vector2.right * startingImpulse, ForceMode2D.Impulse);
            ApplyStartingVelocity();
        }
    }

    private void ApplyStartingVelocity()
    {
        Vector2 velocity = penguinRb.linearVelocity;
        velocity.x = Mathf.Max(velocity.x, startingVelocity.x);

        if (!Mathf.Approximately(startingVelocity.y, 0f))
        {
            velocity.y = startingVelocity.y;
        }

        penguinRb.linearVelocity = velocity;
    }
}
