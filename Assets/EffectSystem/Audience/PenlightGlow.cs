using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// ペンライト先端の発光球と短距離Point Lightを制御します。
/// 発光球は全数表示し、実ライトだけを静的Budget以内へ制限します。
/// </summary>
public sealed class PenlightGlow : MonoBehaviour
{
    private static readonly int EColorId = Shader.PropertyToID("_Color");
    private static readonly int EIntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int EOpacityId = Shader.PropertyToID("_Opacity");
    private static int s_activePointLightCount;
    private static Material s_sharedGlowMaterial;

    private Renderer m_renderer;
    private Light m_pointLight;
    private MaterialPropertyBlock m_propertyBlock;
    private bool b_m_ownsPointLightSlot;
    private float m_minimumLightIntensity;
    private float m_maximumLightIntensity;

    /// <summary>
    /// 色・寸法・Point Light設定を適用します。Light Budgetを超えた個体も
    /// 発光球は表示されるため、会場全体の見た目は維持されます。
    /// </summary>
    public void Configure(
        Color _color,
        float _lightRange,
        float _minimumLightIntensity,
        float _maximumLightIntensity,
        int _maximumActivePointLights)
    {
        m_minimumLightIntensity = Mathf.Max(0.0f, _minimumLightIntensity);
        m_maximumLightIntensity = Mathf.Max(
            m_minimumLightIntensity,
            _maximumLightIntensity);

        m_renderer = GetComponent<Renderer>();
        m_renderer.sharedMaterial = GetSharedGlowMaterial();
        m_renderer.shadowCastingMode = ShadowCastingMode.Off;
        m_renderer.receiveShadows = false;

        ApplySphereAppearance(_color, 0.5f);
        TryCreatePointLight(
            _color,
            Mathf.Max(0.05f, _lightRange),
            Mathf.Max(0, _maximumActivePointLights));
    }

    /// <summary>正規化Voltageから発光球と実ライトの強度を同時更新します。</summary>
    public void ApplyVoltage(float _normalizedVoltage)
    {
        float voltage = Mathf.Clamp01(_normalizedVoltage);
        Color color = Color.white;
        if (m_renderer != null)
        {
            EnsurePropertyBlock();
            m_renderer.GetPropertyBlock(m_propertyBlock);
            color = m_propertyBlock.GetColor(EColorId);
        }
        ApplySphereAppearance(color, voltage);

        if (m_pointLight != null)
        {
            m_pointLight.intensity = Mathf.Lerp(
                m_minimumLightIntensity,
                m_maximumLightIntensity,
                voltage);
        }
    }

    private void TryCreatePointLight(
        Color _color,
        float _range,
        int _maximumActivePointLights)
    {
        if (_maximumActivePointLights <= 0
            || s_activePointLightCount >= _maximumActivePointLights)return;

        m_pointLight = gameObject.AddComponent<Light>();
        m_pointLight.type = LightType.Point;
        m_pointLight.color = _color;
        m_pointLight.range = _range;
        m_pointLight.shadows = LightShadows.None;
        m_pointLight.renderMode = LightRenderMode.Auto;
        s_activePointLightCount++;
        b_m_ownsPointLightSlot = true;
    }

    private void ApplySphereAppearance(Color _color, float _normalizedVoltage)
    {
        if (m_renderer == null)return;
        EnsurePropertyBlock();
        m_renderer.GetPropertyBlock(m_propertyBlock);
        m_propertyBlock.SetColor(EColorId, _color);
        m_propertyBlock.SetFloat(
            EIntensityId,
            Mathf.Lerp(0.35f, 1.8f, Mathf.Clamp01(_normalizedVoltage)));
        // Glow自体は薄く保ち、本体Beamの色を隠さないようにします。
        m_propertyBlock.SetFloat(EOpacityId, 0.18f);
        m_renderer.SetPropertyBlock(m_propertyBlock);
    }

    private void EnsurePropertyBlock()
    {
        if (m_propertyBlock == null)
        {
            m_propertyBlock = new MaterialPropertyBlock();
        }
    }

    private static Material GetSharedGlowMaterial()
    {
        if (s_sharedGlowMaterial != null)return s_sharedGlowMaterial;
        Shader shader = Shader.Find("Muscle/Effects/Penlight Glow Sphere");
        s_sharedGlowMaterial = new Material(shader)
        {
            name = "Penlight Glow Sphere Shared Material",
            hideFlags = HideFlags.HideAndDontSave
        };
        s_sharedGlowMaterial.enableInstancing = true;
        return s_sharedGlowMaterial;
    }

    private void OnDestroy()
    {
        if (!b_m_ownsPointLightSlot)return;
        s_activePointLightCount = Mathf.Max(0, s_activePointLightCount - 1);
        b_m_ownsPointLightSlot = false;
    }
}
