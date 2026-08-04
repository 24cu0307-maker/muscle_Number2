/*━━━━━━━━━*
*@file EffectWorkToGameplaySync.cs*
*@brief EffectWorkの演出変更をGameplayへ一括反映する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks メイン進行Objectを維持して演出所有RootとEffectSystem設定だけを同期する*
*━━━━━━━━━*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gameplay_EffectWorkで作成した演出をGameplayへ安全に反映します。
/// </summary>
public static class EffectWorkToGameplaySync
{
    private const string ESourceScenePath =
        "Assets/Scenes/GameFlow/Gameplay_EffectWork.unity"; //編集元Scene
    private const string ETargetScenePath =
        "Assets/Scenes/GameFlow/Gameplay.unity"; //本番反映先Scene
    private const string EMenuPath =
        "Tools/Effect System/Apply EffectWork To Gameplay"; //本番用Menu
    private const string ETemporaryBundleName =
        "__EffectWorkSyncBundle"; //複製用一時Root
    private const int EMenuPriority = 1; //Menu表示順

    private static readonly string[] EEffectRootNames =
    {
        "EFF_List",
        "LightController",
        "LiveEffectDeployment",
        "AudienceSystem",
        "StageGeneratedEffects60",
        "VenueVoltageSystem",
        "VoltageBGM",
        "VoltageTimelineTemplateDirector",
        "stage_0728_2",
        "Stage",
        "prehub",
        "Prehub"
    }; //Gameplayへ同期する演出所有Root

    /// <summary>
    /// EffectWorkの演出所有ObjectとEffectSystem設定をGameplayへ反映します。
    /// </summary>
    [MenuItem(EMenuPath, priority = EMenuPriority)]
    private static void ApplyToGameplay()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
        if (!ValidateSceneAssets())return;

        bool b_apply = EditorUtility.DisplayDialog(
            "EffectWorkをGameplayへ反映",
            "Gameplay_EffectWorkの演出、ステージ、観客、"
            + "EffectSystem設定をGameplayへ反映します。\n\n"
            + "GameplayのInGame、UI、CameraなどのメインObjectは"
            + "上書きしません。",
            "反映する",
            "キャンセル");
        if (!b_apply)return;

        Scene sourceScene =
            EditorSceneManager.OpenScene(
                ESourceScenePath,
                OpenSceneMode.Single); //編集元
        Scene targetScene =
            EditorSceneManager.OpenScene(
                ETargetScenePath,
                OpenSceneMode.Additive); //反映先
        List<GameObject> sourceRootsList =
            CollectEffectRoots(sourceScene, targetScene); //同期元Root一覧
        if (sourceRootsList.Count == 0)
        {
            Debug.LogError("EffectWorkに同期対象の演出Rootがありません。");
            EditorSceneManager.CloseScene(targetScene, true);
            return;
        }

        RemoveTargetEffectRoots(targetScene);
        GameObject sourceBundle =
            BuildSourceBundle(sourceScene, sourceRootsList); //複製単位
        GameObject copiedBundle =
            Object.Instantiate(sourceBundle); //内部参照を保った複製
        RestoreSourceRoots(sourceBundle);
        SceneManager.MoveGameObjectToScene(copiedBundle, targetScene);
        UnpackBundle(copiedBundle, targetScene);
        Object.DestroyImmediate(copiedBundle);
        int reboundCount =
            RebindSourceSceneReferences(sourceScene, targetScene); //参照修復数
        EffectSystem targetEffectSystem =
            FindComponent<EffectSystem>(targetScene); //複製後の同期先
        VenueVoltageSystem targetVoltageSystem =
            FindComponent<VenueVoltageSystem>(targetScene); //判定連携先
        reboundCount += RebindRuntimeSystemReferences(
            targetScene,
            targetEffectSystem,
            targetVoltageSystem);
        int missingRootCount = ValidateTransferredRoots(
            targetScene,
            sourceRootsList); //反映漏れ数
        if (missingRootCount > 0)
        {
            Debug.LogError(
                $"GameplayへのObject反映に{missingRootCount}件の漏れがあります。");
            EditorSceneManager.CloseScene(targetScene, true);
            EditorSceneManager.CloseScene(sourceScene, true);
            EditorSceneManager.OpenScene(
                ETargetScenePath,
                OpenSceneMode.Single);
            return;
        }

        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);
        EditorSceneManager.CloseScene(sourceScene, true);
        SceneManager.SetActiveScene(targetScene);
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"EffectWorkの演出をGameplayへ反映しました。"
            + $" Root: {sourceRootsList.Count}, 参照再設定: {reboundCount}");
        EditorUtility.DisplayDialog(
            "反映完了",
            "Gameplayへ演出を反映しました。\n"
            + "メイン進行Objectは上書きしていません。\n"
            + $"参照再設定数: {reboundCount}",
            "OK");
    }

    /// <summary>
    /// 同期対象RootがGameplayへ階層ごと生成されたことを確認します。
    /// </summary>
    private static int ValidateTransferredRoots(
        Scene _targetScene,
        List<GameObject> _sourceRootsList)
    {
        int missingRootCount = 0; //反映漏れ数
        for (int i = 0; i < _sourceRootsList.Count; ++i)
        {
            GameObject sourceRoot = _sourceRootsList[i]; //同期元Root
            GameObject targetRoot =
                FindRoot(_targetScene, sourceRoot.name); //反映後Root
            if (targetRoot == null)
            {
                ++missingRootCount;
                Debug.LogError(
                    $"GameplayにRoot「{sourceRoot.name}」が反映されていません。");
                continue;
            }

            int sourceTransformCount =
                sourceRoot.GetComponentsInChildren<Transform>(true).Length;
            int targetTransformCount =
                targetRoot.GetComponentsInChildren<Transform>(true).Length;
            if (sourceTransformCount == targetTransformCount)continue;

            ++missingRootCount;
            Debug.LogError(
                $"Root「{sourceRoot.name}」の階層数が一致しません。"
                + $" EffectWork: {sourceTransformCount}, "
                + $"Gameplay: {targetTransformCount}");
        }

        return missingRootCount;
    }

    /// <summary>
    /// 編集元と反映先のScene Assetが存在するか確認します。
    /// </summary>
    private static bool ValidateSceneAssets()
    {
        SceneAsset sourceAsset =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ESourceScenePath);
        SceneAsset targetAsset =
            AssetDatabase.LoadAssetAtPath<SceneAsset>(ETargetScenePath);
        if (sourceAsset != null && targetAsset != null)return true;

        Debug.LogError(
            "Gameplay_EffectWorkまたはGameplay Sceneが見つかりません。");
        return false;
    }

    /// <summary>
    /// EffectWorkから同期対象のRootを定義順で収集します。
    /// </summary>
    private static List<GameObject> CollectEffectRoots(
        Scene _sourceScene,
        Scene _targetScene)
    {
        List<GameObject> rootsList = new List<GameObject>(); //同期Root
        HashSet<string> selectedNames = new HashSet<string>(); //登録済み名
        for (int i = 0; i < EEffectRootNames.Length; ++i)
        {
            GameObject rootObject =
                FindRoot(_sourceScene, EEffectRootNames[i]); //名前一致Root
            if (rootObject == null)continue;

            rootsList.Add(rootObject);
            selectedNames.Add(rootObject.name);
        }

        HashSet<string> targetRootNames = new HashSet<string>(); //本番Root名
        GameObject[] targetRoots = _targetScene.GetRootGameObjects();
        for (int i = 0; i < targetRoots.Length; ++i)
        {
            targetRootNames.Add(targetRoots[i].name);
        }

        GameObject[] sourceRoots = _sourceScene.GetRootGameObjects();
        for (int i = 0; i < sourceRoots.Length; ++i)
        {
            GameObject sourceRoot = sourceRoots[i]; //EffectWork Root
            if (selectedNames.Contains(sourceRoot.name))continue;
            if (targetRootNames.Contains(sourceRoot.name))continue;
            if (sourceRoot.name.StartsWith("__"))continue;

            rootsList.Add(sourceRoot);
            selectedNames.Add(sourceRoot.name);
        }

        return rootsList;
    }

    /// <summary>
    /// Gameplayにある旧演出Rootを削除して二重配置を防ぎます。
    /// </summary>
    private static void RemoveTargetEffectRoots(Scene _scene)
    {
        for (int i = 0; i < EEffectRootNames.Length; ++i)
        {
            GameObject rootObject =
                FindRoot(_scene, EEffectRootNames[i]); //削除対象
            if (rootObject == null)continue;

            Object.DestroyImmediate(rootObject);
        }
    }

    /// <summary>
    /// 複数Rootを内部参照ごと複製する一時親を作成します。
    /// </summary>
    private static GameObject BuildSourceBundle(
        Scene _scene,
        List<GameObject> _rootsList)
    {
        GameObject bundle = new GameObject(ETemporaryBundleName);
        SceneManager.MoveGameObjectToScene(bundle, _scene);
        for (int i = 0; i < _rootsList.Count; ++i)
        {
            _rootsList[i].transform.SetParent(bundle.transform, true);
        }

        return bundle;
    }

    /// <summary>
    /// 一時的にまとめたEffectWorkのRootを元の階層へ戻します。
    /// </summary>
    private static void RestoreSourceRoots(GameObject _bundle)
    {
        while (_bundle.transform.childCount > 0)
        {
            _bundle.transform.GetChild(0).SetParent(null, true);
        }

        Object.DestroyImmediate(_bundle);
    }

    /// <summary>
    /// 複製した一時親の子をGameplay SceneのRootへ展開します。
    /// </summary>
    private static void UnpackBundle(
        GameObject _bundle,
        Scene _scene)
    {
        while (_bundle.transform.childCount > 0)
        {
            Transform child = _bundle.transform.GetChild(0); //展開対象
            child.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(child.gameObject, _scene);
        }
    }

    /// <summary>
    /// 複製後にEffectWorkを指している参照をGameplay側へ張り直します。
    /// </summary>
    private static int RebindSourceSceneReferences(
        Scene _sourceScene,
        Scene _targetScene)
    {
        Dictionary<string, Transform> targetTransforms =
            BuildTransformMap(_targetScene); //Gameplay階層Map
        Component[] targetComponents =
            CollectComponents(_targetScene); //検査対象Component
        int reboundCount = 0;
        for (int i = 0; i < targetComponents.Length; ++i)
        {
            SerializedObject serializedObject =
                new SerializedObject(targetComponents[i]);
            SerializedProperty property =
                serializedObject.GetIterator();
            bool b_enterChildren = true;
            while (property.Next(b_enterChildren))
            {
                b_enterChildren = true;
                if (property.propertyType
                    != SerializedPropertyType.ObjectReference)continue;

                Object sourceReference = property.objectReferenceValue;
                Object targetReference = ResolveTargetReference(
                    sourceReference,
                    _sourceScene,
                    targetTransforms);
                if (targetReference == null
                    || targetReference == sourceReference)continue;

                property.objectReferenceValue = targetReference;
                ++reboundCount;
            }

            if (serializedObject.ApplyModifiedPropertiesWithoutUndo())
            {
                EditorUtility.SetDirty(targetComponents[i]);
            }
        }

        return reboundCount;
    }

    /// <summary>
    /// 置換前のLiveEffectDeploymentを指していたEffectSystem参照を再設定します。
    /// </summary>
    private static int RebindRuntimeSystemReferences(
        Scene _scene,
        EffectSystem _effectSystem,
        VenueVoltageSystem _voltageSystem)
    {
        if (_effectSystem == null)
        {
            Debug.LogError(
                "反映後のGameplayにEffectSystemが見つかりません。");
            return 0;
        }

        if (_voltageSystem == null)
        {
            Debug.LogError(
                "反映後のGameplayにVenueVoltageSystemが見つかりません。");
            return 0;
        }

        Component[] components = CollectComponents(_scene); //全Component
        int reboundCount = 0;
        for (int i = 0; i < components.Length; ++i)
        {
            SerializedObject serializedObject =
                new SerializedObject(components[i]);
            SerializedProperty property =
                serializedObject.GetIterator();
            bool b_enterChildren = true;
            bool b_changed = false;
            while (property.Next(b_enterChildren))
            {
                b_enterChildren = true;
                if (property.propertyType
                    != SerializedPropertyType.ObjectReference)continue;
                Object targetReference = null; //再接続先
                if (property.name == "m_effectSystem")
                {
                    targetReference = _effectSystem;
                }
                else if (property.name == "m_venueVoltageSystem"
                    || property.name == "m_voltageSystem")
                {
                    targetReference = _voltageSystem;
                }

                if (targetReference == null)continue;

                property.objectReferenceValue = targetReference;
                b_changed = true;
                ++reboundCount;
            }

            if (!b_changed)continue;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(components[i]);
        }

        return reboundCount;
    }

    /// <summary>
    /// Source Scene上の参照に対応するGameplay上のObjectを取得します。
    /// </summary>
    private static Object ResolveTargetReference(
        Object _sourceReference,
        Scene _sourceScene,
        Dictionary<string, Transform> _targetTransforms)
    {
        GameObject sourceObject = GetGameObject(_sourceReference);
        if (sourceObject == null
            || sourceObject.scene != _sourceScene)return _sourceReference;

        string hierarchyKey =
            BuildHierarchyKey(sourceObject.transform); //対応位置
        if (!_targetTransforms.TryGetValue(
            hierarchyKey,
            out Transform targetTransform))return null;

        if (_sourceReference is GameObject)return targetTransform.gameObject;
        if (!(_sourceReference is Component sourceComponent))return null;

        Component[] sourceComponents =
            sourceObject.GetComponents(sourceComponent.GetType());
        Component[] targetComponents =
            targetTransform.GetComponents(sourceComponent.GetType());
        int componentIndex =
            GetComponentIndex(sourceComponents, sourceComponent); //同型番号
        if (componentIndex < 0
            || componentIndex >= targetComponents.Length)return null;

        return targetComponents[componentIndex];
    }

    /// <summary>
    /// Scene内Transformを重複名対応Hierarchy Keyで登録します。
    /// </summary>
    private static Dictionary<string, Transform> BuildTransformMap(
        Scene _scene)
    {
        Dictionary<string, Transform> transforms =
            new Dictionary<string, Transform>(); //Hierarchy Map
        GameObject[] roots = _scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; ++i)
        {
            Transform[] children =
                roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < children.Length; ++j)
            {
                transforms[BuildHierarchyKey(children[j])] = children[j];
            }
        }

        return transforms;
    }

    /// <summary>
    /// 同名Siblingの番号を含むHierarchy Keyを作成します。
    /// </summary>
    private static string BuildHierarchyKey(Transform _transform)
    {
        string key = GetHierarchySegment(_transform); //現在位置
        Transform parent = _transform.parent;
        while (parent != null)
        {
            key = GetHierarchySegment(parent) + "/" + key;
            parent = parent.parent;
        }

        return key;
    }

    /// <summary>
    /// Transform名と同名Sibling内番号を返します。
    /// </summary>
    private static string GetHierarchySegment(Transform _transform)
    {
        int sameNameIndex = 0; //同名内番号
        if (_transform.parent != null)
        {
            for (int i = 0; i < _transform.GetSiblingIndex(); ++i)
            {
                if (_transform.parent.GetChild(i).name == _transform.name)
                {
                    ++sameNameIndex;
                }
            }
        }

        return $"{_transform.name}[{sameNameIndex}]";
    }

    /// <summary>
    /// Scene内の全Componentを収集します。
    /// </summary>
    private static Component[] CollectComponents(Scene _scene)
    {
        List<Component> componentsList = new List<Component>(); //全Component
        GameObject[] roots = _scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; ++i)
        {
            componentsList.AddRange(
                roots[i].GetComponentsInChildren<Component>(true));
        }

        componentsList.RemoveAll(_component => _component == null);
        return componentsList.ToArray();
    }

    /// <summary>
    /// Objectが所属するGameObjectを返します。
    /// </summary>
    private static GameObject GetGameObject(Object _object)
    {
        if (_object is GameObject gameObject)return gameObject;
        if (_object is Component component)return component.gameObject;

        return null;
    }

    /// <summary>
    /// 同型Component配列内の対象番号を返します。
    /// </summary>
    private static int GetComponentIndex(
        Component[] _components,
        Component _target)
    {
        for (int i = 0; i < _components.Length; ++i)
        {
            if (_components[i] == _target)return i;
        }

        return -1;
    }

    /// <summary>
    /// Scene直下から指定名のRootを取得します。
    /// </summary>
    private static GameObject FindRoot(
        Scene _scene,
        string _name)
    {
        GameObject[] roots = _scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; ++i)
        {
            if (roots[i].name == _name)return roots[i];
        }

        return null;
    }

    /// <summary>
    /// Scene内から指定型Componentを一つ取得します。
    /// </summary>
    private static T FindComponent<T>(Scene _scene)
        where T : Component
    {
        GameObject[] roots = _scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; ++i)
        {
            T component = roots[i].GetComponentInChildren<T>(true);
            if (component != null)return component;
        }

        return null;
    }
}
