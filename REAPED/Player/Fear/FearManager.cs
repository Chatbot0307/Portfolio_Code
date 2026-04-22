using DG.Tweening;
using UnityEngine;

public class FearManager : MonoBehaviour
{
    public static FearManager Instance;

    [Header("BPM 세팅")]
    public float currentBPM = 60f;        // 기본 심박수
    private readonly float _minBPM = 60f; // 최저 심박수
    private readonly float _maxBPM = 200f; // 최대 심박수

    [Header("회복 설정")]
    [SerializeField] private float baseRecoveryRate = 2.0f;
    [SerializeField] private float recoveryDelay = 3.0f; // 상승 후 회복까지 기다리는 시간
    private float _recoveryTimer = 0f;

    private CursedItemSO _heldItem;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        HandleBPMDynamics();
    }

    private void HandleBPMDynamics()
    {
        // 물건 소지 시 BPM 상승
        if (_heldItem != null)
        {
            // 아이템 데이터에 정의된 BPM 상승치만큼 가산
            currentBPM += _heldItem.fearIncreaseRate * Time.deltaTime;
            ResetRecoveryTimer();
        }
        // 평상시 BPM 감소
        else
        {
            // 회복지연 타이머가 돌때까찌
            if (_recoveryTimer > 0)
            {
                _recoveryTimer -= Time.deltaTime;
            }
            // 타이머가 끝났을 때만 감소
            else if (currentBPM > _minBPM)
            {
                currentBPM -= baseRecoveryRate * Time.deltaTime;
            }
        }

        // 수치 제한
        currentBPM = Mathf.Clamp(currentBPM, _minBPM, _maxBPM);
    }

    private void ResetRecoveryTimer()
    {
        _recoveryTimer = recoveryDelay;
    }

    // 공포 연출 발생 시 심박수 즉시 추가
    public void AddBPMSpike(float amount)
    {
        currentBPM = Mathf.Min(_maxBPM, currentBPM + amount);
    }

    public void AddBPMGradual(float amount, float duration)
    {
        DOTween.To(() => currentBPM, x => currentBPM = x, Mathf.Min(_maxBPM, currentBPM + amount), duration).SetEase(Ease.Linear);
    }

    public void OnPickUpItem(CursedItemSO item)
    {
        _heldItem = item;
        AddBPMGradual(item.pickupFearSpike, item.fearIncreaseRate);
    }

    public void OnDropItem() => _heldItem = null;

    public float GetCurrentBPM()
    {
        return currentBPM;
    }
}