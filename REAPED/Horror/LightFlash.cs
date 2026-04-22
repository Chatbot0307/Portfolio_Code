using DG.Tweening;
using Member.LCH._01.Scripts.Event;
using Member.LCH._01.Scripts.Horror;
using UnityEngine;

public class LightFlash : DirectingHorror
{
    [Header("라이트 설정")]
    [SerializeField] private Light targetLight;
    [SerializeField] private float flickerDuration = 1.5f; // 깜박이는 총 시간
    [SerializeField] private int flickerCount = 8;        // 깜박이는 횟수

    [Header("사운드 설정")]
    [SerializeField] private AudioClip flickerSound;      // 지지지직 소리
    [SerializeField] private AudioClip burstSound;        // 팍! 하고 터지며 꺼지는 소리

    protected override void Awake()
    {
        base.Awake(); // 이벤트 버스 구독
        if (targetLight == null) targetLight = GetComponent<Light>();
    }

    protected override void HandleHorror(HorrorTriggerEvent evt)
    {
        // 심박수 즉시 상승
        FearManager.Instance.AddBPMSpike(horrorDataSO.fearAmount);

        // 연출 시퀀스 생성
        Sequence lightSeq = DOTween.Sequence();
        float originalIntensity = targetLight.intensity;

        // 지지지직 사운드 재생
        if (flickerSound != null) SoundManager.Instance.Play3DSound(flickerSound, transform.position);

        // 깜박임 연출
        for (int i = 0; i < flickerCount; i++)
        {
            lightSeq.Append(targetLight.DOIntensity(originalIntensity * Random.Range(0.1f, 0.5f), flickerDuration / flickerCount / 2));
            lightSeq.Append(targetLight.DOIntensity(originalIntensity, flickerDuration / flickerCount / 2));
        }

        // 완전히 꺼짐
        lightSeq.AppendCallback(() => {
            targetLight.intensity = 0;
            targetLight.enabled = false;

            // 터지는 소리 재생
            if (burstSound != null) SoundManager.Instance.Play3DSound(burstSound, transform.position);
        });

        Destroy(targetLight.gameObject, flickerDuration + 0.1f);
    }
}

