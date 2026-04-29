using UnityEngine;
using System.Collections;
using UnityEngine.Networking;
using System;

[RequireComponent(typeof(PlayerProgressManager))]
public class EventService : MonoBehaviour
{
    [SerializeField] private PlayerProgressManagerSO config;
    
    private void OnValidate()
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
                durationMs = durationMs,
                result = "success"
            }
        };
        
        string jsonBody = JsonUtility.ToJson(eventData);
        Debug.Log($"[EventService] Posting level completion: {jsonBody}");
        
        using (UnityWebRequest request = new UnityWebRequest($"{config.apiBaseUrl}/api/events", "POST"))
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
                Debug.LogError($"[EventService] Failed to save: {request.error}");
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
        public int durationMs;
        public string result;
    }
}
