using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadSceneByName(string sceneName)
    {
         Debug.Log("Trying to load: " + sceneName);
        SceneManager.LoadScene(sceneName);
    }
}
