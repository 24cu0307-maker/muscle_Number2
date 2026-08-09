/*============================================================
*@file SpotlightConeVisualEffect.cs*
*@brief 実Lightを追加せずSpotlightの光体積だけを生成する*
*@author 24CU0312 久場洸太*
*@date 2026/08/07*
*============================================================*/

using UnityEngine;

/// <summary>
/// Composer用の視覚Effectです。照明計算は親のLightへ任せ、半透明Coneだけを描画します。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class SpotlightConeVisualEffect : LightEffectBase
{
    [SerializeField] private Color m_color = new Color(1.0f, 0.72f, 0.3f, 1.0f); //Cone色
    [SerializeField, Min(0.0f)] private float m_intensity = 1.5f; //発光強度
    [SerializeField, Range(0.0f, 1.0f)] private float m_opacity = 0.12f; //透明度

    private Material m_runtimeMaterial; //個別色を適用する一時Material

    /// <summary>有効化時にCone MeshとControllerを構築します。</summary>
    [Header("Cone Shape")]
    [SerializeField, Min(0.01f)] private float m_length = 5.0f; //光源から終端までの長さ
    [SerializeField, Min(0.01f)] private float m_endRadius = 2.0f; //終端円の半径
    [SerializeField, Range(3, 64)] private int m_segments = 32; //円周の分割数

    private void OnEnable()
    {
        EnsureVisual();
    }

    /// <summary>Inspector変更をその場で表示へ反映します。</summary>
    private void OnValidate()
    {
        ApplySettings();
    }

    /// <summary>生成Materialを破棄します。</summary>
    private void OnDisable()
    {
        if (m_runtimeMaterial == null)return;
        if (Application.isPlaying)
        {
            Destroy(m_runtimeMaterial);
        }
        else
        {
            DestroyImmediate(m_runtimeMaterial);
        }
        m_runtimeMaterial = null;
    }

    /// <summary>必要Componentと専用Materialを一度だけ追加します。</summary>
    private void EnsureVisual()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            gameObject.AddComponent<MeshFilter>();
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        Shader shader = Shader.Find("Muscle/Effects/Spotlight Cone Additive");
        if (shader != null && m_runtimeMaterial == null)
        {
            m_runtimeMaterial = new Material(shader);
            m_runtimeMaterial.name = "Composable Spotlight Cone Material";
            m_runtimeMaterial.hideFlags = HideFlags.HideAndDontSave;
            meshRenderer.sharedMaterial = m_runtimeMaterial;
        }

        if (GetComponent<SpotlightConeMesh>() == null)
        {
            gameObject.AddComponent<SpotlightConeMesh>();
        }
        if (GetComponent<SpotlightConeController>() == null)
        {
            gameObject.AddComponent<SpotlightConeController>();
        }
        ApplySettings();
    }

    /// <summary>Cone Controllerへ現在の色・強度・透明度を反映します。</summary>
    private void ApplySettings()
    {
        SpotlightConeMesh coneMesh = GetComponent<SpotlightConeMesh>();
        if (coneMesh != null)
        {
            coneMesh.ConfigureShape(m_length, m_endRadius, m_segments);
        }

        SpotlightConeController controller = GetComponent<SpotlightConeController>();
        if (controller == null)return;
        controller.SetSurfaceIlluminationEnabled(false);
        controller.LightColor = m_color;
        controller.EmissionIntensity = m_intensity;
        controller.Opacity = m_opacity;
    }
}
