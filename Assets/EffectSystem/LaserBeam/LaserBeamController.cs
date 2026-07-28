/*━━━━━━━━━*
*@file LaserBeamController.cs*
*@brief レーザーライトの表示を制御する*
*@author 24CU0000 Name*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks TimelineのAnimation Trackから操作可能*
*━━━━━━━━━*/

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// レーザーの色、発光強度、透明度、脈動速度を制御します。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public sealed class LaserBeamController : MonoBehaviour
{
    private const float EMinimumValue = 0.0f;               //各表示値の最小値
    private const float EMaximumOpacity = 1.0f;             //透明度の最大値
    private const float EDefaultIntensity = 4.0f;           //標準の発光強度
    private const float EDefaultOpacity = 0.8f;             //標準の透明度
    private const float EDefaultPulseSpeed = 2.0f;          //標準の脈動速度

    private static readonly Color EDefaultLaserColor =
        new Color(1.0f, 0.05f, 0.02f, 1.0f);               //標準のレーザー色
    private static readonly int m_colorId = Shader.PropertyToID("_Color"); //色のShader ID
    private static readonly int m_intensityId =
        Shader.PropertyToID("_Intensity");                  //発光強度のShader ID
    private static readonly int m_opacityId =
        Shader.PropertyToID("_Opacity");                    //透明度のShader ID
    private static readonly int m_pulseSpeedId =
        Shader.PropertyToID("_PulseSpeed");                 //脈動速度のShader ID

    [ColorUsage(true, true)]
    [FormerlySerializedAs("m_color")]
    [SerializeField] private Color m_laserColor = EDefaultLaserColor; //レーザー色
    [Min(EMinimumValue)]
    [FormerlySerializedAs("m_intensity")]
    [SerializeField] private float m_emissionIntensity = EDefaultIntensity; //発光強度
    [Range(EMinimumValue, EMaximumOpacity)]
    [FormerlySerializedAs("m_opacity")]
    [SerializeField] private float m_opacity = EDefaultOpacity; //透明度
    [Min(EMinimumValue)]
    [FormerlySerializedAs("m_pulseSpeed")]
    [SerializeField] private float m_pulseSpeed = EDefaultPulseSpeed; //脈動速度

    private Renderer m_targetRenderer;                      //表示対象Renderer
    private MaterialPropertyBlock m_propertyBlock;          //個別マテリアル設定

    /// <summary>
    /// 有効化時に表示設定を反映します。
    /// </summary>
    private void OnEnable()
    {
        Apply();
    }

    /// <summary>
    /// Inspectorの変更を表示へ反映します。
    /// </summary>
    private void OnValidate()
    {
        Apply();
    }

    /// <summary>
    /// Animation Trackが変更した値を毎フレーム反映します。
    /// </summary>
    private void LateUpdate()
    {
        Apply();
    }

    /// <summary>
    /// レーザーを表示します。
    /// </summary>
    public void Show()
    {
        if (!TryGetRenderer())return;

        m_targetRenderer.enabled = true;
    }

    /// <summary>
    /// レーザーを非表示にします。
    /// </summary>
    public void Hide()
    {
        if (!TryGetRenderer())return;

        m_targetRenderer.enabled = false;
    }

    /// <summary>
    /// レーザーの透明度を設定します。
    /// </summary>
    public void SetOpacity(float _opacity)
    {
        m_opacity = Mathf.Clamp(_opacity, EMinimumValue, EMaximumOpacity);
        Apply();
    }

    /// <summary>
    /// レーザーの発光強度を設定します。
    /// </summary>
    public void SetIntensity(float _intensity)
    {
        m_emissionIntensity = Mathf.Max(EMinimumValue, _intensity);
        Apply();
    }

    /// <summary>
    /// MaterialPropertyBlockへ現在の表示設定を反映します。
    /// </summary>
    private void Apply()
    {
        if (!TryGetRenderer())return;

        if (m_propertyBlock == null)
        {
            m_propertyBlock = new MaterialPropertyBlock();
        }

        m_targetRenderer.GetPropertyBlock(m_propertyBlock);
        m_propertyBlock.SetColor(m_colorId, m_laserColor);
        m_propertyBlock.SetFloat(
            m_intensityId,
            Mathf.Max(EMinimumValue, m_emissionIntensity));
        m_propertyBlock.SetFloat(
            m_opacityId,
            Mathf.Clamp(m_opacity, EMinimumValue, EMaximumOpacity));
        m_propertyBlock.SetFloat(
            m_pulseSpeedId,
            Mathf.Max(EMinimumValue, m_pulseSpeed));
        m_targetRenderer.SetPropertyBlock(m_propertyBlock);
    }

    /// <summary>
    /// 表示対象Rendererを取得します。
    /// </summary>
    private bool TryGetRenderer()
    {
        if (m_targetRenderer == null)
        {
            m_targetRenderer = GetComponent<Renderer>();
        }

        return m_targetRenderer != null;
    }
}
