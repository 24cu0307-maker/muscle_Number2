/*━━━━━━━━━*
*@file LiveEffectQuickTester.cs*
*@brief 展開用ライブエフェクトをすぐ再生確認する簡易UI*
*@author 24cu0312 久場洸太*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks 開発確認用*
*━━━━━━━━━*/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 追加CanvasなしでEffectSystemの登録演出を切り替えて確認します。
/// </summary>
public sealed class LiveEffectQuickTester : MonoBehaviour
{
    private const float EPanelWidth = 420.0f; //パネル横幅
    private const float EPanelHeight = 125.0f; //パネル縦幅
    private const int EFirstEffectIndex = 0; //先頭演出番号

    [SerializeField] private EffectSystem m_effectSystem; //確認対象
    [SerializeField] private bool b_m_showPanel = true; //パネル表示状態

    private readonly List<string> m_effectNames = new List<string>(); //演出名一覧
    private int m_selectedIndex; //選択中演出番号

    /// <summary>
    /// 同じObjectのEffectSystemと登録済み演出名を取得します。
    /// </summary>
    private void Awake()
    {
        if (m_effectSystem == null)
        {
            m_effectSystem = GetComponent<EffectSystem>();
        }

        RefreshNames();
    }

    /// <summary>
    /// 開発確認用操作パネルをゲーム画面へ表示します。
    /// </summary>
    private void OnGUI()
    {
        if (!b_m_showPanel || m_effectNames.Count == 0)return;

        GUILayout.BeginArea(
            new Rect(20.0f, 20.0f, EPanelWidth, EPanelHeight),
            "LIVE EFFECT QUICK TEST",
            GUI.skin.window);
        GUILayout.Label(
            $"{m_selectedIndex + 1}/{m_effectNames.Count}  "
            + m_effectNames[m_selectedIndex]);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("<", GUILayout.Width(55.0f)))
        {
            SelectPrevious();
        }

        if (GUILayout.Button("PLAY"))
        {
            m_effectSystem.PlayEffect(m_effectNames[m_selectedIndex]);
        }

        if (GUILayout.Button("STOP"))
        {
            m_effectSystem.StopEffectTimeline();
        }

        if (GUILayout.Button(">", GUILayout.Width(55.0f)))
        {
            SelectNext();
        }

        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    /// <summary>
    /// 一つ前の演出を選択します。
    /// </summary>
    private void SelectPrevious()
    {
        m_selectedIndex =
            (m_selectedIndex - 1 + m_effectNames.Count)
            % m_effectNames.Count;
    }

    /// <summary>
    /// 一つ次の演出を選択します。
    /// </summary>
    private void SelectNext()
    {
        m_selectedIndex = (m_selectedIndex + 1) % m_effectNames.Count;
    }

    /// <summary>
    /// EffectSystemから名前付きEffectDataを収集します。
    /// </summary>
    private void RefreshNames()
    {
        m_effectNames.Clear();
        m_selectedIndex = EFirstEffectIndex;
        if (m_effectSystem == null)return;

        SEffectData[] effectDatas = m_effectSystem.GetEffectDatas(); //登録済み演出
        if (effectDatas == null)return;

        for (int i = 0; i < effectDatas.Length; ++i)
        {
            if (string.IsNullOrWhiteSpace(effectDatas[i].EffectName))continue;

            m_effectNames.Add(effectDatas[i].EffectName);
        }
    }
}
