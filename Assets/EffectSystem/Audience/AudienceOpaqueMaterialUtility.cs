using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// FBX内蔵Materialを観客用の不透明描画へ正規化します。
/// 透明QueueやZWrite無効のMaterialでは胴体がDepthへ書き込まれず、
/// 本来胴体の後ろにある手まで見えるため、生成時に一度だけ補正します。
/// </summary>
public static class AudienceOpaqueMaterialUtility
{
    private static readonly int ESurfaceId = Shader.PropertyToID("_Surface");
    private static readonly int EZWriteId = Shader.PropertyToID("_ZWrite");
    private static readonly int ESrcBlendId = Shader.PropertyToID("_SrcBlend");
    private static readonly int EDstBlendId = Shader.PropertyToID("_DstBlend");
    private static readonly int EAlphaClipId = Shader.PropertyToID("_AlphaClip");
    private static readonly int EModeId = Shader.PropertyToID("_Mode");

    // 同じFBX Materialからは一つだけ補正版を作り、全観客で共有します。
    private static readonly Dictionary<Material, Material> EOpaqueMaterials =
        new Dictionary<Material, Material>();

    /// <summary>
    /// 観客本体に含まれるRendererを不透明Materialへ差し替えます。
    /// AudiencePenlightはこの呼び出しより後に生成されるため対象になりません。
    /// </summary>
    public static void Apply(GameObject _audienceObject)
    {
        if (_audienceObject == null)return;

        Renderer[] renderers =
            _audienceObject.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; ++i)
        {
            Material[] materials = renderers[i].sharedMaterials;
            bool b_changed = false;
            for (int j = 0; j < materials.Length; ++j)
            {
                Material source = materials[j];
                if (source == null || IsEffectMaterial(source))continue;

                materials[j] = GetOrCreateOpaqueMaterial(source);
                b_changed = true;
            }

            if (b_changed)
            {
                renderers[i].sharedMaterials = materials;
            }
        }
    }

    /// <summary>元のTextureや色を維持したまま、描画状態だけ不透明へ変更します。</summary>
    private static Material GetOrCreateOpaqueMaterial(Material _source)
    {
        if (EOpaqueMaterials.TryGetValue(_source, out Material cached)
            && cached != null)
        {
            return cached;
        }

        Material opaque = new Material(_source)
        {
            name = _source.name + " (Audience Opaque)",
            hideFlags = HideFlags.HideAndDontSave,
            renderQueue = (int)RenderQueue.Geometry
        };
        opaque.SetOverrideTag("RenderType", "Opaque");
        SetFloatIfPresent(opaque, ESurfaceId, 0.0f);
        SetFloatIfPresent(opaque, EModeId, 0.0f);
        SetFloatIfPresent(opaque, EZWriteId, 1.0f);
        SetFloatIfPresent(opaque, ESrcBlendId, (float)BlendMode.One);
        SetFloatIfPresent(opaque, EDstBlendId, (float)BlendMode.Zero);
        SetFloatIfPresent(opaque, EAlphaClipId, 0.0f);
        opaque.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        opaque.DisableKeyword("_ALPHATEST_ON");
        opaque.DisableKeyword("_ALPHABLEND_ON");
        opaque.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        EOpaqueMaterials[_source] = opaque;
        return opaque;
    }

    /// <summary>加算Effectなど、透明描画を維持すべきMaterialを除外します。</summary>
    private static bool IsEffectMaterial(Material _material)
    {
        if (_material.shader == null)return false;
        string shaderName = _material.shader.name;
        return shaderName.Contains("Effects/")
            || shaderName.Contains("Laser Beam");
    }

    private static void SetFloatIfPresent(
        Material _material,
        int _propertyId,
        float _value)
    {
        if (_material.HasProperty(_propertyId))
        {
            _material.SetFloat(_propertyId, _value);
        }
    }
}
