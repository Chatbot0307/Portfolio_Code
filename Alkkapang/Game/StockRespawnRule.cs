using Fusion;
using UnityEngine;

/// <summary>
/// 스톡 리스폰 룰
/// </summary>
public class StockRespawnRule : GameRuleBase
{
    [Header("Stock Rule")]
    [SerializeField] private int defaultStock = 3;

    [Header("KO Rule")]
    [SerializeField] private int validHitTickWindow = 90;

    [Networked] private int Team0Stock { get; set; }
    [Networked] private int Team1Stock { get; set; }

    public override GameRuleType RuleType => GameRuleType.StockRespawn;

    /// <summary>
    /// 초기화
    /// </summary>
    private void Awake()
    {
        if (respawnPolicy == null)
            respawnPolicy = new RespawnPolicy();

        respawnPolicy.enabled = true;
        respawnPolicy.delay = 2f;
    }

    /// <summary>
    /// 룰 시작 후처리
    /// </summary>
    protected override void OnRuleStarted()
    {
        Team0Stock = defaultStock;
        Team1Stock = defaultStock;
    }

    /// <summary>
    /// 골 이벤트 처리
    /// </summary>
    public override void HandleGoal(Stone stone, int goalZoneOwnerTeam)
    {
        if (!HasStateAuthority)
            return;

        if (stone == null)
            return;

        if (RuleState != MatchState.Playing)
            return;

        if (stone.teamID == goalZoneOwnerTeam)
            return;

        if (!stone.HasValidRecentHit(validHitTickWindow))
        {
            Debug.Log("[StockRespawnRule] 유효한 마지막 타격 없음 - KO 무효");
            return;
        }

        if (stone.IsRecentSelfLaunch(validHitTickWindow))
        {
            Debug.Log("[StockRespawnRule] 자기 발사 자가 진입 - KO 무효");
            return;
        }

        HandleKO(stone);
    }

    /// <summary>
    /// KO 후처리
    /// </summary>
    protected override void OnStoneKO(Stone stone)
    {
        if (stone.teamID == 0)
        {
            Team0Stock--;

            if (Team0Stock <= 0)
            {
                EndRule(1);
            }
        }
        else if (stone.teamID == 1)
        {
            Team1Stock--;

            if (Team1Stock <= 0)
            {
                EndRule(0);
            }
        }

        Debug.Log($"[StockRespawnRule] KO 인정 / victimTeam:{stone.teamID} / killerTeam:{stone.LastHitByTeamId} / killerPlayer:{stone.LastHitByPlayerRef}");
    }

    /// <summary>
    /// 시간 종료 처리
    /// </summary>
    protected override void OnTimeExpired()
    {
        if (Team0Stock > Team1Stock)
        {
            EndRule(0);
        }
        else if (Team1Stock > Team0Stock)
        {
            EndRule(1);
        }
        else
        {
            EndRule(-1);
        }
    }

    /// <summary>
    /// 리스폰 가능 여부 판단
    /// </summary>
    protected override bool ShouldRespawn(Stone stone)
    {
        if (IsRuleFinished)
            return false;

        if (stone.teamID == 0 && Team0Stock <= 0)
            return false;

        if (stone.teamID == 1 && Team1Stock <= 0)
            return false;

        return respawnPolicy != null && respawnPolicy.enabled;
    }
}