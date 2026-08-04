/*━━━━━━━━━*
*@file VoltageDebugPanel.cs*
*@brief ボルテージ値の表示と手動操作を行うDebug UI*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks F8キーで表示を切り替える*
*━━━━━━━━━*/

using UnityEngine;

/// <summary>
/// VenueVoltageSystemを数値、Slider、成功・失敗Buttonで操作します。
/// </summary>
[RequireComponent(typeof(VenueVoltageSystem))]
public sealed class VoltageDebugPanel : MonoBehaviour
{
    private const float EPanelWidth = 360.0f; //Debug Panel横幅
    private const float EPanelHeight = 190.0f; //Debug Panel縦幅
    private const float EPanelMargin = 20.0f; //画面端の余白
    private const float EVoltageMinimum = 0.0f; //Slider最小値
    private const float EVoltageMaximum = 100.0f; //Slider最大値
    private const float EDebugStep = 10.0f; //増減Button一回の変化量
    private const int EDebugSuccessScore = 5000; //成功Buttonの仮スコア

    [SerializeField] private bool b_m_enableDebugPanel; //Debug Panel表示状態
    [SerializeField] private VenueVoltageSystem m_voltageSystem; //操作対象

    private EffectDebugKeySettings m_debugKeySettings; //共通Debug Key設定

    /// <summary>
    /// 同じObjectのVenueVoltageSystemを取得します。
    /// </summary>
    private void Awake()
    {
        if (m_voltageSystem == null)
        {
            m_voltageSystem = GetComponent<VenueVoltageSystem>();
        }

        m_debugKeySettings =
            EffectDebugKeySettings.GetOrCreate(gameObject);
    }

    /// <summary>
    /// F8キーでDebug表示を切り替えます。
    /// </summary>
    private void Update()
    {
        if (m_debugKeySettings == null)
        {
            m_debugKeySettings =
                EffectDebugKeySettings.GetOrCreate(gameObject);
        }

        if (m_debugKeySettings == null)return;
        if (EffectDebugKeySettings.IsKeyDown(
            m_debugKeySettings.VoltageToggleKey))
        {
            b_m_enableDebugPanel = !b_m_enableDebugPanel;
        }
    }

    /// <summary>
    /// 現在値、段階、コンボ、操作Buttonを表示します。
    /// </summary>
    private void OnGUI()
    {
        if (!b_m_enableDebugPanel || m_voltageSystem == null)return;

        Rect panelRect = new Rect(
            Screen.width - EPanelWidth - EPanelMargin,
            EPanelMargin,
            EPanelWidth,
            EPanelHeight); //Debug Panel表示範囲
        GUILayout.BeginArea(
            panelRect,
            "VOLTAGE DEBUG",
            GUI.skin.window);
        GUILayout.Label(
            $"Voltage: {m_voltageSystem.Voltage:F1} / {EVoltageMaximum:F0}");
        GUILayout.Label(
            $"Color: {m_voltageSystem.VoltageLevel}   "
            + $"Combo: {m_voltageSystem.ComboCount}");

        float voltage = GUILayout.HorizontalSlider(
            m_voltageSystem.Voltage,
            EVoltageMinimum,
            EVoltageMaximum); //Slider操作後の値
        if (!Mathf.Approximately(voltage, m_voltageSystem.Voltage))
        {
            m_voltageSystem.SetVoltageForDebug(voltage);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("-10"))
        {
            m_voltageSystem.AddVoltageForDebug(-EDebugStep);
        }

        if (GUILayout.Button("+10"))
        {
            m_voltageSystem.AddVoltageForDebug(EDebugStep);
        }

        if (GUILayout.Button("SUCCESS"))
        {
            m_voltageSystem.RegisterSuccess(EDebugSuccessScore);
        }

        if (GUILayout.Button("FAILURE"))
        {
            m_voltageSystem.RegisterFailure();
        }

        GUILayout.EndHorizontal();
        if (GUILayout.Button("RESET"))
        {
            m_voltageSystem.ResetVoltageForDebug();
        }

        GUILayout.Label(
            $"Toggle: {m_debugKeySettings.VoltageToggleKey}");
        GUILayout.EndArea();
    }

    /// <summary>
    /// 外部からDebug Panelの表示状態を設定します。
    /// </summary>
    public void SetDebugEnabled(bool _benabled)
    {
        b_m_enableDebugPanel = _benabled;
    }
}
