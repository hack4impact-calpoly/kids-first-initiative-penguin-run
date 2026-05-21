using System.Collections;
using UnityEngine;

public class SlideDialogueCue : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private Rigidbody2D targetRigidbody;
    [SerializeField] private Transform speakerTransform;
    [SerializeField] private Sprite cueIconSprite;
    [SerializeField, TextArea(1, 3)] private string message = "Wheeeee! Gravity!";
    [SerializeField] private float delayAfterMovement = 1f;
    [SerializeField] private float movementSpeedThreshold = 0.05f;
    [SerializeField] private bool showOnce = true;

    private Coroutine showCoroutine;
    private bool hasShown;

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
                speakerTransform = player.transform;
            }
        }
        else if (speakerTransform == null)
        {
            speakerTransform = targetRigidbody.transform;
        }
    }

    private void OnEnable()
    {
        goal_Indicator.PlayerReachedGoal += HideCue;
    }

    private void OnDisable()
    {
        goal_Indicator.PlayerReachedGoal -= HideCue;
        StopPendingCue();
        dialogueManager?.HideSlideCue();
    }

    private void Update()
    {
        if (!ShouldStartCue())
        {
            return;
        }

        showCoroutine = StartCoroutine(ShowAfterDelay());
    }

    public void HideCue()
    {
        StopPendingCue();
        dialogueManager?.HideSlideCue();
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(delayAfterMovement);

        if (IsTargetMoving() && !DialogueManager.IsDialogueOpen && dialogueManager != null)
        {
            dialogueManager.ShowSlideCue(message, speakerTransform, cueIconSprite);
            hasShown = true;
        }

        showCoroutine = null;
    }

    private bool ShouldStartCue()
    {
        return dialogueManager != null
            && targetRigidbody != null
            && showCoroutine == null
            && (!showOnce || !hasShown)
            && !DialogueManager.IsDialogueOpen
            && IsTargetMoving();
    }

    private bool IsTargetMoving()
    {
        return targetRigidbody != null
            && targetRigidbody.simulated
            && targetRigidbody.linearVelocity.sqrMagnitude >= movementSpeedThreshold * movementSpeedThreshold;
    }

    private void StopPendingCue()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }
    }
}
