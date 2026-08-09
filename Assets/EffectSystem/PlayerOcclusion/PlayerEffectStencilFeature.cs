using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Player Effect Mask LayerのRendererをStencilへ記録し、透過Effectから除外できるようにします。
/// </summary>
public sealed class PlayerEffectStencilFeature : ScriptableRendererFeature
{
    [SerializeField] private LayerMask m_playerLayerMask = 1 << 7;
    [SerializeField] private Shader m_stencilMaskShader;

    private RenderObjectsPass m_maskPass;

    public override void Create()
    {
        if (m_stencilMaskShader == null)
        {
            m_maskPass = null;
            return;
        }

        string[] playerShaderPasses =
        {
            "UniversalGBuffer",
            "UniversalForward",
            "UniversalForwardOnly",
            "SRPDefaultUnlit"
        };
        m_maskPass = new RenderObjectsPass(
            "Player Effect Stencil Mask",
            RenderPassEvent.AfterRenderingOpaques,
            playerShaderPasses,
            RenderQueueType.Opaque,
            m_playerLayerMask,
            new RenderObjects.CustomCameraSettings());
        m_maskPass.overrideShader = m_stencilMaskShader;
        m_maskPass.overrideShaderPassIndex = 0;
        m_maskPass.SetDepthState(false, CompareFunction.LessEqual);
    }

    public override void AddRenderPasses(
        ScriptableRenderer _renderer,
        ref RenderingData _renderingData)
    {
        if (m_maskPass == null)return;
        if (_renderingData.cameraData.cameraType == CameraType.Preview)return;
        _renderer.EnqueuePass(m_maskPass);
    }
}
