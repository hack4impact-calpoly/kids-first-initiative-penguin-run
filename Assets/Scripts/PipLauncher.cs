using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PipLauncher : MonoBehaviour
{
    [Header("Required References")]
    public EdgeCollider2D rampCollider;
    public Button playButton;
    public Button replayButton;
    public Slider dragProgressBar;
    public GameObject energyBubble;
    public Text resultMessage;

    [Header("Optional References")]
    public Collider2D iglooCollider;

    [Header("Bubble")]
    public Vector3 bubbleWorldOffset = new Vector3(0f, 1.25f, 0f);
    public float minBubbleScale = 0.6f;
    public float maxBubbleScale = 1.8f;
    public Color bubbleColor = new Color(1f, 0.86f, 0.05f, 0.85f);
    public float progressBarBubbleVisibleDuration = 0.45f;

    [Header("Progress Bar")]
    public bool createProgressBarIfMissing = true;
    public Vector2 progressBarAnchoredPosition = new Vector2(0f, 90f);
    public Vector2 progressBarSize = new Vector2(420f, 34f);
    public Color progressBarBackgroundColor = new Color(1f, 1f, 1f, 0.35f);
    public Color progressBarFillColor = new Color(1f, 0.86f, 0.05f, 0.95f);
    public Color progressBarHandleColor = new Color(1f, 0.95f, 0.35f, 1f);

    [Header("Height / Potential Energy UI")]
    public bool createHeightEnergyUiIfMissing = true;
    public string heightLabelText = "Height";
    public string potentialEnergyLabelText = "Potential Energy";
    public Vector2 heightLabelSize = new Vector2(160f, 40f);
    public Vector2 potentialEnergyBarSize = new Vector2(240f, 34f);
    public float uiSpacing = 28f;
    public int uiLabelFontSize = 26;
    public Color uiLabelColor = Color.white;
    public Color potentialEnergyBackgroundColor = new Color(1f, 1f, 1f, 0.25f);
    public Color potentialEnergyFillColor = new Color(0.25f, 0.85f, 1f, 0.95f);
    public string interactionHintText = "Drag Pip or move the slider";

    private Text heightLabel;
    private Text potentialEnergyLabel;
    private Text interactionHintLabel;
    private Image potentialEnergyFill;
    private RectTransform potentialEnergyFillRect;
    private bool heightEnergyUiCreated;

    [Header("Launch")]
    public bool useCustomGroundY;
    public float customGroundY;
    public float landingDetectionDelay = 0.25f;

    [Tooltip("Tunable feel multiplier applied to the launch velocity. Range grows with the SQUARE of this value. ~1.25 lands a full-height launch on the igloo; raise for farther, lower for shorter.")]
    public float launchMultiplier = 1.25f;

    [Tooltip("Gravity scale applied to Pip while airborne. This world is built at a large scale, so this needs to be high (the ball prefab uses 200). Raise it if Pip's arc feels too slow/floaty.")]
    public float launchGravityScale = 200f;

    [Header("Trail / Orientation")]
    [Tooltip("If true, Pip's sprite rotates to face his current velocity while airborne.")]
    public bool rotateToVelocity = true;

    [Tooltip("Result message colors.")]
    public Color greatShotColor = new Color(1f, 0.86f, 0.05f, 1f);
    public Color tooShortColor = new Color(1f, 0.55f, 0.1f, 1f);
    public Color overshotColor = new Color(0.25f, 0.6f, 1f, 1f);

    [Tooltip("When off, landing short of the igloo shows no message and does nothing.")]
    public bool showTooShortMessage = false;

    [Header("Goal Completion Popup")]
    [Tooltip("When Pip reaches the igloo, show the same level-complete popup Level 2 uses.")]
    public bool showLevelCompletePopupOnIgloo = true;
    [Tooltip("Scene the popup's Next button loads. Set to the level that follows this one.")]
    public string nextLevelSceneName = "";
    public string completionTitle = "Pip made it home!";
    public string[] completionLearnedItems =
    {
        "Higher start = more stored energy",
        "More energy = a farther launch"
    };
    [TextArea(2, 4)] public string completionMessage = "You did it! The higher I started, the more energy I had \u2014 and it sent me all the way home!";
    [TextArea(2, 4)] public string nextLevelPrompt = "";

    [Header("Completion Concept Card (Screen 2)")]
    public string conceptTitle = "Stored energy sends Pip flying!";
    [TextArea(2, 4)] public string conceptDescription = "The higher Pip starts, the more energy he stores. When he slides down, all that stored energy launches him through the air!";
    public string conceptLabel = "Stored energy sends Pip flying!";
    [Tooltip("Show the gravity-demo animation (sliding mover/arrow + celebration) on the concept card.")]
    public bool showCompletionAnimation = false;

    [HideInInspector]
    public List<Vector2> trail = new List<Vector2>();

    [Header("Fall Reset")]
    [Tooltip("If Pip falls this many world units below the bottom of the screen, the level resets with a hint dialogue.")]
    public float fallResetDistanceBelowScreen = 100f;
    public string fallResetMessage = "Try starting higher or lower on the ramp and GO again!";

    private const string AutoBootstrapSceneName = "Level3_PE";
    private const int MousePointerId = -1;

    private Rigidbody2D pipRigidbody;
    private Collider2D pipCollider;
    private PenguinFrictionBrakes frictionBrakes;
    private Camera mainCamera;
    private RectTransform bubbleRect;
    private Canvas bubbleCanvas;
    private Vector3 bubbleBaseScale = Vector3.one;
    private Vector3 startingRotationEuler;
    private Vector2 lastRampPosition;
    private Vector2 launchStartPosition;
    private Vector2 lastLaunchDirection = Vector2.right;
    private float pipZ;
    private int activePointerId = MousePointerId;
    private float launchedAt;
    private bool isDragging;
    private bool hasLaunched;
    private bool hasResult;
    private bool isUpdatingProgressBar;
    private bool resetTriggered;
    private float hideProgressBarBubbleAt;
    private Button registeredPlayButton;
    private Button registeredReplayButton;
    private Slider registeredDragProgressBar;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void InitializeLevel3Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        BootstrapLevel3(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BootstrapLevel3(scene);
    }

    private static void BootstrapLevel3(Scene scene)
    {
        if (scene.name != AutoBootstrapSceneName)
        {
            return;
        }

        GameObject pip = GameObject.FindGameObjectWithTag("Player") ?? FindFirstRigidbodyObject();
        if (pip == null)
        {
            Debug.LogWarning("[PipLauncher] Could not find Pip in Level3_PE.");
            return;
        }

        RemoveOldLevel3LaunchFlow();

        PipLauncher launcher = pip.GetComponent<PipLauncher>();
        if (launcher == null)
        {
            launcher = pip.AddComponent<PipLauncher>();
        }

        Button resolvedPlayButton = launcher.playButton != null ? launcher.playButton : FindButtonByName("PlayButtonToStartBall");
        Button resolvedReplayButton = launcher.replayButton != null ? launcher.replayButton : FindButtonByName("ResetButton_0");
        if (resolvedPlayButton != null)
        {
            resolvedPlayButton.onClick.RemoveAllListeners();
        }

        if (resolvedReplayButton != null)
        {
            resolvedReplayButton.onClick.RemoveAllListeners();
        }

        launcher.registeredPlayButton = null;
        launcher.registeredReplayButton = null;
        launcher.playButton = resolvedPlayButton;
        launcher.replayButton = resolvedReplayButton;
        launcher.iglooCollider = launcher.iglooCollider != null ? launcher.iglooCollider : FindIglooCollider();
        launcher.rampCollider = launcher.rampCollider != null ? launcher.rampCollider : FindOrCreateRampGuide(pip.transform, pip.GetComponent<Collider2D>(), launcher.iglooCollider);
        launcher.resultMessage = launcher.resultMessage != null ? launcher.resultMessage : CreateDefaultResultMessage();

        if (launcher.iglooCollider != null)
        {
            launcher.iglooCollider.isTrigger = false;
        }

        launcher.RefreshSceneWiring();
    }

    private void Awake()
    {
        pipRigidbody = GetComponent<Rigidbody2D>();
        pipCollider = GetComponent<Collider2D>();
        frictionBrakes = GetComponent<PenguinFrictionBrakes>();
        mainCamera = Camera.main;
        pipZ = transform.position.z;
        startingRotationEuler = transform.eulerAngles;

        ConfigureRigidbodyForAiming();
        CacheBubbleReferences();
        EnsureProgressBarExists();
        HideEnergyBubble();
        HideResultMessage();
    }

    private void OnEnable()
    {
        RegisterControlListeners();
    }

    private void Start()
    {
        RegisterControlListeners();

        if (iglooCollider == null)
        {
            iglooCollider = FindIglooColliderByName();
        }

        lastRampPosition = GetClosestPointOnRamp(transform.position);
        SetPipPosition(lastRampPosition);
        UpdateProgressBarFromPip();
    }

    private void OnDisable()
    {
        UnregisterControlListeners();
    }

    private void Update()
    {
        RegisterControlListeners();

        if (!hasLaunched)
        {
            EnforceFrozenRotation();
            pipRigidbody.bodyType = RigidbodyType2D.Kinematic;
            pipRigidbody.gravityScale = 0f;
            pipRigidbody.linearVelocity = Vector2.zero;
            pipRigidbody.angularVelocity = 0f;
            HandleDragInput();
        }
        else if (!hasResult)
        {
            // Airborne: record the flight trail and face Pip toward his velocity.
            trail.Add(transform.position);
            UpdateFlightOrientation();
        }

        CheckFellBelowScreen();

        if (isDragging)
        {
            UpdateEnergyBubble();
        }

        if (!isDragging && hideProgressBarBubbleAt > 0f && Time.time >= hideProgressBarBubbleAt)
        {
            hideProgressBarBubbleAt = 0f;
            HideEnergyBubble();
        }
    }

    private void UpdateFlightOrientation()
    {
        if (!rotateToVelocity)
        {
            return;
        }

        Vector2 velocity = pipRigidbody.linearVelocity;
        if (velocity.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void CheckFellBelowScreen()
    {
        if (!hasLaunched || resetTriggered)
        {
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return;
        }

        float screenBottomY;
        if (mainCamera.orthographic)
        {
            screenBottomY = mainCamera.transform.position.y - mainCamera.orthographicSize;
        }
        else
        {
            float depth = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
            screenBottomY = mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0f, depth)).y;
        }

        if (transform.position.y < screenBottomY - fallResetDistanceBelowScreen)
        {
            resetTriggered = true;

            // Bypass the normal intro dialogue: queue our hint to show after the reload, then
            // reload the scene directly (ResetLevel.ResetGame would no-op while dialogue is open).
            DialogueManager.QueueResetMessage(fallResetMessage);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    private void FixedUpdate()
    {
        EnforceFrozenRotation();
    }

    public void Launch()
    {
        if (hasLaunched)
        {
            return;
        }

        lastRampPosition = GetClosestPointOnRamp(transform.position);
        SetPipPosition(lastRampPosition);
        HideEnergyBubble();
        HideResultMessage();

        launchStartPosition = pipRigidbody.position;
        trail.Clear();

        // Launch direction: ramp direction reflected upward (negate Y) so Pip arcs off the ramp.
        lastLaunchDirection = GetLaunchDirection();

        // speed = sqrt(2 * g * h), with h = Pip height above the ground.
        // Use the SAME gravity Pip will actually fall under (9.8 * launchGravityScale) so the
        // launch energy matches the heavy gravity of this large-scale world; otherwise the
        // launch looks like "nothing happens".
        float height = Mathf.Max(0f, pipRigidbody.position.y - GetGroundY());
        float effectiveGravity = 9.8f * Mathf.Max(1f, launchGravityScale);
        float speed = Mathf.Sqrt(2f * effectiveGravity * height);

        hasLaunched = true;
        hasResult = false;
        isDragging = false;

        // While airborne, PipLauncher owns Pip's physics. The friction-brake script reads the
        // ramp's "sticky" tile and would zero the launch velocity within a frame, so disable it.
        if (frictionBrakes != null)
        {
            frictionBrakes.enabled = false;
        }

        pipRigidbody.bodyType = RigidbodyType2D.Dynamic;
        pipRigidbody.gravityScale = launchGravityScale;
        pipRigidbody.angularVelocity = 0f;
        pipRigidbody.simulated = true;
        pipRigidbody.WakeUp();
        pipRigidbody.linearVelocity = lastLaunchDirection * speed * launchMultiplier;
        launchedAt = Time.time;

        Debug.Log($"[PipLauncher] Launch h={height:F1} speed={speed:F1} vel={pipRigidbody.linearVelocity} gravityScale={launchGravityScale}");

        if (playButton != null)
        {
            playButton.interactable = false;
        }

        if (dragProgressBar != null)
        {
            dragProgressBar.interactable = false;
        }
    }

    public void Replay()
    {
        hasLaunched = false;
        hasResult = false;
        isDragging = false;
        activePointerId = MousePointerId;
        trail.Clear();
        resetTriggered = false;

        ConfigureRigidbodyForAiming();
        SetPipPosition(lastRampPosition);
        UpdateProgressBarFromPip();
        HideEnergyBubble();
        HideResultMessage();

        if (playButton != null)
        {
            playButton.interactable = true;
        }

        if (dragProgressBar != null)
        {
            dragProgressBar.interactable = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!hasLaunched || hasResult || IsRampCollision(collision))
        {
            return;
        }

        // Igloo always registers as a successful hit, even early.
        if (IsIglooCollision(collision))
        {
            StopPip();
            hasResult = true;
            PenguinLevelProgressService.CompleteLevel(3);

            if (showLevelCompletePopupOnIgloo)
            {
                ShowGoalCompletionPopup();
            }
            else
            {
                ShowResultMessage("Great shot! Higher = farther! \u2605", greatShotColor);
            }
            return;
        }

        // Ignore any other contact during the initial launch window so grazing the ramp/track
        // on the way up doesn't count as a landing.
        if (Time.time - launchedAt < landingDetectionDelay)
        {
            return;
        }

        StopPip();

        float landingX = transform.position.x;

        if (TryGetIglooBounds(out float iglooMinX, out float iglooMaxX))
        {
            if (landingX < iglooMinX)
            {
                if (showTooShortMessage)
                {
                    ShowResultMessage("Too short! Try starting higher!", tooShortColor);
                }
                else
                {
                    hasResult = true;
                }
                return;
            }

            if (landingX > iglooMaxX)
            {
                ShowResultMessage("Overshot! Try a bit lower!", overshotColor);
                return;
            }

            ShowResultMessage("Great shot! Higher = farther! \u2605", greatShotColor);
            return;
        }

        if (HasOvershotTarget())
        {
            ShowResultMessage("Overshot! Try a bit lower!", overshotColor);
        }
        else if (showTooShortMessage)
        {
            ShowResultMessage("Too short! Try starting higher!", tooShortColor);
        }
        else
        {
            hasResult = true;
        }
    }

    private void StopPip()
    {
        pipRigidbody.gravityScale = 0f;
        pipRigidbody.linearVelocity = Vector2.zero;
        pipRigidbody.angularVelocity = 0f;
    }

    private void ShowGoalCompletionPopup()
    {
        // Reuse the same victory popup Level 2 shows on goal completion.
        LevelVictoryPopup popup = FindFirstObjectByType<LevelVictoryPopup>();
        if (popup != null)
        {
            popup.SetFinalActionAsText(true, "Take the quiz");
            popup.SetConceptStep(conceptTitle, conceptDescription, conceptLabel);
            popup.SetConceptAnimationVisible(showCompletionAnimation);
            popup.Show(nextLevelSceneName, completionTitle, completionLearnedItems, null, completionMessage, nextLevelPrompt);
            return;
        }

        DialogueManager manager = FindFirstObjectByType<DialogueManager>();
        if (manager != null)
        {
            manager.ShowLevelCompletePopup(nextLevelSceneName, completionTitle, completionLearnedItems, completionMessage, nextLevelPrompt);
            return;
        }

        // Fallback if neither exists in the scene.
        ShowResultMessage("Great shot! Higher = farther! \u2605", greatShotColor);
    }

    private bool TryGetIglooBounds(out float minX, out float maxX)
    {
        if (iglooCollider == null)
        {
            iglooCollider = FindIglooColliderByName();
        }

        if (iglooCollider == null)
        {
            minX = 0f;
            maxX = 0f;
            return false;
        }

        Bounds bounds = iglooCollider.bounds;
        minX = bounds.min.x;
        maxX = bounds.max.x;
        return true;
    }

    private void HandleDragInput()
    {
        if (!isDragging && TryGetPointerDown(out Vector2 downPosition, out int pointerId))
        {
            if (!IsPointerOverUi(pointerId) && IsPointerOverPip(downPosition))
            {
                activePointerId = pointerId;
                isDragging = true;
                ShowEnergyBubble();
                DragPipToPointer(downPosition);
            }
        }

        if (!isDragging)
        {
            return;
        }

        if (TryGetPointerPosition(activePointerId, out Vector2 pointerPosition))
        {
            DragPipToPointer(pointerPosition);
        }

        if (TryGetPointerUp(activePointerId))
        {
            isDragging = false;
            activePointerId = MousePointerId;
            HideEnergyBubble();
        }
    }

    private void DragPipToPointer(Vector2 screenPosition)
    {
        Vector2 worldPosition = ScreenToWorldPoint(screenPosition);
        Vector2 snappedPosition = GetClosestPointOnRamp(worldPosition);

        SetPipPosition(snappedPosition);
        lastRampPosition = snappedPosition;
        UpdateEnergyBubble();
        UpdateProgressBarFromPip();
    }

    private void HandleProgressBarChanged(float value)
    {
        if (isUpdatingProgressBar || hasLaunched)
        {
            return;
        }

        Vector2 rampPosition = GetPointOnRampByHeightPercent(value);
        SetPipPosition(rampPosition);
        lastRampPosition = rampPosition;

        ShowEnergyBubble();
        UpdateEnergyBubble();
        SyncPotentialEnergyBar();
        hideProgressBarBubbleAt = Time.time + progressBarBubbleVisibleDuration;
    }

    private void ConfigureRigidbodyForAiming()
    {
        if (frictionBrakes != null)
        {
            frictionBrakes.enabled = true;
        }

        pipRigidbody.simulated = true;
        pipRigidbody.bodyType = RigidbodyType2D.Kinematic;
        pipRigidbody.gravityScale = 0f;
        pipRigidbody.linearVelocity = Vector2.zero;
        pipRigidbody.angularVelocity = 0f;
        EnforceFrozenRotation();
    }

    private void EnforceFrozenRotation()
    {
        pipRigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;
    }

    private void SetPipPosition(Vector2 position)
    {
        pipRigidbody.linearVelocity = Vector2.zero;
        pipRigidbody.angularVelocity = 0f;
        pipRigidbody.position = position;
        transform.position = new Vector3(position.x, position.y, pipZ);
        transform.eulerAngles = startingRotationEuler;
    }

    private Vector2 GetClosestPointOnRamp(Vector2 worldPosition)
    {
        if (!TryGetRampEndpoints(out Vector2 start, out Vector2 end))
        {
            return worldPosition;
        }

        Vector2 segment = end - start;
        float segmentLengthSquared = segment.sqrMagnitude;
        if (segmentLengthSquared <= Mathf.Epsilon)
        {
            return start;
        }

        float t = Vector2.Dot(worldPosition - start, segment) / segmentLengthSquared;
        t = Mathf.Clamp01(t);
        return start + segment * t;
    }

    private Vector2 GetLaunchDirection()
    {
        // Ramp direction = second EdgeCollider2D point minus the first, normalized.
        // Reflect upward (negate Y) so Pip launches up-and-forward off the ramp.
        if (!TryGetRampEndpoints(out Vector2 start, out Vector2 end))
        {
            return new Vector2(1f, 1f).normalized;
        }

        Vector2 rampDirection = end - start;
        if (rampDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return new Vector2(1f, 1f).normalized;
        }

        rampDirection.Normalize();
        Vector2 launchDirection = new Vector2(rampDirection.x, -rampDirection.y);

        // Ensure the launch carries Pip upward regardless of how the ramp points were ordered.
        if (launchDirection.y < 0f)
        {
            launchDirection.y = -launchDirection.y;
        }

        return launchDirection.sqrMagnitude > Mathf.Epsilon ? launchDirection.normalized : new Vector2(1f, 1f).normalized;
    }

    private Vector2 GetDownRampDirection()
    {
        if (!TryGetRampEndpoints(out Vector2 start, out Vector2 end))
        {
            return Vector2.right;
        }

        Vector2 highPoint = start.y >= end.y ? start : end;
        Vector2 lowPoint = start.y >= end.y ? end : start;
        Vector2 direction = lowPoint - highPoint;

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            direction = end - start;
        }

        if (direction.sqrMagnitude <= Mathf.Epsilon)
        {
            return Vector2.right;
        }

        return direction.normalized;
    }

    private bool TryGetRampEndpoints(out Vector2 start, out Vector2 end)
    {
        start = transform.position;
        end = transform.position;

        if (rampCollider == null || rampCollider.pointCount < 2)
        {
            return false;
        }

        Vector2[] points = rampCollider.points;
        Vector2 localStart = points[0] + rampCollider.offset;
        Vector2 localEnd = points[points.Length - 1] + rampCollider.offset;
        start = rampCollider.transform.TransformPoint(localStart);
        end = rampCollider.transform.TransformPoint(localEnd);
        return true;
    }

    private float GetGroundY()
    {
        if (useCustomGroundY)
        {
            return customGroundY;
        }

        if (!TryGetRampEndpoints(out Vector2 start, out Vector2 end))
        {
            return 0f;
        }

        return Mathf.Min(start.y, end.y);
    }

    private float GetRampHeightPercent()
    {
        if (!TryGetRampEndpoints(out Vector2 start, out Vector2 end))
        {
            return 0f;
        }

        float minY = Mathf.Min(start.y, end.y);
        float maxY = Mathf.Max(start.y, end.y);
        float heightRange = maxY - minY;
        if (heightRange <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.Clamp01((transform.position.y - minY) / heightRange);
    }

    private Vector2 GetPointOnRampByHeightPercent(float heightPercent)
    {
        heightPercent = Mathf.Clamp01(heightPercent);

        if (!TryGetRampEndpoints(out Vector2 start, out Vector2 end))
        {
            return transform.position;
        }

        if (Mathf.Approximately(start.y, end.y))
        {
            return Vector2.Lerp(start, end, heightPercent);
        }

        Vector2 lowPoint = start.y <= end.y ? start : end;
        Vector2 highPoint = start.y <= end.y ? end : start;
        return Vector2.Lerp(lowPoint, highPoint, heightPercent);
    }

    private void UpdateProgressBarFromPip()
    {
        if (dragProgressBar == null)
        {
            return;
        }

        isUpdatingProgressBar = true;
        dragProgressBar.SetValueWithoutNotify(GetRampHeightPercent());
        isUpdatingProgressBar = false;

        SyncPotentialEnergyBar();
    }

    private bool IsPointerOverPip(Vector2 screenPosition)
    {
        if (pipCollider == null)
        {
            return false;
        }

        return pipCollider.OverlapPoint(ScreenToWorldPoint(screenPosition));
    }

    private Vector2 ScreenToWorldPoint(Vector2 screenPosition)
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return transform.position;
        }

        float zDistance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, zDistance));
        return worldPosition;
    }

    private bool TryGetPointerDown(out Vector2 screenPosition, out int pointerId)
    {
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
            {
                screenPosition = touch.position;
                pointerId = touch.fingerId;
                return true;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            screenPosition = Input.mousePosition;
            pointerId = MousePointerId;
            return true;
        }

        screenPosition = Vector2.zero;
        pointerId = MousePointerId;
        return false;
    }

    private bool TryGetPointerPosition(int pointerId, out Vector2 screenPosition)
    {
        if (pointerId != MousePointerId)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == pointerId)
                {
                    screenPosition = touch.position;
                    return true;
                }
            }

            screenPosition = Vector2.zero;
            return false;
        }

        screenPosition = Input.mousePosition;
        return Input.GetMouseButton(0);
    }

    private bool TryGetPointerUp(int pointerId)
    {
        if (pointerId != MousePointerId)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.fingerId == pointerId)
                {
                    return touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                }
            }

            return true;
        }

        return Input.GetMouseButtonUp(0);
    }

    private bool IsPointerOverUi(int pointerId)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        return pointerId == MousePointerId
            ? EventSystem.current.IsPointerOverGameObject()
            : EventSystem.current.IsPointerOverGameObject(pointerId);
    }

    private void CacheBubbleReferences()
    {
        if (energyBubble == null)
        {
            return;
        }

        bubbleBaseScale = energyBubble.transform.localScale;
        if (bubbleBaseScale == Vector3.zero)
        {
            bubbleBaseScale = Vector3.one;
        }

        bubbleRect = energyBubble.GetComponent<RectTransform>();
        bubbleCanvas = energyBubble.GetComponentInParent<Canvas>();

        SpriteRenderer spriteRenderer = energyBubble.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.color = bubbleColor;
        }

        Graphic graphic = energyBubble.GetComponent<Graphic>();
        if (graphic != null)
        {
            graphic.color = bubbleColor;
        }
    }

    private void ShowEnergyBubble()
    {
        if (energyBubble == null)
        {
            return;
        }

        energyBubble.SetActive(true);
        UpdateEnergyBubble();
    }

    private void HideEnergyBubble()
    {
        if (energyBubble != null)
        {
            energyBubble.SetActive(false);
        }
    }

    private void UpdateEnergyBubble()
    {
        if (energyBubble == null)
        {
            return;
        }

        float scale = Mathf.Lerp(minBubbleScale, maxBubbleScale, GetRampHeightPercent());
        energyBubble.transform.localScale = bubbleBaseScale * scale;

        Vector3 bubbleWorldPosition = transform.position + bubbleWorldOffset;
        if (bubbleRect != null && bubbleCanvas != null && bubbleCanvas.renderMode != RenderMode.WorldSpace && mainCamera != null)
        {
            RectTransform canvasRect = bubbleCanvas.transform as RectTransform;
            Camera uiCamera = bubbleCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : bubbleCanvas.worldCamera;
            Vector2 screenPosition = mainCamera.WorldToScreenPoint(bubbleWorldPosition);

            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out Vector2 localPoint))
            {
                bubbleRect.anchoredPosition = localPoint;
                return;
            }
        }

        energyBubble.transform.position = bubbleWorldPosition;
    }

    private void ShowResultMessage(string message, Color color)
    {
        hasResult = true;

        if (resultMessage == null)
        {
            return;
        }

        resultMessage.text = message;
        resultMessage.color = color;
        resultMessage.gameObject.SetActive(true);
    }

    private void HideResultMessage()
    {
        if (resultMessage == null)
        {
            return;
        }

        resultMessage.text = string.Empty;
        resultMessage.gameObject.SetActive(false);
    }

    private bool IsRampCollision(Collision2D collision)
    {
        return rampCollider != null && (collision.collider == rampCollider || collision.otherCollider == rampCollider);
    }

    private bool IsIglooCollision(Collision2D collision)
    {
        if (iglooCollider != null && (collision.collider == iglooCollider || collision.otherCollider == iglooCollider))
        {
            return true;
        }

        return ColliderNameContainsIgloo(collision.collider) || ColliderNameContainsIgloo(collision.otherCollider);
    }

    private bool HasOvershotTarget()
    {
        if (!TryGetIglooX(out float iglooX))
        {
            return false;
        }

        float travelSign = Mathf.Sign(iglooX - launchStartPosition.x);
        if (Mathf.Approximately(travelSign, 0f))
        {
            travelSign = Mathf.Sign(lastLaunchDirection.x);
        }

        if (Mathf.Approximately(travelSign, 0f))
        {
            travelSign = 1f;
        }

        return (transform.position.x - iglooX) * travelSign > 0f;
    }

    private bool TryGetIglooX(out float iglooX)
    {
        if (iglooCollider == null)
        {
            iglooCollider = FindIglooColliderByName();
        }

        if (iglooCollider == null)
        {
            iglooX = 0f;
            return false;
        }

        iglooX = iglooCollider.bounds.center.x;
        return true;
    }

    private Collider2D FindIglooColliderByName()
    {
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsSortMode.None);
        foreach (Collider2D candidate in colliders)
        {
            if (candidate != pipCollider && ColliderNameContainsIgloo(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private bool ColliderNameContainsIgloo(Collider2D candidate)
    {
        if (candidate == null)
        {
            return false;
        }

        Transform current = candidate.transform;
        while (current != null)
        {
            if (current.name.IndexOf("Igloo", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private void RegisterControlListeners()
    {
        EnsureProgressBarExists();
        EnsureHeightEnergyUi();

        if (registeredPlayButton != playButton)
        {
            if (registeredPlayButton != null)
            {
                registeredPlayButton.onClick.RemoveListener(Launch);
            }

            registeredPlayButton = playButton;
            if (registeredPlayButton != null)
            {
                registeredPlayButton.onClick.AddListener(Launch);
            }
        }

        if (registeredReplayButton != replayButton)
        {
            if (registeredReplayButton != null)
            {
                registeredReplayButton.onClick.RemoveListener(Replay);
            }

            registeredReplayButton = replayButton;
            if (registeredReplayButton != null)
            {
                registeredReplayButton.onClick.AddListener(Replay);
            }
        }

        if (registeredDragProgressBar != dragProgressBar)
        {
            if (registeredDragProgressBar != null)
            {
                registeredDragProgressBar.onValueChanged.RemoveListener(HandleProgressBarChanged);
            }

            registeredDragProgressBar = dragProgressBar;
            if (registeredDragProgressBar != null)
            {
                registeredDragProgressBar.minValue = 0f;
                registeredDragProgressBar.maxValue = 1f;
                registeredDragProgressBar.wholeNumbers = false;
                registeredDragProgressBar.onValueChanged.AddListener(HandleProgressBarChanged);
                UpdateProgressBarFromPip();
            }
        }
    }

    private void RefreshSceneWiring()
    {
        mainCamera = Camera.main;
        CacheBubbleReferences();
        EnsureProgressBarExists();
        RegisterControlListeners();

        if (iglooCollider != null)
        {
            iglooCollider.isTrigger = false;
        }

        if (!hasLaunched)
        {
            ConfigureRigidbodyForAiming();
            lastRampPosition = GetClosestPointOnRamp(transform.position);
            SetPipPosition(lastRampPosition);
            UpdateProgressBarFromPip();
        }
    }

    private void EnsureProgressBarExists()
    {
        if (dragProgressBar != null || !createProgressBarIfMissing)
        {
            return;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return;
        }

        GameObject sliderObject = new GameObject("PipDragProgressBar", typeof(RectTransform), typeof(Image), typeof(Slider));
        sliderObject.transform.SetParent(canvas.transform, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0.5f, 0f);
        sliderRect.anchorMax = new Vector2(0.5f, 0f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = progressBarAnchoredPosition;
        sliderRect.sizeDelta = progressBarSize;

        Image background = sliderObject.GetComponent<Image>();
        background.color = progressBarBackgroundColor;
        background.raycastTarget = true;

        RectTransform fillArea = CreateProgressBarChild("Fill Area", sliderRect);
        fillArea.anchorMin = new Vector2(0f, 0.25f);
        fillArea.anchorMax = new Vector2(1f, 0.75f);
        fillArea.offsetMin = new Vector2(16f, 0f);
        fillArea.offsetMax = new Vector2(-16f, 0f);

        Image fill = CreateProgressBarImage("Fill", fillArea, progressBarFillColor);
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        RectTransform handleArea = CreateProgressBarChild("Handle Slide Area", sliderRect);
        handleArea.anchorMin = Vector2.zero;
        handleArea.anchorMax = Vector2.one;
        handleArea.offsetMin = new Vector2(16f, 0f);
        handleArea.offsetMax = new Vector2(-16f, 0f);

        Image handle = CreateProgressBarImage("Handle", handleArea, progressBarHandleColor);
        RectTransform handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(46f, 46f);

        dragProgressBar = sliderObject.GetComponent<Slider>();
        dragProgressBar.targetGraphic = handle;
        dragProgressBar.fillRect = fillRect;
        dragProgressBar.handleRect = handleRect;
        dragProgressBar.direction = Slider.Direction.LeftToRight;
        dragProgressBar.minValue = 0f;
        dragProgressBar.maxValue = 1f;
        dragProgressBar.wholeNumbers = false;
    }

    private RectTransform CreateProgressBarChild(string childName, Transform parent)
    {
        GameObject childObject = new GameObject(childName, typeof(RectTransform));
        childObject.transform.SetParent(parent, false);
        return childObject.GetComponent<RectTransform>();
    }

    private Image CreateProgressBarImage(string childName, Transform parent, Color color)
    {
        GameObject childObject = new GameObject(childName, typeof(RectTransform), typeof(Image));
        childObject.transform.SetParent(parent, false);

        Image image = childObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private void EnsureHeightEnergyUi()
    {
        if (heightEnergyUiCreated || !createHeightEnergyUiIfMissing || dragProgressBar == null)
        {
            return;
        }

        RectTransform sliderRect = dragProgressBar.GetComponent<RectTransform>();
        if (sliderRect == null || sliderRect.parent == null)
        {
            return;
        }

        Transform parent = sliderRect.parent;
        float sliderWidth = sliderRect.sizeDelta.x;
        Vector2 sliderPos = sliderRect.anchoredPosition;

        // "Height" label to the LEFT of the adjustable bar.
        heightLabel = CreateUiLabel("PipHeightLabel", parent, sliderRect, heightLabelText, TextAnchor.MiddleRight);
        heightLabel.rectTransform.sizeDelta = heightLabelSize;
        heightLabel.rectTransform.anchoredPosition = sliderPos +
            new Vector2(-(sliderWidth * 0.5f + uiSpacing + heightLabelSize.x * 0.5f), 0f);

        // Potential-energy bar to the RIGHT of the adjustable bar.
        GameObject barObject = new GameObject("PipPotentialEnergyBar", typeof(RectTransform), typeof(Image));
        barObject.transform.SetParent(parent, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = sliderRect.anchorMin;
        barRect.anchorMax = sliderRect.anchorMax;
        barRect.pivot = sliderRect.pivot;
        barRect.sizeDelta = potentialEnergyBarSize;
        barRect.anchoredPosition = sliderPos +
            new Vector2(sliderWidth * 0.5f + uiSpacing + potentialEnergyBarSize.x * 0.5f, 0f);

        Image barBackground = barObject.GetComponent<Image>();
        barBackground.color = potentialEnergyBackgroundColor;
        barBackground.raycastTarget = false;

        GameObject fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(barRect, false);

        potentialEnergyFillRect = fillObject.GetComponent<RectTransform>();
        potentialEnergyFillRect.anchorMin = new Vector2(0f, 0f);
        potentialEnergyFillRect.anchorMax = new Vector2(0f, 1f);
        potentialEnergyFillRect.pivot = new Vector2(0f, 0.5f);
        potentialEnergyFillRect.offsetMin = new Vector2(4f, 4f);
        potentialEnergyFillRect.offsetMax = new Vector2(-4f, -4f);

        potentialEnergyFill = fillObject.GetComponent<Image>();
        potentialEnergyFill.color = potentialEnergyFillColor;
        potentialEnergyFill.raycastTarget = false;

        // "Potential Energy" label above its bar.
        potentialEnergyLabel = CreateUiLabel("PipPotentialEnergyLabel", parent, sliderRect, potentialEnergyLabelText, TextAnchor.MiddleCenter);
        potentialEnergyLabel.rectTransform.sizeDelta = new Vector2(potentialEnergyBarSize.x + 60f, 32f);
        potentialEnergyLabel.rectTransform.anchoredPosition = barRect.anchoredPosition +
            new Vector2(0f, potentialEnergyBarSize.y * 0.5f + 24f);

        interactionHintLabel = CreateUiLabel(
            "PipInteractionHint",
            parent,
            sliderRect,
            interactionHintText,
            TextAnchor.MiddleCenter);
        interactionHintLabel.rectTransform.sizeDelta = new Vector2(310f, 32f);
        interactionHintLabel.rectTransform.anchoredPosition = sliderPos + new Vector2(0f, 48f);
        interactionHintLabel.fontSize = 18;
        interactionHintLabel.fontStyle = FontStyle.Bold;
        interactionHintLabel.color = new Color(1f, 1f, 1f, 0.92f);

        heightEnergyUiCreated = true;
        SyncPotentialEnergyBar();
    }

    private Text CreateUiLabel(string objectName, Transform parent, RectTransform reference, string content, TextAnchor alignment)
    {
        GameObject labelObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(parent, false);

        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.anchorMin = reference.anchorMin;
        rect.anchorMax = reference.anchorMax;
        rect.pivot = reference.pivot;

        Text text = labelObject.GetComponent<Text>();
        text.text = content;
        text.alignment = alignment;
        text.color = uiLabelColor;
        text.fontSize = uiLabelFontSize;
        text.fontStyle = FontStyle.Bold;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.font = font;
        return text;
    }

    private void SyncPotentialEnergyBar()
    {
        if (potentialEnergyFillRect == null)
        {
            return;
        }

        // Potential energy is proportional to height, so the fill tracks the height percent directly.
        float percent = Mathf.Clamp01(GetRampHeightPercent());
        Vector2 anchorMax = potentialEnergyFillRect.anchorMax;
        anchorMax.x = percent;
        potentialEnergyFillRect.anchorMax = anchorMax;
    }

    private static GameObject FindFirstRigidbodyObject()
    {
        Rigidbody2D[] rigidbodies = FindObjectsByType<Rigidbody2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Rigidbody2D candidate in rigidbodies)
        {
            if (candidate != null && candidate.GetComponent<Collider2D>() != null)
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private static Button FindButtonByName(string objectName)
    {
        GameObject buttonObject = GameObject.Find(objectName);
        if (buttonObject == null)
        {
            return null;
        }

        return buttonObject.GetComponent<Button>();
    }

    private static Collider2D FindIglooCollider()
    {
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (Collider2D candidate in colliders)
        {
            if (candidate != null && candidate.name.IndexOf("Igloo", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return candidate;
            }
        }

        foreach (Collider2D candidate in colliders)
        {
            if (candidate != null && candidate.CompareTag("Finish"))
            {
                return candidate;
            }
        }

        return null;
    }

    private static EdgeCollider2D FindOrCreateRampGuide(Transform pip, Collider2D pipCollider, Collider2D igloo)
    {
        EdgeCollider2D existingRamp = FindFirstObjectByType<EdgeCollider2D>();
        if (existingRamp != null)
        {
            return existingRamp;
        }

        List<Collider2D> rampPieces = FindRampCandidates(pip);
        if (rampPieces.Count > 0)
        {
            return CreateRampGuideFromColliders(rampPieces, pipCollider);
        }

        return CreateFallbackRampGuide(pip, igloo);
    }

    private static List<Collider2D> FindRampCandidates(Transform pip)
    {
        Collider2D[] colliders = FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<Collider2D> preferredTrackPieces = new List<Collider2D>();
        List<Collider2D> fallbackRampPieces = new List<Collider2D>();

        foreach (Collider2D candidate in colliders)
        {
            if (!IsRampCandidate(candidate))
            {
                continue;
            }

            if (NameOrParentContains(candidate.transform, "LavaStraightTrack"))
            {
                preferredTrackPieces.Add(candidate);
            }
            else
            {
                fallbackRampPieces.Add(candidate);
            }
        }

        if (preferredTrackPieces.Count > 0)
        {
            return preferredTrackPieces;
        }

        if (fallbackRampPieces.Count <= 1)
        {
            return fallbackRampPieces;
        }

        return SelectNearbyRampGroup(fallbackRampPieces, pip);
    }

    private static bool IsRampCandidate(Collider2D candidate)
    {
        if (candidate == null || candidate.attachedRigidbody != null)
        {
            return false;
        }

        if (candidate.isTrigger || ColliderNameContainsAny(candidate, "Igloo", "OutOfBounds", "Cliff", "Slot", "PipLauncherRampGuide"))
        {
            return false;
        }

        return ColliderNameContainsAny(candidate, "LavaStraightTrack", "Ramp", "Track", "ice ramp", "steep");
    }

    private static List<Collider2D> SelectNearbyRampGroup(List<Collider2D> candidates, Transform pip)
    {
        if (pip == null || candidates.Count <= 1)
        {
            return candidates;
        }

        Collider2D nearest = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (Collider2D candidate in candidates)
        {
            float distance = Vector2.Distance(candidate.bounds.center, pip.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = candidate;
            }
        }

        if (nearest == null)
        {
            return candidates;
        }

        Vector2 rampDirection = GetColliderLongAxis(nearest);
        Vector2 perpendicular = new Vector2(-rampDirection.y, rampDirection.x);
        float nearestPerp = Vector2.Dot(nearest.bounds.center, perpendicular);
        float maxPerpDistance = Mathf.Max(2f, Mathf.Min(nearest.bounds.size.x, nearest.bounds.size.y) * 2.5f);
        List<Collider2D> selected = new List<Collider2D>();

        foreach (Collider2D candidate in candidates)
        {
            float perpDistance = Mathf.Abs(Vector2.Dot(candidate.bounds.center, perpendicular) - nearestPerp);
            if (perpDistance <= maxPerpDistance)
            {
                selected.Add(candidate);
            }
        }

        return selected.Count > 0 ? selected : candidates;
    }

    private static EdgeCollider2D CreateRampGuideFromColliders(List<Collider2D> sources, Collider2D pipCollider)
    {
        Vector2 direction = GetAverageRampDirection(sources);
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        if (perpendicular.y < 0f)
        {
            perpendicular = -perpendicular;
        }

        float minProjection = float.PositiveInfinity;
        float maxProjection = float.NegativeInfinity;
        float perpendicularSum = 0f;
        float rampHalfThicknessSum = 0f;
        int counted = 0;
        float radius = 0.05f;

        foreach (Collider2D source in sources)
        {
            Vector2 center = source.bounds.center;
            float halfLength = GetRampHalfLength(source);
            float projection = Vector2.Dot(center, direction);
            minProjection = Mathf.Min(minProjection, projection - halfLength);
            maxProjection = Mathf.Max(maxProjection, projection + halfLength);
            perpendicularSum += Vector2.Dot(center, perpendicular);
            rampHalfThicknessSum += GetColliderHalfExtentAlongAxis(source, perpendicular);
            counted++;
            radius = Mathf.Max(radius, Mathf.Min(source.bounds.size.x, source.bounds.size.y) * 0.05f);
        }

        if (counted == 0 || maxProjection <= minProjection)
        {
            return CreateFallbackRampGuide(null, null);
        }

        float midProjection = (minProjection + maxProjection) * 0.5f;
        float midPerpendicular = perpendicularSum / counted;
        Vector2 guideCenter = direction * midProjection + perpendicular * midPerpendicular;
        float surfaceOffset = (rampHalfThicknessSum / counted) + GetColliderHalfExtentAlongAxis(pipCollider, perpendicular);
        guideCenter += perpendicular * surfaceOffset;
        float halfGuideLength = (maxProjection - minProjection) * 0.5f;

        GameObject guide = new GameObject("PipLauncherRampGuide");
        guide.transform.position = guideCenter;
        guide.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction);

        EdgeCollider2D edge = guide.AddComponent<EdgeCollider2D>();
        edge.points = new[]
        {
            new Vector2(-halfGuideLength, 0f),
            new Vector2(halfGuideLength, 0f)
        };

        edge.edgeRadius = radius;
        edge.isTrigger = true;
        Debug.Log($"[PipLauncher] Built ramp guide from {counted} track colliders.");
        return edge;
    }

    private static EdgeCollider2D CreateFallbackRampGuide(Transform pip, Collider2D igloo)
    {
        Vector2 highPoint = pip != null ? pip.position : new Vector3(-3f, 3f, 0f);
        Vector2 lowPoint = igloo != null
            ? new Vector2(Mathf.Lerp(highPoint.x, igloo.bounds.center.x, 0.45f), igloo.bounds.min.y)
            : highPoint + new Vector2(8f, -4f);

        Vector2 center = (highPoint + lowPoint) * 0.5f;
        Vector2 direction = lowPoint - highPoint;
        float halfLength = direction.magnitude * 0.5f;
        if (halfLength <= Mathf.Epsilon)
        {
            halfLength = 4f;
            direction = Vector2.right;
        }

        GameObject guide = new GameObject("PipLauncherRampGuide");
        guide.transform.position = center;
        guide.transform.rotation = Quaternion.FromToRotation(Vector3.right, direction.normalized);

        EdgeCollider2D edge = guide.AddComponent<EdgeCollider2D>();
        edge.points = new[]
        {
            new Vector2(-halfLength, 0f),
            new Vector2(halfLength, 0f)
        };

        edge.edgeRadius = 0.05f;
        edge.isTrigger = true;
        return edge;
    }

    private static Vector2 GetAverageRampDirection(List<Collider2D> sources)
    {
        Vector2 direction = Vector2.zero;
        bool hasDirection = false;

        foreach (Collider2D source in sources)
        {
            Vector2 candidateDirection = GetColliderLongAxis(source);
            if (!hasDirection)
            {
                direction = candidateDirection;
                hasDirection = true;
                continue;
            }

            if (Vector2.Dot(direction, candidateDirection) < 0f)
            {
                candidateDirection = -candidateDirection;
            }

            direction += candidateDirection;
        }

        return direction.sqrMagnitude > Mathf.Epsilon ? direction.normalized : Vector2.right;
    }

    private static Vector2 GetColliderLongAxis(Collider2D source)
    {
        if (source == null)
        {
            return Vector2.right;
        }

        BoxCollider2D box = source as BoxCollider2D;
        if (box == null || Mathf.Abs(box.size.x * source.transform.lossyScale.x) >= Mathf.Abs(box.size.y * source.transform.lossyScale.y))
        {
            Vector2 right = source.transform.right;
            return right.sqrMagnitude > Mathf.Epsilon ? right.normalized : Vector2.right;
        }

        Vector2 up = source.transform.up;
        return up.sqrMagnitude > Mathf.Epsilon ? up.normalized : Vector2.up;
    }

    private static float GetColliderHalfExtentAlongAxis(Collider2D source, Vector2 axis)
    {
        if (source == null)
        {
            return 0f;
        }

        axis = axis.sqrMagnitude > Mathf.Epsilon ? axis.normalized : Vector2.up;

        BoxCollider2D box = source as BoxCollider2D;
        if (box != null)
        {
            Vector2 scale = source.transform.lossyScale;
            float halfX = Mathf.Abs(box.size.x * scale.x) * 0.5f;
            float halfY = Mathf.Abs(box.size.y * scale.y) * 0.5f;
            return Mathf.Abs(Vector2.Dot(axis, source.transform.right)) * halfX
                + Mathf.Abs(Vector2.Dot(axis, source.transform.up)) * halfY;
        }

        CircleCollider2D circle = source as CircleCollider2D;
        if (circle != null)
        {
            Vector2 scale = source.transform.lossyScale;
            return circle.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        }

        Bounds bounds = source.bounds;
        return Mathf.Abs(axis.x) * bounds.extents.x + Mathf.Abs(axis.y) * bounds.extents.y;
    }

    private static bool ColliderNameContainsAny(Collider2D candidate, params string[] patterns)
    {
        if (candidate == null)
        {
            return false;
        }

        foreach (string pattern in patterns)
        {
            if (NameOrParentContains(candidate.transform, pattern))
            {
                return true;
            }
        }

        return false;
    }

    private static bool NameOrParentContains(Transform transform, string pattern)
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private static float GetRampHalfLength(Collider2D source)
    {
        BoxCollider2D box = source as BoxCollider2D;
        if (box != null)
        {
            Vector2 scale = source.transform.lossyScale;
            return Mathf.Max(Mathf.Abs(box.size.x * scale.x), Mathf.Abs(box.size.y * scale.y)) * 0.5f;
        }

        SpriteRenderer spriteRenderer = source.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            Vector2 size = spriteRenderer.bounds.size;
            return Mathf.Max(size.x, size.y) * 0.5f;
        }

        Vector2 boundsSize = source.bounds.size;
        return Mathf.Max(boundsSize.x, boundsSize.y) * 0.5f;
    }

    private static Text CreateDefaultResultMessage()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            return null;
        }

        GameObject messageObject = new GameObject("PipResultMessage", typeof(RectTransform), typeof(Text));
        messageObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = messageObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -120f);
        rect.sizeDelta = new Vector2(900f, 120f);

        Text text = messageObject.GetComponent<Text>();
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (text.font == null)
        {
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        text.fontSize = 58;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(1f, 0.86f, 0.05f, 1f);
        text.raycastTarget = false;
        messageObject.SetActive(false);
        return text;
    }

    private static void RemoveOldLevel3LaunchFlow()
    {
        PlayButtonPressed[] oldLaunchers = FindObjectsByType<PlayButtonPressed>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (PlayButtonPressed oldLauncher in oldLaunchers)
        {
            if (oldLauncher != null)
            {
                oldLauncher.enabled = false;
                UnityEngine.Object.Destroy(oldLauncher);
            }
        }

        ballScript[] oldBallScripts = FindObjectsByType<ballScript>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (ballScript oldBallScript in oldBallScripts)
        {
            if (oldBallScript != null)
            {
                oldBallScript.enabled = false;
                UnityEngine.Object.Destroy(oldBallScript);
            }
        }
    }

    private void UnregisterControlListeners()
    {
        if (registeredPlayButton != null)
        {
            registeredPlayButton.onClick.RemoveListener(Launch);
            registeredPlayButton = null;
        }

        if (registeredReplayButton != null)
        {
            registeredReplayButton.onClick.RemoveListener(Replay);
            registeredReplayButton = null;
        }

        if (registeredDragProgressBar != null)
        {
            registeredDragProgressBar.onValueChanged.RemoveListener(HandleProgressBarChanged);
            registeredDragProgressBar = null;
        }
    }
}
