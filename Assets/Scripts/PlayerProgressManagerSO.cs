using UnityEngine;

[CreateAssetMenu(menuName = "Configs/Player Progress Manager")]
public class PlayerProgressManagerSO : ScriptableObject
{
    [Header("API Configuration")]
    public string apiBaseUrl = "http://localhost:3000";
    
    [Header("Game Configuration")]
    public string gameId = "penguin-run";
}
