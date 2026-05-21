using UnityEngine;
using UnityEngine.SceneManagement;

public class SlideLessonCard : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Rigidbody2D targetRigidbody;
    [SerializeField] private string lessonId = "gravity_down";
    [SerializeField] private string title = "Gravity pulls things DOWN!";
    [SerializeField, TextArea(2, 5)] private string body = "Gravity is an invisible force that pulls everything toward the ground. The steeper the hill, the more gravity speeds you up!";
    [SerializeField] private string buttonLabel = "Got it!";
    [SerializeField] private float movementSpeedThreshold = 0.05f;
    [SerializeField] private bool persistDismissal = true;
    [SerializeField] private bool pauseTargetWhileOpen = true;

    private bool hasTriggered;
    private bool pausedTarget;
    private bool resumeSimulation;
    private Vector2 savedVelocity;
    private float savedAngularVelocity;

    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindFirstObjectByType<DialogueManager>();
        }

        if (targetRigidbody == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                targetRigidbody = player.GetComponent<Rigidbody2D>();
            }
        }

        hasTriggered = IsDismissed();
    }

    private void OnDisable()
    {
        ResumeTarget();
    }

    private void Update()
    {
        if (!ShouldShow())
        {
            return;
        }

        hasTriggered = true;
        PauseTarget();
        dialogueManager.ShowLessonCard(title, body, buttonLabel, HandleDismissed);
    }

    private void HandleDismissed()
    {
        MarkDismissed();
        ResumeTarget();
    }

    private bool ShouldShow()
    {
        return dialogueManager != null
            && targetRigidbody != null
            && !hasTriggered
            && !DialogueManager.IsDialogueOpen
            && IsTargetMoving();
    }

    private bool IsTargetMoving()
    {
        return targetRigidbody != null
            && targetRigidbody.simulated
            && targetRigidbody.linearVelocity.sqrMagnitude >= movementSpeedThreshold * movementSpeedThreshold;
    }

    private void PauseTarget()
    {
        if (!pauseTargetWhileOpen || targetRigidbody == null || pausedTarget)
        {
            return;
        }

        resumeSimulation = targetRigidbody.simulated;
        savedVelocity = targetRigidbody.linearVelocity;
        savedAngularVelocity = targetRigidbody.angularVelocity;
        targetRigidbody.simulated = false;
        pausedTarget = true;
    }

    private void ResumeTarget()
    {
        if (!pausedTarget || targetRigidbody == null)
        {
            return;
        }

        targetRigidbody.simulated = resumeSimulation;
        if (resumeSimulation)
        {
            targetRigidbody.linearVelocity = savedVelocity;
            targetRigidbody.angularVelocity = savedAngularVelocity;
        }

        pausedTarget = false;
    }

    private bool IsDismissed()
    {
        return persistDismissal && PlayerPrefs.GetInt(PlayerPrefsKey, 0) == 1;
    }

    private void MarkDismissed()
    {
        if (!persistDismissal)
        {
            return;
        }

        PlayerPrefs.SetInt(PlayerPrefsKey, 1);
        PlayerPrefs.Save();
    }

    private string PlayerPrefsKey
    {
        get
        {
            string sceneName = SceneManager.GetActiveScene().name;
            return $"PenguinRun.LessonDismissed.{sceneName}.{lessonId}";
        }
    }
}
