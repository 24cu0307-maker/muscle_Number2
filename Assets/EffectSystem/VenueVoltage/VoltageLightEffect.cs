/*━━━━━━━━━*
*@file VoltageLightEffect.cs*
*@brief EffectSystem再生Lightへボルテージ連動の色と光量を適用する*
*@author 24CU0000 Name*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Lightと同じObjectへ追加して使用*
*━━━━━━━━━*/

using UnityEngine;

/// <summary>
/// Lightの点灯開始時に会場ボルテージから色と光量を設定します。
/// </summary>
[RequireComponent(typeof(Light))]
public sealed class VoltageLightEffect : MonoBehaviour
{
    private const float EMinimumIntensityMultiplier = 0.7f; //最低光量倍率
    private const float EMaximumIntensityMultiplier = 2.0f; //最高光量倍率
    private const float EMinimumColorBlend = 0.2f; //最低色反映率
    private const float EMaximumColorBlend = 0.8f; //最高色反映率

    [SerializeField] private Light m_light; //制御対象Light
    [SerializeField] private VenueVoltageSystem m_voltageSystem; //ボルテージ参照元
    [SerializeField] private float m_minimumIntensityMultiplier =
        EMinimumIntensityMultiplier; //最低時の光量倍率
    [SerializeField] private float m_maximumIntensityMultiplier =
        EMaximumIntensityMultiplier; //最高時の光量倍率
    [SerializeField] private float m_minimumColorBlend =
        EMinimumColorBlend; //最低時の色反映率
    [SerializeField] private float m_maximumColorBlend =
        EMaximumColorBlend; //最高時の色反映率

    private Color m_baseColor; //点灯前の元色
    private Color m_appliedColor; //点灯中に維持する色
    private float m_baseIntensity; //点灯前の元光量
    private float m_appliedIntensity; //点灯中に維持する光量
    private bool b_m_wasEnabled; //直前の点灯状態

    /// <summary>
    /// Lightとボルテージ参照を取得します。
    /// </summary>
    private void Awake()
    {
        if (m_light == null)
        {
            m_light = GetComponent<Light>();
        }

        m_voltageSystem = FindFirstObjectByType<VenueVoltageSystem>();
        b_m_wasEnabled = m_light.enabled;
        if (b_m_wasEnabled)
        {
            CaptureAndApplyVoltage();
        }
    }

    /// <summary>
    /// 点灯開始を検出し、LightController更新後にVoltage設定を維持します。
    /// </summary>
    private void LateUpdate()
    {
        bool b_isEnabled = m_light.enabled; //現在の点灯状態
        if (b_isEnabled && !b_m_wasEnabled)
        {
            CaptureAndApplyVoltage();
        }
        else if (!b_isEnabled && b_m_wasEnabled)
        {
            RestoreBaseSettings();
        }

        if (b_isEnabled)
        {
            m_light.color = m_appliedColor;
            m_light.intensity = m_appliedIntensity;
        }

        b_m_wasEnabled = b_isEnabled;
    }

    /// <summary>
    /// 点灯時の元設定とボルテージから適用値を作成します。
    /// </summary>
    private void CaptureAndApplyVoltage()
    {
        if (m_voltageSystem == null)
        {
            m_voltageSystem = FindFirstObjectByType<VenueVoltageSystem>();
        }

        m_baseColor = m_light.color;
        m_baseIntensity = m_light.intensity;
        float voltage = m_voltageSystem == null
            ? 0.0f
            : m_voltageSystem.NormalizedVoltage; //0から1のボルテージ
        Color voltageColor = m_voltageSystem == null
            ? Color.blue
            : m_voltageSystem.CurrentVoltageColor; //現在の段階色
        float colorBlend = Mathf.Lerp(
            m_minimumColorBlend,
            m_maximumColorBlend,
            voltage); //色の反映率

        m_appliedColor = Color.Lerp(
            m_baseColor,
            voltageColor,
            colorBlend);
        m_appliedIntensity =
            m_baseIntensity
            * Mathf.Lerp(
                m_minimumIntensityMultiplier,
                m_maximumIntensityMultiplier,
                voltage);
    }

    /// <summary>
    /// 消灯時に元のLight設定へ戻します。
    /// </summary>
    private void RestoreBaseSettings()
    {
        m_light.color = m_baseColor;
        m_light.intensity = m_baseIntensity;
    }
}
