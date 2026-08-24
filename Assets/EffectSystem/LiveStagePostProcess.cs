using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Gameplay専用のBloomとColor Gradingを実行時に構築します。
/// Scene Assetを直接編集せず、通常GameplayとEffect Debugで同じ見た目を使えます。
/// </summary>
[DisallowMultipleComponent]
public sealed class LiveStagePostProcess : MonoBehaviour
{
    private const string EVolumeName = "Live Stage Post Process";
    private const float EVolumePriority = 100.0f;

    [Header("Bloom")]
    [SerializeField, Min(0.0f)] private float m_bloomThreshold = 0.85f;
    [SerializeField, Min(0.0f)] private float m_bloomIntensity = 1.1f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_bloomScatter = 0.72f;
    [SerializeField] private bool b_m_highQualityFiltering = true;

    [Header("Color Adjustments")]
    [SerializeField, Range(-10.0f, 10.0f)] private float m_postExposure = 0.08f;
    [SerializeField, Range(-100.0f, 100.0f)] private float m_contrast = 18.0f;
    [SerializeField, Range(-100.0f, 100.0f)] private float m_saturation = 8.0f;
    [SerializeField] private Color m_colorFilter =
        new Color(1.0f, 0.98f, 0.97f, 1.0f);

    private VolumeProfile m_runtimeProfile;
    private Bloom m_bloom;
    private ColorAdjustments m_colorAdjustments;

    /// <summary>Scene内に設定がなければ指定Objectへ追加します。</summary>
    public static LiveStagePostProcess GetOrCreate(GameObject _owner)
    {
        LiveStagePostProcess presentation =
            FindFirstObjectByType<LiveStagePostProcess>();
        if (presentation != null)return presentation;
        if (_owner == null)return null;

        return _owner.AddComponent<LiveStagePostProcess>();
    }

    private void Awake()
    {
        CreateGlobalVolume();
        EnableCameraPostProcessing();
    }

    private void CreateGlobalVolume()
    {
        GameObject volumeObject = new GameObject(EVolumeName);
        volumeObject.transform.SetParent(transform, false);
        Volume volume = volumeObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = EVolumePriority;
        volume.weight = 1.0f;

        m_runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        m_runtimeProfile.name = "Live Stage Runtime Profile";
        volume.sharedProfile = m_runtimeProfile;

        m_bloom = m_runtimeProfile.Add<Bloom>(true);
        m_colorAdjustments = m_runtimeProfile.Add<ColorAdjustments>(true);
        ApplySettings();
    }

    /// <summary>Inspectorに保存されたBloomと色調補正を実行中Profileへ反映します。</summary>
    private void ApplySettings()
    {
        if (m_bloom != null)
        {
            m_bloom.active = true;
            m_bloom.threshold.Override(Mathf.Max(0.0f, m_bloomThreshold));
            m_bloom.intensity.Override(Mathf.Max(0.0f, m_bloomIntensity));
            m_bloom.scatter.Override(Mathf.Clamp01(m_bloomScatter));
            m_bloom.highQualityFiltering.Override(b_m_highQualityFiltering);
        }

        if (m_colorAdjustments != null)
        {
            m_colorAdjustments.active = true;
            m_colorAdjustments.postExposure.Override(m_postExposure);
            m_colorAdjustments.contrast.Override(m_contrast);
            m_colorAdjustments.saturation.Override(m_saturation);
            m_colorAdjustments.colorFilter.Override(m_colorFilter);
        }
    }

    private void OnValidate()
    {
        ApplySettings();
    }

    private static void EnableCameraPostProcessing()
    {
        Camera[] cameras = FindObjectsByType<Camera>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; ++i)
        {
            UniversalAdditionalCameraData cameraData =
                cameras[i].GetUniversalAdditionalCameraData();
            cameraData.renderPostProcessing = true;
        }
    }

    private void OnDestroy()
    {
        if (m_runtimeProfile != null)
        {
            Destroy(m_runtimeProfile);
        }
    }
}
