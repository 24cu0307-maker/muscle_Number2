/*━━━━━━━━━*
*@file VoltageTimelineTemplateBuilder.cs*
*@brief Voltageとライブ演出を階層化したTimeline Templateを生成する*
*@author 24CU0000 Name*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks 現在SceneのEffect配置をTrackへBindingするEditor専用機能*
*━━━━━━━━━*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// Voltage Pattern内の一つのClip設定です。
/// </summary>
public readonly struct SVoltagePatternClip
{
    public readonly string m_name; //Clip表示名
    public readonly double m_start; //開始秒
    public readonly double m_duration; //長さ
    public readonly float m_startVoltage; //開始Voltage
    public readonly float m_endVoltage; //終了Voltage

    /// <summary>
    /// Voltage Clip設定を生成します。
    /// </summary>
    public SVoltagePatternClip(
        string _name,
        double _start,
        double _duration,
        float _startvoltage,
        float _endvoltage)
    {
        m_name = _name;
        m_start = _start;
        m_duration = _duration;
        m_startVoltage = _startvoltage;
        m_endVoltage = _endvoltage;
    }
}

/// <summary>
/// 一つのVoltage Timeline Pattern設定です。
/// </summary>
public readonly struct SVoltagePattern
{
    public readonly string m_assetName; //Timeline Asset名
    public readonly string m_effectName; //EffectSystem登録名
    public readonly string m_directorName; //Director Object名
    public readonly SVoltagePatternClip[] m_clips; //Voltage Clip一覧

    /// <summary>
    /// Voltage Pattern設定を生成します。
    /// </summary>
    public SVoltagePattern(
        string _assetname,
        string _effectname,
        string _directorname,
        SVoltagePatternClip[] _clips)
    {
        m_assetName = _assetname;
        m_effectName = _effectname;
        m_directorName = _directorname;
        m_clips = _clips;
    }
}

/// <summary>
/// Voltageを大見出しにしたライブ演出Timeline Templateを生成します。
/// </summary>
public static class VoltageTimelineTemplateBuilder
{
    private const string EEffectSystemFolder = "Assets/EffectSystem"; //親Folder
    private const string ETimelineFolder =
        "Assets/EffectSystem/EffectTimeLine"; //Timeline親Folder
    private const string ETemplateFolder =
        "Assets/EffectSystem/EffectTimeLine/Templates"; //Template保存先
    private const string ETemplatePath =
        "Assets/EffectSystem/EffectTimeLine/Templates/VoltageLiveTemplate.playable"; //保存先
    private const string EDirectorName =
        "VoltageTimelineTemplateDirector"; //Scene Director名
    private const string EEffectName = "VoltageTimeline"; //EffectSystem登録名
    private const string EVoltageGroupName = "Voltage"; //大見出し
    private const string EVoltageTrackName = "Voltage Value"; //Voltage制御Track名
    private const string ESpotLightGroupName = "SpotLight"; //SpotLight見出し
    private const string EBeamGroupName = "Beam"; //Beam見出し
    private const string EParticleGroupName = "Particle"; //Particle見出し
    private const string ESpotLightRootName = "EffectSpotLight"; //Scene Root名
    private const string EBeamRootName = "EffectLaser"; //Scene Root名
    private const string EParticleRootName = "EffectParticle"; //Scene Root名
    private const double ETemplateDurationSeconds = 30.0d; //初期Voltage Clip長
    private const float EStartVoltage = 0.0f; //初期開始Voltage
    private const float EEndVoltage = 100.0f; //初期終了Voltage
    private const double ETimelineStartSeconds = 0.0d; //Clip開始時刻
    private const int EMenuPriority = 152; //Menu表示順
    private const int EPatternMenuPriority = 153; //Pattern Menu表示順

    /// <summary>
    /// 現在Sceneの配置を使用してVoltage Timeline Templateを生成します。
    /// </summary>
    private static void CreateTemplate()
    {
        EnsureFolders();
        TimelineAsset existingTimeline =
            AssetDatabase.LoadAssetAtPath<TimelineAsset>(ETemplatePath); //既存Template
        if (existingTimeline != null)
        {
            PlayableDirector existingDirector =
                GetOrCreateDirector(EDirectorName); //再設定対象
            existingDirector.playableAsset = existingTimeline;
            existingDirector.playOnAwake = false;
            RebindTimeline(existingTimeline, existingDirector);
            RegisterEffectSystem(
                existingTimeline,
                existingDirector,
                EEffectName);
            EditorUtility.SetDirty(existingDirector);
            EditorSceneManager.MarkSceneDirty(
                existingDirector.gameObject.scene);
            Selection.activeObject = existingTimeline;
            EditorGUIUtility.PingObject(existingTimeline);
            Debug.Log(
                "既存Voltage Timeline TemplateをSceneとEffectSystemへ再設定しました。");
            return;
        }

        TimelineAsset timeline =
            ScriptableObject.CreateInstance<TimelineAsset>(); //新規Template
        AssetDatabase.CreateAsset(timeline, ETemplatePath);
        PlayableDirector director =
            GetOrCreateDirector(EDirectorName); //Scene再生Director
        director.playableAsset = timeline;
        director.playOnAwake = false;

        GroupTrack voltageGroup =
            timeline.CreateTrack<GroupTrack>(
                null,
                EVoltageGroupName); //大見出し
        AddVoltageTrack(timeline, director, voltageGroup);
        AddEffectGroup(
            timeline,
            director,
            voltageGroup,
            ESpotLightGroupName,
            ESpotLightRootName);
        AddEffectGroup(
            timeline,
            director,
            voltageGroup,
            EBeamGroupName,
            EBeamRootName);
        AddEffectGroup(
            timeline,
            director,
            voltageGroup,
            EParticleGroupName,
            EParticleRootName);
        RegisterEffectSystem(
            timeline,
            director,
            EEffectName);

        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(director);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        Selection.activeObject = timeline;
        EditorGUIUtility.PingObject(timeline);
        Debug.Log(
            $"Voltage Timeline Templateを生成しました: {ETemplatePath}");
    }

    /// <summary>
    /// ライブ向けVoltage Timelineを5種類生成または再設定します。
    /// </summary>
    private static void CreatePatterns()
    {
        EnsureFolders();
        SVoltagePattern[] patterns = CreatePatternDefinitions(); //生成設定一覧
        for (int i = 0; i < patterns.Length; ++i)
        {
            CreateOrUpdatePattern(patterns[i]);
        }

        AssetDatabase.SaveAssets();
        Debug.Log(
            $"Voltage Timeline Patternを{patterns.Length}種類準備しました。");
    }

    /// <summary>
    /// 一つのVoltage Pattern AssetとScene Bindingを準備します。
    /// </summary>
    private static void CreateOrUpdatePattern(SVoltagePattern _pattern)
    {
        string path =
            $"{ETemplateFolder}/{_pattern.m_assetName}.playable"; //保存先
        TimelineAsset timeline =
            AssetDatabase.LoadAssetAtPath<TimelineAsset>(path); //既存Pattern
        PlayableDirector director =
            GetOrCreateDirector(_pattern.m_directorName); //専用Director
        if (timeline == null)
        {
            timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            AssetDatabase.CreateAsset(timeline, path);
            CreatePatternTracks(
                timeline,
                director,
                _pattern.m_clips);
        }

        director.playableAsset = timeline;
        director.playOnAwake = false;
        RebindTimeline(timeline, director);
        RegisterEffectSystem(
            timeline,
            director,
            _pattern.m_effectName);
        EditorUtility.SetDirty(timeline);
        EditorUtility.SetDirty(director);
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
    }

    /// <summary>
    /// Voltage Pattern共通の階層と指定Voltage Clip群を作成します。
    /// </summary>
    private static void CreatePatternTracks(
        TimelineAsset _timeline,
        PlayableDirector _director,
        SVoltagePatternClip[] _clips)
    {
        GroupTrack voltageGroup =
            _timeline.CreateTrack<GroupTrack>(
                null,
                EVoltageGroupName); //大見出し
        AddVoltageTrack(
            _timeline,
            _director,
            voltageGroup,
            _clips);
        AddEffectGroup(
            _timeline,
            _director,
            voltageGroup,
            ESpotLightGroupName,
            ESpotLightRootName);
        AddEffectGroup(
            _timeline,
            _director,
            voltageGroup,
            EBeamGroupName,
            EBeamRootName);
        AddEffectGroup(
            _timeline,
            _director,
            voltageGroup,
            EParticleGroupName,
            EParticleRootName);
    }

    /// <summary>
    /// ライブ向けの5種類のVoltage変化設定を返します。
    /// </summary>
    private static SVoltagePattern[] CreatePatternDefinitions()
    {
        return new[]
        {
            new SVoltagePattern(
                "Voltage_01_GradualRise",
                "Voltage_01_GradualRise",
                "Voltage_01_GradualRise_Director",
                new[]
                {
                    new SVoltagePatternClip(
                        "Gradual 0 → 100",
                        0.0d,
                        30.0d,
                        0.0f,
                        100.0f)
                }),
            new SVoltagePattern(
                "Voltage_02_StepUp",
                "Voltage_02_StepUp",
                "Voltage_02_StepUp_Director",
                new[]
                {
                    new SVoltagePatternClip("Step 1", 0.0d, 8.0d, 0.0f, 30.0f),
                    new SVoltagePatternClip("Step 2", 8.0d, 8.0d, 30.0f, 60.0f),
                    new SVoltagePatternClip("Step 3", 16.0d, 8.0d, 60.0f, 100.0f)
                }),
            new SVoltagePattern(
                "Voltage_03_Wave",
                "Voltage_03_Wave",
                "Voltage_03_Wave_Director",
                new[]
                {
                    new SVoltagePatternClip("Wave Up 1", 0.0d, 5.0d, 20.0f, 75.0f),
                    new SVoltagePatternClip("Wave Down", 5.0d, 5.0d, 75.0f, 35.0f),
                    new SVoltagePatternClip("Wave Up 2", 10.0d, 6.0d, 35.0f, 90.0f),
                    new SVoltagePatternClip("Wave Peak", 16.0d, 4.0d, 90.0f, 100.0f)
                }),
            new SVoltagePattern(
                "Voltage_04_FinaleBurst",
                "Voltage_04_FinaleBurst",
                "Voltage_04_FinaleBurst_Director",
                new[]
                {
                    new SVoltagePatternClip("Build", 0.0d, 6.0d, 25.0f, 60.0f),
                    new SVoltagePatternClip("Finale Burst", 6.0d, 2.0d, 60.0f, 100.0f),
                    new SVoltagePatternClip("Keep Peak", 8.0d, 8.0d, 100.0f, 100.0f)
                }),
            new SVoltagePattern(
                "Voltage_05_CoolDown",
                "Voltage_05_CoolDown",
                "Voltage_05_CoolDown_Director",
                new[]
                {
                    new SVoltagePatternClip(
                        "Cool Down 100 → 0",
                        0.0d,
                        20.0d,
                        100.0f,
                        0.0f)
                })
        };
    }

    /// <summary>
    /// 既存TemplateのTrackを現在SceneのObjectへ再Bindingします。
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
                    Object.FindFirstObjectByType<VenueVoltageSystem>(); //Voltage先
                if (voltageSystem != null)
                {
                    _director.SetGenericBinding(track, voltageSystem);
                }

                continue;
            }

            if (!(track is ActivationTrack))continue;

            GameObject effectObject = GameObject.Find(track.name); //Effect先
            if (effectObject != null)
            {
                _director.SetGenericBinding(track, effectObject);
            }
        }
    }

    /// <summary>
    /// Voltage Timelineを現在SceneのEffectSystemへ登録します。
    /// </summary>
    private static void RegisterEffectSystem(
        TimelineAsset _timeline,
        PlayableDirector _director,
        string _effectname)
    {
        EffectSystem effectSystem =
            Object.FindFirstObjectByType<EffectSystem>(); //登録先
        if (effectSystem == null)
        {
            Debug.LogWarning(
                "EffectSystemが見つからないためVoltageTimelineを登録できません。");
            return;
        }

        SerializedObject serializedSystem =
            new SerializedObject(effectSystem); //EffectSystem設定
        SerializedProperty effects =
            serializedSystem.FindProperty("m_effectDatas"); //Effect一覧
        int effectIndex = FindEffectIndex(effects, _effectname); //登録位置
        if (effectIndex < 0)
        {
            effectIndex = effects.arraySize;
            effects.InsertArrayElementAtIndex(effectIndex);
        }

        SerializedProperty effect =
            effects.GetArrayElementAtIndex(effectIndex); //Voltage項目
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
    /// EffectSystem内から指定名のEffect番号を取得します。
    /// </summary>
    private static int FindEffectIndex(
        SerializedProperty _effects,
        string _effectname)
    {
        for (int i = 0; i < _effects.arraySize; ++i)
        {
            SerializedProperty effect =
                _effects.GetArrayElementAtIndex(i); //確認項目
            if (effect.FindPropertyRelative("m_effectName").stringValue
                == _effectname)return i;
        }

        return -1;
    }

    /// <summary>
    /// Voltage制御Trackと0から100へ変化する初期Clipを作成します。
    /// </summary>
    private static void AddVoltageTrack(
        TimelineAsset _timeline,
        PlayableDirector _director,
        GroupTrack _voltagegroup)
    {
        VoltageTrack voltageTrack =
            _timeline.CreateTrack<VoltageTrack>(
                _voltagegroup,
                EVoltageTrackName); //Voltage専用Track
        TimelineClip voltageClip = voltageTrack.CreateDefaultClip(); //初期Clip
        voltageClip.displayName = "Voltage 0 → 100";
        voltageClip.start = ETimelineStartSeconds;
        voltageClip.duration = ETemplateDurationSeconds;
        VoltagePlayableAsset voltageAsset =
            voltageClip.asset as VoltagePlayableAsset; //Clip設定
        if (voltageAsset != null)
        {
            voltageAsset.SetVoltageRange(EStartVoltage, EEndVoltage);
        }

        VenueVoltageSystem voltageSystem =
            Object.FindFirstObjectByType<VenueVoltageSystem>(); //Binding対象
        if (voltageSystem != null)
        {
            _director.SetGenericBinding(voltageTrack, voltageSystem);
        }
    }

    /// <summary>
    /// 指定された複数のVoltage Clipを専用Trackへ追加します。
    /// </summary>
    private static void AddVoltageTrack(
        TimelineAsset _timeline,
        PlayableDirector _director,
        GroupTrack _voltagegroup,
        SVoltagePatternClip[] _clips)
    {
        VoltageTrack voltageTrack =
            _timeline.CreateTrack<VoltageTrack>(
                _voltagegroup,
                EVoltageTrackName); //Voltage専用Track
        for (int i = 0; i < _clips.Length; ++i)
        {
            SVoltagePatternClip definition = _clips[i]; //現在Clip設定
            TimelineClip voltageClip = voltageTrack.CreateDefaultClip(); //Voltage Clip
            voltageClip.displayName = definition.m_name;
            voltageClip.start = definition.m_start;
            voltageClip.duration = definition.m_duration;
            VoltagePlayableAsset voltageAsset =
                voltageClip.asset as VoltagePlayableAsset; //値設定先
            if (voltageAsset != null)
            {
                voltageAsset.SetVoltageRange(
                    definition.m_startVoltage,
                    definition.m_endVoltage);
            }
        }

        VenueVoltageSystem voltageSystem =
            Object.FindFirstObjectByType<VenueVoltageSystem>(); //Binding対象
        if (voltageSystem != null)
        {
            _director.SetGenericBinding(voltageTrack, voltageSystem);
        }
    }

    /// <summary>
    /// 指定カテゴリGroupとScene内の各Effect用Activation Trackを作成します。
    /// </summary>
    private static void AddEffectGroup(
        TimelineAsset _timeline,
        PlayableDirector _director,
        GroupTrack _voltagegroup,
        string _groupname,
        string _rootname)
    {
        GroupTrack effectGroup =
            _timeline.CreateTrack<GroupTrack>(
                _voltagegroup,
                _groupname); //中見出し
        GameObject effectRoot = FindRoot(_rootname); //配置済みEffect Root
        if (effectRoot == null)return;

        List<GameObject> effectObjectsList =
            GetDirectChildren(effectRoot); //各Effect Object
        for (int i = 0; i < effectObjectsList.Count; ++i)
        {
            GameObject effectObject = effectObjectsList[i]; //Binding対象
            ActivationTrack activationTrack =
                _timeline.CreateTrack<ActivationTrack>(
                    effectGroup,
                    effectObject.name);
            activationTrack.postPlaybackState =
                ActivationTrack.PostPlaybackState.Inactive;
            _director.SetGenericBinding(activationTrack, effectObject);
        }
    }

    /// <summary>
    /// 現在Sceneから指定名のRoot Objectを取得します。
    /// </summary>
    private static GameObject FindRoot(string _name)
    {
        Scene scene = SceneManager.GetActiveScene(); //現在Scene
        if (!scene.IsValid())return null;

        GameObject[] rootObjects = scene.GetRootGameObjects(); //Scene Root一覧
        for (int i = 0; i < rootObjects.Length; ++i)
        {
            if (rootObjects[i].name == _name)return rootObjects[i];
        }

        return null;
    }

    /// <summary>
    /// Root直下の各Effect Objectを取得します。
    /// </summary>
    private static List<GameObject> GetDirectChildren(GameObject _root)
    {
        List<GameObject> childrenList = new List<GameObject>(); //取得結果
        for (int i = 0; i < _root.transform.childCount; ++i)
        {
            childrenList.Add(_root.transform.GetChild(i).gameObject);
        }

        return childrenList;
    }

    /// <summary>
    /// Template再生用Directorを現在Sceneへ取得または生成します。
    /// </summary>
    private static PlayableDirector GetOrCreateDirector(string _directorname)
    {
        GameObject directorObject =
            GameObject.Find(_directorname); //既存Director Object
        if (directorObject == null)
        {
            directorObject = new GameObject(_directorname);
            Undo.RegisterCreatedObjectUndo(
                directorObject,
                "Create Voltage Timeline Director");
        }

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
    /// Timeline Template保存用Folderを準備します。
    /// </summary>
    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(ETimelineFolder))
        {
            AssetDatabase.CreateFolder(
                EEffectSystemFolder,
                "EffectTimeLine");
        }

        if (!AssetDatabase.IsValidFolder(ETemplateFolder))
        {
            AssetDatabase.CreateFolder(
                ETimelineFolder,
                "Templates");
        }
    }
}
