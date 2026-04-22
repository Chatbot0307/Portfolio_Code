using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 경기 종료 화면 뷰
/// </summary>
public class MatchEndView : MonoBehaviour
{
    [SerializeField] private GameObject rootObject;
    [SerializeField] private Text resultText;

    /// <summary>
    /// 승패 텍스트 표시
    /// </summary>
    public void ShowResult(bool isWin)
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (resultText != null)
            resultText.text = isWin ? "승리" : "패배";
    }

    /// <summary>
    /// 무승부 텍스트 표시
    /// </summary>
    public void ShowDraw()
    {
        if (rootObject != null)
            rootObject.SetActive(true);

        if (resultText != null)
            resultText.text = "무승부";
    }

    /// <summary>
    /// 화면 숨김
    /// </summary>
    public void Hide()
    {
        if (rootObject != null)
            rootObject.SetActive(false);
    }
}