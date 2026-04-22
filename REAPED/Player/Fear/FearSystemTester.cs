using UnityEngine;
using Member.LCH._01.Scripts.Horror;

public class FearSystemTester : MonoBehaviour
{
    public CursedItemSO testItem; // 아까 만든 SO 할당
    public float testSpikeAmount = 20f;

    void Update()
    {
        // 1번 누르면 물건 집기 (심박수 상승 시작)
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            FearManager.Instance.OnPickUpItem(testItem);
            Debug.Log("Item Picked Up! BPM Rising...");
        }

        // 2번 누르면 물건 내려놓기 (심박수 감소 시작)
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            FearManager.Instance.OnDropItem();
            Debug.Log("Item Dropped. BPM Recovering...");
        }

        // 3번 누르면 즉시 공포 연출 발생 (스파이크)
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            FearManager.Instance.AddBPMSpike(testSpikeAmount);
            Debug.Log("Fear Spike Triggered!");
        }
    }
}