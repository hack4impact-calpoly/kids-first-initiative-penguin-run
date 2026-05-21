using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayButtonPressed : MonoBehaviour
{
    public ResetLevel resetLevel;
    [SerializeField] private Vector2 startingVelocity = new Vector2(3000f, 0f);
    [SerializeField] private bool applyStartingVelocity = true;
    [SerializeField] private float startingVelocityDuration = 1f;
    [SerializeField] private float startingImpulse = 3000f;
    [SerializeField] private float stoppedSpeedThreshold = 8f;
    [SerializeField] private float stoppedDurationBeforeFailure = 1.1f;
    [SerializeField] private float minimumRunTimeBeforeStopFailure = 1.25f;

    private float camWidth;
    private float camHeight;
    private Camera cam;

    public Rigidbody2D penguinRb;
    public GameObject penguin;
    private bool hasStarted;
    private bool reachedGoal;
    private bool hasMovedSinceStart;
    private float applyStartingVelocityUntil;
    private float startTime;
    private float stoppedTimer;
    private Vector3 initialPenguinPosition;
    private Quaternion initialPenguinRotation;
    private FailureFeedbackManager failureFeedback;
    private bool failureFeedbackEnabled;

    private void OnEnable()
    {
        goal_Indicator.PlayerReachedGoal += HandlePlayerReachedGoal;
    }

    private void OnDisable()
    {
        goal_Indicator.PlayerReachedGoal -= HandlePlayerReachedGoal;
    }

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
        initialPenguinPosition = penguin.transform.position;
        initialPenguinRotation = penguin.transform.rotation;

        penguinRb.simulated = false;

        // Get the camera information
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        camHeight = 2f * cam.orthographicSize;
        camWidth = camHeight * cam.aspect;

        resetLevel = FindFirstObjectByType<ResetLevel>();
        failureFeedbackEnabled = SceneManager.GetActiveScene().name == "Penguin Run Level 1";
        if (failureFeedbackEnabled)
        {
            failureFeedback = FindFirstObjectByType<FailureFeedbackManager>();
            if (failureFeedback == null)
            {
                failureFeedback = gameObject.AddComponent<FailureFeedbackManager>();
            }

            failureFeedback.Initialize(this);
        }
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
            if (failureFeedbackEnabled)
            {
                ReportFailure(FailureFeedbackManager.FailureState.GapLeft);
            }
            else
            {
                resetLevel.ResetGame();
            }

            return;
        }

        if (failureFeedbackEnabled)
        {
            DetectStoppedBeforeGoal();
        }
    }

    public void PlayButtonClicked()
    {
        if (DialogueManager.IsDialogueOpen){
            return;
        }

        if (failureFeedbackEnabled && failureFeedback != null && failureFeedback.PlacedPieceCount == 0)
        {
            ReportFailure(FailureFeedbackManager.FailureState.NoPieces);
            return;
        }

        StartPenguin();
    }

    public void ResetPenguinForBuilding()
    {
        StopAllCoroutines();
        ResetPenguinToStart();
    }

    public void ReplayCurrentBuild()
    {
        StopAllCoroutines();
        StartCoroutine(ReplayCurrentBuildRoutine());
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
        reachedGoal = false;
        hasMovedSinceStart = false;
        stoppedTimer = 0f;
        startTime = Time.time;
        penguinRb.simulated = true;
        if (applyStartingVelocity)
        {
            applyStartingVelocityUntil = Time.fixedTime + startingVelocityDuration;
            penguinRb.WakeUp();
            penguinRb.AddForce(Vector2.right * startingImpulse, ForceMode2D.Impulse);
            ApplyStartingVelocity();
        }
    }

    private IEnumerator ReplayCurrentBuildRoutine()
    {
        ResetPenguinToStart();
        yield return null;
        StartPenguin();
    }

    private void DetectStoppedBeforeGoal()
    {
        if (!hasStarted || reachedGoal || penguinRb == null || !penguinRb.simulated)
        {
            return;
        }

        float speed = penguinRb.linearVelocity.magnitude;
        if (speed > stoppedSpeedThreshold)
        {
            hasMovedSinceStart = true;
            stoppedTimer = 0f;
            return;
        }

        if (!hasMovedSinceStart || Time.time - startTime < minimumRunTimeBeforeStopFailure)
        {
            return;
        }

        stoppedTimer += Time.deltaTime;
        if (stoppedTimer >= stoppedDurationBeforeFailure)
        {
            ReportFailure(FailureFeedbackManager.FailureState.GapLeft);
        }
    }

    private void ReportFailure(FailureFeedbackManager.FailureState state)
    {
        FreezePenguin();
        failureFeedback?.ShowFailure(state);
    }

    private void FreezePenguin()
    {
        if (penguinRb == null)
        {
            return;
        }

        penguinRb.linearVelocity = Vector2.zero;
        penguinRb.angularVelocity = 0f;
        penguinRb.simulated = false;
        hasStarted = false;
        stoppedTimer = 0f;
        hasMovedSinceStart = false;
    }

    private void ResetPenguinToStart()
    {
        if (penguin == null || penguinRb == null)
        {
            return;
        }

        penguinRb.linearVelocity = Vector2.zero;
        penguinRb.angularVelocity = 0f;
        penguinRb.simulated = false;
        penguin.transform.position = initialPenguinPosition;
        penguin.transform.rotation = initialPenguinRotation;
        hasStarted = false;
        reachedGoal = false;
        hasMovedSinceStart = false;
        stoppedTimer = 0f;
    }

    private void HandlePlayerReachedGoal()
    {
        reachedGoal = true;
        hasStarted = false;
        failureFeedback?.HideVisualHints();
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
