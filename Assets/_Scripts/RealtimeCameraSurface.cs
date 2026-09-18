using Mediapipe.Unity.Sample;
using Mediapipe.Unity;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public sealed class RealtimeCameraSurface : MonoBehaviour
{
    private const float EDefaultWidth = 2.0f;
    private const string EPreviewLayerName = "LiveCameraPreview";
    private const string EMonitorBodyName = "MonitorBody";
    private const int ERightAngleDegrees = 90;
    private const int EHalfTurnDegrees = 180;
    private const float EMinimumSurfaceSize = 0.0001f;
    private const float EHalfSize = 0.5f;
    private static readonly int ETextureId = Shader.PropertyToID("_MainTex");
    private static readonly int EFlipXId = Shader.PropertyToID("_FlipX");
    private static readonly int EFlipYId = Shader.PropertyToID("_FlipY");
    private static readonly int ERotationId = Shader.PropertyToID("_RotationRadians");
    private static readonly int EViewRectId = Shader.PropertyToID("_ViewRect");

    [SerializeField] private bool b_m_mirrorHorizontally = true;
    [SerializeField] private bool b_m_keepAspectRatio = true;
    [Min(0.01f)] [SerializeField] private float m_width = EDefaultWidth;
    [SerializeField] private bool b_m_cropToSurface = true; //枠に合わせて切り取り、映像の歪みを防ぐ
    [SerializeField] private Vector2 m_viewCenter = new Vector2(EHalfSize, EHalfSize);
    [Min(1.0f)] [SerializeField] private float m_viewZoom = 1.0f;

    private MeshRenderer m_renderer;
    private MaterialPropertyBlock m_properties;

    private void Awake()
    {
        m_renderer = GetComponent<MeshRenderer>();
        m_properties = new MaterialPropertyBlock();
        m_renderer.enabled = false;
        int previewLayer = LayerMask.NameToLayer(EPreviewLayerName);
        if (previewLayer >= 0)
        {
            gameObject.layer = previewLayer;
            if (transform.parent != null)
            {
                Transform body = transform.parent.Find(EMonitorBodyName);
                if (body != null)
                {
                    body.gameObject.layer = previewLayer;
                    transform.parent.gameObject.layer = previewLayer;
                }
            }
        }
    }

    private void LateUpdate()
    {
        ImageSource source = ImageSourceProvider.ImageSource;
        if (source == null || !source.isPrepared)
        {
            m_renderer.enabled = false;
            return;
        }
        Texture texture = source.GetCurrentTexture();
        if (texture == null || texture.width <= 0 || texture.height <= 0)
        {
            m_renderer.enabled = false;
            return;
        }
        m_renderer.enabled = true;
        int rotationDegrees = (int)source.rotation;
        float horizontalFlip = 0.0f;
        float verticalFlip = 0.0f;
        if (b_m_mirrorHorizontally)
        {
            horizontalFlip = 1.0f;
        }
        if (source.isVerticallyFlipped)
        {
            verticalFlip = 1.0f;
        }
        m_properties.SetTexture(ETextureId, texture);
        m_properties.SetFloat(EFlipXId, horizontalFlip);
        m_properties.SetFloat(EFlipYId, verticalFlip);
        m_properties.SetFloat(ERotationId, rotationDegrees * Mathf.Deg2Rad);
        float widthPixels = texture.width;
        float heightPixels = texture.height;
        if (rotationDegrees % EHalfTurnDegrees == ERightAngleDegrees)
        {
            widthPixels = texture.height;
            heightPixels = texture.width;
        }
        if (b_m_keepAspectRatio)
        {
            transform.localScale = new Vector3(
                m_width,
                m_width * heightPixels / widthPixels,
                1.0f);
        }
        Vector2 viewSize = Vector2.one;
        if (b_m_cropToSurface)
        {
            //親Objectの縦横別Scaleも含めた実際の表示面の比率を使います。
            float surfaceWidth = transform.TransformVector(Vector3.right).magnitude;
            float surfaceHeight = transform.TransformVector(Vector3.up).magnitude;
            float surfaceAspect = surfaceWidth / Mathf.Max(EMinimumSurfaceSize, surfaceHeight);
            float imageAspect = widthPixels / heightPixels;
            if (surfaceAspect < imageAspect)
            {
                viewSize.x = Mathf.Max(EMinimumSurfaceSize, surfaceAspect / imageAspect);
            }
            else
            {
                viewSize.y = imageAspect / Mathf.Max(EMinimumSurfaceSize, surfaceAspect);
            }
        }
        viewSize /= Mathf.Max(1.0f, m_viewZoom);
        Vector2 halfView = viewSize * EHalfSize;
        float centerX = Mathf.Clamp(m_viewCenter.x, halfView.x, 1.0f - halfView.x);
        float centerY = Mathf.Clamp(m_viewCenter.y, halfView.y, 1.0f - halfView.y);
        m_properties.SetVector(EViewRectId, new Vector4(
            viewSize.x,
            viewSize.y,
            centerX - halfView.x,
            centerY - halfView.y));
        m_renderer.SetPropertyBlock(m_properties);
    }

    private void OnDisable()
    {
        if (m_renderer != null)
        {
            m_renderer.enabled = false;
        }
        //共有Textureの停止・破棄は認識処理側が担当します。
    }
}
