using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class goal_Indicator : MonoBehaviour
{
    [SerializeField]
    [Tooltip("UI panel to show when goal is reached")]
    public GameObject goalUI;
    
    [SerializeField]
    [Tooltip("Reference to LevelProgressManager for local progress tracking")]
    private LevelProgressManager levelProgressManager;

    private float levelStartTime;

    private void OnValidate()
    {
        if (levelProgressManager == null)
            levelProgressManager = FindFirstObjectByType<LevelProgressManager>();
    }

    private void Start()
    {
        levelStartTime = Time.time;
        if (goalUI != null){
            goalUI.SetActive(false);
        }
        
        if (levelProgressManager == null)
            levelProgressManager = FindFirstObjectByType<LevelProgressManager>();
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
        
        LoadNextLevel();
    }
    
    private void LoadNextLevel()
    {
        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        
        if (nextSceneIndex < sceneCount)
        {
            Debug.Log($"[goal_Indicator] Loading next level (scene index {nextSceneIndex})");
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("[goal_Indicator] No more levels. Game complete!");
        }
    }

    private void HandleLevelComplete()
    {
        float levelDuration = Time.time - levelStartTime;
        string currentLevelName = SceneManager.GetActiveScene().name;
        
        Debug.Log($"[goal_Indicator] Level '{currentLevelName}' completed in {levelDuration:F2}s");
        
        if (levelProgressManager == null)
        {
            Debug.LogError("[goal_Indicator] LevelProgressManager not assigned!");
            return;
        }
        
        int levelNumber = ExtractLevelNumber(currentLevelName);
        if (levelNumber > 0)
        {
            levelProgressManager.MarkLevelComplete(levelNumber);
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