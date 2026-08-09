/*━━━━━━━━━*
*@file RectangularLightCookie.cs*
*@brief URP Spot Lightへ四角形Cookieを適用する*
*@remarks URP非対応のPyramid Lightを使わず、機能するSpot Lightで矩形照射を作る*
*━━━━━━━━━*/

using UnityEngine;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public sealed class RectangularLightCookie : MonoBehaviour
{
    private const int ECookieResolution = 64;

    [SerializeField, Range(0.1f, 1.0f)] private float m_widthRatio = 1.0f;
    [SerializeField, Range(0.1f, 1.0f)] private float m_heightRatio = 0.62f;
    [SerializeField, Range(0.0f, 0.3f)] private float m_edgeSoftness = 0.08f;

    private Light m_targetLight;
    private Texture2D m_runtimeCookie;

    private void OnEnable()
    {
        ApplyCookie();
    }

    private void OnValidate()
    {
        if (!isActiveAndEnabled)return;
        ApplyCookie();
    }

    private void OnDisable()
    {
        if (m_targetLight != null && m_targetLight.cookie == m_runtimeCookie)
        {
            m_targetLight.cookie = null;
        }
        DestroyRuntimeCookie();
    }

    private void ApplyCookie()
    {
        if (m_targetLight == null)m_targetLight = GetComponent<Light>();
        if (m_targetLight == null)return;

        //URPはPyramidを実照明として処理しないため、対応済みのSpotを使用します。
        m_targetLight.type = LightType.Spot;
        RebuildCookie();
        m_targetLight.cookie = m_runtimeCookie;
    }

    private void RebuildCookie()
    {
        DestroyRuntimeCookie();
        m_runtimeCookie = new Texture2D(
            ECookieResolution,
            ECookieResolution,
            TextureFormat.RGBA32,
            false,
            true)
        {
            name = "Rectangular Light Runtime Cookie",
            hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color[] pixels = new Color[ECookieResolution * ECookieResolution];
        float width = Mathf.Max(0.1f, m_widthRatio);
        float height = Mathf.Max(0.1f, m_heightRatio);
        float softness = Mathf.Max(0.001f, m_edgeSoftness);
        for (int y = 0; y < ECookieResolution; ++y)
        {
            for (int x = 0; x < ECookieResolution; ++x)
            {
                float normalizedX = Mathf.Abs((x + 0.5f) / ECookieResolution * 2.0f - 1.0f) / width;
                float normalizedY = Mathf.Abs((y + 0.5f) / ECookieResolution * 2.0f - 1.0f) / height;
                float edgeDistance = Mathf.Max(normalizedX, normalizedY);
                float brightness = 1.0f - Mathf.SmoothStep(
                    1.0f - softness,
                    1.0f,
                    edgeDistance);
                pixels[y * ECookieResolution + x] = new Color(
                    brightness,
                    brightness,
                    brightness,
                    1.0f);
            }
        }
        m_runtimeCookie.SetPixels(pixels);
        m_runtimeCookie.Apply(false, true);
    }

    private void DestroyRuntimeCookie()
    {
        if (m_runtimeCookie == null)return;
        if (Application.isPlaying)Destroy(m_runtimeCookie);
        else DestroyImmediate(m_runtimeCookie);
        m_runtimeCookie = null;
    }
}
