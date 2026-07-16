using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class goal_Indicator : MonoBehaviour
{
    public static event System.Action PlayerReachedGoal;

    public GameObject goalUI;
    [SerializeField] private PlayerProgressManager playerProgressManager;
    [SerializeField] private DialogueManager dialogueManager;
    [SerializeField] private LevelVictoryPopup victoryPopup;
    [SerializeField] private bool showLevelCompletePopup = true;
    [SerializeField] private string nextLevelSceneName = "Level2_Friction";
    [SerializeField] private string completionTitle = "Pip made it home!";
    [SerializeField] private string[] learnedItems =
    {
        "Gravity pulls things downhill",
        "Steeper hill = more speed"
    };
    [SerializeField] private Sprite[] learnedItemIcons;
    [SerializeField, TextArea(2, 4)] private string completionMessage = "You did it! Gravity pulled me right home!";
    [SerializeField, TextArea(2, 4)] private string nextLevelPrompt = "Hmm... but what if the hill is really steep and I go <color=#E95E00><b>too fast</b></color>? That could be a problem.";

    private float levelStartTime;
    private bool completed;

    private void OnValidate()
    {
        if (playerProgressManager == null)
            playerProgressManager = FindFirstObjectByType<PlayerProgressManager>();

        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();

        if (victoryPopup == null)
            victoryPopup = FindFirstObjectByType<LevelVictoryPopup>();
    }

    private void Start()
    {
        levelStartTime = Time.time;
        if (goalUI != null){
            goalUI.SetActive(false);
        }

        // Find PlayerProgressManager at runtime if not assigned in Inspector
        if (playerProgressManager == null)
            playerProgressManager = FindFirstObjectByType<PlayerProgressManager>();

        if (dialogueManager == null)
            dialogueManager = FindFirstObjectByType<DialogueManager>();

        if (victoryPopup == null)
            victoryPopup = FindFirstObjectByType<LevelVictoryPopup>();
    }

    private void ShowGoalUI()
    {
        if (goalUI != null)
            goalUI.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (completed || !collision.CompareTag("Player"))
        {
            return;
        }

        completed = true;
        PlayerReachedGoal?.Invoke();
        StopPlayer(collision);
        HandleLevelComplete();

        if (showLevelCompletePopup && (victoryPopup != null || dialogueManager != null))
        {
            if (goalUI != null)
            {
                goalUI.SetActive(false);
            }

            if (victoryPopup != null)
            {
                victoryPopup.Show(nextLevelSceneName, completionTitle, learnedItems, learnedItemIcons, completionMessage, nextLevelPrompt);
            }
            else
            {
                dialogueManager.ShowLevelCompletePopup(nextLevelSceneName, completionTitle, learnedItems, completionMessage, nextLevelPrompt);
            }
        }
        else
        {
            StartCoroutine(ShowAndHide());
        }
    }

    private IEnumerator ShowAndHide()
    {
        if (goalUI == null)
        {
            Debug.LogError("[goal_Indicator] Goal UI not assigned!");
            yield break;
        }

        ShowGoalUI();
        yield return new WaitForSeconds(3f);
        goalUI.SetActive(false);
    }

    private void HandleLevelComplete()
    {
        float levelDuration = Time.time - levelStartTime;
        string currentLevelName = SceneManager.GetActiveScene().name;

        Debug.Log($"[goal_Indicator] Level '{currentLevelName}' completed in {levelDuration:F2}s");

        if (playerProgressManager != null)
        {
            playerProgressManager.SaveLevelCompletion(currentLevelName, levelDuration);
        }
        else
        {
            PlayerProgressManager.ReportLevelCompletion(currentLevelName, levelDuration);
        }

    }

    private void StopPlayer(Collider2D collision)
    {
        Rigidbody2D playerRigidbody = collision.attachedRigidbody;
        if (playerRigidbody == null)
        {
            return;
        }

        playerRigidbody.linearVelocity = Vector2.zero;
        playerRigidbody.angularVelocity = 0f;
        playerRigidbody.simulated = false;
    }
}
