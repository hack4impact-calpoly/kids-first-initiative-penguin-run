using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 8f;
    public float jumpPower = 16f;
    
    private Rigidbody2D rb;
    private bool canJump = false;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Update()
    {
        // Movement
        float moveX = 0f;
        
        if (Input.GetKey(KeyCode.RightArrow))
            moveX = 1f;
        if (Input.GetKey(KeyCode.LeftArrow))
            moveX = -1f;
            
        rb.linearVelocity = new Vector2(moveX * speed, rb.linearVelocity.y);
        
        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && canJump)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpPower);
        }
    }
    
    void OnCollisionStay2D(Collision2D collision)
    {
        canJump = true;
    }
    
    void OnCollisionExit2D(Collision2D collision)
    {
        canJump = false;
    }
}