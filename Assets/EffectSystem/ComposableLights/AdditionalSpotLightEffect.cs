/*============================================================
*@file AdditionalSpotLightEffect.cs*
*@brief Composerへ追加できる補助Spot Light演出を生成する*
*@author 24CU0312 久場洸太*
*@date 2026/08/07*
*============================================================*/

using UnityEngine;

/// <summary>
/// フロント、45度キー、エッジ、アンダーなど、土台Light以外の補助照明を追加します。
/// LightEffectBaseを継承しているため、Composerへ任意の個数を登録できます。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class AdditionalSpotLightEffect : LightEffectBase
{
    [SerializeField] private Color m_color = Color.white; //補助Lightの色
    [SerializeField, Min(0.0f)] private float m_intensity = 500.0f; //補助Lightの光量
    [SerializeField, Min(0.1f)] private float m_range = 12.0f; //照射距離
    [SerializeField, Range(1.0f, 179.0f)] private float m_spotAngle = 55.0f; //外側照射角
    [SerializeField, Range(0.0f, 1.0f)] private float m_innerAngleRatio = 0.58f; //内側照射角比率
    [SerializeField] private bool b_m_useShadows; //この補助Lightで影を描画するか
    [SerializeField, Range(0.0f, 1.0f)] private float m_shadowStrength = 0.5f; //影の濃さ

    private Light m_effectLight; //このAttachmentが所有する実Spot Light

    /// <summary>Sceneへ生成された時点で補助Spot Lightを用意します。</summary>
    private void OnEnable()
    {
        if (!CanBuildInCurrentContext())return;
        EnsureLight();
        ApplySettings();
    }

    /// <summary>Inspector変更をリアルタイムで実Lightへ反映します。</summary>
    private void OnValidate()
    {
        ApplySettings();
    }

    /// <summary>親Lightの受け取り後も、このEffect固有の照明設定を維持します。</summary>
    public override void AttachToLight(Light _light)
    {
        base.AttachToLight(_light);
        EnsureLight();
        ApplySettings();
    }

    /// <summary>同じObjectへ補助用Light Componentを一度だけ追加します。</summary>
    private void EnsureLight()
    {
        if (m_effectLight == null)
        {
            m_effectLight = GetComponent<Light>();
        }
        if (m_effectLight == null)
        {
            m_effectLight = gameObject.AddComponent<Light>();
        }
    }

    /// <summary>Serializeされた色・光量・角度・影設定をLightへ適用します。</summary>
    private void ApplySettings()
    {
        if (m_effectLight == null)return;
        m_effectLight.type = LightType.Spot;
        m_effectLight.color = m_color;
        m_effectLight.intensity = Mathf.Max(0.0f, m_intensity);
        m_effectLight.range = Mathf.Max(0.1f, m_range);
        m_effectLight.spotAngle = Mathf.Clamp(m_spotAngle, 1.0f, 179.0f);
        m_effectLight.innerSpotAngle =
            m_effectLight.spotAngle * Mathf.Clamp01(m_innerAngleRatio);
        m_effectLight.shadows =
            b_m_useShadows ? LightShadows.Soft : LightShadows.None;
        m_effectLight.shadowStrength = Mathf.Clamp01(m_shadowStrength);
        m_effectLight.renderMode = LightRenderMode.ForcePixel;
    }

    /// <summary>通常SceneとPlay ModeだけでLight Componentを生成します。</summary>
    private bool CanBuildInCurrentContext()
    {
        if (Application.isPlaying)return true;
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)return false;
        return gameObject.scene.path.EndsWith(".unity");
    }
}
