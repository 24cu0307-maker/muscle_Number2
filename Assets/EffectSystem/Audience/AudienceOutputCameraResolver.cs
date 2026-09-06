using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Game画面へ出力しているCameraを観客表示用に取得します。
/// </summary>
public static class AudienceOutputCameraResolver
{
    private const int EGameDisplayIndex = 0;
    private const float ECameraRefreshIntervalSeconds = 0.25f;

    private static Camera m_cachedCamera;
    private static int m_cachedFrame = -1;
    private static float m_nextCameraRefreshTime;

    public static Camera GetCurrent(Camera _fallback = null)
    {
        if (m_cachedFrame == Time.frameCount && IsGameOutputCamera(m_cachedCamera))
        {
            return m_cachedCamera;
        }

        if (IsGameOutputCamera(m_cachedCamera)
            && Time.unscaledTime < m_nextCameraRefreshTime)
        {
            m_cachedFrame = Time.frameCount;
            return m_cachedCamera;
        }

        m_cachedFrame = Time.frameCount;
        m_nextCameraRefreshTime =
            Time.unscaledTime + ECameraRefreshIntervalSeconds;
        m_cachedCamera = FindCinemachineOutputCamera();
        if (m_cachedCamera == null && IsGameOutputCamera(Camera.main))
        {
            m_cachedCamera = Camera.main;
        }
        if (m_cachedCamera == null)
        {
            m_cachedCamera = FindHighestDepthOutputCamera();
        }
        if (m_cachedCamera == null && IsGameOutputCamera(_fallback))
        {
            m_cachedCamera = _fallback;
        }

        return m_cachedCamera;
    }

    private static Camera FindCinemachineOutputCamera()
    {
        CinemachineBrain[] brains = Object.FindObjectsByType<CinemachineBrain>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        Camera selectedCamera = null;
        for (int i = 0; i < brains.Length; ++i)
        {
            CinemachineBrain brain = brains[i];
            if (brain == null || !brain.isActiveAndEnabled)continue;

            Camera camera = brain.GetComponent<Camera>();
            if (!IsGameOutputCamera(camera))continue;
            if (selectedCamera == null || camera.depth > selectedCamera.depth)
            {
                selectedCamera = camera;
            }
        }
        return selectedCamera;
    }

    private static Camera FindHighestDepthOutputCamera()
    {
        Camera selectedCamera = null;
        Camera[] cameras = Camera.allCameras;
        for (int i = 0; i < cameras.Length; ++i)
        {
            Camera camera = cameras[i];
            if (!IsGameOutputCamera(camera))continue;
            if (selectedCamera == null || camera.depth > selectedCamera.depth)
            {
                selectedCamera = camera;
            }
        }
        return selectedCamera;
    }

    private static bool IsGameOutputCamera(Camera _camera)
    {
        return _camera != null
            && _camera.isActiveAndEnabled
            && _camera.cameraType == CameraType.Game
            && _camera.targetTexture == null
            && _camera.targetDisplay == EGameDisplayIndex;
    }
}
