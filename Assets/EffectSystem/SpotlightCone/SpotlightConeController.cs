/*━━━━━━━━━*
*@file SpotlightConeController.cs*
*@brief スポットライトコーンの表示を制御する*
*@author 24cu0312 久場洸太*
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
    private const float EMinimumLightRange = 0.01f;        //実ライトへ設定できる最短距離
    private const float ERadiansToDegrees = 2.0f * Mathf.Rad2Deg; //コーン半角からSpot Light全角への変換係数
    private const float ESurfaceLightRangeExtension = 1.25f; //終端面でも光量を残すためコーンより先まで届かせる倍率
    private const float ESurfaceLightAngleExtension = 1.15f; //コーン外側と照射面まで包む実ライト角度倍率
    private const float ESurfaceLightIntensityScale = 0.5f; //距離二乗とメッシュ強度からCandelaへ変換する係数
    private const float EMaximumSurfaceLightIntensity = 12000.0f; //長距離ライト用のCandela安全上限
    private const int EMaximumShadowLightsPerFrame = 2;    //同時にShadow Mapを作成するSpot Light数
    private const float ESurfaceLightShadowStrength = 0.65f; //真っ黒になり過ぎない影の濃さ

    private static readonly Color EDefaultLightColor =
        new Color(1.0f, 0.72f, 0.3f, 1.0f);               //標準の光色

    private static readonly int m_colorId = Shader.PropertyToID("_Color");         //色のShader ID
    private static readonly int m_intensityId = Shader.PropertyToID("_Intensity"); //発光強度のShader ID
    private static readonly int m_opacityId = Shader.PropertyToID("_Opacity");     //透明度のShader ID
    private static int m_shadowBudgetFrame = -1;              //Shadow枠を数えているFrame番号
    private static int m_usedShadowLightCount;                //現在FrameでShadowを許可したLight数

    [ColorUsage(true, true)]
    [FormerlySerializedAs("m_color")]
    [SerializeField] private Color m_lightColor = EDefaultLightColor; //光の色
    [Min(EMinimumIntensity)]
    [FormerlySerializedAs("m_intensity")]
    [SerializeField] private float m_emissionIntensity = EDefaultIntensity; //発光強度
    [Range(EMinimumOpacity, EMaximumOpacity)]
    [FormerlySerializedAs("m_opacity")]
    [SerializeField] private float m_opacity = EDefaultOpacity;         //透明度

    [Header("Surface Illumination")]
    [SerializeField] private bool b_m_illuminateSurfaces = true; //床・人物など照射先を実際に明るくするか
    [SerializeField] private Light m_surfaceLight; //コーンの色・距離・角度へ同期する実Spot Light

    private Renderer m_targetRenderer;                                 //表示対象Renderer
    private MaterialPropertyBlock m_propertyBlock;                     //個別マテリアル設定
    private SpotlightConeMesh m_coneMesh;                              //実ライトの形状を取得するコーンメッシュ
    private int m_lastShadowRequestFrame = -1;                         //同一Frameの重複Shadow要求を防ぐ番号
    private bool b_m_castsShadowThisFrame;                             //現在FrameにこのLightが影を担当するか

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
    /// ゲーム再生開始後の安全な時点で実Spot Lightを一度だけ用意します。
    /// OnValidateや毎Frame処理ではComponentを生成せず、Unityの検証中警告と余分な探索を防ぎます。
    /// </summary>
    private void Start()
    {
        if (!b_m_illuminateSurfaces)return;

        if (m_surfaceLight == null)
        {
            m_surfaceLight = GetComponent<Light>();
        }
        if (m_surfaceLight == null)
        {
            m_surfaceLight = gameObject.AddComponent<Light>();
        }
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
    /// 無効化時に実ライトも停止し、非表示のコーンから照明だけが残ることを防ぎます。
    /// </summary>
    private void OnDisable()
    {
        if (m_surfaceLight != null)
        {
            m_surfaceLight.enabled = false;
        }
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
        UpdateSurfaceLight();
    }

    /// <summary>
    /// コーンの色・距離・照射角へ実Spot Lightを同期し、床や人物へ光が当たる表現を作ります。
    /// 編集中にSceneを汚さないよう、Lightが未登録の場合の自動生成はゲーム再生中だけ行います。
    /// </summary>
    private void UpdateSurfaceLight()
    {
        if (!b_m_illuminateSurfaces)
        {
            if (m_surfaceLight != null)
            {
                m_surfaceLight.enabled = false;
            }
            return;
        }

        if (m_coneMesh == null)
        {
            m_coneMesh = GetComponent<SpotlightConeMesh>();
        }
        if (m_coneMesh == null)return;

        if (m_surfaceLight == null)
        {
            m_surfaceLight = GetComponent<Light>();
        }
        if (m_surfaceLight == null)return;

        float coneLength = Mathf.Max(
            EMinimumLightRange,
            m_coneMesh.ConfiguredLength);
        float halfAngleRadians = Mathf.Atan(
            m_coneMesh.ConfiguredEndRadius / coneLength);
        m_surfaceLight.type = LightType.Spot;
        m_surfaceLight.color = m_lightColor;
        //UnityのSpot Lightはrange終端で光量が0になるため、表示コーンより少し先まで延長します。
        //これによりコーン終端が地面に一致していても、照射面へ明るさと影が残ります。
        m_surfaceLight.range = coneLength * ESurfaceLightRangeExtension;
        float coneAngle = halfAngleRadians * ERadiansToDegrees;
        m_surfaceLight.spotAngle = Mathf.Min(
            179.0f,
            coneAngle * ESurfaceLightAngleExtension);
        m_surfaceLight.innerSpotAngle = m_surfaceLight.spotAngle * 0.55f;
        //Spot LightのCandelaは距離の二乗に比例して増やさないと、長距離の照射面で見えなくなります。
        //コーン長を使って終端付近の照度を揃え、メッシュ自身の白飛び制御とは独立させます。
        float distanceCompensatedIntensity = m_emissionIntensity
            * coneLength
            * coneLength
            * ESurfaceLightIntensityScale;
        m_surfaceLight.intensity = Mathf.Clamp(
            distanceCompensatedIntensity,
            0.0f,
            EMaximumSurfaceLightIntensity);
        if (TryAcquireShadowSlot())
        {
            m_surfaceLight.shadows = LightShadows.Hard;
            m_surfaceLight.shadowStrength = ESurfaceLightShadowStrength;
            m_surfaceLight.shadowCustomResolution = 256;
        }
        else
        {
            m_surfaceLight.shadows = LightShadows.None;
        }
        m_surfaceLight.enabled = true;
    }

    /// <summary>
    /// 一Frame内で先着二灯だけへHard Shadowを許可します。
    /// 全SpotlightがShadow Mapを作る負荷を避けつつ、ライト角度に沿った影を表示します。
    /// </summary>
    private bool TryAcquireShadowSlot()
    {
        if (m_lastShadowRequestFrame == Time.frameCount)
        {
            return b_m_castsShadowThisFrame;
        }

        if (m_shadowBudgetFrame != Time.frameCount)
        {
            m_shadowBudgetFrame = Time.frameCount;
            m_usedShadowLightCount = 0;
        }

        m_lastShadowRequestFrame = Time.frameCount;
        b_m_castsShadowThisFrame = false;
        if (m_targetRenderer == null || !m_targetRenderer.enabled)
        {
            return false;
        }
        if (m_usedShadowLightCount >= EMaximumShadowLightsPerFrame)
        {
            return false;
        }

        ++m_usedShadowLightCount;
        b_m_castsShadowThisFrame = true;
        return true;
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
