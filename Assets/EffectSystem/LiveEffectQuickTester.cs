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

    [SerializeField] private EffectSystem m_effectSystem; //確認対象

    private readonly List<string> m_effectNames = new List<string>(); //演出名一覧
    private int m_selectedIndex; //選択中演出番号
    private EffectDebugKeySettings m_debugKeySettings; //共通Debug Key設定

    /// <summary>
    /// 同じObjectのEffectSystemと登録済み演出名を取得します。
    /// </summary>
    private void Awake()
    {
        LiveStagePostProcess.GetOrCreate(gameObject);
        m_debugKeySettings = EffectDebugKeySettings.GetOrCreate(gameObject);

        if (m_effectSystem == null)
        {
            m_effectSystem = GetComponent<EffectSystem>();
        }

        RefreshNames();
    }

    private void Update()
    {
        if (m_debugKeySettings == null)
        {
            m_debugKeySettings = EffectDebugKeySettings.GetOrCreate(gameObject);
        }

        if (m_debugKeySettings != null
            && EffectDebugKeySettings.IsKeyDown(
                m_debugKeySettings.ExitDebugKey))
        {
            ExitDebug();
        }
    }

    /// <summary>EditorではPlay Modeを停止し、BuildではApplicationを終了します。</summary>
    private static void ExitDebug()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
