/*━━━━━━━━━*
*@file RealtimeCameraUI.cs*
*@brief ポーズ認識の実映像をUIの枠に切り取って共有表示する*
*@date 2026/09/18*
*━━━━━━━━━*/
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public sealed class RealtimeCameraUI : MonoBehaviour
{
    private const string EShaderName = "MuscleBeat/RealtimeCameraUI";
    private const float EMinimumSize = 0.0001f;
    private const float EHalfSize = 0.5f;
    private const int ERightAngleDegrees = 90;
    private const int EHalfTurnDegrees = 180;
    private static readonly int EFlipXId = Shader.PropertyToID("_FlipX");
    private static readonly int EFlipYId = Shader.PropertyToID("_FlipY");
    private static readonly int ERotationId = Shader.PropertyToID("_RotationRadians");
    private static readonly int EViewRectId = Shader.PropertyToID("_ViewRect");

    [SerializeField] private bool b_m_mirrorHorizontally = true;
    [SerializeField] private bool b_m_cropToSurface = true;
    [SerializeField] private Vector2 m_viewCenter = new Vector2(EHalfSize, EHalfSize);
    [Min(1.0f)] [SerializeField] private float m_viewZoom = 1.0f;

    private RawImage m_image;
    private Material m_baseMaterial;
    private Material m_runtimeMaterial;

    private void OnEnable()
    {
        m_image = GetComponent<RawImage>();
        m_image.enabled = false;
        m_baseMaterial = m_image.material;
        if (m_baseMaterial == null || m_baseMaterial.shader == null || m_baseMaterial.shader.name != EShaderName)
        {
            Debug.LogWarning("[RealtimeCameraUI] RawImageにRealtimeCameraUIのMaterialを設定してください。", this);
            return;
        }
        //各UIに独立したMaterialを用意し、複数の窓で別々の表示範囲を設定できます。
        m_runtimeMaterial = new Material(m_baseMaterial);
        m_image.material = m_runtimeMaterial;
    }

    private void LateUpdate()
    {
        if (m_runtimeMaterial == null) { return; }
        ImageSource source = ImageSourceProvider.ImageSource;
        if (source == null || !source.isPrepared)
        {
            m_image.enabled = false;
            return;
        }
        Texture texture = source.GetCurrentTexture();
        if (texture == null || texture.width <= 0 || texture.height <= 0)
        {
            m_image.enabled = false;
            return;
        }
        int rotationDegrees = (int)source.rotation;
        float imageAspect = (float)texture.width / texture.height;
        if (rotationDegrees % EHalfTurnDegrees == ERightAngleDegrees)
        {
            imageAspect = (float)texture.height / texture.width;
        }
        Rect rect = m_image.rectTransform.rect;
        float surfaceWidth = Mathf.Abs(rect.width) * transform.TransformVector(Vector3.right).magnitude;
        float surfaceHeight = Mathf.Abs(rect.height) * transform.TransformVector(Vector3.up).magnitude;
        float surfaceAspect = surfaceWidth / Mathf.Max(EMinimumSize, surfaceHeight);
        Vector2 viewSize = Vector2.one;
        if (b_m_cropToSurface)
        {
            if (surfaceAspect < imageAspect)
            {
                viewSize.x = Mathf.Max(EMinimumSize, surfaceAspect / imageAspect);
            }
            else
            {
                viewSize.y = imageAspect / Mathf.Max(EMinimumSize, surfaceAspect);
            }
        }
        viewSize /= Mathf.Max(1.0f, m_viewZoom);
        Vector2 halfView = viewSize * EHalfSize;
        Vector4 viewRect = new Vector4(
            viewSize.x,
            viewSize.y,
            Mathf.Clamp(m_viewCenter.x, halfView.x, 1.0f - halfView.x) - halfView.x,
            Mathf.Clamp(m_viewCenter.y, halfView.y, 1.0f - halfView.y) - halfView.y);
        float flipX = 0.0f;
        float flipY = 0.0f;
        if (b_m_mirrorHorizontally)
        {
            flipX = 1.0f;
        }
        if (source.isVerticallyFlipped)
        {
            flipY = 1.0f;
        }
        m_image.texture = texture;
        m_image.enabled = true;
        SetViewProperties(
            m_runtimeMaterial,
            viewRect,
            flipX,
            flipY,
            rotationDegrees * Mathf.Deg2Rad);
        //Maskによって複製されるStencil用Materialにも同じ設定を渡します。
        Material renderMaterial = m_image.materialForRendering;
        if (renderMaterial != m_runtimeMaterial)
        {
            SetViewProperties(
                renderMaterial,
                viewRect,
                flipX,
                flipY,
                rotationDegrees * Mathf.Deg2Rad);
        }
    }

    private static void SetViewProperties(
        Material _material,
        Vector4 _viewRect,
        float _flipX,
        float _flipY,
        float _rotation)
    {
        _material.SetVector(EViewRectId, _viewRect);
        _material.SetFloat(EFlipXId, _flipX);
        _material.SetFloat(EFlipYId, _flipY);
        _material.SetFloat(ERotationId, _rotation);
    }

    private void OnDisable()
    {
        if (m_image != null)
        {
            m_image.enabled = false;
            m_image.texture = null;
            m_image.material = m_baseMaterial;
        }
        if (m_runtimeMaterial != null)
        {
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
        //共有カメラTextureの停止・破棄は行いません。
    }
}
