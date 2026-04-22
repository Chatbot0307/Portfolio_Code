using UnityEngine;

public class ShakeTrash : MonoBehaviour
{
    [Header("Movement")]
    public float fallSpeed = 0.8f; // 아래로 떨어지는 기본 속도
    public float swayWidth = 0.5f; // 좌우로 흔들리는 폭
    public float swaySpeed = 2.0f; // 흔들리는 속도 

    private Vector3 startPosition;
    private float accumulatedY; // 누적된 Y축 이동 거리

    void Start()
    {
        // 생성된 위치 
        startPosition = transform.position;
        accumulatedY = startPosition.y;
    }

    void Update()
    {
        accumulatedY -= fallSpeed * Time.deltaTime;

        float swayOffset = Mathf.Sin(Time.time * swaySpeed) * swayWidth;

        transform.position = new Vector3(startPosition.x + swayOffset, accumulatedY, 0);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("void"))
        {
            GameManager.Instance.MissItem();
            ComboManager.Instance.ComboReset();
            CameraShake.Instance.TriggerShake(0.05f, 0.05f);
            Destroy(this.gameObject);
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            GameManager.Instance.GetItem();
            ComboManager.Instance.AddCombo();
            Destroy(this.gameObject);
        }
    }
}