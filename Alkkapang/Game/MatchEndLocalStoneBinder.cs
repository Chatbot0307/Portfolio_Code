using UnityEngine;

/// <summary>
/// 로컬 스톤 자동 연결
/// </summary>
public class MatchEndLocalStoneBinder : MonoBehaviour
{
    [SerializeField] private MatchEndPresenter presenter;

    /// <summary>
    /// 로컬 스톤 탐색 연결
    /// </summary>
    private void Start()
    {
        Stone[] stones = FindObjectsByType<Stone>(FindObjectsSortMode.None);

        for (int i = 0; i < stones.Length; i++)
        {
            if (stones[i] != null && stones[i].HasInputAuthority)
            {
                presenter.SetLocalStone(stones[i]);
                break;
            }
        }
    }
}