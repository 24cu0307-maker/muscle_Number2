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
    private const int EFirstEffectIndex = 0; //先頭演出番号
    private const float EPanelWidth = 360.0f;
    private const float EPanelHeight = 520.0f;
    private const float EPanelMargin = 16.0f;

    [Header("Keyboard")]
    [SerializeField] private KeyCode m_previousEffectKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode m_nextEffectKey = KeyCode.RightArrow;
    [SerializeField] private KeyCode m_playEffectKey = KeyCode.Space;
    [SerializeField] private KeyCode m_stopEffectKey = KeyCode.Backspace;

    [SerializeField] private EffectSystem m_effectSystem; //確認対象

    private readonly List<string> m_effectNames = new List<string>(); //演出名一覧
    private int m_selectedIndex; //選択中演出番号
    private Vector2 m_effectListScrollPosition;
    private Rect m_panelRect;

    /// <summary>
    /// 同じObjectのEffectSystemと登録済み演出名を取得します。
    /// </summary>
    private void Awake()
    {
        LiveStagePostProcess.GetOrCreate(gameObject);
        EffectDebugKeySettings.GetOrCreate(gameObject);

        if (m_effectSystem == null)
        {
            m_effectSystem = GetComponent<EffectSystem>();
        }

        RefreshNames();
        LogSelection();
        m_panelRect = new Rect(
            EPanelMargin,
            EPanelMargin,
            EPanelWidth,
            Mathf.Min(EPanelHeight, Screen.height - EPanelMargin * 2.0f));
    }

    /// <summary>
    /// 左右キーでEffectを選び、Spaceで個別再生、Backspaceで停止します。
    /// 画面Panelと同じ操作をKeyboardからも行えます。
    /// </summary>
    private void Update()
    {
        if (m_effectNames.Count == 0)return;

        if (EffectDebugKeySettings.IsKeyDown(m_previousEffectKey))
        {
            SelectRelative(-1);
        }
        if (EffectDebugKeySettings.IsKeyDown(m_nextEffectKey))
        {
            SelectRelative(1);
        }
        if (EffectDebugKeySettings.IsKeyDown(m_playEffectKey))
        {
            PlaySelectedEffect();
        }
        if (EffectDebugKeySettings.IsKeyDown(m_stopEffectKey))
        {
            m_effectSystem?.StopAllEffects();
            Debug.Log("Effect確認: すべての演出を停止しました。", this);
        }
    }

    /// <summary>選択、再生、停止を行うスクロール対応の確認Panelを表示します。</summary>
    private void OnGUI()
    {
        float maximumHeight = Mathf.Max(240.0f, Screen.height - EPanelMargin * 2.0f);
        m_panelRect.width = Mathf.Min(EPanelWidth, Screen.width - EPanelMargin * 2.0f);
        m_panelRect.height = Mathf.Min(EPanelHeight, maximumHeight);
        m_panelRect.x = Mathf.Clamp(
            m_panelRect.x,
            0.0f,
            Mathf.Max(0.0f, Screen.width - m_panelRect.width));
        m_panelRect.y = Mathf.Clamp(
            m_panelRect.y,
            0.0f,
            Mathf.Max(0.0f, Screen.height - m_panelRect.height));
        m_panelRect = GUI.Window(
            GetInstanceID(),
            m_panelRect,
            DrawEffectPanel,
            "Effect Debug");
    }

    private void DrawEffectPanel(int _windowId)
    {
        GUILayout.Label(
            m_effectNames.Count == 0
                ? "Effectが登録されていません"
                : $"選択中: {m_effectNames[m_selectedIndex]}");

        GUILayout.BeginHorizontal();
        GUI.enabled = m_effectNames.Count > 0;
        if (GUILayout.Button("再生", GUILayout.Height(34.0f)))
        {
            PlaySelectedEffect();
        }
        GUI.enabled = m_effectSystem != null;
        if (GUILayout.Button("すべて停止", GUILayout.Height(34.0f)))
        {
            m_effectSystem.StopAllEffects();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        GUILayout.Space(4.0f);
        GUILayout.Label("Effect List");
        m_effectListScrollPosition = GUILayout.BeginScrollView(
            m_effectListScrollPosition,
            GUI.skin.box);
        for (int i = 0; i < m_effectNames.Count; ++i)
        {
            Color previousColor = GUI.backgroundColor;
            if (i == m_selectedIndex)
            {
                GUI.backgroundColor = new Color(0.25f, 0.75f, 1.0f);
            }
            if (GUILayout.Button(m_effectNames[i], GUILayout.Height(28.0f)))
            {
                m_selectedIndex = i;
                LogSelection();
            }
            GUI.backgroundColor = previousColor;
        }
        GUILayout.EndScrollView();

        GUILayout.Label("←/→ 選択  Space 再生  Backspace 停止");
        GUI.DragWindow(new Rect(0.0f, 0.0f, m_panelRect.width, 24.0f));
    }

    private void SelectRelative(int _offset)
    {
        m_selectedIndex = (m_selectedIndex + _offset + m_effectNames.Count)
            % m_effectNames.Count;
        LogSelection();
    }

    [ContextMenu("Play Selected Effect")]
    public void PlaySelectedEffect()
    {
        if (m_effectSystem == null || m_effectNames.Count == 0)return;

        string effectName = m_effectNames[m_selectedIndex];
        m_effectSystem.StopAllEffects();
        m_effectSystem.PlayEffect(effectName);
        Debug.Log($"Effect確認: {effectName} を再生しました。", this);
    }

    private void LogSelection()
    {
        if (m_effectNames.Count == 0)
        {
            Debug.LogWarning("Effect確認: 登録されたEffectがありません。", this);
            return;
        }

        Debug.Log(
            $"Effect確認 [{m_selectedIndex + 1}/{m_effectNames.Count}]: "
            + $"{m_effectNames[m_selectedIndex]}  "
            + "(←/→ 選択, Space 再生, Backspace 停止)",
            this);
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
