using Fusion;
using UnityEngine;

/// <summary>
/// 경기 종료 화면 표시 제어
/// </summary>
public class MatchEndPresenter : MonoBehaviour
{
    [SerializeField] private MatchEndView view;
    [SerializeField] private Stone localStone;

    private bool hasShown;

    /// <summary>
    /// 초기화
    /// </summary>
    private void Start()
    {
        if (view != null)
            view.Hide();
    }

    /// <summary>
    /// 화면 갱신
    /// </summary>
    private void Update()
    {
        if (hasShown)
            return;

        if (GameRuleManager.Instance == null)
            return;

        if (GameRuleManager.Instance.CurrentRule == null)
            return;

        if (localStone == null)
            return;

        GameRuleBase rule = GameRuleManager.Instance.CurrentRule;

        if (!rule.IsRuleFinished)
            return;

        int winnerTeam = rule.WinnerTeam;
        int myTeam = localStone.teamID;

        if (winnerTeam < 0)
        {
            view.ShowDraw();
        }
        else
        {
            bool isWin = winnerTeam == myTeam;
            view.ShowResult(isWin);
        }

        hasShown = true;
    }

    /// <summary>
    /// 로컬 스톤 설정
    /// </summary>
    public void SetLocalStone(Stone stone)
    {
        localStone = stone;
    }
}