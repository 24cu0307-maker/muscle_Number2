/*============================================================
*@file LiveLightingPatternRig.cs*
*@brief ライブ撮影で代表的なライティング配置を再現する*
*@author 24CU0312 久場洸太*
*@date 2026/08/07*
*@remarks Prefabの原点を演者位置として、必要なLightと演出Prefabを自動配置する*
*============================================================*/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ライブステージ向けの代表的なライティングパターンです。
/// Rootを演者の足元へ置くと、ローカル-Zを客席・カメラ側として各光源を配置します。
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public sealed class LiveLightingPatternRig : MonoBehaviour
{
    /// <summary>再現するライティングの種類です。</summary>
    public enum ELightingPattern
    {
        Plain45,
        Front,
        Edge,
        BrightBacklight,
        SilhouetteBacklight,
        Under,
        Spotlight,
        Laser,
        MultipleExposure,
        BacklightStreaks,
        HaloOnly
    }

    private const string ERuntimeRootName = "Generated Lighting Pattern";
    private static readonly Vector3 EPerformerFocus = new Vector3(0.0f, 1.25f, 0.0f);

    [SerializeField] private ELightingPattern m_pattern; //このPrefabが再現する配置パターン
    [SerializeField] private GameObject m_spotlightPrefab; //光のコーンも見せる通常Spotlight
    [SerializeField] private GameObject m_backlightPrefab; //光源ハローを持つ逆光Spotlight
    [SerializeField] private GameObject m_laserPrefab; //レーザー・多重露光風演出に使用するBeam

    [Header("Realtime Tuning")]
    [SerializeField, Min(0.1f)] private float m_intensityScale = 1.0f; //パターン全体の実Light光量倍率
    [SerializeField, Min(0.1f)] private float m_positionScale = 1.0f; //演者から各光源までの配置距離倍率
    [SerializeField, Min(0.1f)] private float m_rangeScale = 1.0f; //実Spot Lightの照射距離倍率
    [SerializeField, Range(0.25f, 2.0f)] private float m_spotAngleScale = 1.0f; //実Spot Lightの照射角倍率
    [SerializeField] private Color m_colorTint = Color.white; //パターン全体へ乗算する調整色
    [SerializeField, Range(0.0f, 1.0f)] private float m_shadowStrength = 0.55f; //影を使用するLightの濃さ
    [SerializeField] private bool b_m_animate = true; //レーザー系を緩やかに動かすか
    [SerializeField, Range(0.0f, 30.0f)] private float m_animationSpeed = 6.0f; //回転速度

    private Transform m_generatedRoot; //実行時に生成したLightと演出Prefabの所有Root
    private bool b_m_rebuildRequested; //Inspector変更を安全な更新タイミングで反映する予約Flag
    private readonly List<Material> m_runtimeMaterials = new List<Material>(); //参照切れ時の代替描画用Material

    /// <summary>有効化されたパターンを組み立てます。</summary>
    private void OnEnable()
    {
        if (!CanBuildInCurrentContext())return;
        BuildPattern();
    }

    /// <summary>生成物を残さず破棄します。</summary>
    private void OnDisable()
    {
        ClearPattern();
    }

    /// <summary>レーザー系パターンにライブらしい緩やかな動きを加えます。</summary>
    private void Update()
    {
        if (!CanBuildInCurrentContext())return;

        if (b_m_rebuildRequested)
        {
            b_m_rebuildRequested = false;
            BuildPattern();
        }

        if (!Application.isPlaying)return;
        if (!b_m_animate || m_generatedRoot == null)return;
        if (m_pattern != ELightingPattern.Laser &&
            m_pattern != ELightingPattern.MultipleExposure)return;

        m_generatedRoot.Rotate(
            Vector3.forward,
            m_animationSpeed * Time.deltaTime,
            Space.Self);
    }

    /// <summary>
    /// Inspectorから値が変更された場合、Play中・停止中のどちらでも再構築を予約します。
    /// OnValidate内で直接Objectを破棄せず、Updateで処理することでUnityのSerialize中断を防ぎます。
    /// </summary>
    private void OnValidate()
    {
        m_intensityScale = Mathf.Max(0.1f, m_intensityScale);
        m_positionScale = Mathf.Max(0.1f, m_positionScale);
        m_rangeScale = Mathf.Max(0.1f, m_rangeScale);
        m_spotAngleScale = Mathf.Clamp(m_spotAngleScale, 0.25f, 2.0f);
        m_shadowStrength = Mathf.Clamp01(m_shadowStrength);
        b_m_rebuildRequested = isActiveAndEnabled && CanBuildInCurrentContext();
    }

    /// <summary>InspectorのContext Menuから現在の配置を作り直します。</summary>
    [ContextMenu("Rebuild Lighting Pattern")]
    public void BuildPattern()
    {
        ClearPattern();

        GameObject generatedObject = new GameObject(ERuntimeRootName);
        if (!Application.isPlaying)
        {
            generatedObject.hideFlags = HideFlags.DontSaveInEditor;
        }
        m_generatedRoot = generatedObject.transform;
        m_generatedRoot.SetParent(transform, false);

        switch (m_pattern)
        {
            case ELightingPattern.Plain45:
                BuildPlain45();
                break;
            case ELightingPattern.Front:
                BuildFront();
                break;
            case ELightingPattern.Edge:
                BuildEdge();
                break;
            case ELightingPattern.BrightBacklight:
                BuildBrightBacklight();
                break;
            case ELightingPattern.SilhouetteBacklight:
                BuildSilhouetteBacklight();
                break;
            case ELightingPattern.Under:
                BuildUnder();
                break;
            case ELightingPattern.Spotlight:
                BuildSpotlight();
                break;
            case ELightingPattern.Laser:
                BuildLaser();
                break;
            case ELightingPattern.MultipleExposure:
                BuildMultipleExposure();
                break;
            case ELightingPattern.BacklightStreaks:
                BuildBacklightStreaks();
                break;
            case ELightingPattern.HaloOnly:
                BuildHaloOnly();
                break;
        }
    }

    /// <summary>斜め前45度から陰影を残して演者を立体的に照らします。</summary>
    private void BuildPlain45()
    {
        CreateSpotLight("Plain Key", new Vector3(-4.2f, 4.0f, -4.2f),
            new Color(1.0f, 0.78f, 0.58f), 950.0f, 13.0f, 48.0f, true);
        CreateSpotLight("Plain Fill", new Vector3(3.0f, 2.8f, -3.4f),
            new Color(0.45f, 0.62f, 1.0f), 220.0f, 11.0f, 58.0f, false);
    }

    /// <summary>正面から均一に照らし、表情と衣装色を見やすくします。</summary>
    private void BuildFront()
    {
        CreateSpotLight("Front Left", new Vector3(-2.0f, 3.4f, -5.2f),
            new Color(1.0f, 0.86f, 0.72f), 620.0f, 12.0f, 58.0f, false);
        CreateSpotLight("Front Right", new Vector3(2.0f, 3.4f, -5.2f),
            new Color(0.78f, 0.86f, 1.0f), 520.0f, 12.0f, 58.0f, false);
    }

    /// <summary>後方側面の強い光で演者の片側輪郭を浮かせます。</summary>
    private void BuildEdge()
    {
        CreateSpotLight("Edge Rim", new Vector3(4.4f, 3.1f, 2.8f),
            new Color(1.0f, 0.22f, 0.08f), 1450.0f, 12.0f, 38.0f, true);
        CreateSpotLight("Edge Face Fill", new Vector3(-2.2f, 2.4f, -4.0f),
            new Color(0.16f, 0.26f, 0.55f), 100.0f, 9.0f, 62.0f, false);
    }

    /// <summary>強い後光と弱い正面補助光で、眩しい逆光を作ります。</summary>
    private void BuildBrightBacklight()
    {
        CreateEffectPrefab("Bright Backlight", m_backlightPrefab,
            new Vector3(0.0f, 3.0f, 5.5f), EPerformerFocus);
        CreateSpotLight("Backlight Front Fill", new Vector3(0.0f, 3.0f, -4.8f),
            new Color(1.0f, 0.65f, 0.32f), 260.0f, 11.0f, 72.0f, false);
    }

    /// <summary>正面光を置かず、輪郭とシルエットを強調する逆光を作ります。</summary>
    private void BuildSilhouetteBacklight()
    {
        CreateEffectPrefab("Silhouette Backlight", m_backlightPrefab,
            new Vector3(0.0f, 2.3f, 4.8f), EPerformerFocus);
        CreateSpotLight("Silhouette Rim Left", new Vector3(-3.4f, 2.8f, 3.6f),
            new Color(0.18f, 0.28f, 1.0f), 700.0f, 10.0f, 34.0f, true);
    }

    /// <summary>足元前方から上向きに照らし、不穏で強い表情を作ります。</summary>
    private void BuildUnder()
    {
        CreateSpotLight("Under Light", new Vector3(0.0f, 0.12f, -1.5f),
            new Color(1.0f, 0.2f, 0.08f), 900.0f, 7.0f, 66.0f, true);
    }

    /// <summary>演者だけを上方から切り取る舞台Spotlightを配置します。</summary>
    private void BuildSpotlight()
    {
        CreateEffectPrefab("Overhead Spotlight", m_spotlightPrefab,
            new Vector3(0.0f, 7.0f, -1.2f), EPerformerFocus);
    }

    /// <summary>演者後方へ扇状のレーザーを配置します。</summary>
    private void BuildLaser()
    {
        for (int index = -2; index <= 2; index++)
        {
            Vector3 position = new Vector3(index * 1.35f, 1.2f, 3.2f);
            Vector3 target = new Vector3(index * 3.0f, 3.0f, -5.0f);
            CreateEffectPrefab($"Laser {index + 3}", m_laserPrefab, position, target);
        }
    }

    /// <summary>色違いの光軌跡を重ね、写真の多重露光に近い画面構成を作ります。</summary>
    private void BuildMultipleExposure()
    {
        for (int index = 0; index < 8; index++)
        {
            float angle = index * 45.0f;
            float radians = angle * Mathf.Deg2Rad;
            Vector3 position = new Vector3(
                Mathf.Cos(radians) * 2.4f,
                1.0f + (index % 3) * 0.7f,
                2.8f);
            Vector3 target = new Vector3(
                Mathf.Cos(radians) * 5.5f,
                Mathf.Sin(radians) * 3.0f + 2.0f,
                -5.0f);
            CreateEffectPrefab($"Exposure Trail {index + 1}", m_laserPrefab, position, target);
        }

        CreateSpotLight("Exposure Subject Light", new Vector3(-3.0f, 3.5f, -3.0f),
            new Color(0.8f, 0.3f, 1.0f), 380.0f, 10.0f, 52.0f, false);
    }

    /// <summary>コーン外側へ伸びる光条を持った、派手な逆光ライトを配置します。</summary>
    private void BuildBacklightStreaks()
    {
        CreateEffectPrefab("Backlight Streaks", m_backlightPrefab,
            new Vector3(0.0f, 2.8f, 5.2f), EPerformerFocus);
    }

    /// <summary>光条とコーンを表示せず、実照明と柔らかなハローだけを配置します。</summary>
    private void BuildHaloOnly()
    {
        GameObject haloInstance = CreateEffectPrefab(
            "Backlight Halo Only",
            m_backlightPrefab,
            new Vector3(0.0f, 2.8f, 5.2f),
            EPerformerFocus);
        if (haloInstance == null)return;

        BacklightSourceGlow[] glows =
            haloInstance.GetComponentsInChildren<BacklightSourceGlow>(true);
        foreach (BacklightSourceGlow glow in glows)
        {
            glow.SetHaloOnly(true);
        }
    }

    /// <summary>指定位置から演者へ向く軽量なRealtime Spot Lightを生成します。</summary>
    private Light CreateSpotLight(
        string _name,
        Vector3 _localPosition,
        Color _color,
        float _intensity,
        float _range,
        float _spotAngle,
        bool _useShadows)
    {
        GameObject lightObject = new GameObject(_name, typeof(Light));
        lightObject.transform.SetParent(m_generatedRoot, false);
        lightObject.transform.localPosition = _localPosition * m_positionScale;
        AimAtLocalPoint(lightObject.transform, EPerformerFocus);

        Light lightComponent = lightObject.GetComponent<Light>();
        lightComponent.type = LightType.Spot;
        lightComponent.color = _color * m_colorTint;
        lightComponent.intensity = _intensity * m_intensityScale;
        lightComponent.range = _range * m_rangeScale;
        float adjustedSpotAngle = Mathf.Clamp(
            _spotAngle * m_spotAngleScale,
            1.0f,
            179.0f);
        lightComponent.spotAngle = adjustedSpotAngle;
        lightComponent.innerSpotAngle = adjustedSpotAngle * 0.58f;
        lightComponent.shadows = _useShadows ? LightShadows.Soft : LightShadows.None;
        lightComponent.shadowStrength = m_shadowStrength;
        lightComponent.renderMode = LightRenderMode.ForcePixel;
        return lightComponent;
    }

    /// <summary>既存EffectSystem用Prefabを指定位置へ複製し、目標点へ向けます。</summary>
    private GameObject CreateEffectPrefab(
        string _name,
        GameObject _prefab,
        Vector3 _localPosition,
        Vector3 _localTarget)
    {
        if (_prefab == null)
        {
            return CreateFallbackEffect(_name, _localPosition, _localTarget);
        }

        GameObject instance = Instantiate(_prefab, m_generatedRoot);
        instance.name = _name;
        instance.transform.localPosition = _localPosition * m_positionScale;
        instance.transform.localScale = Vector3.one;
        AimAtLocalPoint(instance.transform, _localTarget);
        ApplyPrefabTuning(instance);
        return instance;
    }

    /// <summary>
    /// 外部Prefab参照が失われても演出が見えなくならないよう、同じController構成をその場で生成します。
    /// 完成Presetはこの経路だけでもLight・Cone・Halo・Laserを表示できるため、配置直後から使用できます。
    /// </summary>
    private GameObject CreateFallbackEffect(
        string _name,
        Vector3 _localPosition,
        Vector3 _localTarget)
    {
        bool isLaser = _name.Contains("Laser") || _name.Contains("Exposure");
        if (isLaser)
        {
            return CreateFallbackLaser(_name, _localPosition, _localTarget);
        }

        GameObject lightObject = new GameObject(
            _name,
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(Light));
        lightObject.transform.SetParent(m_generatedRoot, false);
        lightObject.transform.localPosition = _localPosition * m_positionScale;
        AimAtLocalPoint(lightObject.transform, _localTarget);

        Shader coneShader = Shader.Find("Muscle/Effects/Spotlight Cone Additive");
        if (coneShader != null)
        {
            Material coneMaterial = new Material(coneShader);
            coneMaterial.name = "Lighting Pattern Runtime Cone";
            coneMaterial.hideFlags = HideFlags.HideAndDontSave;
            m_runtimeMaterials.Add(coneMaterial);
            lightObject.GetComponent<MeshRenderer>().sharedMaterial = coneMaterial;
        }

        lightObject.AddComponent<SpotlightConeMesh>();
        SpotlightConeController controller =
            lightObject.AddComponent<SpotlightConeController>();
        controller.LightColor = new Color(1.0f, 0.58f, 0.2f, 1.0f) * m_colorTint;
        controller.EmissionIntensity = 1.8f * m_intensityScale;

        bool isBacklight = _name.Contains("Backlight");
        if (isBacklight)
        {
            BacklightSourceGlow glow = lightObject.AddComponent<BacklightSourceGlow>();
            glow.ApplyRealtimeTuning(m_colorTint, m_intensityScale);

            if (_name.Contains("Halo Only"))
            {
                glow.SetHaloOnly(true);
            }
            else if (_name.Contains("Streaks"))
            {
                controller.SetOuterStreakIntensity(0.14f);
            }
        }

        Light sourceLight = lightObject.GetComponent<Light>();
        sourceLight.type = LightType.Spot;
        sourceLight.color = new Color(1.0f, 0.58f, 0.2f) * m_colorTint;
        sourceLight.intensity = 900.0f * m_intensityScale;
        sourceLight.range = 16.0f * m_rangeScale;
        sourceLight.spotAngle = Mathf.Clamp(68.0f * m_spotAngleScale, 1.0f, 179.0f);
        sourceLight.innerSpotAngle = sourceLight.spotAngle * 0.58f;
        sourceLight.shadows = LightShadows.Soft;
        sourceLight.shadowStrength = m_shadowStrength;
        return lightObject;
    }

    /// <summary>Prefab参照なしでも表示できる軽量なLaser Meshを生成します。</summary>
    private GameObject CreateFallbackLaser(
        string _name,
        Vector3 _localPosition,
        Vector3 _localTarget)
    {
        GameObject laserObject = new GameObject(
            _name,
            typeof(MeshFilter),
            typeof(MeshRenderer));
        laserObject.transform.SetParent(m_generatedRoot, false);
        laserObject.transform.localPosition = _localPosition * m_positionScale;
        AimAtLocalPoint(laserObject.transform, _localTarget);

        Shader laserShader = Shader.Find("Muscle/Effects/Laser Beam Additive");
        if (laserShader != null)
        {
            Material laserMaterial = new Material(laserShader);
            laserMaterial.name = "Lighting Pattern Runtime Laser";
            laserMaterial.hideFlags = HideFlags.HideAndDontSave;
            m_runtimeMaterials.Add(laserMaterial);
            laserObject.GetComponent<MeshRenderer>().sharedMaterial = laserMaterial;
        }

        laserObject.AddComponent<LaserBeamMesh>();
        LaserBeamController controller = laserObject.AddComponent<LaserBeamController>();
        controller.LaserColor = new Color(1.0f, 0.78f, 0.18f, 1.0f) * m_colorTint;
        controller.EmissionIntensity = 5.0f * m_intensityScale;
        return laserObject;
    }

    /// <summary>
    /// 複製した既存Spotlight・Backlight・LaserにもRigのリアルタイム調整値を適用します。
    /// 各Controller経由で変更するため、毎FrameのMaterialPropertyBlock更新にも上書きされません。
    /// </summary>
    private void ApplyPrefabTuning(GameObject _instance)
    {
        SpotlightConeController[] spotlightControllers =
            _instance.GetComponentsInChildren<SpotlightConeController>(true);
        foreach (SpotlightConeController controller in spotlightControllers)
        {
            controller.LightColor *= m_colorTint;
            controller.EmissionIntensity *= m_intensityScale;
        }

        LaserBeamController[] laserControllers =
            _instance.GetComponentsInChildren<LaserBeamController>(true);
        foreach (LaserBeamController controller in laserControllers)
        {
            controller.LaserColor *= m_colorTint;
            controller.EmissionIntensity *= m_intensityScale;
        }

        BacklightSourceGlow[] backlightGlows =
            _instance.GetComponentsInChildren<BacklightSourceGlow>(true);
        foreach (BacklightSourceGlow glow in backlightGlows)
        {
            glow.ApplyRealtimeTuning(m_colorTint, m_intensityScale);
        }

        Light[] lights = _instance.GetComponentsInChildren<Light>(true);
        foreach (Light lightComponent in lights)
        {
            lightComponent.range *= m_rangeScale;
            lightComponent.shadowStrength = m_shadowStrength;
        }
    }

    /// <summary>生成ObjectのforwardをRigローカル座標上の目標へ向けます。</summary>
    private void AimAtLocalPoint(Transform _target, Vector3 _localTarget)
    {
        Vector3 localDirection = _localTarget - _target.localPosition;
        if (localDirection.sqrMagnitude <= Mathf.Epsilon)return;
        _target.localRotation = Quaternion.LookRotation(localDirection.normalized, Vector3.up);
    }

    /// <summary>このRigが生成した一時Objectだけを安全に破棄します。</summary>
    private void ClearPattern()
    {
        if (m_generatedRoot == null)
        {
            Transform existingRoot = transform.Find(ERuntimeRootName);
            if (existingRoot != null)
            {
                m_generatedRoot = existingRoot;
            }
        }
        if (m_generatedRoot == null)return;

        GameObject generatedObject = m_generatedRoot.gameObject;
        m_generatedRoot = null;
        if (Application.isPlaying)
        {
            Destroy(generatedObject);
        }
        else
        {
            DestroyImmediate(generatedObject);
        }

        foreach (Material runtimeMaterial in m_runtimeMaterials)
        {
            if (runtimeMaterial == null)continue;
            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }
        }
        m_runtimeMaterials.Clear();
    }

    /// <summary>
    /// 通常SceneまたはPlay Modeだけで生成し、Prefab ImporterのPreview Sceneでは生成しません。
    /// Import中のPrefab複製と一時Object保存を防ぐための判定です。
    /// </summary>
    private bool CanBuildInCurrentContext()
    {
        if (Application.isPlaying)return true;
        if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded)return false;
        return gameObject.scene.path.EndsWith(".unity");
    }
}
