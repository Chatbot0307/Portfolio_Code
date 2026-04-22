using DG.Tweening;
using Member.LCH._01.Scripts.Event;
using Member.LCH._01.Scripts.Horror;
using UnityEngine;

public class ObjectMove : DirectingHorror
{
    [Header("이동 설정")]
    [SerializeField] private Transform targetObject; // 움직일 대상
    [SerializeField] private Vector3 moveOffset;    // 현재 위치에서 얼마나 움직일지
    [SerializeField] private float moveDuration = 0.2f; // 이동 속도
    [SerializeField] private Ease moveEase = Ease.OutQuad;

    [Header("사운드 설정")]
    [SerializeField] private AudioClip scareSound;
    [SerializeField] private float volume = 1.0f;

    protected override void Awake()
    {
        base.Awake();
        if (targetObject == null) targetObject = transform;
    }

    protected override void HandleHorror(HorrorTriggerEvent evt)
    {
        // 심박수 상승
        FearManager.Instance.AddBPMSpike(horrorDataSO.fearAmount);

        if (SoundManager.Instance != null && scareSound != null)
        {
            float requiredPitch = scareSound.length / moveDuration;

            SoundManager.Instance.Play3DSoundFollow(scareSound, targetObject, volume, requiredPitch);
        }

        // 사물 이동 연출
        targetObject.DOComplete();
        targetObject.DOMove(targetObject.position + moveOffset, moveDuration).SetEase(moveEase);
    }
}
