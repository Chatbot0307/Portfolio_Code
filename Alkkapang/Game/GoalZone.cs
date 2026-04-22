using UnityEngine;

/// <summary>
/// 골 존 처리
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class GoalZone : MonoBehaviour
{
    [SerializeField] private int ownerTeam;
    [SerializeField] private GameRuleManager ruleManager;

    /// <summary>
    /// 트리거 초기화
    /// </summary>
    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    /// <summary>
    /// 골 진입 감지
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ruleManager == null)
            return;

        if (!other.TryGetComponent(out Stone stone))
            return;

        if (stone.IsKO || stone.IsRespawning)
            return;

        ruleManager.NotifyGoal(stone, ownerTeam);
    }
}