using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class goal_Indicator : MonoBehaviour
{
    public GameObject goalUI;
    [SerializeField] private PlayerProgressManager playerProgressManager;

    private float levelStartTime;

    private void OnValidate()
    {
        if (playerProgressManager == null)
            playerProgressManager = FindFirstObjectByType<PlayerProgressManager>();
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
    }

    private void ShowGoalUI()
    {
        if (goalUI != null)
            goalUI.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            HandleLevelComplete();
            StartCoroutine(ShowAndHide());
        }

        // I was thinking we can do like a time delay as done in ShowAndHide
        // And then move to the next level
        // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    private void OnMouseDown()
    {
        // Click the igloo in Play Mode to test to see if Congrats works
        StartCoroutine(ShowAndHide());
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
        
        // Save to backend (K1-57)
        if (playerProgressManager != null)
        {
            playerProgressManager.SaveLevelCompletion(currentLevelName, levelDuration);
        }
        else
        {
            Debug.LogError("[goal_Indicator] PlayerProgressManager not found!");
        }
        
        // Update local progress & unlock next level
        int levelNumber = ExtractLevelNumber(currentLevelName);
        if (levelNumber > 0)
        {
            LevelProgressManager levelProgressManager = FindFirstObjectByType<LevelProgressManager>();
            if (levelProgressManager != null)
            {
                levelProgressManager.MarkLevelComplete(levelNumber);
            }
            else
            {
                Debug.LogWarning("[goal_Indicator] LevelProgressManager not found. Local progress not saved.");
            }
        }
    }
    
    private int ExtractLevelNumber(string sceneName)
    {
        var match = System.Text.RegularExpressions.Regex.Match(sceneName, @"\d+");
        if (match.Success && int.TryParse(match.Value, out int levelNumber))
            return levelNumber;
        return -1;
    }
}