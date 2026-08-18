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

    private VolumeProfile m_runtimeProfile;

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

        Bloom bloom = m_runtimeProfile.Add<Bloom>(true);
        bloom.active = true;
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(0.72f);
        bloom.highQualityFiltering.Override(true);

        ColorAdjustments color =
            m_runtimeProfile.Add<ColorAdjustments>(true);
        color.active = true;
        color.postExposure.Override(0.08f);
        color.contrast.Override(18.0f);
        color.saturation.Override(8.0f);
        color.colorFilter.Override(
            new Color(1.0f, 0.98f, 0.97f, 1.0f));
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
