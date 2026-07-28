/*━━━━━━━━━*
*@file LiveEffectDeploymentBuilder.cs*
*@brief 配置とTimeline Bindingを保持した展開用Prefabを生成する*
*@author 24CU0000 Name*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks Editor専用*
*━━━━━━━━━*/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// 現在のライブエフェクト配置を一つの移植用Prefabへまとめます。
/// </summary>
[InitializeOnLoad]
public static class LiveEffectDeploymentBuilder
{
    private const string EDeploymentFolder = "Assets/EffectSystem/Deployment"; //保存先
    private const string EPrefabPath =
        "Assets/EffectSystem/Deployment/LiveEffectDeployment.prefab"; //Prefab保存先
    private const string ETimelineFolder =
        "Assets/EffectSystem/EffectTimeLine/LiveShows"; //Timeline保存先
    private const string EDeploymentName = "LiveEffectDeployment"; //展開Root名
    private const string EDirectorRootName = "LiveShowDirectors"; //Director親名
    private const int EShowCount = 5; //登録演出数
    private const int EMenuPriority = 134; //メニュー表示順
    private const int EDeployMenuPriority = 135; //Hierarchy展開メニュー表示順
    private const int ERebindMenuPriority = 136; //Binding修復メニュー表示順

    private static readonly string[] m_effectRootNames =
    {
        "EffectParticle",
        "EffectSpotLight",
        "EffectLaser"
    }; //配置を取り込む親名

    private static readonly string[] m_showNames =
    {
        "Live_01_Opening",
        "Live_02_ColorWave",
        "Live_03_LaserRush",
        "Live_04_ParticleBurst",
        "Live_05_Finale"
    }; //同梱する演出名

    /// <summary>
    /// 初回導入時に配置済みシーンから展開用Prefabを生成します。
    /// </summary>
    static LiveEffectDeploymentBuilder()
    {
        EditorApplication.delayCall += BuildIfRequired;
    }

    /// <summary>
    /// 展開用Prefabがなく、必要な配置がある場合だけ自動生成します。
    /// </summary>
    private static void BuildIfRequired()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(EPrefabPath) != null)return;

        Scene scene = SceneManager.GetActiveScene(); //現在のシーン
        if (!scene.IsValid()
            || FindRoot(scene, "EffectSpotLight") == null
            || FindRoot(scene, "EffectLaser") == null)return;

        Build();
    }

    /// <summary>
    /// 現在の配置とBindingから展開用Prefabを生成します。
    /// </summary>
    [MenuItem(
        "Tools/Effect System/Build Deployment Prefab",
        priority = EMenuPriority)]
    private static void Build()
    {
        EnsureFolder();
        Scene scene = SceneManager.GetActiveScene(); //現在のシーン
        GameObject deploymentRoot = new GameObject(EDeploymentName); //一時Root
        deploymentRoot.transform.SetPositionAndRotation(
            Vector3.zero,
            Quaternion.identity);
        Dictionary<string, GameObject> effectObjects =
            new Dictionary<string, GameObject>(); //Binding対象

        for (int i = 0; i < m_effectRootNames.Length; ++i)
        {
            GameObject sourceRoot =
                FindRoot(scene, m_effectRootNames[i]); //現在配置の親
            if (sourceRoot == null)
            {
                Debug.LogWarning($"{m_effectRootNames[i]}が見つかりません。");
                continue;
            }

            GameObject clonedRoot = Object.Instantiate(sourceRoot); //配置複製
            clonedRoot.name = sourceRoot.name;
            clonedRoot.transform.SetParent(deploymentRoot.transform, true);
            CollectObjects(clonedRoot.transform, effectObjects);
        }

        GameObject directorRoot = new GameObject(EDirectorRootName); //Director格納先
        directorRoot.transform.SetParent(deploymentRoot.transform, false);
        List<TimelineAsset> timelines = new List<TimelineAsset>(); //Timeline一覧
        List<PlayableDirector> directors = new List<PlayableDirector>(); //Director一覧
        for (int i = 0; i < EShowCount; ++i)
        {
            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(
                $"{ETimelineFolder}/{m_showNames[i]}.playable"); //対象Timeline
            if (timeline == null)
            {
                Debug.LogWarning($"{m_showNames[i]}が見つかりません。");
                continue;
            }

            GameObject directorObject = new GameObject(m_showNames[i]); //Director Object
            directorObject.transform.SetParent(directorRoot.transform, false);
            PlayableDirector director =
                directorObject.AddComponent<PlayableDirector>(); //専用Director
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            BindTimeline(timeline, director, effectObjects);
            timelines.Add(timeline);
            directors.Add(director);
        }

        EffectSystem effectSystem =
            deploymentRoot.AddComponent<EffectSystem>(); //同梱EffectSystem
        RegisterEffects(effectSystem, timelines, directors);
        deploymentRoot.AddComponent<LiveEffectQuickTester>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(
            deploymentRoot,
            EPrefabPath); //生成したPrefab
        Object.DestroyImmediate(deploymentRoot);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = prefab;
        Debug.Log($"展開用Prefabを生成しました: {EPrefabPath}");
    }

    /// <summary>
    /// 展開用Prefabを現在のシーンへ通常のHierarchy Objectとして展開します。
    /// </summary>
    [MenuItem(
        "Tools/Effect System/Deploy Live Effects To Current Scene",
        priority = EDeployMenuPriority)]
    private static void DeployToCurrentScene()
    {
        Scene scene = SceneManager.GetActiveScene(); //展開先シーン
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("展開先のシーンを開いてください。");
            return;
        }

        GameObject existingObject = FindRoot(scene, EDeploymentName); //既存展開Object
        if (existingObject != null)
        {
            Selection.activeGameObject = existingObject;
            Debug.LogWarning(
                "LiveEffectDeploymentは既にHierarchyへ存在します。"
                + "二重配置を防ぐため展開を中止しました。");
            return;
        }

        GameObject deploymentPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(EPrefabPath); //展開元Prefab
        if (deploymentPrefab == null)
        {
            Debug.LogWarning(
                "展開用Prefabがありません。先にBuild Deployment Prefabを実行してください。");
            return;
        }

        GameObject deployedObject = PrefabUtility.InstantiatePrefab(
            deploymentPrefab,
            scene) as GameObject; //Prefab Instance
        if (deployedObject == null)return;

        deployedObject.name = EDeploymentName;
        deployedObject.transform.SetPositionAndRotation(
            Vector3.zero,
            Quaternion.identity);
        deployedObject.transform.localScale = Vector3.one;
        PrefabUtility.UnpackPrefabInstance(
            deployedObject,
            PrefabUnpackMode.Completely,
            InteractionMode.AutomatedAction);
        RebindDeployment(deployedObject);
        Undo.RegisterCreatedObjectUndo(
            deployedObject,
            "Deploy Live Effects");
        Selection.activeGameObject = deployedObject;
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            "ライブエフェクト一式を通常のHierarchy Objectとして展開しました。");
    }

    /// <summary>
    /// 現在のシーンへ展開済みのDirector Bindingを作り直します。
    /// </summary>
    [MenuItem(
        "Tools/Effect System/Rebind Deployed Live Effects",
        priority = ERebindMenuPriority)]
    private static void RebindCurrentDeployment()
    {
        Scene scene = SceneManager.GetActiveScene(); //現在のシーン
        GameObject deploymentRoot = FindRoot(scene, EDeploymentName); //展開済みRoot
        if (deploymentRoot == null)
        {
            Debug.LogWarning("LiveEffectDeploymentが現在のシーンにありません。");
            return;
        }

        int bindingCount = RebindDeployment(deploymentRoot); //再設定数
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log($"Timeline Bindingを{bindingCount}件再設定しました。");
    }

    /// <summary>
    /// 展開済み階層を現在のTimeline TrackへBindingします。
    /// </summary>
    private static int RebindDeployment(GameObject _deploymentroot)
    {
        Dictionary<string, GameObject> effectObjects =
            new Dictionary<string, GameObject>(); //Binding対象一覧
        for (int i = 0; i < m_effectRootNames.Length; ++i)
        {
            Transform effectRoot =
                _deploymentroot.transform.Find(m_effectRootNames[i]); //対象親
            if (effectRoot != null)
            {
                CollectObjects(effectRoot, effectObjects);
            }
        }

        Transform directorRoot =
            _deploymentroot.transform.Find(EDirectorRootName); //Director親
        if (directorRoot == null)return 0;

        int bindingCount = 0; //再設定したBinding数
        for (int i = 0; i < EShowCount; ++i)
        {
            Transform directorTransform = directorRoot.Find(m_showNames[i]); //専用Director
            if (directorTransform == null)continue;

            PlayableDirector director =
                directorTransform.GetComponent<PlayableDirector>(); //再設定対象
            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(
                $"{ETimelineFolder}/{m_showNames[i]}.playable"); //現在のTimeline
            if (director == null || timeline == null)continue;

            director.playableAsset = timeline;
            bindingCount += BindTimeline(
                timeline,
                director,
                effectObjects);
            EditorUtility.SetDirty(director);
        }

        return bindingCount;
    }

    /// <summary>
    /// Timeline内のActivationとAnimation Trackを複製ObjectへBindingします。
    /// </summary>
    private static int BindTimeline(
        TimelineAsset _timeline,
        PlayableDirector _director,
        Dictionary<string, GameObject> _effectobjects)
    {
        int bindingCount = 0; //設定したBinding数
        foreach (TrackAsset track in _timeline.GetOutputTracks())
        {
            if (track is ActivationTrack)
            {
                if (_effectobjects.TryGetValue(track.name, out GameObject effectObject))
                {
                    _director.SetGenericBinding(track, effectObject);
                    ++bindingCount;
                }

                continue;
            }

            if (!(track is AnimationTrack))continue;

            const string rotationSuffix = " Rotation"; //回転Track末尾
            string objectName = track.name.EndsWith(rotationSuffix)
                ? track.name.Substring(
                    0,
                    track.name.Length - rotationSuffix.Length)
                : track.name; //Binding対象名
            if (!_effectobjects.TryGetValue(objectName, out GameObject animationObject))
            {
                continue;
            }

            Animator animator = animationObject.GetComponent<Animator>(); //Animation対象
            if (animator == null)
            {
                animator = animationObject.AddComponent<Animator>();
            }

            _director.SetGenericBinding(track, animator);
            ++bindingCount;
        }

        return bindingCount;
    }

    /// <summary>
    /// Prefab内EffectSystemへTimelineと専用Directorを登録します。
    /// </summary>
    private static void RegisterEffects(
        EffectSystem _effectsystem,
        List<TimelineAsset> _timelines,
        List<PlayableDirector> _directors)
    {
        SerializedObject serializedSystem =
            new SerializedObject(_effectsystem); //編集対象EffectSystem
        SerializedProperty effects =
            serializedSystem.FindProperty("m_effectDatas"); //EffectData配列
        effects.arraySize = Mathf.Min(_timelines.Count, _directors.Count);
        for (int i = 0; i < effects.arraySize; ++i)
        {
            SerializedProperty effect = effects.GetArrayElementAtIndex(i); //登録項目
            effect.FindPropertyRelative("m_effectName").stringValue = m_showNames[i];
            effect.FindPropertyRelative("m_playDelaySeconds").floatValue = 0.0f;
            effect.FindPropertyRelative("m_timeline").objectReferenceValue = _timelines[i];
            effect.FindPropertyRelative("m_director").objectReferenceValue = _directors[i];
        }

        serializedSystem.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// 複製した階層を名前から検索できるよう登録します。
    /// </summary>
    private static void CollectObjects(
        Transform _root,
        Dictionary<string, GameObject> _objects)
    {
        if (!_objects.ContainsKey(_root.name))
        {
            _objects.Add(_root.name, _root.gameObject);
        }

        for (int i = 0; i < _root.childCount; ++i)
        {
            CollectObjects(_root.GetChild(i), _objects);
        }
    }

    /// <summary>
    /// シーン直下から指定名のObjectを検索します。
    /// </summary>
    private static GameObject FindRoot(Scene _scene, string _name)
    {
        GameObject[] roots = _scene.GetRootGameObjects(); //シーン直下一覧
        for (int i = 0; i < roots.Length; ++i)
        {
            if (roots[i].name == _name)return roots[i];
        }

        return null;
    }

    /// <summary>
    /// 展開用Prefabの保存フォルダを生成します。
    /// </summary>
    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(EDeploymentFolder))return;

        AssetDatabase.CreateFolder(
            "Assets/EffectSystem",
            "Deployment");
    }
}
