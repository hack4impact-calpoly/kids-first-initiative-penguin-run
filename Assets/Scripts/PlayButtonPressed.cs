using UnityEngine;

public class PlayButtonPressed : MonoBehaviour
{
    public ResetLevel resetLevel;

    private float camWidth;
    private float camHeight;
    private Camera cam;

    public Rigidbody2D penguinRb;
    public GameObject penguin;

    private void Start()
    {
        // Session ID should be set by web backend/authentication before game loads
        string sessionId = PlayerPrefs.GetString("sessionId", "");
        if (string.IsNullOrEmpty(sessionId))
        {
            Debug.LogWarning("[PlayButtonPressed] Session ID not set. Game events won't be saved.");
        }

        penguin = GameObject.FindGameObjectWithTag("Player");
        penguinRb = penguin.GetComponent<Rigidbody2D>();

        penguinRb.simulated = false;

        // Get the camera information
        cam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        camHeight = 2f * cam.orthographicSize;
        camWidth = camHeight * cam.aspect;

        resetLevel = FindFirstObjectByType<ResetLevel>();
    }

    void Update()
    {   
        // These lines are to debug
        // Vector3 penguinPos = penguin.transform.position;
        // Debug.Log("Penguin position: " + penguinPos);

        // If the penguin's position is outside of the camera position
        // cam.transform.position gets the midpoint of the camera
        if (penguin.transform.position.x > (cam.transform.position.x + camWidth / 2f) ||
            penguin.transform.position.y > (cam.transform.position.y + camHeight / 2f) ||
            penguin.transform.position.x < (cam.transform.position.x - camWidth / 2f) ||
            penguin.transform.position.y < (cam.transform.position.y - camHeight / 2f))
        {
            resetLevel.ResetGame();
        }
    }

    public void PlayButtonClicked()
    {
        penguinRb.simulated = true;
    }
}