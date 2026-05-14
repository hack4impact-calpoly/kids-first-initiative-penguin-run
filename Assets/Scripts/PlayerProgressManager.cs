using UnityEngine;
using UnityEngine.SceneManagement;
using System;

[RequireComponent(typeof(EventService))]
public class PlayerProgressManager : MonoBehaviour
{
    private static PlayerProgressManager instance;
    
    private string anonUserId;
    private string sessionId;
    private EventService eventService;
    private float levelStartTime;
    
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
    
    private void OnEnable()
    {
        LevelProgressManager.OnLevelComplete += HandleLevelComplete;
    }
    
    private void OnDisable()
    {
        LevelProgressManager.OnLevelComplete -= HandleLevelComplete;
    }
    
    private void OnValidate()
    {
        if (GetComponent<EventService>() == null)
            gameObject.AddComponent<EventService>();
    }
    
    private void InitializePlayer()
    {
        eventService = GetComponent<EventService>();
        levelStartTime = Time.time;
        
        // Generate or retrieve anonymous user ID
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
        
        // Get session ID (must be set by web backend or login system)
        sessionId = PlayerPrefs.GetString("sessionId", "");
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogWarning("[PlayerProgressManager] Session ID not set. Call SetSessionId() before saving progress.");
        }
    }
    
    private void HandleLevelComplete(int levelNumber)
    {
        float levelDuration = Time.time - levelStartTime;
        string currentLevelName = SceneManager.GetActiveScene().name;
        
        SaveLevelCompletion(currentLevelName, levelDuration);
        
        Debug.Log($"[PlayerProgressManager] Level {levelNumber} event sent to backend");
    }
    
    public void SaveLevelCompletion(string levelId, float durationSeconds)
    {
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogError("[PlayerProgressManager] Cannot save progress: Session ID is missing");
            return;
        }
        
        int durationMs = (int)(durationSeconds * 1000);
        eventService.SendLevelCompletionEvent(anonUserId, sessionId, levelId, durationMs);
    }
    
    public static void SetSessionId(string newSessionId)
    {
        PlayerPrefs.SetString("sessionId", newSessionId);
        Debug.Log($"[PlayerProgressManager] Session ID set: {newSessionId}");
        
        // Update instance if it exists
        if (instance != null)
            instance.sessionId = newSessionId;
    }
    
    public string GetAnonUserId()
    {
        return anonUserId;
    }
}
