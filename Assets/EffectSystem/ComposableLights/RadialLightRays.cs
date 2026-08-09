/*============================================================
*@file RadialLightRays.cs*
*@brief Haloから独立した放射状の光条Effect
*@author 24CU0312 久場洸太
*@date 2026/08/07
*============================================================*/

using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class RadialLightRays : LightEffectBase
{
    private enum ERayBoundaryShape
    {
        Circular,
        Rectangular
    }

    private const string EShaderName = "EffectSystem/RadialLightRays";

    [SerializeField] private Color m_color = new Color(1.0f, 0.58f, 0.2f, 1.0f);
    [SerializeField, Min(0.01f)] private float m_size = 18.0f;
    [SerializeField] private Vector2 m_aspect = Vector2.one;
    [SerializeField] private ERayBoundaryShape m_boundaryShape =
        ERayBoundaryShape.Circular;
    [SerializeField, Range(0.0f, 4.0f)] private float m_intensity = 1.0f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_opacity = 0.45f;
    [SerializeField, Range(2.0f, 24.0f)] private float m_rayCount = 9.0f;
    [SerializeField, Range(1.0f, 20.0f)] private float m_raySharpness = 4.0f;
    [SerializeField, Range(0.0f, 0.8f)] private float m_innerFade = 0.08f;
    [SerializeField, Range(0.1f, 1.0f)] private float m_outerFadeStart = 0.5f;
    [Header("Center Brightness")]
    [SerializeField, Range(0.0f, 4.0f)] private float m_centerBrightness = 1.5f;
    [SerializeField, Range(0.25f, 8.0f)] private float m_centerFalloff = 2.0f;
    [Header("Off Axis Tilt")]
    [SerializeField, Range(0.0f, 0.5f)] private float m_offAxisTiltStrength = 0.15f;
    [SerializeField, Range(0.0f, 30.0f)] private float m_maximumTiltAngle = 12.0f;
    [Header("Depth Placement")]
    [SerializeField, Range(0.0f, 5.0f)] private float m_cameraDepthOffset;
    [SerializeField] private bool b_m_renderThroughGeometry = true;
    [SerializeField] private Vector3 m_localOffset = new Vector3(0.0f, 0.0f, 0.07f);

    private Transform m_quadTransform;
    private MeshRenderer m_renderer;
    private Mesh m_mesh;
    private Material m_material;
    private MaterialPropertyBlock m_properties;

    private void Awake()
    {
        if (!CanPreview())return;
        EnsureVisual();
        ApplySettings();
    }

    private void OnValidate()
    {
        if (!CanPreview())return;
        EnsureVisual();
        ApplySettings();
    }

    private void LateUpdate()
    {
        if (m_quadTransform == null)return;
        Camera outputCamera = Camera.main;
        if (outputCamera == null)outputCamera = FindFirstObjectByType<Camera>();
        if (outputCamera == null)return;
        Vector3 direction = m_quadTransform.position - outputCamera.transform.position;
        if (direction.sqrMagnitude <= Mathf.Epsilon)return;
        Vector3 basePosition = transform.TransformPoint(m_localOffset);
        Vector3 towardCamera = (outputCamera.transform.position - basePosition).normalized;
        m_quadTransform.position = basePosition + towardCamera * m_cameraDepthOffset;
        direction = m_quadTransform.position - outputCamera.transform.position;
        Vector3 lightToCamera = -direction.normalized;
        Quaternion cameraFacing = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Vector3 emissionDirection = transform.forward.normalized;
        float offAxisAngle = Vector3.Angle(emissionDirection, lightToCamera);
        float tiltAngle = Mathf.Min(
            offAxisAngle * m_offAxisTiltStrength,
            m_maximumTiltAngle);
        Vector3 tiltAxis = Vector3.Cross(lightToCamera, emissionDirection);
        if (tiltAxis.sqrMagnitude <= Mathf.Epsilon || tiltAngle <= 0.0f)
        {
            m_quadTransform.rotation = cameraFacing;
            return;
        }
        m_quadTransform.rotation =
            Quaternion.AngleAxis(tiltAngle, tiltAxis.normalized) * cameraFacing;
    }

    private void OnDestroy()
    {
        DestroyTemporary(m_material);
        DestroyTemporary(m_mesh);
    }

    private void EnsureVisual()
    {
        if (m_quadTransform != null)return;
        RemoveDuplicateVisuals();
        Shader shader = Shader.Find(EShaderName);
        if (shader == null)return;

        GameObject quad = new GameObject("Radial Light Rays Visual", typeof(MeshFilter), typeof(MeshRenderer));
        if (!Application.isPlaying)quad.hideFlags = HideFlags.DontSaveInEditor;
        m_quadTransform = quad.transform;
        m_quadTransform.SetParent(transform, false);

        m_mesh = CreateQuad();
        quad.GetComponent<MeshFilter>().sharedMesh = m_mesh;
        m_material = new Material(shader) { name = "Radial Light Rays Runtime Material" };
        m_renderer = quad.GetComponent<MeshRenderer>();
        m_renderer.sharedMaterial = m_material;
        m_renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_renderer.receiveShadows = false;
    }

    /// <summary>Timelineの再有効化後も光条Quadが重複しないよう既存描画物を回収します。</summary>
    private void RemoveDuplicateVisuals()
    {
        for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = transform.GetChild(childIndex);
            if (child.name != "Radial Light Rays Visual")continue;
            child.gameObject.SetActive(false);
            if (Application.isPlaying)Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        m_quadTransform = null;
        m_renderer = null;
    }

    private void ApplySettings()
    {
        if (m_quadTransform == null || m_renderer == null)return;
        m_quadTransform.localPosition = m_localOffset;
        m_quadTransform.localScale = new Vector3(
            m_size * Mathf.Max(0.01f, m_aspect.x),
            m_size * Mathf.Max(0.01f, m_aspect.y),
            1.0f);
        if (m_properties == null)m_properties = new MaterialPropertyBlock();
        m_properties.SetColor("_Color", m_color);
        m_properties.SetFloat("_Intensity", m_intensity);
        m_properties.SetFloat("_Opacity", m_opacity);
        m_properties.SetFloat("_RayCount", m_rayCount);
        m_properties.SetFloat("_RaySharpness", m_raySharpness);
        m_properties.SetFloat("_InnerFade", m_innerFade);
        m_properties.SetFloat("_OuterFadeStart", m_outerFadeStart);
        m_properties.SetFloat("_CenterBrightness", m_centerBrightness);
        m_properties.SetFloat("_CenterFalloff", m_centerFalloff);
        m_properties.SetFloat(
            "_Shape",
            m_boundaryShape == ERayBoundaryShape.Rectangular ? 1.0f : 0.0f);
        if (m_material != null)
        {
            //ZTestは描画StateなのでMaterialPropertyBlockではなくMaterialへ設定します。
            m_material.SetFloat("_ZTest", b_m_renderThroughGeometry ? 8.0f : 4.0f);
        }
        m_renderer.SetPropertyBlock(m_properties);
    }

    private static Mesh CreateQuad()
    {
        Mesh mesh = new Mesh { name = "Radial Light Rays Generated Quad" };
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0.0f), new Vector3(0.5f, -0.5f, 0.0f),
            new Vector3(-0.5f, 0.5f, 0.0f), new Vector3(0.5f, 0.5f, 0.0f)
        };
        mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void DestroyTemporary(Object _target)
    {
        if (_target == null)return;
        if (Application.isPlaying)Destroy(_target);
        else DestroyImmediate(_target);
    }

    private bool CanPreview()
    {
        if (Application.isPlaying)return true;
        return gameObject.scene.IsValid()
            && gameObject.scene.isLoaded
            && gameObject.scene.path.EndsWith(".unity");
    }
}
