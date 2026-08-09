/*============================================================
*@file LightEffectComposer.cs*
*@brief 1つの実Lightへ任意数の演出Prefabを合成する*
*@author 24CU0312 久場洸太*
*@date 2026/08/07*
*@remarks Effect PrefabをListへ追加するだけで配置・再生できる*
*============================================================*/

using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>Lightへ追加する1個分の視覚Effect設定です。</summary>
[Serializable]
public sealed class SLightEffectAttachment
{
    [SerializeField] private string m_name = "Light Effect"; //Inspector上で用途を識別する名前
    [SerializeField] private bool b_m_enabled = true; //このEffectを生成するか
    [SerializeField] private LightEffectBase m_effectPrefab; //LightEffectBaseを継承した演出Prefabだけを登録可能
    [SerializeField] private Vector3 m_localPosition; //Light原点からの位置補正
    [SerializeField] private Vector3 m_localEulerAngles; //Light方向からの回転補正
    [SerializeField] private Vector3 m_localScale = Vector3.one; //Effectだけの大きさ倍率

    public string Name => m_name;
    public bool Enabled => b_m_enabled;
    public LightEffectBase EffectPrefab => m_effectPrefab;
    public Vector3 LocalPosition => m_localPosition;
    public Vector3 LocalEulerAngles => m_localEulerAngles;
    public Vector3 LocalScale
    {
        get
        {
            //Listへ新規追加した直後のVector3既定値(0,0,0)でEffectが消えることを防ぎます。
            if (m_localScale == Vector3.zero)return Vector3.one;
            return m_localScale;
        }
    }
}

/// <summary>
/// 同じGameObjectの実Lightを土台として、任意数の視覚Effect Prefabを子へ生成します。
/// ListのSizeを増やせば、同種を含めて必要な数だけEffectを重ねられます。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public sealed class LightEffectComposer : MonoBehaviour
{
    private const string EGeneratedRootName = "Attached Light Effects";

    [SerializeField] private bool b_m_previewEffectsInEditMode = true;

    [SerializeField] private List<SLightEffectAttachment> m_effects =
        new List<SLightEffectAttachment>(); //このLightへ合成するEffect一覧

    private Transform m_generatedRoot; //生成したEffectだけを所有する一時Root
    private bool b_m_rebuildRequested; //Inspector変更後の安全な再構築予約
    private Light m_sourceLight; //Effectへ渡す同一Prefab内の実Light

    /// <summary>Sceneへ配置された時点で登録Effectを生成します。</summary>
    /// <summary>Inspectorのリアルタイム調整欄へ、現在生成されているEffectを返します。</summary>
#if UNITY_EDITOR
    private sealed class SEffectPreviewSnapshot
    {
        public string Name;
        public string SourceAssetPath;
        public string ComponentType;
        public string Json;
        public bool Active;
        public bool Used;
    }
#endif

    public LightEffectBase[] GetGeneratedEffects()
    {
        if (m_generatedRoot == null)
        {
            m_generatedRoot = transform.Find(EGeneratedRootName);
        }
        if (m_generatedRoot == null)return Array.Empty<LightEffectBase>();
        return m_generatedRoot.GetComponentsInChildren<LightEffectBase>(true);
    }

    /// <summary>生成済みのCone・Halo等を一括で表示または非表示にします。</summary>
    public void SetEffectVisibility(bool _visible)
    {
        if (m_generatedRoot == null)
        {
            m_generatedRoot = transform.Find(EGeneratedRootName);
        }
        if (m_generatedRoot != null)m_generatedRoot.gameObject.SetActive(_visible);
    }

    private void OnEnable()
    {
        if (!CanBuildInCurrentContext())return;
        RebuildEffects();
    }

    /// <summary>無効化時に生成Effectを残さず破棄します。</summary>
    private void OnDisable()
    {
        ClearEffects();
    }

    /// <summary>Inspector変更をSerialize完了後のFrameで反映します。</summary>
    private void Update()
    {
        if (b_m_rebuildRequested && CanBuildInCurrentContext())
        {
            b_m_rebuildRequested = false;
            RebuildEffects();
        }

        //LightControllerを唯一のON/OFF管理元とし、全付属Effectを親Lightへ追従させます。
        if (m_sourceLight == null)m_sourceLight = GetComponent<Light>();
        if (!Application.isPlaying)
        {
            SetEffectVisibility(b_m_previewEffectsInEditMode);
            return;
        }
        if (m_sourceLight != null)SetEffectVisibility(m_sourceLight.enabled);
    }

    /// <summary>Effect Listの変更をリアルタイム再構築へ予約します。</summary>
    private void OnValidate()
    {
        b_m_rebuildRequested = isActiveAndEnabled && CanBuildInCurrentContext();
    }

    /// <summary>現在のList内容から全Effectを作り直します。</summary>
    [ContextMenu("Rebuild Attached Effects")]
    public void RebuildEffects()
    {
#if UNITY_EDITOR
        List<SEffectPreviewSnapshot> previewSnapshots = null;
        if (!Application.isPlaying)
        {
            previewSnapshots = CapturePreviewSnapshots();
        }
#endif
        ClearEffects();

        if (m_sourceLight == null)
        {
            m_sourceLight = GetComponent<Light>();
        }

        GameObject rootObject = new GameObject(EGeneratedRootName);
        if (!Application.isPlaying)
        {
            rootObject.hideFlags = HideFlags.DontSaveInEditor;
        }
        m_generatedRoot = rootObject.transform;
        m_generatedRoot.SetParent(transform, false);

        foreach (SLightEffectAttachment effect in m_effects)
        {
            if (effect == null || !effect.Enabled || effect.EffectPrefab == null)continue;

            LightEffectBase effectInstance = CreateEffectInstance(effect.EffectPrefab);
            if (effectInstance == null)continue;
            effectInstance.name = effect.Name;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                RestorePreviewSnapshot(
                    effectInstance,
                    effect.EffectPrefab,
                    previewSnapshots);
            }
#endif
            effectInstance.transform.localPosition = effect.LocalPosition;
            effectInstance.transform.localRotation =
                Quaternion.Euler(effect.LocalEulerAngles);
            effectInstance.transform.localScale = effect.LocalScale;
            effectInstance.AttachToLight(m_sourceLight);
        }
    }

#if UNITY_EDITOR
    private List<SEffectPreviewSnapshot> CapturePreviewSnapshots()
    {
        List<SEffectPreviewSnapshot> snapshots = new List<SEffectPreviewSnapshot>();
        foreach (LightEffectBase effectInstance in GetGeneratedEffects())
        {
            if (effectInstance == null)continue;
            LightEffectBase sourceEffect =
                PrefabUtility.GetCorrespondingObjectFromSource(effectInstance);
            if (sourceEffect == null)continue;

            snapshots.Add(new SEffectPreviewSnapshot
            {
                Name = effectInstance.name,
                SourceAssetPath = AssetDatabase.GetAssetPath(sourceEffect),
                ComponentType = effectInstance.GetType().AssemblyQualifiedName,
                Json = EditorJsonUtility.ToJson(effectInstance),
                Active = effectInstance.gameObject.activeSelf
            });
        }
        return snapshots;
    }

    private static void RestorePreviewSnapshot(
        LightEffectBase _effectInstance,
        LightEffectBase _sourcePrefab,
        List<SEffectPreviewSnapshot> _snapshots)
    {
        if (_snapshots == null)return;
        string sourcePath = AssetDatabase.GetAssetPath(_sourcePrefab);
        string componentType = _effectInstance.GetType().AssemblyQualifiedName;
        foreach (SEffectPreviewSnapshot snapshot in _snapshots)
        {
            if (snapshot.Used)continue;
            if (snapshot.Name != _effectInstance.name)continue;
            if (snapshot.SourceAssetPath != sourcePath)continue;
            if (snapshot.ComponentType != componentType)continue;

            EditorJsonUtility.FromJsonOverwrite(snapshot.Json, _effectInstance);
            _effectInstance.gameObject.SetActive(snapshot.Active);
            snapshot.Used = true;
            return;
        }
    }
#endif

    /// <summary>
    /// Edit ModeではPrefab接続を維持したまま生成し、リアルタイム調整後に
    /// InspectorのOverridesから元Effect Prefabへ値を適用できるようにします。
    /// </summary>
    private LightEffectBase CreateEffectInstance(LightEffectBase _effectPrefab)
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            GameObject instanceObject = PrefabUtility.InstantiatePrefab(
                _effectPrefab.gameObject,
                m_generatedRoot) as GameObject;
            if (instanceObject == null)return null;
            return instanceObject.GetComponent<LightEffectBase>();
        }
#endif
        return Instantiate(_effectPrefab, m_generatedRoot);
    }

    /// <summary>Composerが生成したEffectだけを安全に破棄します。</summary>
    private void ClearEffects()
    {
        //参照が失われた古い生成Rootもすべて回収し、Effectの二重表示を防ぎます。
        for (int childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = transform.GetChild(childIndex);
            if (child.name != EGeneratedRootName)continue;
            DestroyGeneratedRoot(child.gameObject);
        }
        m_generatedRoot = null;
    }

    private static void DestroyGeneratedRoot(GameObject _rootObject)
    {
        if (_rootObject == null)return;
        if (Application.isPlaying)
        {
            _rootObject.SetActive(false);
            Destroy(_rootObject);
        }
        else
        {
            DestroyImmediate(_rootObject);
        }
    }

    /// <summary>Prefab Importer内では生成せず、通常SceneとPlay Modeだけで動作させます。</summary>
    private bool CanBuildInCurrentContext()
    {
        if (Application.isPlaying)return true;
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)return false;
        return gameObject.scene.path.EndsWith(".unity");
    }
}
