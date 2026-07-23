/*━━━━━━━━━
@file GameFlowButton.cs
@brief 画面遷移ボタンの動作を制御する
@author 24CU○○○○ 氏名
@date 2026/07/14
最終更新日 2026/07/14
@remarks なし
━━━━━━━━━*/
using UnityEngine;

/// <summary>
/// ボタンに割り当てた画面遷移処理を実行します。
/// </summary>
public sealed class GameFlowButton : MonoBehaviour
{
    public enum EAction
    {
        StartGame,
        OpenGameplay,
        FinishGameplay,
        Retry,
        ReturnToTitle,
        Quit
    }

    [SerializeField] private EAction m_action;   // ボタン押下時の処理

    /// <summary>
    /// Inspectorで指定した処理を実行します。
    /// </summary>
    public void Execute()
    {
        switch (m_action)
        {
            case EAction.StartGame:
                GameSession.StartNewGame();
                break;
            case EAction.OpenGameplay:
            case EAction.Retry:
                GameSession.Load(GameSession.GameplayScene);
                break;
            case EAction.FinishGameplay:
                GameSession.FinishGame();
                break;
            case EAction.ReturnToTitle:
                GameSession.Load(GameSession.TitleScene);
                break;
            case EAction.Quit:
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                break;
        }
    }
}
