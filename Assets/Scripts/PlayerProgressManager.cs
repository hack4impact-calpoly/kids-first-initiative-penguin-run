using UnityEngine;
using System;

[RequireComponent(typeof(EventService))]
public class PlayerProgressManager : MonoBehaviour
{
    private static PlayerProgressManager instance;

    private string anonUserId;
    private string sessionId;
    private EventService eventService;

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

    private void OnValidate()
    {
        if (GetComponent<EventService>() == null)
            gameObject.AddComponent<EventService>();
    }

    private void InitializePlayer()
    {
        eventService = GetComponent<EventService>();

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

    /// <summary>
    /// Call this when a level is completed. Triggers event save.
    /// </summary>
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

    /// <summary>
    /// Set the session ID (call from authentication/web backend)
    /// </summary>
    public static void SetSessionId(string newSessionId)
    {
        PlayerPrefs.SetString("sessionId", newSessionId);
        Debug.Log($"[PlayerProgressManager] Session ID set: {newSessionId}");

        // Update instance if it exists
        if (instance != null)
            instance.sessionId = newSessionId;
    }

    /// <summary>
    /// Get the current anonymous user ID
    /// </summary>
    public string GetAnonUserId()
    {
        return anonUserId;
    }
}
