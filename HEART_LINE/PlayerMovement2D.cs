using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement2D : MonoBehaviour
{
    enum PlayerIndex { Player1, Player2 }
    [SerializeField] private PlayerIndex playerIndex = PlayerIndex.Player1;

    private string horizontalInput;
    private string verticalInput;

    [SerializeField] private float speed = 5f;
    private Rigidbody2D rb;

    public Vector2 direction = Vector2.right;

    private Vector3 originalScale;

   [SerializeField]  private Animator animator;
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        SetInputAxes();
        originalScale = transform.localScale;
    }

    void OnValidate()
    {
        SetInputAxes();
    }

    void SetInputAxes()
    {
        switch (playerIndex)
        {
            case PlayerIndex.Player1:
                horizontalInput = "Horizontal_P1";
                verticalInput = "Vertical_P1";
                break;
            case PlayerIndex.Player2:
                horizontalInput = "Horizontal_P2";
                verticalInput = "Vertical_P2";
                break;
        }
    }

    void Update()
    {
        Move();
        Flip();
    }

    void Move()
    {
        float h = Input.GetAxisRaw(horizontalInput);
        float v = Input.GetAxisRaw(verticalInput);
        Vector2 movement = new Vector2(h, v).normalized * speed;

        direction = new Vector2(movement.x, movement.y);
        rb.velocity = movement;
        direction = movement;

        if (rb.velocity != new Vector2(0f,0f))
        {
            animator.SetInteger("PlayerSet", 1); 
        }
        else
        {
            animator.SetInteger("PlayerSet", 0); 
        }
       
        

    }

    void Flip()
    {
        if (playerIndex == PlayerIndex.Player1)
        {
            if (direction.x < 0.01f)
                transform.localScale = new Vector3(Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
            else if (direction.x > -0.01f)
                transform.localScale = new Vector3(-Mathf.Abs(originalScale.x), originalScale.y, originalScale.z);
        }
        
    }
}
