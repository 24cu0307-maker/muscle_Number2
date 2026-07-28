/*━━━━━━━━━*
*@file SpotlightConeController.cs*
*@brief スポットライトコーンの表示を制御する*
*@author 24CU0000 Name*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks TimelineのAnimation Trackから操作可能*
*━━━━━━━━━*/

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// スポットライトコーンの色、発光強度、透明度を制御します。
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public sealed class SpotlightConeController : MonoBehaviour
{
    private const float EMinimumIntensity = 0.0f;          //発光強度の最小値
    private const float EMinimumOpacity = 0.0f;            //透明度の最小値
    private const float EMaximumOpacity = 1.0f;            //透明度の最大値
    private const float EDefaultIntensity = 2.0f;          //標準の発光強度
    private const float EDefaultOpacity = 0.18f;           //標準の透明度

    private static readonly Color EDefaultLightColor =
        new Color(1.0f, 0.72f, 0.3f, 1.0f);               //標準の光色

    private static readonly int m_colorId = Shader.PropertyToID("_Color");         //色のShader ID
    private static readonly int m_intensityId = Shader.PropertyToID("_Intensity"); //発光強度のShader ID
    private static readonly int m_opacityId = Shader.PropertyToID("_Opacity");     //透明度のShader ID

    [ColorUsage(true, true)]
    [FormerlySerializedAs("m_color")]
    [SerializeField] private Color m_lightColor = EDefaultLightColor; //光の色
    [Min(EMinimumIntensity)]
    [FormerlySerializedAs("m_intensity")]
    [SerializeField] private float m_emissionIntensity = EDefaultIntensity; //発光強度
    [Range(EMinimumOpacity, EMaximumOpacity)]
    [FormerlySerializedAs("m_opacity")]
    [SerializeField] private float m_opacity = EDefaultOpacity;         //透明度

    private Renderer m_targetRenderer;                                 //表示対象Renderer
    private MaterialPropertyBlock m_propertyBlock;                     //個別マテリアル設定

    public Color LightColor
    {
        get
        {
            return m_lightColor;
        }
        set
        {
            m_lightColor = value;
            Apply();
        }
    }

    public float EmissionIntensity
    {
        get
        {
            return m_emissionIntensity;
        }
        set
        {
            m_emissionIntensity = Mathf.Max(EMinimumIntensity, value);
            Apply();
        }
    }

    public float Opacity
    {
        get
        {
            return m_opacity;
        }
        set
        {
            m_opacity = Mathf.Clamp(value, EMinimumOpacity, EMaximumOpacity);
            Apply();
        }
    }

    /// <summary>
    /// 有効化時にマテリアル設定を反映します。
    /// </summary>
    private void OnEnable()
    {
        Apply();
    }

    /// <summary>
    /// Inspectorの変更をマテリアルへ反映します。
    /// </summary>
    private void OnValidate()
    {
        Apply();
    }

    /// <summary>
    /// Animation Trackが直接変更した値を毎フレーム反映します。
    /// </summary>
    private void LateUpdate()
    {
        Apply();
    }

    /// <summary>
    /// コーンを表示します。
    /// </summary>
    public void Show()
    {
        if (!TryGetRenderer())return;

        m_targetRenderer.enabled = true;
    }

    /// <summary>
    /// コーンを非表示にします。
    /// </summary>
    public void Hide()
    {
        if (!TryGetRenderer())return;

        m_targetRenderer.enabled = false;
    }

    /// <summary>
    /// コーンの透明度を設定します。
    /// </summary>
    public void SetOpacity(float _opacity)
    {
        Opacity = _opacity;
    }

    /// <summary>
    /// コーンの発光強度を設定します。
    /// </summary>
    public void SetIntensity(float _intensity)
    {
        EmissionIntensity = _intensity;
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
        m_propertyBlock.SetColor(m_colorId, m_lightColor);
        m_propertyBlock.SetFloat(
            m_intensityId,
            Mathf.Max(EMinimumIntensity, m_emissionIntensity));
        m_propertyBlock.SetFloat(
            m_opacityId,
            Mathf.Clamp(m_opacity, EMinimumOpacity, EMaximumOpacity));
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
