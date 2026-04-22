using Fusion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 경기 룰 매니저
/// </summary>
public class GameRuleManager : NetworkBehaviour
{
    public static GameRuleManager Instance { get; private set; }

    [Header("Rule")]
    [SerializeField] private GameRuleType currentRuleType = GameRuleType.StockRespawn;
    [SerializeField] private GameRuleBase[] registeredRules;

    [Header("Match Settings")]
    [SerializeField] private MatchSettings matchSettings;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] teamSpawnPoints;

    [Header("Dependencies")]
    [SerializeField] private TeamManager teamManager;
    [SerializeField] private RespawnSystem respawnSystem;

    [Networked] public MatchState MatchState { get; private set; }
    [Networked] public TickTimer CountdownTimer { get; private set; }

    private readonly Dictionary<GameRuleType, GameRuleBase> ruleRegistry = new();
    private readonly List<Stone> participants = new();

    public GameRuleBase CurrentRule { get; private set; }
    public RespawnSystem RespawnSystem => respawnSystem;

    /// <summary>
    /// 스폰 초기화
    /// </summary>
    public override void Spawned()
    {
        if (Instance == null)
            Instance = this;

        BuildRegistry();
        ActivateRule(currentRuleType);

        if (HasStateAuthority)
            MatchState = MatchState.Waiting;

        Debug.Log($"[GameRuleManager] Spawned / HasStateAuthority:{HasStateAuthority}");
    }

    /// <summary>
    /// 네트워크 틱 처리
    /// </summary>
    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (MatchState == MatchState.Waiting)
        {
            TryStartCountdown();
        }

        UpdateCountdown();
    }

    /// <summary>
    /// 룰 레지스트리 구성
    /// </summary>
    private void BuildRegistry()
    {
        ruleRegistry.Clear();

        for (int i = 0; i < registeredRules.Length; i++)
        {
            GameRuleBase rule = registeredRules[i];

            if (rule == null)
                continue;

            rule.Initialize(this);

            if (ruleRegistry.ContainsKey(rule.RuleType))
                continue;

            ruleRegistry.Add(rule.RuleType, rule);
            Debug.Log($"[GameRuleManager] 룰 등록:{rule.RuleType}");
        }
    }

    /// <summary>
    /// 룰 활성화
    /// </summary>
    public void ActivateRule(GameRuleType ruleType)
    {
        if (!ruleRegistry.TryGetValue(ruleType, out GameRuleBase rule))
        {
            Debug.LogError($"[GameRuleManager] 등록되지 않은 룰:{ruleType}");
            CurrentRule = null;
            return;
        }

        currentRuleType = ruleType;
        CurrentRule = rule;

        Debug.Log($"[GameRuleManager] 룰 활성화:{CurrentRule.GetType().Name}");
    }

    /// <summary>
    /// 참가자 등록
    /// </summary>
    public void RegisterParticipant(Stone stone)
    {
        if (stone == null)
            return;

        if (participants.Contains(stone))
            return;

        participants.Add(stone);

        Debug.Log($"[GameRuleManager] 참가자 등록 / count:{participants.Count} / team:{stone.teamID} / ready:{stone.IsReady}");
    }

    /// <summary>
    /// 참가자 제거
    /// </summary>
    public void UnregisterParticipant(Stone stone)
    {
        if (stone == null)
            return;

        participants.Remove(stone);
    }

    /// <summary>
    /// 참가자 목록 반환
    /// </summary>
    public List<Stone> GetAllParticipants()
    {
        return participants;
    }

    /// <summary>
    /// 카운트다운 시작 시도
    /// </summary>
    public void TryStartCountdown()
    {
        if (!HasStateAuthority)
            return;

        if (CurrentRule == null)
            return;

        if (MatchState != MatchState.Waiting)
            return;

        if (!AreAllPlayersReady())
            return;

        CountdownTimer = TickTimer.CreateFromSeconds(Runner, matchSettings.countdownSeconds);
        MatchState = MatchState.Countdown;

        Debug.Log("[GameRuleManager] Countdown 시작");
    }

    /// <summary>
    /// 시작 조건 검사
    /// </summary>
    private bool AreAllPlayersReady()
    {
        if (matchSettings == null)
            return false;

        if (participants.Count < matchSettings.requiredPlayerCount)
            return false;

        for (int i = 0; i < participants.Count; i++)
        {
            if (participants[i] == null)
                return false;

            if (!participants[i].IsReady)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 카운트다운 갱신
    /// </summary>
    private void UpdateCountdown()
    {
        if (MatchState != MatchState.Countdown)
            return;

        if (!CountdownTimer.Expired(Runner))
            return;

        MatchState = MatchState.Playing;
        Debug.Log("[GameRuleManager] MatchState -> Playing");

        if (CurrentRule != null)
            CurrentRule.StartRule();
    }

    /// <summary>
    /// 리스폰 위치 반환
    /// </summary>
    public Vector3 GetRespawnPosition(int teamID)
    {
        if (teamSpawnPoints != null &&
            teamID >= 0 &&
            teamID < teamSpawnPoints.Length &&
            teamSpawnPoints[teamID] != null)
        {
            return teamSpawnPoints[teamID].position;
        }

        return Vector3.zero;
    }

    /// <summary>
    /// 골 이벤트 전달
    /// </summary>
    public void NotifyGoal(Stone stone, int goalZoneOwnerTeam)
    {
        if (CurrentRule == null)
            return;

        CurrentRule.HandleGoal(stone, goalZoneOwnerTeam);
    }
}