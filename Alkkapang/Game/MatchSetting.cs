using UnityEngine;

/// <summary>
/// 매치 설정 데이터
/// </summary>
[System.Serializable]
public class MatchSettings
{
    public int requiredPlayerCount = 2;
    public int teamCount = 2;
    public int maxPlayersPerTeam = 1;
    public float countdownSeconds = 3f;
}