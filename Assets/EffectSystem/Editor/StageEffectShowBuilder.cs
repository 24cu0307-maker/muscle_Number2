/*━━━━━━━━━*
*@file StageEffectShowBuilder.cs*
*@brief ステージ地形へ60個のEffectと20本のTimelineを生成する*
*@author 24CU0000 Name*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Gameplay_EffectWork専用のEditor生成機能*
*━━━━━━━━━*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// ステージBoundsを基準にEffect配置とTimeline一式を生成します。
/// </summary>
public static class StageEffectShowBuilder
{
    private const string EStageName = "stage_0728_2"; //ステージObject名
    private const string EPlayableStageName = "stage"; //キャラクター足場の子Object名
    private const string EGeneratedRootName = "StageGeneratedEffects60"; //生成Root
    private const string ESpotLightRootName = "SpotLight"; //SpotLight親
    private const string EBeamRootName = "Beam"; //Beam親
    private const string EParticleRootName = "Particle"; //Particle親
    private const string EDirectorRootName = "StageGeneratedDirectors"; //Director親
    private const string ETimelineFolder =
        "Assets/EffectSystem/EffectTimeLine/GeneratedStageShow"; //Timeline保存先
    private const string ESpotLightFolder =
        "Assets/EffectSystem/SpotlightCone/Variants"; //SpotLight Prefab先
    private const string EBeamFolder =
        "Assets/EffectSystem/LaserBeam/Variants"; //Beam Prefab先
    private const string EParticleFolder =
        "Assets/EffectSystem/ParticleEffects"; //Particle Prefab先
    private const int ECategoryEffectCount = 20; //各カテゴリ生成数
    private const int ETimelineCount = 10; //各Timeline生成数
    private const int ETotalEffectCount = 60; //合計生成数
    private const int EMenuPriority = 155; //Menu表示順
    private const float EMinimumRadius = 8.0f; //最小配置半径
    private const float EMaximumRadius = 36.0f; //最大配置半径
    private const float ESpotLightHeight = 6.0f; //SpotLight床上高さ
    private const float EBeamHeight = 4.5f; //Beam床上高さ
    private const float EParticleHeight = 0.15f; //Particle床上高さ
    private const float ETargetHeight = 5.0f; //照射中心高さ
    private const float ERaycastHeight = 100.0f; //床検索開始高さ
    private const float ERaycastDistance = 300.0f; //床検索距離
    private const double EEffectDuration = 4.0d; //Effect Clip長
    private const double EEffectInterval = 0.35d; //Effect開始間隔

    /// <summary>
    /// Menuから生成または既存Bindingの修復を行います。
    /// </summary>
    private static void BuildFromMenu()
    {
        Build(false);
    }

    /// <summary>
    /// Effect60個、Effect Timeline10本、Voltage Timeline10本を生成します。
    /// </summary>
    private static void Build(bool _bautomatic)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)return;

        GameObject stageObject = GameObject.Find(EStageName); //地形基準
        if (stageObject == null)
        {
            Debug.LogWarning($"{EStageName}が見つからないため生成できません。");
            return;
        }

        EnsureTimelineFolder();
        GameObject generatedRoot =
            GameObject.Find(EGeneratedRootName); //既存生成Root
        if (generatedRoot == null)
        {
            generatedRoot = new GameObject(EGeneratedRootName);
            Undo.RegisterCreatedObjectUndo(
                generatedRoot,
                "Create Stage Generated Effects");
        }

        GameObject playableStage =
            FindPlayableStage(stageObject); //キャラクターが乗る実ステージ
        if (playableStage == null)
        {
            Debug.LogWarning(
                $"{EStageName}内に子Object「{EPlayableStageName}」が見つかりません。");
            return;
        }

        Bounds stageBounds = CalculateStageBounds(playableStage); //実ステージ範囲
        List<GameObject> spotLightsList = CreateCategoryEffects(
            generatedRoot.transform,
            ESpotLightRootName,
            LoadPrefabs(ESpotLightFolder),
            stageObject,
            stageBounds,
            ESpotLightHeight,
            true,
            0.0f);
        List<GameObject> beamsList = CreateCategoryEffects(
            generatedRoot.transform,
            EBeamRootName,
            LoadPrefabs(EBeamFolder),
            stageObject,
            stageBounds,
            EBeamHeight,
            true,
            0.5f);
        List<GameObject> particlesList = CreateCategoryEffects(
            generatedRoot.transform,
            EParticleRootName,
            LoadPrefabs(EParticleFolder),
            stageObject,
            stageBounds,
            EParticleHeight,
            false,
            0.25f);

        GameObject directorRoot = GetOrCreateChild(
            generatedRoot.transform,
            EDirectorRootName); //Director格納先
        CreateEffectTimelines(
            directorRoot,
            spotLightsList,
            beamsList,
            particlesList);
        CreateVoltageTimelines(
            directorRoot,
            spotLightsList,
            beamsList,
            particlesList);

        EditorSceneManager.MarkSceneDirty(generatedRoot.scene);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(generatedRoot.scene);
        Selection.activeGameObject = generatedRoot;
        Debug.Log(
            $"ステージEffect {ETotalEffectCount}個、"
            + $"Effect Timeline {ETimelineCount}本、"
            + $"Voltage Timeline {ETimelineCount}本を"
            + $"{(_bautomatic ? "自動再配置" : "手動生成")}しました。"
            + $" 照射対象: {playableStage.name}"
            + $" / Center: {stageBounds.center}"
            + $" / Size: {stageBounds.size}");
    }

    /// <summary>
    /// 指定カテゴリのEffectをステージ外周へ20個生成します。
    /// </summary>
    private static List<GameObject> CreateCategoryEffects(
        Transform _parent,
        string _categoryname,
        List<GameObject> _prefabsList,
        GameObject _stageroot,
        Bounds _stagebounds,
        float _height,
        bool _baimatcenter,
        float _angleoffset)
    {
        GameObject categoryRoot =
            GetOrCreateChild(_parent, _categoryname); //カテゴリ親
        List<GameObject> effectsList = GetDirectChildren(
            categoryRoot.transform); //既存生成物
        if (_prefabsList.Count == 0)return effectsList;

        float radiusX = Mathf.Clamp(
            _stagebounds.extents.x * 1.2f,
            EMinimumRadius,
            EMaximumRadius); //横配置半径
        float radiusZ = Mathf.Clamp(
            _stagebounds.extents.z * 1.2f,
            EMinimumRadius,
            EMaximumRadius); //奥行配置半径
        Vector3 targetPosition =
            _stagebounds.center + Vector3.up * ETargetHeight; //照射中心
        for (int i = 0; i < ECategoryEffectCount; ++i)
        {
            float angle =
                ((float)i / ECategoryEffectCount + _angleoffset)
                * Mathf.PI
                * 2.0f; //外周角度
            Vector3 horizontalPosition = new Vector3(
                _stagebounds.center.x + Mathf.Cos(angle) * radiusX,
                _stagebounds.max.y + ERaycastHeight,
                _stagebounds.center.z + Mathf.Sin(angle) * radiusZ); //床検索位置
            Vector3 position = FindGroundPosition(
                horizontalPosition,
                _stagebounds,
                _stageroot); //床位置
            position.y += _height;
            GameObject effectObject = i < effectsList.Count
                ? effectsList[i]
                : null; //既存Effect
            if (effectObject == null)
            {
                GameObject prefab =
                    _prefabsList[i % _prefabsList.Count]; //今回Prefab
                effectObject =
                    PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (effectObject == null)continue;

                effectObject.name =
                    $"Generated_{_categoryname}_{i + 1:00}_{prefab.name}";
                effectObject.transform.SetParent(categoryRoot.transform, true);
                effectObject.SetActive(false);
                effectsList.Add(effectObject);
            }

            Undo.RecordObject(
                effectObject.transform,
                "Reposition Stage Effect");
            effectObject.transform.position = position;
            if (_baimatcenter)
            {
                Vector3 direction = targetPosition - position; //照射方向
                if (direction.sqrMagnitude > 0.001f)
                {
                    effectObject.transform.rotation =
                        Quaternion.LookRotation(direction.normalized, Vector3.up);
                }
            }

        }

        return effectsList;
    }

    /// <summary>
    /// ステージColliderへRaycastして床位置を返します。
    /// </summary>
    private static Vector3 FindGroundPosition(
        Vector3 _rayorigin,
        Bounds _stagebounds,
        GameObject _stageroot)
    {
        RaycastHit[] hits = Physics.RaycastAll(
            _rayorigin,
            Vector3.down,
            ERaycastDistance,
            ~0,
            QueryTriggerInteraction.Ignore); //床候補
        float nearestDistance = float.MaxValue; //最短距離
        Vector3 selectedPosition = new Vector3(
            _rayorigin.x,
            _stagebounds.min.y,
            _rayorigin.z); //床未検出時
        for (int i = 0; i < hits.Length; ++i)
        {
            if (!hits[i].collider.transform.IsChildOf(
                _stageroot.transform))continue;
            if (hits[i].distance >= nearestDistance)continue;

            nearestDistance = hits[i].distance;
            selectedPosition = hits[i].point;
        }

        return selectedPosition;
    }

    /// <summary>
    /// 60個を組み合わせたEffect Timelineを10本生成します。
    /// </summary>
    private static void CreateEffectTimelines(
        GameObject _directorroot,
        List<GameObject> _spotlightsList,
        List<GameObject> _beamsList,
        List<GameObject> _particlesList)
    {
        for (int i = 0; i < ETimelineCount; ++i)
        {
            string effectName = $"StageEffect_{i + 1:00}"; //登録名
            TimelineAsset timeline = GetOrCreateTimeline(effectName); //Timeline
            PlayableDirector director =
                GetOrCreateDirector(_directorroot, effectName); //専用Director
            if (!HasTracks(timeline))
            {
                AddEffectTracks(
                    timeline,
                    director,
                    _spotlightsList,
                    _beamsList,
                    _particlesList,
                    i);
            }

            director.playableAsset = timeline;
            director.playOnAwake = false;
            RebindTimeline(timeline, director);
            RegisterEffect(effectName, timeline, director);
        }
    }

    /// <summary>
    /// Voltage変化と60個を組み合わせたTimelineを10本生成します。
    /// </summary>
    private static void CreateVoltageTimelines(
        GameObject _directorroot,
        List<GameObject> _spotlightsList,
        List<GameObject> _beamsList,
        List<GameObject> _particlesList)
    {
        for (int i = 0; i < ETimelineCount; ++i)
        {
            string effectName = $"StageVoltage_{i + 1:00}"; //登録名
            TimelineAsset timeline = GetOrCreateTimeline(effectName); //Timeline
            PlayableDirector director =
                GetOrCreateDirector(_directorroot, effectName); //専用Director
            if (!HasTracks(timeline))
            {
                GroupTrack voltageGroup =
                    timeline.CreateTrack<GroupTrack>(null, "Voltage"); //大見出し
                AddVoltagePattern(
                    timeline,
                    director,
                    voltageGroup,
                    i);
                AddEffectGroups(
                    timeline,
                    director,
                    voltageGroup,
                    _spotlightsList,
                    _beamsList,
                    _particlesList,
                    i);
            }

            director.playableAsset = timeline;
            director.playOnAwake = false;
            RebindTimeline(timeline, director);
            RegisterEffect(effectName, timeline, director);
        }
    }

    /// <summary>
    /// Effect Timelineへカテゴリ別Activation Trackを追加します。
    /// </summary>
    private static void AddEffectTracks(
        TimelineAsset _timeline,
        PlayableDirector _director,
        List<GameObject> _spotlightsList,
        List<GameObject> _beamsList,
        List<GameObject> _particlesList,
        int _patternindex)
    {
        GroupTrack rootGroup =
            _timeline.CreateTrack<GroupTrack>(null, "Live Effects"); //大見出し
        AddEffectGroups(
            _timeline,
            _director,
            rootGroup,
            _spotlightsList,
            _beamsList,
            _particlesList,
            _patternindex);
    }

    /// <summary>
    /// SpotLight、Beam、Particleの中見出しと各Trackを追加します。
    /// </summary>
    private static void AddEffectGroups(
        TimelineAsset _timeline,
        PlayableDirector _director,
        GroupTrack _parentgroup,
        List<GameObject> _spotlightsList,
        List<GameObject> _beamsList,
        List<GameObject> _particlesList,
        int _patternindex)
    {
        AddCategoryTracks(
            _timeline,
            _director,
            _parentgroup,
            ESpotLightRootName,
            _spotlightsList,
            _patternindex);
        AddCategoryTracks(
            _timeline,
            _director,
            _parentgroup,
            EBeamRootName,
            _beamsList,
            _patternindex + 2);
        AddCategoryTracks(
            _timeline,
            _director,
            _parentgroup,
            EParticleRootName,
            _particlesList,
            _patternindex + 4);
    }

    /// <summary>
    /// 一カテゴリの全EffectへPattern別のActivation Clipを追加します。
    /// </summary>
    private static void AddCategoryTracks(
        TimelineAsset _timeline,
        PlayableDirector _director,
        GroupTrack _parentgroup,
        string _groupname,
        List<GameObject> _effectsList,
        int _patternindex)
    {
        GroupTrack group =
            _timeline.CreateTrack<GroupTrack>(
                _parentgroup,
                _groupname); //中見出し
        for (int i = 0; i < _effectsList.Count; ++i)
        {
            GameObject effectObject = _effectsList[i]; //対象Effect
            ActivationTrack track =
                _timeline.CreateTrack<ActivationTrack>(
                    group,
                    effectObject.name);
            track.postPlaybackState =
                ActivationTrack.PostPlaybackState.Inactive;
            TimelineClip clip = track.CreateDefaultClip(); //再生区間
            clip.displayName = effectObject.name;
            clip.start =
                ((i + _patternindex * 2) % ECategoryEffectCount)
                * EEffectInterval;
            clip.duration =
                EEffectDuration + (_patternindex % 3);
            _director.SetGenericBinding(track, effectObject);
        }
    }

    /// <summary>
    /// Pattern番号ごとに異なるVoltage Clip列を作成します。
    /// </summary>
    private static void AddVoltagePattern(
        TimelineAsset _timeline,
        PlayableDirector _director,
        GroupTrack _group,
        int _patternindex)
    {
        VoltageTrack track =
            _timeline.CreateTrack<VoltageTrack>(
                _group,
                "Voltage Value"); //Voltage Track
        int clipCount = 2 + _patternindex % 4; //Pattern別Clip数
        float previousVoltage =
            _patternindex % 2 == 0 ? 0.0f : 80.0f; //開始Voltage
        for (int i = 0; i < clipCount; ++i)
        {
            float nextVoltage = Mathf.Repeat(
                (_patternindex + 1) * 17.0f + (i + 1) * 28.0f,
                101.0f); //次のVoltage
            TimelineClip clip = track.CreateDefaultClip(); //Voltage区間
            clip.displayName =
                $"Voltage {previousVoltage:F0} → {nextVoltage:F0}";
            clip.start = i * 5.0d;
            clip.duration = 5.0d;
            VoltagePlayableAsset asset =
                clip.asset as VoltagePlayableAsset; //値設定先
            if (asset != null)
            {
                asset.SetVoltageRange(previousVoltage, nextVoltage);
            }

            previousVoltage = nextVoltage;
        }

        VenueVoltageSystem voltageSystem =
            Object.FindFirstObjectByType<VenueVoltageSystem>(); //Binding先
        if (voltageSystem != null)
        {
            _director.SetGenericBinding(track, voltageSystem);
        }
    }

    /// <summary>
    /// Timelineの全出力Trackを現在Sceneへ再Bindingします。
    /// </summary>
    private static void RebindTimeline(
        TimelineAsset _timeline,
        PlayableDirector _director)
    {
        foreach (TrackAsset track in _timeline.GetOutputTracks())
        {
            if (track is VoltageTrack)
            {
                VenueVoltageSystem voltageSystem =
                    Object.FindFirstObjectByType<VenueVoltageSystem>();
                if (voltageSystem != null)
                {
                    _director.SetGenericBinding(track, voltageSystem);
                }

                continue;
            }

            if (!(track is ActivationTrack))continue;

            GameObject effectObject = GameObject.Find(track.name); //名前対応先
            if (effectObject != null)
            {
                _director.SetGenericBinding(track, effectObject);
            }
        }
    }

    /// <summary>
    /// 生成TimelineをEffectSystemへ名前付き登録します。
    /// </summary>
    private static void RegisterEffect(
        string _effectname,
        TimelineAsset _timeline,
        PlayableDirector _director)
    {
        EffectSystem effectSystem =
            Object.FindFirstObjectByType<EffectSystem>(); //登録先
        if (effectSystem == null)return;

        SerializedObject serializedSystem =
            new SerializedObject(effectSystem); //EffectSystem設定
        SerializedProperty effects =
            serializedSystem.FindProperty("m_effectDatas"); //Effect一覧
        int index = -1; //既存登録位置
        for (int i = 0; i < effects.arraySize; ++i)
        {
            SerializedProperty current =
                effects.GetArrayElementAtIndex(i); //確認項目
            if (current.FindPropertyRelative("m_effectName").stringValue
                == _effectname)
            {
                index = i;
                break;
            }
        }

        if (index < 0)
        {
            index = effects.arraySize;
            effects.InsertArrayElementAtIndex(index);
        }

        SerializedProperty effect =
            effects.GetArrayElementAtIndex(index); //登録項目
        effect.FindPropertyRelative("m_effectName").stringValue = _effectname;
        effect.FindPropertyRelative("m_playDelaySeconds").floatValue = 0.0f;

        effect.FindPropertyRelative("m_cameraSequence").objectReferenceValue =
            null;
        effect.FindPropertyRelative("m_timeline").objectReferenceValue =
            _timeline;
        effect.FindPropertyRelative("m_director").objectReferenceValue =
            _director;
        serializedSystem.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(effectSystem);
    }

    /// <summary>
    /// ステージ配下Rendererの統合Boundsを計算します。
    /// </summary>
    private static Bounds CalculateStageBounds(GameObject _stageobject)
    {
        Renderer[] renderers =
            _stageobject.GetComponentsInChildren<Renderer>(true); //Stage Renderer
        if (renderers.Length == 0)
        {
            return new Bounds(_stageobject.transform.position, Vector3.one * 50.0f);
        }

        Bounds bounds = renderers[0].bounds; //統合Bounds
        for (int i = 1; i < renderers.Length; ++i)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    /// <summary>
    /// FBX Root配下からキャラクターが乗るstage子Objectを検索します。
    /// </summary>
    private static GameObject FindPlayableStage(GameObject _stageroot)
    {
        Transform[] transforms =
            _stageroot.GetComponentsInChildren<Transform>(true); //FBX階層
        for (int i = 0; i < transforms.Length; ++i)
        {
            Transform current = transforms[i]; //確認対象
            if (current == _stageroot.transform)continue;
            if (!string.Equals(
                current.name,
                EPlayableStageName,
                System.StringComparison.OrdinalIgnoreCase))continue;
            if (current.GetComponentsInChildren<Renderer>(true).Length == 0)continue;

            return current.gameObject;
        }

        return null;
    }

    /// <summary>
    /// 指定Folder以下のPrefabを名前順で取得します。
    /// </summary>
    private static List<GameObject> LoadPrefabs(string _folder)
    {
        string[] guids = AssetDatabase.FindAssets(
            "t:Prefab",
            new[] {_folder}); //Prefab GUID一覧
        List<GameObject> prefabsList = new List<GameObject>(); //取得結果
        for (int i = 0; i < guids.Length; ++i)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]); //Asset Path
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path); //Prefab
            if (prefab != null)
            {
                prefabsList.Add(prefab);
            }
        }

        prefabsList.Sort(
            (_left, _right) =>
                string.CompareOrdinal(_left.name, _right.name));
        return prefabsList;
    }

    /// <summary>
    /// 指定名のTimelineを取得または生成します。
    /// </summary>
    private static TimelineAsset GetOrCreateTimeline(string _name)
    {
        string path = $"{ETimelineFolder}/{_name}.playable"; //保存先
        TimelineAsset timeline =
            AssetDatabase.LoadAssetAtPath<TimelineAsset>(path); //既存Timeline
        if (timeline != null)return timeline;

        timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, path);
        return timeline;
    }

    /// <summary>
    /// 専用Directorを取得または生成します。
    /// </summary>
    private static PlayableDirector GetOrCreateDirector(
        GameObject _root,
        string _name)
    {
        GameObject directorObject = GetOrCreateChild(
            _root.transform,
            $"{_name}_Director"); //Director Object
        PlayableDirector director =
            directorObject.GetComponent<PlayableDirector>(); //再生Component
        if (director == null)
        {
            director = Undo.AddComponent<PlayableDirector>(directorObject);
        }

        director.playOnAwake = false;
        return director;
    }

    /// <summary>
    /// 親直下の指定名Objectを取得または生成します。
    /// </summary>
    private static GameObject GetOrCreateChild(
        Transform _parent,
        string _name)
    {
        Transform child = _parent.Find(_name); //既存Child
        if (child != null)return child.gameObject;

        GameObject childObject = new GameObject(_name); //新規Child
        childObject.transform.SetParent(_parent, false);
        Undo.RegisterCreatedObjectUndo(childObject, $"Create {_name}");
        return childObject;
    }

    /// <summary>
    /// 親直下のGameObjectを取得します。
    /// </summary>
    private static List<GameObject> GetDirectChildren(Transform _parent)
    {
        List<GameObject> childrenList = new List<GameObject>(); //取得結果
        for (int i = 0; i < _parent.childCount; ++i)
        {
            childrenList.Add(_parent.GetChild(i).gameObject);
        }

        return childrenList;
    }

    /// <summary>
    /// TimelineにTrackが存在するか返します。
    /// </summary>
    private static bool HasTracks(TimelineAsset _timeline)
    {
        foreach (TrackAsset track in _timeline.GetRootTracks())
        {
            if (track != null)return true;
        }

        return false;
    }

    /// <summary>
    /// Timeline保存Folderを準備します。
    /// </summary>
    private static void EnsureTimelineFolder()
    {
        string parent =
            "Assets/EffectSystem/EffectTimeLine"; //親Folder
        if (!AssetDatabase.IsValidFolder(parent))
        {
            AssetDatabase.CreateFolder(
                "Assets/EffectSystem",
                "EffectTimeLine");
        }

        if (!AssetDatabase.IsValidFolder(ETimelineFolder))
        {
            AssetDatabase.CreateFolder(
                parent,
                "GeneratedStageShow");
        }
    }
}
