using UnityEngine;

[CreateAssetMenu(fileName = "NewStoneData", menuName = "Stone/Character Data")]
public class StoneData : ScriptableObject
{
    public string characterName;

    [Header("Visuals")]
    public RuntimeAnimatorController animatorController;
    public GameObject hitEffect;

    [Header("Stats")]
    [Tooltip("무게: 높을수록 적에게 맞았을 때 적게 날아갑니다.")]
    public float weight = 1f;

    [Tooltip("속도: 내가 날릴 때 얼마나 빠르게 나가는지 결정합니다.")]
    public float launchSpeedMultiplier = 10f;

    [Tooltip("파워: 충돌 시 상대방을 밀어내는 추가적인 힘입니다.")]
    public float impactPower = 1.2f;

    [Tooltip("핸들링이 높을수록 이 속도에서 더 빨리 멈춥니다.")]
    [Range(0.1f, 5f)] public float handling = 1.0f;
    public float baseStopThreshold = 1.0f; // 멈춤 판정 값

    public Sprite characterSprite;
}
