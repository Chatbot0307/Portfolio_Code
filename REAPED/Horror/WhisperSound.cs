using Member.LCH._01.Scripts.Horror;
using UnityEngine;
using System.Collections;
using Member.LCH._01.Scripts.Event;

public class WhisperSound : DirectingHorror
{
    [Header("환청 사운드 리스트")]
    [SerializeField] private AudioClip[] whisperClips; // 속삭임 소리

    [Header("연출 설정")]
    [SerializeField] private int totalWhisperCount = 7; // 총 몇 번 들릴지
    [SerializeField] private float minInterval = 0.2f;    // 최소 간격
    [SerializeField] private float maxInterval = 1.5f;    // 최대 간격
    [SerializeField] private float whisperRadius = 4f;    // 소리가 발생할 반경
    [SerializeField] private float volume = 0.6f;

    protected override void HandleHorror(HorrorTriggerEvent evt)
    {
        // 환청이 시작될 때 BPM을 서서히 올리기
        FearManager.Instance.AddBPMGradual(horrorDataSO.fearAmount, horrorDataSO.fearDuration);

        StartCoroutine(PlayWhisperSequence());
    }

    private IEnumerator PlayWhisperSequence()
    {
        for (int i = 0; i < totalWhisperCount; i++)
        {
            // 랜덤한 시간 대기
            float waitTime = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(waitTime);

            // 랜덤한 사운드 선택
            AudioClip clip = whisperClips[Random.Range(0, whisperClips.Length)];

            // 플레이어 주변 랜덤 위치
            Vector3 randomOffset = Random.insideUnitSphere * whisperRadius;
            randomOffset.y = Random.Range(0.5f, 2.0f); // 귀 높이 정도로 보정
            Vector3 playPos = transform.position + randomOffset;

            SoundManager.Instance.Play3DSound(clip, playPos, volume);

            FearManager.Instance.AddBPMSpike(1.5f);
        }
    }
}
