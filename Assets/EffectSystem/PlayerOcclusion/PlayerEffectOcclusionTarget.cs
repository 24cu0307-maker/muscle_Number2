using UnityEngine;

/// <summary>Player Prefab全体のRendererをEffect遮蔽専用Layerへまとめます。</summary>
[DisallowMultipleComponent]
public sealed class PlayerEffectOcclusionTarget : MonoBehaviour
{
    private const string EPlayerEffectMaskLayerName = "PlayerEffectMask";

    private void Awake()
    {
        ApplyMaskLayer();
    }

    private void OnTransformChildrenChanged()
    {
        ApplyMaskLayer();
    }

    [ContextMenu("Apply Player Effect Mask Layer")]
    private void ApplyMaskLayer()
    {
        int maskLayer = LayerMask.NameToLayer(EPlayerEffectMaskLayerName);
        if (maskLayer < 0)
        {
            Debug.LogWarning(
                $"Layer '{EPlayerEffectMaskLayerName}' が見つかりません。",
                this);
            return;
        }

        // HumanoidSkeletonはモデルRendererの親ではなく兄弟に置かれているため、
        // Component自身ではなくPrefabの最上位Rootから全Rendererを取得します。
        Transform playerRoot = transform.root;
        Renderer[] playerRenderers =
            playerRoot.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer playerRenderer in playerRenderers)
        {
            playerRenderer.gameObject.layer = maskLayer;
        }
    }
}
