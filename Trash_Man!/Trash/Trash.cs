using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trash : MonoBehaviour
{
    [HideInInspector]
    public float fallSpeed = 0f;
    public TrashData myData;

    private void Update()
    {
        transform.Translate(Vector2.down * fallSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("void"))
        {
            // 방해물이 아니면 미스 처리
            if (myData != null && !myData.isBomb)
            {
                GameManager.Instance.MissItem();
                ComboManager.Instance.ComboReset();
                CameraShake.Instance.TriggerShake(0.05f, 0.05f);
            }
            Destroy(this.gameObject);
        }
        else if (collision.gameObject.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();

            if (player != null && myData != null)
            {
                if (myData.isBomb)
                {
                    // 방해물
                    ComboManager.Instance.ComboReset();
                    CameraShake.Instance.TriggerShake(0.2f, 0.2f);
                }
                else
                {
                    // 분리수거 로직
                    if (player.CurrentData == this.myData)
                    {
                        // 성공
                        GameManager.Instance.GetItem();
                        ComboManager.Instance.AddCombo();
                        player.AddTrashStack();
                    }
                    else
                    {
                        // 실패
                        GameManager.Instance.MissItem();
                        ComboManager.Instance.ComboReset();
                        CameraShake.Instance.TriggerShake(0.1f, 0.1f);
                    }
                }
            }
            Destroy(this.gameObject);
        }
    }
}