/*━━━━━━━━━*
*@file VoltageSpotlightConeEffect.cs*
*@brief SpotlightConeの発光強度を会場ボルテージへ連動する
*@author 24cu0312 久場洸太
*@date 2026/08/06
*最終更新日 2026/08/06
*@remarks Timelineが設定した強度を基準に倍率を適用する
*━━━━━━━━━*/

using UnityEngine;

[RequireComponent(typeof(SpotlightConeController))]
/// <summary>
/// SpotlightConeが本来持つ発光強度へ、会場ボルテージに応じた倍率を重ねます。
/// TimelineやAnimatorが再生中に強度を書き換えた場合も、その値を新しい基準値として追従します。
/// </summary>
public sealed class VoltageSpotlightConeEffect : MonoBehaviour
{
    [SerializeField] private SpotlightConeController m_spotlightCone; //発光強度を実際に変更するSpotlightCone本体
    [SerializeField] private VenueVoltageSystem m_voltageSystem; //現在のボルテージとライト倍率を提供する管理クラス

    private float m_baseIntensity; //ボルテージ倍率を掛ける前の、Timeline等が指定した基準強度
    private float m_appliedIntensity; //直前のLateUpdateで本クラスが書き込んだ最終強度
    private bool b_m_hasAppliedIntensity; //現在の値が本クラスによる補正済みかを判別するフラグ

    /// <summary>
    /// 初期化時に参照を補完し、InspectorまたはTimelineが設定した初期強度を保存します。
    /// </summary>
    private void Awake()
    {
        FindReferences();
        CaptureBaseIntensity();
    }

    /// <summary>
    /// 非アクティブ状態から再利用された場合に、古い補正値を持ち越さないよう基準値を取り直します。
    /// </summary>
    private void OnEnable()
    {
        FindReferences();
        CaptureBaseIntensity();
    }

    /// <summary>
    /// Timeline・Animatorの更新後に実行し、最新の基準強度へボルテージ倍率を適用します。
    /// 前回の適用値と異なる値が入っていれば、外部からの変更と判断して基準値を更新します。
    /// </summary>
    private void LateUpdate()
    {
        if (m_spotlightCone == null)return;

        float currentIntensity = m_spotlightCone.EmissionIntensity;
        if (!b_m_hasAppliedIntensity
            || !Mathf.Approximately(currentIntensity, m_appliedIntensity))
        {
            m_baseIntensity = currentIntensity;
        }

        float multiplier = 1.0f;
        if (m_voltageSystem != null)
        {
            multiplier = m_voltageSystem.GetLightIntensityMultiplier();
        }
        m_appliedIntensity = Mathf.Max(0.0f, m_baseIntensity * multiplier);
        b_m_hasAppliedIntensity = true;
        m_spotlightCone.EmissionIntensity = m_appliedIntensity;
    }

    /// <summary>
    /// コンポーネント停止時にボルテージ補正前の強度へ戻し、次回有効化時の二重乗算を防ぎます。
    /// </summary>
    private void OnDisable()
    {
        if (m_spotlightCone != null && b_m_hasAppliedIntensity)
        {
            m_spotlightCone.EmissionIntensity = m_baseIntensity;
        }
        b_m_hasAppliedIntensity = false;
    }

    /// <summary>
    /// Inspectorで未設定の参照だけをシーンから補完します。
    /// SpotlightConeは同一Object、ボルテージ管理はシーン内の一意なSystemを対象とします。
    /// </summary>
    private void FindReferences()
    {
        if (m_spotlightCone == null)
        {
            m_spotlightCone = GetComponent<SpotlightConeController>();
        }
        if (m_voltageSystem == null)
        {
            m_voltageSystem = FindFirstObjectByType<VenueVoltageSystem>();
        }
    }

    /// <summary>
    /// 現在のSpotlightCone強度を、倍率計算に使用する基準値として記録します。
    /// </summary>
    private void CaptureBaseIntensity()
    {
        if (m_spotlightCone == null)return;

        m_baseIntensity = m_spotlightCone.EmissionIntensity;
        b_m_hasAppliedIntensity = false;
    }
}
