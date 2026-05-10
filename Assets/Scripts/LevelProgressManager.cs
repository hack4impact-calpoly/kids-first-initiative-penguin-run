using UnityEngine;
using System.Collections.Generic;

public class LevelProgressManager : MonoBehaviour
{
    private static LevelProgressManager instance;
    
    private Dictionary<string, LevelProgress> levelProgress = new();
    
    [SerializeField]
    [Tooltip("Number of levels in the game")]
    private int totalLevels = 3;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeLevels();
            LoadProgressFromPrefs();
            Debug.Log("[LevelProgressManager] Initialized. L1 unlocked by default.");
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > totalLevels)
        {
            Debug.LogWarning($"[LevelProgressManager] Invalid level number: {levelNumber}");
            return false;
        }
        
        // L1 always unlocked
        if (levelNumber == 1) return true;
        
        // Other levels unlocked if previous level is complete
        return IsLevelComplete(levelNumber - 1);
    }
    
    public bool IsLevelComplete(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > totalLevels)
        {
            Debug.LogWarning($"[LevelProgressManager] Invalid level number: {levelNumber}");
            return false;
        }
        
        string levelId = $"level-{levelNumber}";
        return levelProgress.ContainsKey(levelId) && levelProgress[levelId].completed;
    }
    
    public void MarkLevelComplete(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > totalLevels)
        {
            Debug.LogWarning($"[LevelProgressManager] Cannot mark invalid level: {levelNumber}");
            return;
        }
        
        string levelId = $"level-{levelNumber}";
        
        if (!levelProgress.ContainsKey(levelId))
        {
            levelProgress[levelId] = new LevelProgress { levelId = levelId };
        }
        
        LevelProgress progress = levelProgress[levelId];
        progress.completed = true;
        progress.attempts++;
        progress.lastPlayedAt = System.DateTime.UtcNow.ToString("o");
        
        SaveProgressToPrefs();
        
        // Fire K1-63 onProgress callback
        OnLevelComplete?.Invoke(levelNumber);
        
        Debug.Log($"[LevelProgressManager] Level {levelNumber} marked complete. Attempts: {progress.attempts}");
        
        // Log unlock of next level
        if (levelNumber < totalLevels)
        {
            Debug.Log($"[LevelProgressManager] Level {levelNumber + 1} is now unlocked!");
        }
    }
    
    public int GetAttempts(int levelNumber)
    {
        string levelId = $"level-{levelNumber}";
        if (levelProgress.ContainsKey(levelId))
            return levelProgress[levelId].attempts;
        return 0;
    }
    
    public static System.Action<int> OnLevelComplete;
    
    // ============ PRIVATE HELPER METHODS ============
    
    private void InitializeLevels()
    {
        for (int i = 1; i <= totalLevels; i++)
        {
            string levelId = $"level-{i}";
            if (!levelProgress.ContainsKey(levelId))
            {
                levelProgress[levelId] = new LevelProgress 
                { 
                    levelId = levelId,
                    completed = false,
                    attempts = 0,
                    lastPlayedAt = ""
                };
            }
        }
    }
    
    private void SaveProgressToPrefs()
    {
        foreach (var kvp in levelProgress)
        {
            string levelId = kvp.Key;
            LevelProgress progress = kvp.Value;
            
            PlayerPrefs.SetInt($"{levelId}_completed", progress.completed ? 1 : 0);
            PlayerPrefs.SetInt($"{levelId}_attempts", progress.attempts);
            PlayerPrefs.SetString($"{levelId}_lastPlayedAt", progress.lastPlayedAt);
        }
        PlayerPrefs.Save();
        Debug.Log("[LevelProgressManager] Progress saved to PlayerPrefs");
    }
    
    private void LoadProgressFromPrefs()
    {
        for (int i = 1; i <= totalLevels; i++)
        {
            string levelId = $"level-{i}";
            
            int completed = PlayerPrefs.GetInt($"{levelId}_completed", 0);
            int attempts = PlayerPrefs.GetInt($"{levelId}_attempts", 0);
            string lastPlayedAt = PlayerPrefs.GetString($"{levelId}_lastPlayedAt", "");
            
            levelProgress[levelId] = new LevelProgress
            {
                levelId = levelId,
                completed = completed == 1,
                attempts = attempts,
                lastPlayedAt = lastPlayedAt
            };
        }
        
        // Log loaded state
        for (int i = 1; i <= totalLevels; i++)
        {
            string state = IsLevelComplete(i) ? "COMPLETE" : "NOT COMPLETE";
            string unlocked = IsLevelUnlocked(i) ? "UNLOCKED" : "LOCKED";
            Debug.Log($"[LevelProgressManager] L{i}: {state}, {unlocked}, Attempts: {GetAttempts(i)}");
        }
    }
}

/// <summary>
/// Data structure tracking progress for a single level.
/// </summary>
[System.Serializable]
public class LevelProgress
{
    public string levelId;
    public bool completed;
    public int attempts;
    public string lastPlayedAt;
}
