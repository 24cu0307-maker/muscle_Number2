/*━━━━━━━━━
@file GameSession.cs
@brief シーン遷移とプレイ結果を管理する
@author 24CU○○○○ 氏名
@date 2026/07/14
最終更新日 2026/07/14
@remarks ゲーム本体と画面遷移の接続口
━━━━━━━━━*/
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// シーンをまたいで今回のプレイ結果を保持する、ゲーム本体との接続口です。
/// </summary>
public static class GameSession
{
    public const string TitleScene = "Title";           // タイトルシーン名
    public const string TutorialScene = "Tutorial";     // チュートリアルシーン名
    public const string GameplayScene = "Gameplay";     // インゲームシーン名
    public const string ResultScene = "Result";         // リザルトシーン名

    private const int MinimumScore = 0;                  // スコアの最小値
    private static int m_score;                          // 現在のスコア

    public static int Score => m_score;

    /// <summary>
    /// スコアを初期化してチュートリアルへ遷移します。
    /// </summary>
    public static void StartNewGame()
    {
        m_score = MinimumScore;
        Load(TutorialScene);
    }

    /// <summary>
    /// スコアを設定します。
    /// </summary>
    public static void SetScore(int _score)
    {
        m_score = Mathf.Max(MinimumScore, _score);
    }

    /// <summary>
    /// 現在のスコアへ加算します。
    /// </summary>
    public static void AddScore(int _amount)
    {
        m_score = Mathf.Max(MinimumScore, m_score + _amount);
    }

    /// <summary>
    /// リザルトへ遷移します。
    /// </summary>
    public static void FinishGame()
    {
        Load(ResultScene);
    }

    /// <summary>
    /// 指定したシーンへ遷移します。
    /// </summary>
    public static void Load(string _sceneName)
    {
        SceneManager.LoadScene(_sceneName);
    }
}
