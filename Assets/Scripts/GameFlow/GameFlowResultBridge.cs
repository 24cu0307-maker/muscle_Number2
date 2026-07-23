/*━━━━━━━━━
@file GameFlowResultBridge.cs
@brief 既存Flow Systemの結果を画面遷移側へ受け渡す
@author 24CU○○○○ 氏名
@date 2026/07/14
最終更新日 2026/07/14
@remarks GameFlowTemplate.GameManagerと同じGameObjectへ追加する
━━━━━━━━━*/
using GameFlowTemplate;
using UnityEngine;

/// <summary>
/// 既存GameManagerの終了イベントをGameSessionへ接続します。
/// </summary>
[RequireComponent(typeof(GameManager))]
public sealed class GameFlowResultBridge : MonoBehaviour
{
    private GameManager m_gameManager; // 接続対象の既存GameManager

    /// <summary>
    /// GameManagerを取得してイベントを購読します。
    /// </summary>
    private void Awake()
    {
        m_gameManager = GetComponent<GameManager>();
        m_gameManager.GameFinished += StoreResult;
    }

    /// <summary>
    /// イベント購読を解除します。
    /// </summary>
    private void OnDestroy()
    {
        if (m_gameManager == null)return;

        m_gameManager.GameFinished -= StoreResult;
    }

    /// <summary>
    /// Flow Systemが生成した最終スコアをシーン間保存領域へ渡します。
    /// </summary>
    private void StoreResult(GameResultContainer _result)
    {
        if (_result == null)return;

        GameSession.SetScore(_result.m_finalScore);
    }
}
