using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTest : MonoBehaviour
{
    // 
    //Test를 플레이어 테스트르ㄹ 위한 스크립트입니다 
    //
    [SerializeField] private float speed;
    [SerializeField] private float lerpspeed; // lerp 곱해서 부드러운 움직임을 구현해볼게연

    private Rigidbody2D rb;
    private Vector2 moveInput;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        moveInput = new Vector2(h, v).normalized;
    }

    private void FixedUpdate()
    {
        Vector2 targetSpeed = moveInput * speed;

        rb.velocity = Vector2.Lerp(rb.velocity, targetSpeed, Time.deltaTime * lerpspeed);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Bullet")
        {
            PlayerHP.Instance.DiscountHP();

            Destroy(collision.gameObject);
        }
    }
}