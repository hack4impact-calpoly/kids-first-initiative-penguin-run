using UnityEngine;

public class PlayButtonPressed : MonoBehaviour
{
    
     private Rigidbody2D rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         rb = GetComponent<Rigidbody2D>();
        rb.simulated = false; 
    }

    // Update is called once per frame
    public void PlayButtonClicked()
    {
        Debug.Log("PLAY BUTTON CLICKED"+ gameObject.name);
            rb.simulated = true;
        
    }
}
