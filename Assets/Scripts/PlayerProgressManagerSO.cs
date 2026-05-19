using UnityEngine;

[CreateAssetMenu(fileName = "PlayerProgressManagerSO", menuName = "Scriptable Objects/PlayerProgressManagerSO")]
public class PlayerProgressManagerSO : ScriptableObject
{
    [Header("API Configuration")]
    public string apiBaseUrl = "http://localhost:3000";
    
    [Header("Game Configuration")]
    public string gameId = "penguinRunGame";
}
