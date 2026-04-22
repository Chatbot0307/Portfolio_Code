using Fusion;
using UnityEngine;

/// <summary>
/// 룰 공통 베이스
/// </summary>
public abstract class GameRuleBase : NetworkBehaviour
{
    [Header("Common Rule")]
    [SerializeField] protected float matchDuration = 60f;
    [SerializeField] protected RespawnPolicy respawnPolicy;

    [Networked] public TickTimer MatchTimer { get; protected set; }
    [Networked] public MatchState RuleState { get; protected set; }
    [Networked] public int WinnerTeam { get; protected set; }
    [Networked] public NetworkBool IsRuleFinished { get; protected set; }

    protected GameRuleManager ruleManager;

    public abstract GameRuleType RuleType { get; }

    /// <summary>
    /// 룰 초기화
    /// </summary>
    public virtual void Initialize(GameRuleManager manager)
    {
        ruleManager = manager;
    }

    /// <summary>
    /// 스폰 초기화
    /// </summary>
    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;

        RuleState = MatchState.Waiting;
        WinnerTeam = -1;
        IsRuleFinished = false;
    }

    /// <summary>
    /// 룰 시작
    /// </summary>
    public virtual void StartRule()
    {
        if (!HasStateAuthority)
            return;

        if (RuleState == MatchState.Playing)
            return;

        RuleState = MatchState.Playing;
        WinnerTeam = -1;
        IsRuleFinished = false;
        MatchTimer = TickTimer.CreateFromSeconds(Runner, matchDuration);

        ResetParticipantsToSpawn();
        OnRuleStarted();
    }

    /// <summary>
    /// 룰 종료
    /// </summary>
    public virtual void EndRule(int winnerTeamValue)
    {
        if (!HasStateAuthority)
            return;

        if (IsRuleFinished)
            return;

        WinnerTeam = winnerTeamValue;
        RuleState = MatchState.Ended;
        IsRuleFinished = true;
        MatchTimer = TickTimer.None;

        StopAllParticipants();
        OnRuleEnded(winnerTeamValue);
    }

    /// <summary>
    /// 틱 처리
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (RuleState != MatchState.Playing)
            return;

        if (IsRuleFinished)
            return;

        if (MatchTimer.Expired(Runner))
        {
            OnTimeExpired();
            return;
        }

        OnRuleFixedUpdate();
    }

    /// <summary>
    /// 골 이벤트 처리
    /// </summary>
    public virtual void HandleGoal(Stone stone, int goalZoneOwnerTeam)
    {
    }

    /// <summary>
    /// KO 이벤트 처리
    /// </summary>
    public virtual void HandleKO(Stone stone)
    {
        if (!HasStateAuthority)
            return;

        if (stone == null)
            return;

        if (stone.IsKO)
            return;

        stone.EnterKO();
        OnStoneKO(stone);

        if (ShouldRespawn(stone))
        {
            Vector3 respawnPos = ruleManager.GetRespawnPosition(stone.teamID);
            ruleManager.RespawnSystem.EnqueueRespawn(stone, respawnPolicy.delay, respawnPos);
        }
    }

    /// <summary>
    /// 리스폰 가능 여부 판단
    /// </summary>
    protected virtual bool ShouldRespawn(Stone stone)
    {
        return respawnPolicy != null && respawnPolicy.enabled && !IsRuleFinished;
    }

    /// <summary>
    /// 룰 시작 후처리
    /// </summary>
    protected virtual void OnRuleStarted()
    {
    }

    /// <summary>
    /// 룰 종료 후처리
    /// </summary>
    protected virtual void OnRuleEnded(int winnerTeamValue)
    {
    }

    /// <summary>
    /// 시간 종료 처리
    /// </summary>
    protected virtual void OnTimeExpired()
    {
        EndRule(-1);
    }

    /// <summary>
    /// 룰 프레임 처리
    /// </summary>
    protected virtual void OnRuleFixedUpdate()
    {
    }

    /// <summary>
    /// KO 후처리
    /// </summary>
    protected virtual void OnStoneKO(Stone stone)
    {
    }

    /// <summary>
    /// 전체 참가자 정지
    /// </summary>
    protected void StopAllParticipants()
    {
        var stones = ruleManager.GetAllParticipants();

        for (int i = 0; i < stones.Count; i++)
        {
            if (stones[i] == null)
                continue;

            stones[i].ForceStop();
        }
    }

    /// <summary>
    /// 전체 참가자 초기화
    /// </summary>
    protected void ResetParticipantsToSpawn()
    {
        var stones = ruleManager.GetAllParticipants();

        for (int i = 0; i < stones.Count; i++)
        {
            if (stones[i] == null)
                continue;

            Vector3 spawnPos = ruleManager.GetRespawnPosition(stones[i].teamID);
            stones[i].FinishRespawn(spawnPos);
            stones[i].SetReady(true);
        }
    }
}