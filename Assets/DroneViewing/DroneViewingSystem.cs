using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 会場上空を飛ぶDrone Cameraの映像を、Stage内のMonitor Materialへ表示します。
/// </summary>
public sealed class DroneViewingSystem : MonoBehaviour
{
    private const string EDefaultMonitorKeyword = "moniter"; //既存Stage Assetの命名に合わせる
    private const string EBaseMapProperty = "_BaseMap";
    private const string EMainTextureProperty = "_MainTex";
    private const string EEmissionMapProperty = "_EmissionMap";
    private const string EEmissionColorProperty = "_EmissionColor";
    private const string EDroneObjectName = "VenueViewingDrone";
    private const string EDroneCameraName = "DroneCamera";
    private const int EMinimumTextureSize = 256;
    private const int ESplineSamplesPerSegment = 24;

    [Header("Drone Flight")]
    [SerializeField, Range(1, 3)] private int m_droneCount = 3;
    [SerializeField] private Transform m_lookAtTarget;
    [SerializeField] private Vector3 m_lookAtPosition = new Vector3(0.0f, 2.5f, 0.0f);
    [SerializeField, Min(0.0f)] private float m_orbitRadius = 12.0f;
    [SerializeField] private float m_flightHeight = 8.0f;
    [SerializeField] private float m_orbitDegreesPerSecond = 8.0f;
    [SerializeField] private float m_startAngleDegrees = 210.0f;
    [SerializeField, Min(0.0f)] private float m_verticalBobAmount = 0.25f;
    [SerializeField, Min(0.01f)] private float m_verticalBobSpeed = 0.8f;

    [Header("Spline Flight")]
    [SerializeField] private bool b_m_useSplineFlight = true;
    [SerializeField] private bool b_m_loopSpline = true;
    [SerializeField, Min(0.01f)] private float m_splineSpeed = 5.0f;
    [SerializeField] private Vector3[] m_splinePoints =
    {
        new Vector3(-12.0f, 8.0f, -12.0f),
        new Vector3(-12.0f, 10.0f, 12.0f),
        new Vector3(12.0f, 8.0f, 12.0f),
        new Vector3(12.0f, 10.0f, -12.0f)
    };
    [SerializeField] private bool b_m_showSpline = true;

    [Header("Drone Camera")]
    [SerializeField, Range(20.0f, 100.0f)] private float m_fieldOfView = 55.0f;
    [SerializeField, Min(0.01f)] private float m_nearClipPlane = 0.1f;
    [SerializeField, Min(1.0f)] private float m_farClipPlane = 300.0f;
    [SerializeField] private LayerMask m_cameraCullingMask = ~0;

    [Header("Monitor Output")]
    [SerializeField] private string m_monitorMaterialKeyword = EDefaultMonitorKeyword;
    [SerializeField, Min(EMinimumTextureSize)] private int m_textureWidth = 1280;
    [SerializeField, Min(EMinimumTextureSize)] private int m_textureHeight = 720;
    [SerializeField] private bool b_m_enableEmission = true;
    [SerializeField, Min(0.0f)] private float m_emissionIntensity = 1.5f;

    [Header("Drone Visual")]
    [SerializeField] private bool b_m_createDroneVisual = true;
    [SerializeField] private Color m_droneColor = new Color(0.08f, 0.1f, 0.12f, 1.0f);

    private readonly List<MonitorMaterialSlot> m_monitorSlotsList =
        new List<MonitorMaterialSlot>();
    private readonly Dictionary<Renderer, bool> m_monitorOriginalForceRenderingOff =
        new Dictionary<Renderer, bool>();
    private readonly List<Transform> m_droneTransforms = new List<Transform>();
    private readonly List<DroneSplineRoute> m_droneRoutes =
        new List<DroneSplineRoute>();
    private readonly List<Camera> m_droneCameras = new List<Camera>();
    private readonly List<RenderTexture> m_monitorTextures =
        new List<RenderTexture>();
    private Material m_droneMaterial;
    private float m_orbitAngleDegrees;
    private bool b_m_monitorRenderersHidden;
    private readonly List<Vector3> m_splineSamplePositions =
        new List<Vector3>();
    private readonly List<float> m_splineSampleDistances =
        new List<float>();
    private float m_splineDistance;
    private float m_splineTotalDistance;
    private bool b_m_splineCacheDirty = true;

    [Serializable]
    private struct MonitorMaterialSlot
    {
        public Renderer Renderer;
        public int MaterialIndex;
        public Material OriginalMaterial;
        public Material RuntimeMaterial;
    }

    private void Awake()
    {
        m_orbitAngleDegrees = m_startAngleDegrees;
        CreateDrones();
        CreateMonitorTextures();
        FindMonitorMaterialSlots();
        ApplyMonitorTexture();
        SubscribeCameraRendering();
    }

    private void LateUpdate()
    {
        if (m_droneTransforms.Count == 0)return;

        Vector3 targetPosition = GetLookAtPosition();
        if (b_m_useSplineFlight && m_splinePoints != null
            && m_splinePoints.Length >= 2)
        {
            UpdateSplineFlightPositions();
        }
        else
        {
            UpdateOrbitFlightPositions(targetPosition);
        }

        for (int i = 0; i < m_droneTransforms.Count; ++i)
        {
            Transform droneTransform = m_droneTransforms[i];
            if (droneTransform == null)continue;
            Vector3 lookDirection = targetPosition - droneTransform.position;
            if (lookDirection.sqrMagnitude > Mathf.Epsilon)
            {
                droneTransform.rotation = Quaternion.LookRotation(
                    lookDirection.normalized,
                    Vector3.up);
            }
        }
    }

    private void UpdateOrbitFlightPositions(Vector3 _targetPosition)
    {
        m_orbitAngleDegrees += m_orbitDegreesPerSecond * Time.deltaTime;
        for (int i = 0; i < m_droneTransforms.Count; ++i)
        {
            float angleOffset = 360.0f * i / m_droneTransforms.Count;
            float angleRadians =
                (m_orbitAngleDegrees + angleOffset) * Mathf.Deg2Rad;
            float verticalOffset = Mathf.Sin(
                Time.time * m_verticalBobSpeed + i * 2.0f)
                * m_verticalBobAmount;
            m_droneTransforms[i].position = _targetPosition + new Vector3(
                Mathf.Cos(angleRadians) * m_orbitRadius,
                m_flightHeight + verticalOffset,
                Mathf.Sin(angleRadians) * m_orbitRadius);
        }
    }

    private void UpdateSplineFlightPositions()
    {
        if (m_droneRoutes.Count == m_droneTransforms.Count)
        {
            for (int i = 0; i < m_droneRoutes.Count; ++i)
            {
                if (m_droneRoutes[i] != null)
                {
                    m_droneRoutes[i].Advance(Time.deltaTime);
                }
            }
            return;
        }

        if (b_m_splineCacheDirty)RebuildSplineCache();
        if (m_splineTotalDistance <= Mathf.Epsilon)return;

        m_splineDistance += Mathf.Max(0.01f, m_splineSpeed) * Time.deltaTime;
        m_splineDistance = b_m_loopSpline
            ? Mathf.Repeat(m_splineDistance, m_splineTotalDistance)
            : Mathf.Min(m_splineDistance, m_splineTotalDistance);
        for (int i = 0; i < m_droneTransforms.Count; ++i)
        {
            float distanceOffset =
                m_splineTotalDistance * i / m_droneTransforms.Count;
            float droneDistance = b_m_loopSpline
                ? Mathf.Repeat(
                    m_splineDistance + distanceOffset,
                    m_splineTotalDistance)
                : Mathf.Min(
                    m_splineDistance + distanceOffset,
                    m_splineTotalDistance);
            Vector3 localPosition = GetSplinePositionAtDistance(droneDistance);
            m_droneTransforms[i].position =
                transform.TransformPoint(localPosition);
        }
    }

    private void RebuildSplineCache()
    {
        b_m_splineCacheDirty = false;
        m_splineSamplePositions.Clear();
        m_splineSampleDistances.Clear();
        m_splineTotalDistance = 0.0f;
        if (m_splinePoints == null || m_splinePoints.Length < 2)return;

        int segmentCount = b_m_loopSpline
            ? m_splinePoints.Length
            : m_splinePoints.Length - 1;
        Vector3 previousPosition = EvaluateSplineSegment(0, 0.0f);
        m_splineSamplePositions.Add(previousPosition);
        m_splineSampleDistances.Add(0.0f);
        for (int i = 0; i < segmentCount; ++i)
        {
            for (int j = 1; j <= ESplineSamplesPerSegment; ++j)
            {
                float segmentTime = (float)j / ESplineSamplesPerSegment;
                Vector3 position = EvaluateSplineSegment(i, segmentTime);
                m_splineTotalDistance += Vector3.Distance(
                    previousPosition,
                    position);
                m_splineSamplePositions.Add(position);
                m_splineSampleDistances.Add(m_splineTotalDistance);
                previousPosition = position;
            }
        }
    }

    private Vector3 GetSplinePositionAtDistance(float _distance)
    {
        for (int i = 1; i < m_splineSampleDistances.Count; ++i)
        {
            if (_distance > m_splineSampleDistances[i])continue;

            float previousDistance = m_splineSampleDistances[i - 1];
            float sectionLength = m_splineSampleDistances[i] - previousDistance;
            float interpolation = sectionLength > Mathf.Epsilon
                ? (_distance - previousDistance) / sectionLength
                : 0.0f;
            return Vector3.Lerp(
                m_splineSamplePositions[i - 1],
                m_splineSamplePositions[i],
                interpolation);
        }
        return m_splineSamplePositions[m_splineSamplePositions.Count - 1];
    }

    private Vector3 EvaluateSplineSegment(int _segmentIndex, float _time)
    {
        int pointCount = m_splinePoints.Length;
        int currentIndex = Mathf.Clamp(_segmentIndex, 0, pointCount - 1);
        int nextIndex = GetSplinePointIndex(currentIndex + 1);
        int previousIndex = GetSplinePointIndex(currentIndex - 1);
        int followingIndex = GetSplinePointIndex(currentIndex + 2);
        Vector3 previous = m_splinePoints[previousIndex];
        Vector3 current = m_splinePoints[currentIndex];
        Vector3 next = m_splinePoints[nextIndex];
        Vector3 following = m_splinePoints[followingIndex];
        float timeSquared = _time * _time;
        float timeCubed = timeSquared * _time;
        return 0.5f * (
            (2.0f * current)
            + (-previous + next) * _time
            + (2.0f * previous - 5.0f * current
                + 4.0f * next - following) * timeSquared
            + (-previous + 3.0f * current
                - 3.0f * next + following) * timeCubed);
    }

    private int GetSplinePointIndex(int _index)
    {
        if (b_m_loopSpline)
        {
            return (_index % m_splinePoints.Length + m_splinePoints.Length)
                % m_splinePoints.Length;
        }
        return Mathf.Clamp(_index, 0, m_splinePoints.Length - 1);
    }

    private void OnValidate()
    {
        b_m_splineCacheDirty = true;
        m_splineSpeed = Mathf.Max(0.01f, m_splineSpeed);
    }

    private void OnDestroy()
    {
        UnsubscribeCameraRendering();
        RestoreMonitorRenderers();
        ClearMonitorTexture();
        for (int i = 0; i < m_monitorTextures.Count; ++i)
        {
            RenderTexture monitorTexture = m_monitorTextures[i];
            if (monitorTexture == null)continue;
            monitorTexture.Release();
            Destroy(monitorTexture);
        }
        m_monitorTextures.Clear();
        if (m_droneMaterial != null)
        {
            Destroy(m_droneMaterial);
            m_droneMaterial = null;
        }
    }

    private void CreateDrones()
    {
        int droneCount = Mathf.Clamp(m_droneCount, 1, 3);
        DroneSplineRoute[] sceneRoutes =
            GetComponentsInChildren<DroneSplineRoute>(true);
        Array.Sort(
            sceneRoutes,
            (_left, _right) => string.Compare(
                _left.name,
                _right.name,
                StringComparison.OrdinalIgnoreCase));
        for (int i = 0; i < droneCount; ++i)
        {
            DroneSplineRoute route;
            GameObject droneObject;
            if (i < sceneRoutes.Length && sceneRoutes[i] != null)
            {
                route = sceneRoutes[i];
                droneObject = route.gameObject;
            }
            else
            {
                droneObject = new GameObject($"{EDroneObjectName}_{i + 1}");
                droneObject.transform.SetParent(transform, false);
                route = droneObject.AddComponent<DroneSplineRoute>();
            }
            m_droneTransforms.Add(droneObject.transform);
            m_droneRoutes.Add(route);

            GameObject cameraObject = new GameObject(
                $"{EDroneCameraName}_{i + 1}");
            cameraObject.transform.SetParent(droneObject.transform, false);
            cameraObject.transform.localPosition =
                new Vector3(0.0f, -0.15f, 0.55f);
            Camera droneCamera = cameraObject.AddComponent<Camera>();
            droneCamera.fieldOfView = m_fieldOfView;
            droneCamera.nearClipPlane = m_nearClipPlane;
            droneCamera.farClipPlane = m_farClipPlane;
            droneCamera.cullingMask = m_cameraCullingMask;
            droneCamera.depth = -10.0f - i;
            droneCamera.allowHDR = true;
            droneCamera.allowMSAA = true;
            m_droneCameras.Add(droneCamera);

            if (b_m_createDroneVisual)
            {
                CreateDroneVisual(droneObject.transform);
            }
        }
    }

    private void CreateMonitorTextures()
    {
        int width = Mathf.Max(EMinimumTextureSize, m_textureWidth);
        int height = Mathf.Max(EMinimumTextureSize, m_textureHeight);
        for (int i = 0; i < m_droneCameras.Count; ++i)
        {
            RenderTexture monitorTexture = new RenderTexture(
                width,
                height,
                24,
                RenderTextureFormat.ARGB32)
            {
                name = $"DroneVenueView_{i + 1}",
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false
            };
            monitorTexture.Create();
            m_droneCameras[i].targetTexture = monitorTexture;
            m_monitorTextures.Add(monitorTexture);
        }
    }

    private void FindMonitorMaterialSlots()
    {
        m_monitorSlotsList.Clear();
        string keyword = string.IsNullOrWhiteSpace(m_monitorMaterialKeyword)
            ? EDefaultMonitorKeyword
            : m_monitorMaterialKeyword.Trim();
        Renderer[] renderers = FindObjectsByType<Renderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; ++i)
        {
            Material[] materials = renderers[i].sharedMaterials;
            for (int j = 0; j < materials.Length; ++j)
            {
                Material material = materials[j];
                bool b_isMonitorMaterial = material != null
                    && material.name.IndexOf(
                        keyword,
                        StringComparison.OrdinalIgnoreCase) >= 0;
                if (!b_isMonitorMaterial)continue;

                m_monitorSlotsList.Add(new MonitorMaterialSlot
                {
                    Renderer = renderers[i],
                    MaterialIndex = j,
                    OriginalMaterial = material,
                    RuntimeMaterial = null
                });
            }

        }

        if (m_monitorSlotsList.Count == 0)
        {
            Debug.LogWarning(
                $"Drone Viewing: Material名に「{keyword}」を含むMonitorが見つかりません。",
                this);
        }
        m_monitorSlotsList.Sort(CompareMonitorSlots);
    }

    private static int CompareMonitorSlots(
        MonitorMaterialSlot _left,
        MonitorMaterialSlot _right)
    {
        string leftName = _left.OriginalMaterial != null
            ? _left.OriginalMaterial.name
            : string.Empty;
        string rightName = _right.OriginalMaterial != null
            ? _right.OriginalMaterial.name
            : string.Empty;
        return string.Compare(leftName, rightName, StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyMonitorTexture()
    {
        for (int i = 0; i < m_monitorSlotsList.Count; ++i)
        {
            MonitorMaterialSlot slot = m_monitorSlotsList[i];
            if (slot.Renderer == null || slot.OriginalMaterial == null
                || m_monitorTextures.Count == 0)continue;

            RenderTexture monitorTexture =
                m_monitorTextures[i % m_monitorTextures.Count];

            Material runtimeMaterial = new Material(slot.OriginalMaterial)
            {
                name = $"{slot.OriginalMaterial.name} Drone View"
            };
            SetTextureIfSupported(
                runtimeMaterial,
                EBaseMapProperty,
                monitorTexture);
            SetTextureIfSupported(
                runtimeMaterial,
                EMainTextureProperty,
                monitorTexture);
            if (b_m_enableEmission)
            {
                runtimeMaterial.EnableKeyword("_EMISSION");
                SetTextureIfSupported(
                    runtimeMaterial,
                    EEmissionMapProperty,
                    monitorTexture);
                if (runtimeMaterial.HasProperty(EEmissionColorProperty))
                {
                    runtimeMaterial.SetColor(
                        EEmissionColorProperty,
                        Color.white * m_emissionIntensity);
                }
            }

            Material[] materials = slot.Renderer.sharedMaterials;
            if (slot.MaterialIndex < 0 || slot.MaterialIndex >= materials.Length)
            {
                Destroy(runtimeMaterial);
                continue;
            }
            materials[slot.MaterialIndex] = runtimeMaterial;
            slot.Renderer.sharedMaterials = materials;
            slot.RuntimeMaterial = runtimeMaterial;
            m_monitorSlotsList[i] = slot;
        }
    }

    private static void SetTextureIfSupported(
        Material _material,
        string _propertyName,
        Texture _texture)
    {
        if (_material.HasProperty(_propertyName))
        {
            _material.SetTexture(_propertyName, _texture);
        }
    }

    private void ClearMonitorTexture()
    {
        for (int i = 0; i < m_monitorSlotsList.Count; ++i)
        {
            MonitorMaterialSlot slot = m_monitorSlotsList[i];
            if (slot.Renderer != null)
            {
                Material[] materials = slot.Renderer.sharedMaterials;
                if (slot.MaterialIndex >= 0
                    && slot.MaterialIndex < materials.Length)
                {
                    materials[slot.MaterialIndex] = slot.OriginalMaterial;
                    slot.Renderer.sharedMaterials = materials;
                }
            }
            if (slot.RuntimeMaterial != null)Destroy(slot.RuntimeMaterial);
        }
        m_monitorSlotsList.Clear();
    }

    /// <summary>
    /// Drone映像内へMonitor自身が映り込む再帰描画だけを防ぎます。
    /// </summary>
    private void SubscribeCameraRendering()
    {
        Camera.onPreCull += HandleCameraPreCull;
        Camera.onPostRender += HandleCameraPostRender;
        RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
    }

    private void UnsubscribeCameraRendering()
    {
        Camera.onPreCull -= HandleCameraPreCull;
        Camera.onPostRender -= HandleCameraPostRender;
        RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
        RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;
    }

    private void HandleCameraPreCull(Camera _camera)
    {
        if (IsDroneCamera(_camera))HideMonitorRenderers();
    }

    private void HandleCameraPostRender(Camera _camera)
    {
        if (IsDroneCamera(_camera))RestoreMonitorRenderers();
    }

    private void HandleBeginCameraRendering(
        ScriptableRenderContext _context,
        Camera _camera)
    {
        if (IsDroneCamera(_camera))HideMonitorRenderers();
    }

    private void HandleEndCameraRendering(
        ScriptableRenderContext _context,
        Camera _camera)
    {
        if (IsDroneCamera(_camera))RestoreMonitorRenderers();
    }

    private bool IsDroneCamera(Camera _camera)
    {
        return _camera != null && m_droneCameras.Contains(_camera);
    }

    public bool IsVisibleFromDroneCamera(Bounds _worldBounds)
    {
        for (int i = 0; i < m_droneCameras.Count; ++i)
        {
            Camera droneCamera = m_droneCameras[i];
            if (droneCamera == null || !droneCamera.isActiveAndEnabled)continue;
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(droneCamera);
            if (GeometryUtility.TestPlanesAABB(planes, _worldBounds))return true;
        }
        return false;
    }

    private void HideMonitorRenderers()
    {
        if (b_m_monitorRenderersHidden)return;

        m_monitorOriginalForceRenderingOff.Clear();
        for (int i = 0; i < m_monitorSlotsList.Count; ++i)
        {
            Renderer renderer = m_monitorSlotsList[i].Renderer;
            if (renderer == null
                || m_monitorOriginalForceRenderingOff.ContainsKey(renderer))continue;

            m_monitorOriginalForceRenderingOff.Add(
                renderer,
                renderer.forceRenderingOff);
            renderer.forceRenderingOff = true;
        }
        b_m_monitorRenderersHidden = true;
    }

    private void RestoreMonitorRenderers()
    {
        if (!b_m_monitorRenderersHidden)return;

        foreach (KeyValuePair<Renderer, bool> rendererEntry
            in m_monitorOriginalForceRenderingOff)
        {
            if (rendererEntry.Key != null)
            {
                rendererEntry.Key.forceRenderingOff = rendererEntry.Value;
            }
        }
        m_monitorOriginalForceRenderingOff.Clear();
        b_m_monitorRenderersHidden = false;
    }

    private Vector3 GetLookAtPosition()
    {
        return m_lookAtTarget != null
            ? m_lookAtTarget.position
            : m_lookAtPosition;
    }

    private void CreateDroneVisual(Transform _parent)
    {
        Shader droneShader = Shader.Find("Universal Render Pipeline/Lit");
        if (droneShader == null)droneShader = Shader.Find("Standard");
        if (m_droneMaterial == null)
        {
            m_droneMaterial = new Material(droneShader);
            m_droneMaterial.color = m_droneColor;
        }

        CreateVisualPrimitive(
            PrimitiveType.Sphere,
            "DroneBody",
            _parent,
            Vector3.zero,
            new Vector3(1.2f, 0.35f, 0.8f),
            m_droneMaterial);
        CreateVisualPrimitive(
            PrimitiveType.Cube,
            "DroneArmHorizontal",
            _parent,
            Vector3.zero,
            new Vector3(2.4f, 0.08f, 0.12f),
            m_droneMaterial);
        CreateVisualPrimitive(
            PrimitiveType.Cube,
            "DroneArmVertical",
            _parent,
            Vector3.zero,
            new Vector3(0.12f, 0.08f, 2.0f),
            m_droneMaterial);

        Vector3[] rotorPositions =
        {
            new Vector3(-1.05f, 0.05f, -0.85f),
            new Vector3(1.05f, 0.05f, -0.85f),
            new Vector3(-1.05f, 0.05f, 0.85f),
            new Vector3(1.05f, 0.05f, 0.85f)
        };
        for (int i = 0; i < rotorPositions.Length; ++i)
        {
            CreateVisualPrimitive(
                PrimitiveType.Cylinder,
                $"Rotor_{i + 1}",
                _parent,
                rotorPositions[i],
                new Vector3(0.45f, 0.025f, 0.45f),
                m_droneMaterial);
        }
    }

    private static void CreateVisualPrimitive(
        PrimitiveType _primitiveType,
        string _name,
        Transform _parent,
        Vector3 _localPosition,
        Vector3 _localScale,
        Material _material)
    {
        GameObject visual = GameObject.CreatePrimitive(_primitiveType);
        visual.name = _name;
        visual.transform.SetParent(_parent, false);
        visual.transform.localPosition = _localPosition;
        visual.transform.localScale = _localScale;
        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)Destroy(collider);
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)renderer.sharedMaterial = _material;
    }

    private void OnDrawGizmosSelected()
    {
        if (b_m_useSplineFlight && b_m_showSpline
            && m_splinePoints != null && m_splinePoints.Length >= 2)
        {
            DrawSplineGizmos();
            return;
        }

        Vector3 targetPosition = GetLookAtPosition();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(
            targetPosition + Vector3.up * m_flightHeight,
            Mathf.Max(0.0f, m_orbitRadius));
        Gizmos.DrawLine(transform.position, targetPosition);
    }

    private void DrawSplineGizmos()
    {
        int segmentCount = b_m_loopSpline
            ? m_splinePoints.Length
            : m_splinePoints.Length - 1;
        Gizmos.color = Color.cyan;
        for (int i = 0; i < m_splinePoints.Length; ++i)
        {
            Gizmos.DrawSphere(transform.TransformPoint(m_splinePoints[i]), 0.25f);
        }
        for (int i = 0; i < segmentCount; ++i)
        {
            Vector3 previous = transform.TransformPoint(
                EvaluateSplineSegment(i, 0.0f));
            for (int j = 1; j <= ESplineSamplesPerSegment; ++j)
            {
                Vector3 current = transform.TransformPoint(
                    EvaluateSplineSegment(
                        i,
                        (float)j / ESplineSamplesPerSegment));
                Gizmos.DrawLine(previous, current);
                previous = current;
            }
        }
    }
}
