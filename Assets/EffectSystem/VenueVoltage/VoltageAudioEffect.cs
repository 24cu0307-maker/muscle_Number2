/*━━━━━━━━━*
*@file VoltageAudioEffect.cs*
*@brief EffectSystem再生音へボルテージ連動の音量と音程を適用する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks AudioSourceと同じObjectへ追加して使用*
*━━━━━━━━━*/

using UnityEngine;

/// <summary>
/// AudioSourceの再生開始時に会場ボルテージから音量と音程を設定します。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class VoltageAudioEffect : MonoBehaviour
{
    private const float EMinimumVolumeMultiplier = 0.65f; //最低音量倍率
    private const float EMaximumVolumeMultiplier = 1.25f; //最高音量倍率
    private const float EMinimumPitchMultiplier = 0.85f; //最低音程倍率
    private const float EMaximumPitchMultiplier = 1.55f; //最高音程倍率

    [SerializeField] private AudioSource m_audioSource; //制御対象音源
    [SerializeField] private VenueVoltageSystem m_voltageSystem; //ボルテージ参照元
    [SerializeField] private float m_minimumVolumeMultiplier =
        EMinimumVolumeMultiplier; //ボルテージ最低時の音量倍率
    [SerializeField] private float m_maximumVolumeMultiplier =
        EMaximumVolumeMultiplier; //ボルテージ最高時の音量倍率
    [SerializeField] private float m_minimumPitchMultiplier =
        EMinimumPitchMultiplier; //ボルテージ最低時の音程倍率
    [SerializeField] private float m_maximumPitchMultiplier =
        EMaximumPitchMultiplier; //ボルテージ最高時の音程倍率

    private float m_baseVolume; //元の音量
    private float m_basePitch; //元の音程
    private bool b_m_wasPlaying; //直前の再生状態

    /// <summary>
    /// 元のAudioSource設定を保存します。
    /// </summary>
    private void Awake()
    {
        if (m_audioSource == null)
        {
            m_audioSource = GetComponent<AudioSource>();
        }

        m_voltageSystem = FindFirstObjectByType<VenueVoltageSystem>();
        m_baseVolume = m_audioSource.volume;
        m_basePitch = m_audioSource.pitch;
    }

    /// <summary>
    /// 再生開始を検出して現在のボルテージを反映します。
    /// </summary>
    private void Update()
    {
        bool b_isPlaying = m_audioSource.isPlaying; //現在の再生状態
        if (b_isPlaying && !b_m_wasPlaying)
        {
            ApplyVoltage();
        }
        else if (!b_isPlaying && b_m_wasPlaying)
        {
            RestoreBaseSettings();
        }

        b_m_wasPlaying = b_isPlaying;
    }

    /// <summary>
    /// 現在のボルテージから音量と音程を計算します。
    /// </summary>
    private void ApplyVoltage()
    {
        if (m_voltageSystem == null)
        {
            m_voltageSystem = FindFirstObjectByType<VenueVoltageSystem>();
        }

        float voltage = m_voltageSystem == null
            ? 0.0f
            : m_voltageSystem.NormalizedVoltage; //0から1のボルテージ
        m_audioSource.volume =
            m_baseVolume
            * Mathf.Lerp(
                m_minimumVolumeMultiplier,
                m_maximumVolumeMultiplier,
                voltage);
        m_audioSource.pitch =
            m_basePitch
            * Mathf.Lerp(
                m_minimumPitchMultiplier,
                m_maximumPitchMultiplier,
                voltage);
    }

    /// <summary>
    /// 再生終了後に元のAudioSource設定へ戻します。
    /// </summary>
    private void RestoreBaseSettings()
    {
        m_audioSource.volume = m_baseVolume;
        m_audioSource.pitch = m_basePitch;
    }
}
