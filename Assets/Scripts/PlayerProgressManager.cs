using UnityEngine;
public class PlayerProgressManager : MonoBehaviour
{
    private static PlayerProgressManager instance;

    private string anonUserId;
    private string sessionId;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializePlayer();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void InitializePlayer()
    {
        if (!PlayerPrefs.HasKey("anonUserId"))
        {
            anonUserId = System.Guid.NewGuid().ToString();
            PlayerPrefs.SetString("anonUserId", anonUserId);
            Debug.Log($"[PlayerProgressManager] Generated new anonUserId: {anonUserId}");
        }
        else
        {
            anonUserId = PlayerPrefs.GetString("anonUserId");
        }
        
        sessionId = PlayerPrefs.GetString("sessionId", "");
    }

    public void SaveLevelCompletion(string levelId, float durationSeconds)
    {
        ReportLevelCompletion(levelId, durationSeconds);
    }

    public static void ReportLevelCompletion(string sceneName, float durationSeconds)
    {
        if (!PenguinLevelIds.TryGetLevelNumber(sceneName, out int levelNumber))
        {
            Debug.LogWarning($"[PlayerProgressManager] Scene '{sceneName}' does not have a stable level number.");
            return;
        }

        bool newlyCompleted = PenguinLevelProgressService.CompleteLevel(levelNumber);
        string result = newlyCompleted ? "completed" : "already complete";
        Debug.Log($"[PlayerProgressManager] Level '{sceneName}' {result} after {durationSeconds:F2}s.");
    }

    public static void SetSessionId(string newSessionId)
    {
        PlayerPrefs.SetString("sessionId", newSessionId);
        Debug.Log($"[PlayerProgressManager] Session ID set: {newSessionId}");
        
        if (instance != null)
            instance.sessionId = newSessionId;
    }

    public string GetAnonUserId()
    {
        return anonUserId;
    }
}
