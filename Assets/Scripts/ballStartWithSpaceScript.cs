using UnityEngine;

public class ballStartWithSpaceScript : MonoBehaviour
{
     private Rigidbody2D rb;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         rb = GetComponent<Rigidbody2D>();
        rb.simulated = false; 
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            rb.simulated = true;
        }
    }
}
