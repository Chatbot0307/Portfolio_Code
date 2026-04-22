using Fusion;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 팀 관리
/// </summary>
public class TeamManager : MonoBehaviour
{
    [SerializeField] private MatchSettings settings;

    private readonly Dictionary<PlayerRef, int> playerTeamMap = new();
    private readonly Dictionary<int, int> teamPlayerCountMap = new();

    public int TeamCount => settings != null ? settings.teamCount : 0;

    /// <summary>
    /// 플레이어 팀 배정
    /// </summary>
    public int AssignTeam(PlayerRef playerRef)
    {
        if (settings == null)
        {
            Debug.LogError("[TeamManager] settings null");
            return -1;
        }

        if (settings.teamCount <= 0)
        {
            Debug.LogError($"[TeamManager] teamCount 비정상: {settings.teamCount}");
            return -1;
        }

        if (settings.maxPlayersPerTeam <= 0)
        {
            Debug.LogError($"[TeamManager] maxPlayersPerTeam 비정상: {settings.maxPlayersPerTeam}");
            return -1;
        }

        if (playerTeamMap.TryGetValue(playerRef, out int existingTeam))
            return existingTeam;

        for (int team = 0; team < settings.teamCount; team++)
        {
            int currentCount = teamPlayerCountMap.TryGetValue(team, out int count) ? count : 0;

            if (currentCount < settings.maxPlayersPerTeam)
            {
                playerTeamMap[playerRef] = team;
                teamPlayerCountMap[team] = currentCount + 1;

                Debug.Log($"[TeamManager] 팀 배정 완료 / player:{playerRef} / team:{team}");
                return team;
            }
        }

        Debug.LogError("[TeamManager] 배정 가능한 팀 없음");
        return -1;
    }

    /// <summary>
    /// 플레이어 팀 조회
    /// </summary>
    public int GetTeam(PlayerRef playerRef)
    {
        if (playerTeamMap.TryGetValue(playerRef, out int team))
            return team;

        return -1;
    }

    /// <summary>
    /// 플레이어 팀 제거
    /// </summary>
    public void RemovePlayer(PlayerRef playerRef)
    {
        if (!playerTeamMap.TryGetValue(playerRef, out int team))
            return;

        playerTeamMap.Remove(playerRef);

        if (teamPlayerCountMap.ContainsKey(team))
        {
            teamPlayerCountMap[team]--;

            if (teamPlayerCountMap[team] <= 0)
                teamPlayerCountMap.Remove(team);
        }
    }
}