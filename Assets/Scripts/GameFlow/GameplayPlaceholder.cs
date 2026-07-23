/*━━━━━━━━━
@file GameplayPlaceholder.cs
@brief メインゲーム接続前の仮動作を提供する
@author 24CU○○○○ 氏名
@date 2026/07/14
最終更新日 2026/07/14
@remarks 本番のゲーム本体を接続した後は削除可能
━━━━━━━━━*/
using UnityEngine;
using GameFlowTemplate;

/// <summary>
/// 仮のインゲームです。本番のゲーム本体を接続したら、このコンポーネントは削除できます。
/// </summary>
public sealed class GameplayPlaceholder : MonoBehaviour
{
    private const int DefaultPreviewScore = 25252;                // 仮スコアの初期値

    [SerializeField] private int m_previewScore = DefaultPreviewScore; // 仮スコア
    [SerializeField] private GameManager m_gameManager;                 // 既存Flow System

    /// <summary>
    /// 仮スコアを保存してリザルトへ遷移します。
    /// </summary>
    public void CompletePreview()
    {
        if (m_gameManager == null)return;

        m_gameManager.StartGame();
        m_gameManager.AddScore(m_previewScore);
        m_gameManager.FinishGame();
    }
}
