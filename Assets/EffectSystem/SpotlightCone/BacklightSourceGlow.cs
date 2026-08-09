/*━━━━━━━━━*
*@file BacklightSourceGlow.cs*
*@brief 実ライトが照射面を持たない場合でも逆光源を視認できるようにする*
*@author 24cu0312 久場洸太*
*@date 2026/08/07*
*最終更新日 2026/08/07*
*@remarks カメラへ向く軽量な放射状Quadを実行時に生成する*
*━━━━━━━━━*/

using UnityEngine;

/// <summary>
/// Spot Lightの光源位置へ、カメラから視認できる放射状の光源面を追加します。
/// 実ライトによる人物・床の照明とは独立しており、照射対象がない場合も光源だけを表示できます。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class BacklightSourceGlow : LightEffectBase
{
    private enum EGlowShape
    {
        Circular,
        Rectangular
    }

    private const string EGlowObjectName = "Backlight Source Glow"; //実行時に生成する子Object名
    private const string EGlowShaderName = "EffectSystem/BacklightSourceGlow"; //専用Shader名

    [SerializeField] private Color m_glowColor =
        new Color(1.0f, 0.58f, 0.2f, 1.0f); //逆光源の中心色
    [SerializeField, Min(0.01f)] private float m_glowSize = 22.0f; //ハローと光条を含む光源面のWorld Scale
    [SerializeField] private Vector2 m_glowAspect = Vector2.one; //遠景でも横長に見えない正円のハロー比率
    [SerializeField] private EGlowShape m_glowShape = EGlowShape.Circular; //Halo外周の形状
    [SerializeField, Min(0.0f)] private float m_glowIntensity = 3.0f; //Shaderへ渡す発光強度
    [SerializeField, Range(0.0f, 1.0f)] private float m_glowOpacity = 0.74f; //光源全体の透明度
    [SerializeField, Range(0.0f, 1.0f)] private float m_coreWhiteness = 0.96f; //光源中心を白へ寄せる強さ
    [SerializeField, Min(1.0f)] private float m_coreIntensityMultiplier = 5.0f; //中心だけを白飛びさせる輝度倍率
    [Header("Halo Ring")]
    [SerializeField, Range(0.0f, 2.0f)] private float m_ringIntensity = 0.65f;
    [SerializeField, Range(0.05f, 0.9f)] private float m_ringRadius = 0.42f;
    [SerializeField, Range(0.01f, 0.4f)] private float m_ringWidth = 0.12f;
    [Header("Off Axis Tilt")]
    [SerializeField, Range(0.0f, 0.5f)] private float m_offAxisTiltStrength = 0.15f;
    [SerializeField, Range(0.0f, 30.0f)] private float m_maximumTiltAngle = 12.0f;
    [Header("Depth Placement")]
    [SerializeField, Range(0.0f, 5.0f)] private float m_cameraDepthOffset = 0.35f;
    [SerializeField] private bool b_m_renderThroughGeometry;
    [SerializeField] private Vector3 m_localOffset =
        new Vector3(0.0f, 0.0f, 0.08f); //Spot Light原点との重なりを避ける位置補正
    [SerializeField] private Light m_sourceLight; //表示状態を同期する同一Prefab内の実Spot Light
    [SerializeField] private bool b_m_haloOnly; //実ライトとハローだけを残し、コーン描画を無効にするか

    private Transform m_glowTransform; //カメラ方向へ向ける光源面Transform
    private Mesh m_glowMesh; //実行時に生成したQuad Mesh
    private Material m_glowMaterial; //専用Shaderを使用する実行時Material
    private MaterialPropertyBlock m_propertyBlock; //色設定をMaterial共有せず反映する領域
    private MeshRenderer m_glowRenderer; //光源面を描画するRenderer
    private bool b_m_sourceLightEnabledAtStartup = true; //Controllerが再有効化する前のInspector設定

    /// <summary>
    /// Lighting Pattern Rigの共通調整値を、生成済みハローへ即時反映します。
    /// Prefabから生成し直すたびに元値へ戻るため、倍率が累積することはありません。
    /// </summary>
    public void ApplyRealtimeTuning(Color _colorTint, float _intensityScale)
    {
        m_glowColor *= _colorTint;
        m_glowIntensity *= Mathf.Max(0.1f, _intensityScale);
        ApplySettings();
    }

    /// <summary>
    /// 実Lightとハローだけを残す表示へ切り替えます。
    /// Lighting Patternから生成した逆光PrefabをHalo Onlyとして再利用するために使用します。
    /// </summary>
    public void SetHaloOnly(bool _haloOnly)
    {
        b_m_haloOnly = _haloOnly;
        if (!b_m_haloOnly)return;

        SpotlightConeController coneController =
            GetComponent<SpotlightConeController>();
        if (coneController == null)return;
        coneController.SetOuterStreakIntensity(0.0f);
        coneController.Hide();
    }

    /// <summary>Composerから渡された親Lightを表示状態の同期対象として設定します。</summary>
    public override void AttachToLight(Light _light)
    {
        base.AttachToLight(_light);
        m_sourceLight = _light;
        if (m_sourceLight != null)
        {
            b_m_sourceLightEnabledAtStartup = m_sourceLight.enabled;
        }
    }

    /// <summary>
    /// 有効化時に光源面を生成します。
    /// Composerの共通親が再度有効化された場合も、他のEffectと同じLifecycleで復元します。
    /// </summary>
    private void OnEnable()
    {
        if (!CanRunInCurrentContext())return;

        if (b_m_haloOnly)
        {
            SpotlightConeController coneController =
                GetComponent<SpotlightConeController>();
            if (coneController != null)
            {
                coneController.SetOuterStreakIntensity(0.0f);
                coneController.Hide();
            }
        }

        if (m_sourceLight == null)
        {
            m_sourceLight = GetComponent<Light>();
        }
        if (m_sourceLight != null)
        {
            //SpotlightConeControllerは実行中にLightを同期するため、
            //その処理より前のInspector上の有効状態を保存します。
            b_m_sourceLightEnabledAtStartup = m_sourceLight.enabled;
        }

        CreateGlowObject();
        ApplySettings();
    }

    /// <summary>無効化時にHaloの描画Objectと実行時Assetを残さず破棄します。</summary>
    private void OnDisable()
    {
        ClearGlowVisual();
    }

    /// <summary>
    /// Inspectorで変更したHaloの色・大きさ・透明度・光条をScene Viewへ即時反映します。
    /// Prefab Importer内では生成せず、通常Scene上のPreviewだけを更新します。
    /// </summary>
    private void OnValidate()
    {
        if (!isActiveAndEnabled)return;
        if (!CanRunInCurrentContext())return;

        if (m_glowTransform == null)
        {
            CreateGlowObject();
        }
        RefreshTransformSettings();
        ApplySettings();
    }

    /// <summary>
    /// カメラが移動しても常に円形の光源として見える向きを維持します。
    /// </summary>
    private void LateUpdate()
    {
        if (m_glowTransform == null)return;

        bool shouldRender = m_sourceLight == null || m_sourceLight.enabled;
        if (m_glowRenderer != null)
        {
            m_glowRenderer.enabled = shouldRender;
        }
        if (!shouldRender)return;

        Camera outputCamera = Camera.main; //現在のGame画面を描画するCamera
        if (outputCamera == null)
        {
            //Gameplayの出力CameraにはMainCamera Tagがない場合があるため、
            //Cinemachineへ依存せずScene内の実Cameraを直接取得します。
            outputCamera = FindFirstObjectByType<Camera>();
        }
        if (outputCamera == null)return;

        //Quadの表面が出力Cameraを向くよう、Cameraから光源へ向かう方向をforwardへ設定します。
        Vector3 cameraDirection =
            m_glowTransform.position - outputCamera.transform.position;
        if (cameraDirection.sqrMagnitude <= Mathf.Epsilon)return;

        Vector3 basePosition = transform.TransformPoint(m_localOffset);
        Vector3 towardCamera = (outputCamera.transform.position - basePosition).normalized;
        m_glowTransform.position = basePosition + towardCamera * m_cameraDepthOffset;
        cameraDirection = m_glowTransform.position - outputCamera.transform.position;

        Vector3 lightToCamera = -cameraDirection.normalized;
        Quaternion cameraFacing = Quaternion.LookRotation(
            cameraDirection.normalized,
            Vector3.up);
        m_glowTransform.rotation = ApplyOffAxisTilt(
            cameraFacing,
            lightToCamera);
    }

    /// <summary>Light射出軸からカメラが外れた量に応じ、Billboardへ弱い傾きを加えます。</summary>
    private Quaternion ApplyOffAxisTilt(
        Quaternion _cameraFacing,
        Vector3 _lightToCamera)
    {
        Vector3 emissionDirection = transform.forward.normalized;
        float offAxisAngle = Vector3.Angle(emissionDirection, _lightToCamera);
        float tiltAngle = Mathf.Min(
            offAxisAngle * m_offAxisTiltStrength,
            m_maximumTiltAngle);
        Vector3 tiltAxis = Vector3.Cross(_lightToCamera, emissionDirection);
        if (tiltAxis.sqrMagnitude <= Mathf.Epsilon || tiltAngle <= 0.0f)
        {
            return _cameraFacing;
        }
        return Quaternion.AngleAxis(tiltAngle, tiltAxis.normalized) * _cameraFacing;
    }

    /// <summary>生成したHaloと専用Assetをまとめて破棄し、再有効化可能な状態へ戻します。</summary>
    private void ClearGlowVisual()
    {
        RemoveDuplicateGlowObjects();

        if (m_glowMaterial != null)
        {
            DestroyPreviewObject(m_glowMaterial);
            m_glowMaterial = null;
        }
        if (m_glowMesh != null)
        {
            DestroyPreviewObject(m_glowMesh);
            m_glowMesh = null;
        }
        m_propertyBlock = null;
    }

    /// <summary>Play ModeとEdit Modeに合った方法で一時描画Assetを破棄します。</summary>
    private static void DestroyPreviewObject(Object _target)
    {
        if (_target == null)return;
        if (Application.isPlaying)
        {
            Destroy(_target);
            return;
        }
        DestroyImmediate(_target);
    }

    /// <summary>
    /// Colliderを持たない最小構成のQuadを子Objectとして生成します。
    /// </summary>
    private void CreateGlowObject()
    {
        RemoveDuplicateGlowObjects();

        Shader glowShader = Shader.Find(EGlowShaderName); //放射状減衰を描画するShader
        if (glowShader == null)
        {
            Debug.LogWarning($"逆光用Shader「{EGlowShaderName}」が見つかりません。", this);
            return;
        }

        GameObject glowObject = new GameObject(
            EGlowObjectName,
            typeof(MeshFilter),
            typeof(MeshRenderer));
        if (!Application.isPlaying)
        {
            //Inspector調整用の一時描画物なので、SceneやPrefabへ保存しません。
            glowObject.hideFlags = HideFlags.DontSaveInEditor;
        }
        m_glowTransform = glowObject.transform;
        m_glowTransform.SetParent(transform, false);
        RefreshTransformSettings();

        m_glowMesh = CreateQuadMesh();
        glowObject.GetComponent<MeshFilter>().sharedMesh = m_glowMesh;

        m_glowMaterial = new Material(glowShader);
        m_glowMaterial.name = "Backlight Source Glow Runtime Material";
        m_glowRenderer = glowObject.GetComponent<MeshRenderer>();
        m_glowRenderer.sharedMaterial = m_glowMaterial;
        m_glowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        m_glowRenderer.receiveShadows = false;
        m_glowRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        m_glowRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
    }

    /// <summary>再有効化や再構築を繰り返してもGlow Quadを一枚だけに保ちます。</summary>
    private void RemoveDuplicateGlowObjects()
    {
        for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = transform.GetChild(childIndex);
            if (child.name != EGlowObjectName)continue;
            child.gameObject.SetActive(false);
            if (Application.isPlaying)Destroy(child.gameObject);
            else DestroyImmediate(child.gameObject);
        }
        m_glowTransform = null;
        m_glowRenderer = null;
    }

    /// <summary>Halo Quadの位置と縦横サイズを現在のInspector値へ更新します。</summary>
    private void RefreshTransformSettings()
    {
        if (m_glowTransform == null)return;
        m_glowTransform.localPosition = m_localOffset;
        m_glowTransform.localScale = new Vector3(
            m_glowSize * Mathf.Max(0.01f, m_glowAspect.x),
            m_glowSize * Mathf.Max(0.01f, m_glowAspect.y),
            1.0f);
    }

    /// <summary>
    /// 光源色、強度、透明度をRenderer単位で反映します。
    /// </summary>
    private void ApplySettings()
    {
        if (m_glowRenderer == null)return;

        if (m_propertyBlock == null)
        {
            m_propertyBlock = new MaterialPropertyBlock();
        }

        m_glowRenderer.GetPropertyBlock(m_propertyBlock);
        m_propertyBlock.SetColor("_GlowColor", m_glowColor);
        m_propertyBlock.SetFloat("_Intensity", Mathf.Max(0.0f, m_glowIntensity));
        m_propertyBlock.SetFloat("_Opacity", Mathf.Clamp01(m_glowOpacity));
        m_propertyBlock.SetFloat("_CoreWhiteness", Mathf.Clamp01(m_coreWhiteness));
        m_propertyBlock.SetFloat(
            "_CoreIntensityMultiplier",
            Mathf.Max(1.0f, m_coreIntensityMultiplier));
        m_propertyBlock.SetFloat("_RingIntensity", Mathf.Max(0.0f, m_ringIntensity));
        m_propertyBlock.SetFloat("_RingRadius", Mathf.Clamp(m_ringRadius, 0.05f, 0.9f));
        m_propertyBlock.SetFloat("_RingWidth", Mathf.Max(0.01f, m_ringWidth));
        m_propertyBlock.SetFloat(
            "_Shape",
            m_glowShape == EGlowShape.Rectangular ? 1.0f : 0.0f);
        if (m_glowMaterial != null)
        {
            m_glowMaterial.SetFloat("_ZTest", b_m_renderThroughGeometry ? 8.0f : 4.0f);
        }
        m_glowRenderer.SetPropertyBlock(m_propertyBlock);
    }

    /// <summary>
    /// 中心が原点になる両面描画用Quad Meshを作成します。
    /// </summary>
    private static Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Backlight Source Glow Runtime Mesh";
        mesh.vertices = new[]
        {
            new Vector3(-0.5f, -0.5f, 0.0f),
            new Vector3(0.5f, -0.5f, 0.0f),
            new Vector3(-0.5f, 0.5f, 0.0f),
            new Vector3(0.5f, 0.5f, 0.0f)
        };
        mesh.uv = new[]
        {
            new Vector2(0.0f, 0.0f),
            new Vector2(1.0f, 0.0f),
            new Vector2(0.0f, 1.0f),
            new Vector2(1.0f, 1.0f)
        };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.RecalculateBounds();
        return mesh;
    }

    /// <summary>Play Modeまたは保存済みの通常Scene上だけでPreview生成を許可します。</summary>
    private bool CanRunInCurrentContext()
    {
        if (Application.isPlaying)return true;
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)return false;
        return gameObject.scene.path.EndsWith(".unity");
    }
}
