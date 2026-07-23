/*━━━━━━━━━
@file ResultView.cs
@brief リザルト画面へスコアを表示する
@author 24CU○○○○ 氏名
@date 2026/07/14
最終更新日 2026/07/14
@remarks なし
━━━━━━━━━*/
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 保存されたスコアをリザルトUIへ反映します。
/// </summary>
public sealed class ResultView : MonoBehaviour
{
    private const string ScoreFormat = "SCORE\n{0:N0}"; // スコアの表示形式

    [SerializeField] private Text m_scoreText;           // スコア表示先

    /// <summary>
    /// 画面表示時にスコアを反映します。
    /// </summary>
    private void Start()
    {
        if (m_scoreText == null)return;

        m_scoreText.text = string.Format(ScoreFormat, GameSession.Score);
    }
}
