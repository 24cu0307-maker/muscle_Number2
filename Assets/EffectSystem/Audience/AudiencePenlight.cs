using System.Collections.Generic;
using UnityEngine;

/// <summary>観客が持つペンライトの本数と左右配置パターンです。</summary>
public enum EAudiencePenlightPattern
{
    OneInOneHand,
    OneInEachHand,
    TwoInOneHand,
    TwoInOneHandAndOneInOther
}

/// <summary>
/// 観客の左右Hand AnchorへLaserBeam Effectを短くしたペンライトを生成し、
/// VenueVoltageSystemの値に合わせて全ペンライトの発光強度を変更します。
/// </summary>
public sealed class AudiencePenlight : MonoBehaviour
{
    private const string ELeftAnchorName = "PenlightLeftHandAnchor";
    private const string ERightAnchorName = "PenlightRightHandAnchor";
    private const int EBeamPlaneCount = 3;
    private const float ELengthToAudienceHeight = 0.28f;
    private const float ERadiusToAudienceHeight = 0.012f;
    private const float EMinimumBeamLength = 0.18f;
    private const float EMinimumBeamRadius = 0.015f;
    private const float EDefaultMinimumIntensity = 1.5f;
    private const float EDefaultMaximumIntensity = 8.0f;
    private const float EDefaultVAngle = 16.0f;
    private const float EDefaultPairSpacing = 0.025f;

    private static readonly Color[] EPenlightColors =
    {
        new Color(0.15f, 0.9f, 1.0f, 1.0f),
        new Color(1.0f, 0.15f, 0.65f, 1.0f),
        new Color(0.55f, 1.0f, 0.12f, 1.0f)
    };

    private static Material s_sharedBeamMaterial;

    [Header("Visual Hand Anchors")]
    [Tooltip("Prefabビューで位置を調整する左手の生成基準です。")]
    [SerializeField] private Transform m_leftHandAnchor;
    [Tooltip("Prefabビューで位置を調整する右手の生成基準です。")]
    [SerializeField] private Transform m_rightHandAnchor;

    [Header("Two Penlights In One Hand")]
    [SerializeField, Range(0.0f, 45.0f)] private float m_vAngle = EDefaultVAngle;
    [SerializeField, Min(0.0f)] private float m_pairSpacing = EDefaultPairSpacing;

    [Header("Tip Glow And Point Lights")]
    [SerializeField] private bool b_m_enableTipGlow = true;
    [SerializeField, Min(0.01f)] private float m_glowSizeMultiplier = 6.0f;
    [SerializeField, Min(0.05f)] private float m_pointLightRange = 1.4f;
    [SerializeField, Min(0.0f)] private float m_minimumPointLightIntensity = 0.05f;
    [SerializeField, Min(0.0f)] private float m_maximumPointLightIntensity = 0.55f;
    [SerializeField, Min(0)] private int m_maximumActivePointLights = 24;

    private readonly List<LaserBeamController> m_beamControllers =
        new List<LaserBeamController>();
    private readonly List<PenlightGlow> m_tipGlows =
        new List<PenlightGlow>();
    private VenueVoltageSystem m_voltageSystem;
    private float m_minimumIntensity = EDefaultMinimumIntensity;
    private float m_maximumIntensity = EDefaultMaximumIntensity;

    public Transform LeftHandAnchor => m_leftHandAnchor;
    public Transform RightHandAnchor => m_rightHandAnchor;

    /// <summary>
    /// Prefabに保存されたAnchorを優先し、未設定の場合だけモデルBoundsから
    /// 左右の手元を推定して実行時Anchorを作成します。
    /// </summary>
    public void Initialize(
        VenueVoltageSystem _voltageSystem,
        float _minimumIntensity,
        float _maximumIntensity)
    {
        UnsubscribeVoltageEvents();
        m_voltageSystem = _voltageSystem;
        m_minimumIntensity = Mathf.Max(0.0f, _minimumIntensity);
        m_maximumIntensity = Mathf.Max(m_minimumIntensity, _maximumIntensity);

        ResolveOrCreateHandAnchors();
        if (m_beamControllers.Count == 0)
        {
            CreateRandomPattern();
        }

        SubscribeVoltageEvents();
        ApplyVoltageIntensity();
    }

    /// <summary>Editor Toolが作成した左右AnchorをComponentへ登録します。</summary>
    public void SetHandAnchors(Transform _left, Transform _right)
    {
        m_leftHandAnchor = _left;
        m_rightHandAnchor = _right;
    }

    private void OnDestroy()
    {
        UnsubscribeVoltageEvents();
    }

    /// <summary>指定された四種類から等確率で一つを生成します。</summary>
    private void CreateRandomPattern()
    {
        EAudiencePenlightPattern pattern =
            (EAudiencePenlightPattern)Random.Range(0, 4);
        bool b_primaryIsLeft = Random.value < 0.5f;
        Transform primary = b_primaryIsLeft
            ? m_leftHandAnchor
            : m_rightHandAnchor;
        Transform other = b_primaryIsLeft
            ? m_rightHandAnchor
            : m_leftHandAnchor;

        switch (pattern)
        {
            case EAudiencePenlightPattern.OneInOneHand:
                CreateSingleBeam(primary, 0.0f, 0.0f);
                break;
            case EAudiencePenlightPattern.OneInEachHand:
                CreateSingleBeam(m_leftHandAnchor, 0.0f, 0.0f);
                CreateSingleBeam(m_rightHandAnchor, 0.0f, 0.0f);
                break;
            case EAudiencePenlightPattern.TwoInOneHand:
                CreateVPair(primary);
                break;
            case EAudiencePenlightPattern.TwoInOneHandAndOneInOther:
                CreateVPair(primary);
                CreateSingleBeam(other, 0.0f, 0.0f);
                break;
        }
    }

    /// <summary>同じ手の二本を正面から見てV字になる角度へ開きます。</summary>
    private void CreateVPair(Transform _anchor)
    {
        CreateSingleBeam(_anchor, -m_vAngle, -m_pairSpacing * 0.5f);
        CreateSingleBeam(_anchor, m_vAngle, m_pairSpacing * 0.5f);
    }

    /// <summary>Anchorを基準に一本の短いLaserBeamを生成します。</summary>
    private void CreateSingleBeam(
        Transform _anchor,
        float _spreadAngle,
        float _horizontalOffset)
    {
        if (_anchor == null)return;

        Bounds audienceBounds = CalculateAudienceBounds();
        float audienceHeight = Mathf.Max(0.1f, audienceBounds.size.y);
        GameObject beamObject = new GameObject(
            "AudiencePenlight",
            typeof(MeshFilter),
            typeof(MeshRenderer));
        beamObject.transform.SetParent(_anchor, false);
        beamObject.transform.localPosition = Vector3.right * _horizontalOffset;
        // LaserBeamはLocal +Zへ伸びます。Z軸回転ではBeamの断面しか回らないため、
        // AnchorのLocal Y軸を中心に傾け、正面から見て左右へ開くV字にします。
        beamObject.transform.localRotation =
            Quaternion.Euler(0.0f, _spreadAngle, 0.0f);

        MeshRenderer renderer = beamObject.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = GetSharedBeamMaterial();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        float beamLength =
            Mathf.Max(EMinimumBeamLength, audienceHeight * ELengthToAudienceHeight);
        float beamRadius =
            Mathf.Max(EMinimumBeamRadius, audienceHeight * ERadiusToAudienceHeight);
        LaserBeamMesh beamMesh = beamObject.AddComponent<LaserBeamMesh>();
        beamMesh.Configure(
            beamLength,
            beamRadius,
            EBeamPlaneCount);

        LaserBeamController controller =
            beamObject.AddComponent<LaserBeamController>();
        Color penlightColor =
            EPenlightColors[Random.Range(0, EPenlightColors.Length)];
        controller.LaserColor = penlightColor;
        m_beamControllers.Add(controller);

        if (b_m_enableTipGlow)
        {
            CreateTipGlow(
                beamObject.transform,
                beamLength,
                beamRadius,
                penlightColor);
        }
    }

    /// <summary>
    /// Beam本体と同じLaserBeamMeshを少し太く重ね、Light Budgetに空きが
    /// あればPoint Lightも追加します。長さ・交差平面数・向きは本体と同じで、
    /// 半径だけを広げるため完全に同じ輪郭の外側Glowになります。
    /// </summary>
    private void CreateTipGlow(
        Transform _beamTransform,
        float _beamLength,
        float _beamRadius,
        Color _color)
    {
        GameObject glowObject = new GameObject(
            "PenlightOuterGlow",
            typeof(MeshFilter),
            typeof(MeshRenderer));
        glowObject.transform.SetParent(_beamTransform, false);
        float glowMultiplier = Mathf.Max(1.0f, m_glowSizeMultiplier);
        // 横方向へ増やした半径差を長さ方向の余白にも使用します。
        // 根元側と先端側へ同量を追加し、Glowの中心がずれないようにします。
        float longitudinalPadding =
            _beamRadius * (glowMultiplier - 1.0f);
        glowObject.transform.localPosition =
            Vector3.back * longitudinalPadding;
        glowObject.transform.localRotation = Quaternion.identity;

        LaserBeamMesh glowMesh = glowObject.AddComponent<LaserBeamMesh>();
        glowMesh.Configure(
            _beamLength + longitudinalPadding * 2.0f,
            _beamRadius * glowMultiplier,
            EBeamPlaneCount);

        PenlightGlow glow = glowObject.AddComponent<PenlightGlow>();
        glow.Configure(
            _color,
            m_pointLightRange,
            m_minimumPointLightIntensity,
            m_maximumPointLightIntensity,
            m_maximumActivePointLights);
        m_tipGlows.Add(glow);
    }

    /// <summary>Prefab Anchorを名前でも検索し、なければBoundsから生成します。</summary>
    private void ResolveOrCreateHandAnchors()
    {
        if (m_leftHandAnchor == null)
        {
            m_leftHandAnchor = FindChildByName(ELeftAnchorName);
        }
        if (m_rightHandAnchor == null)
        {
            m_rightHandAnchor = FindChildByName(ERightAnchorName);
        }
        if (m_leftHandAnchor != null && m_rightHandAnchor != null)return;

        Bounds bounds = CalculateAudienceBounds();
        if (m_leftHandAnchor == null)
        {
            m_leftHandAnchor = CreateRuntimeAnchor(
                ELeftAnchorName,
                bounds.center - transform.right * bounds.extents.x * 0.72f
                    + transform.up * bounds.extents.y * 0.25f);
        }
        if (m_rightHandAnchor == null)
        {
            m_rightHandAnchor = CreateRuntimeAnchor(
                ERightAnchorName,
                bounds.center + transform.right * bounds.extents.x * 0.72f
                    + transform.up * bounds.extents.y * 0.25f);
        }
    }

    private Transform CreateRuntimeAnchor(string _name, Vector3 _worldPosition)
    {
        Transform anchor = new GameObject(_name).transform;
        anchor.SetParent(transform, true);
        anchor.position = _worldPosition;
        anchor.rotation = Quaternion.LookRotation(transform.up, transform.forward);
        return anchor;
    }

    private Transform FindChildByName(string _name)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; ++i)
        {
            if (children[i].name == _name)return children[i];
        }
        return null;
    }

    private Bounds CalculateAudienceBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(transform.position + transform.up, Vector3.one * 2.0f);
        }
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; ++i)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    private static Material GetSharedBeamMaterial()
    {
        if (s_sharedBeamMaterial != null)return s_sharedBeamMaterial;
        Shader shader = Shader.Find("Muscle/Effects/Laser Beam Additive");
        s_sharedBeamMaterial = new Material(shader)
        {
            name = "Audience Penlight Shared Material",
            hideFlags = HideFlags.HideAndDontSave
        };
        return s_sharedBeamMaterial;
    }

    private void SubscribeVoltageEvents()
    {
        if (m_voltageSystem == null)return;
        m_voltageSystem.m_audienceSuccess += OnVoltageSuccess;
        m_voltageSystem.m_audienceFailure += OnVoltageFailure;
    }

    private void UnsubscribeVoltageEvents()
    {
        if (m_voltageSystem == null)return;
        m_voltageSystem.m_audienceSuccess -= OnVoltageSuccess;
        m_voltageSystem.m_audienceFailure -= OnVoltageFailure;
    }

    private void OnVoltageSuccess(float _normalizedVoltage)
    {
        ApplyNormalizedIntensity(_normalizedVoltage);
    }

    private void OnVoltageFailure()
    {
        ApplyVoltageIntensity();
    }

    private void ApplyVoltageIntensity()
    {
        ApplyNormalizedIntensity(
            m_voltageSystem != null ? m_voltageSystem.NormalizedVoltage : 0.5f);
    }

    private void ApplyNormalizedIntensity(float _normalizedVoltage)
    {
        float intensity = Mathf.Lerp(
            m_minimumIntensity,
            m_maximumIntensity,
            Mathf.Clamp01(_normalizedVoltage));
        for (int i = 0; i < m_beamControllers.Count; ++i)
        {
            if (m_beamControllers[i] != null)
            {
                m_beamControllers[i].EmissionIntensity = intensity;
            }
        }
        for (int i = 0; i < m_tipGlows.Count; ++i)
        {
            if (m_tipGlows[i] != null)
            {
                m_tipGlows[i].ApplyVoltage(_normalizedVoltage);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawAnchorGizmo(m_leftHandAnchor, Color.cyan, m_vAngle);
        DrawAnchorGizmo(m_rightHandAnchor, Color.magenta, m_vAngle);
    }

    /// <summary>一本の中心線と、二本持ち時のV字方向をPrefabビューへ表示します。</summary>
    private static void DrawAnchorGizmo(
        Transform _anchor,
        Color _color,
        float _vAngle)
    {
        if (_anchor == null)return;
        Gizmos.color = _color;
        Gizmos.DrawWireSphere(_anchor.position, 0.04f);
        Gizmos.DrawLine(_anchor.position, _anchor.position + _anchor.forward * 0.3f);
        Vector3 leftDirection =
            _anchor.rotation
            * Quaternion.Euler(0.0f, -_vAngle, 0.0f)
            * Vector3.forward;
        Vector3 rightDirection =
            _anchor.rotation
            * Quaternion.Euler(0.0f, _vAngle, 0.0f)
            * Vector3.forward;
        Gizmos.DrawLine(_anchor.position, _anchor.position + leftDirection * 0.25f);
        Gizmos.DrawLine(_anchor.position, _anchor.position + rightDirection * 0.25f);
    }
}
