using UnityEngine;
using System.Collections;

public class Goal : MonoBehaviour
{
    public GameObject goalUI;

    private void Start()
    {
        if (goalUI != null){
            goalUI.SetActive(false);
        }    
    }

    private void ShowGoalUI()
    {
        if (goalUI != null)
            goalUI.SetActive(true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            StartCoroutine(ShowAndHide());

            // I was thinking we can do like a time delay as done in ShowAndHide
            // And then move to the next level
            // SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

    }

    private void OnMouseDown()
    {
        // Click the igloo in Play Mode to test to see if Congrats works
        StartCoroutine(ShowAndHide());
    }

    private IEnumerator ShowAndHide()
    {
        goalUI.SetActive(true);

        yield return new WaitForSeconds(3f);

        goalUI.SetActive(false);
    }
}