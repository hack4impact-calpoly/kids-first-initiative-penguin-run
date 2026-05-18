using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

public class EventService : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Configuration for API endpoints and game ID")]
    private PlayerProgressManagerSO config;
    
    private void Awake()
    {
        if (config == null)
            config = Resources.Load<PlayerProgressManagerSO>("Configs/PlayerProgressManager");
    }
    
    public void SendLevelCompletionEvent(string anonUserId, string sessionId, string levelId, int durationMs)
    {
        if (config == null)
        {
            Debug.LogError("EventService: Config not assigned");
            return;
        }
        
        StartCoroutine(PostEvent(anonUserId, sessionId, levelId, durationMs));
    }
    
    private IEnumerator PostEvent(string anonUserId, string sessionId, string levelId, int durationMs)
    {
        string eventId = System.Guid.NewGuid().ToString();
        
        EventData eventData = new EventData
        {
            eventId = eventId,
            anonUserId = anonUserId,
            sessionId = sessionId,
            @event = "level_completed",
            ts = System.DateTime.UtcNow.ToString("o"),
            props = new EventProps
            {
                gameId = config.gameId,
                levelId = levelId,
                durationMs = durationMs,
                result = "success"
            }
        };
        
        string jsonBody = JsonUtility.ToJson(eventData);
        string url = $"{config.apiBaseUrl}/api/events";
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"[EventService] Posting to URL: {url}");
        Debug.Log($"[EventService] Posting level completion: {jsonBody}");
        #else
        Debug.Log($"[EventService] Posting level completion event");
        #endif
        
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[EventService] Level completion saved successfully");
            }
            else
            {
                Debug.LogError($"[EventService] Failed to save: HTTP {request.responseCode} - {request.error}");
                Debug.LogError($"[EventService] Response: {request.downloadHandler.text}");
            }
        }
    }
    
    [System.Serializable]
    public class EventData
    {
        public string eventId;
        public string anonUserId;
        public string sessionId;
        public string @event;
        public string ts;
        public EventProps props;
    }
    
    [System.Serializable]
    public class EventProps
    {
        public string gameId;
        public string levelId;
        public int durationMs;
        public string result;
    }
}
