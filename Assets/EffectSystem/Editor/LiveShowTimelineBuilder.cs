/*━━━━━━━━━*
*@file LiveShowTimelineBuilder.cs*
*@brief InGame配置済みエフェクトから複数のライブ演出Timelineを生成する*
*@author 24cu0312 久場洸太*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks Editor専用*
*━━━━━━━━━*/

using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

/// <summary>
/// ライブ向けの5種類のTimeline、Director、EffectSystem設定を生成します。
/// </summary>
public static class LiveShowTimelineBuilder
{
    private const string EScenePath = "Assets/Scenes/InGame.unity"; //対象シーン
    private const string ETimelineFolder = "Assets/EffectSystem/EffectTimeLine/LiveShows"; //保存先
    private const string EAnimationFolder =
        "Assets/EffectSystem/EffectTimeLine/LiveShows/Animations"; //回転Animation保存先
    private const string EDirectorRootName = "LiveShowDirectors"; //Director親名
    private const string EParticleRootName = "EffectParticle"; //Particle親名
    private const string ESpotRootName = "EffectSpotLight"; //SpotLight親名
    private const string EBeamRootName = "EffectLaser"; //Beam親名
    private const double EFrameRate = 60.0; //Timelineフレームレート
    private const double EShortDuration = 0.8; //短い点灯時間
    private const double ENormalDuration = 2.0; //通常点灯時間
    private const float ESpotSweepAngle = 38.0f; //SpotLight首振り角度
    private const float EBeamSweepAngle = 72.0f; //Beam首振り角度
    private const float EParticleSweepAngle = 18.0f; //Particle回転角度
    private const float ESpotTiltAngle = 12.0f; //SpotLight上下角度
    private const float EBeamTiltAngle = 24.0f; //Beam上下角度
    private const float EParticleTiltAngle = 8.0f; //Particle上下角度
    private const int EDefaultRotationCycles = 1; //標準往復回数
    private const int EFastRotationCycles = 3; //高速往復回数
    private const int EShowCount = 5; //生成する演出数
    private const int EMenuPriority = 131; //メニュー表示順

    private static readonly string[] m_showNames =
    {
        "Live_01_Opening",
        "Live_02_ColorWave",
        "Live_03_LaserRush",
        "Live_04_ParticleBurst",
        "Live_05_Finale"
    }; //生成する演出名

    /// <summary>
    /// InGameの現在配置から5種類のライブ演出を生成します。
    /// </summary>
    [MenuItem(
        "Tools/Effect System/Build Live Show Timelines",
        priority = EMenuPriority)]
    private static void Build()
    {
        Scene scene = GetInGameScene(); //対象シーン
        if (!scene.IsValid())return;

        EnsureFolders();
        GameObject directorRoot =
            GetOrCreateRoot(scene, EDirectorRootName); //Director格納先
        List<GameObject> particles =
            CollectChildren(FindRoot(scene, EParticleRootName)); //Particle一覧
        List<GameObject> spots =
            CollectChildren(FindRoot(scene, ESpotRootName)); //SpotLight一覧
        List<GameObject> beams =
            CollectChildren(FindRoot(scene, EBeamRootName)); //Beam一覧
        List<PlayableDirector> directors = new List<PlayableDirector>(); //生成Director一覧
        List<TimelineAsset> timelines = new List<TimelineAsset>(); //生成Timeline一覧

        for (int i = 0; i < EShowCount; ++i)
        {
            TimelineAsset timeline = GetOrCreateTimeline(m_showNames[i]); //今回のTimeline
            ClearTracks(timeline);
            PlayableDirector director =
                GetOrCreateDirector(directorRoot, m_showNames[i]); //専用Director
            director.playableAsset = timeline;
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            CreateShow(i, timeline, director, particles, spots, beams);
            timelines.Add(timeline);
            directors.Add(director);
        }

        SetInitialInactive(particles);
        SetInitialInactive(spots);
        SetInitialInactive(beams);
        RegisterEffects(timelines, directors);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = directorRoot;
        Debug.Log("ライブ演出Timelineを5種類生成し、EffectSystemへ登録しました。");
    }

    /// <summary>
    /// 演出番号に対応するTrack構成を生成します。
    /// </summary>
    private static void CreateShow(
        int _showindex,
        TimelineAsset _timeline,
        PlayableDirector _director,
        List<GameObject> _particles,
        List<GameObject> _spots,
        List<GameObject> _beams)
    {
        switch (_showindex)
        {
            case 0:
                CreateOpening(_timeline, _director, _particles, _spots, _beams);
                break;
            case 1:
                CreateColorWave(_timeline, _director, _spots, _beams);
                break;
            case 2:
                CreateLaserRush(_timeline, _director, _spots, _beams);
                break;
            case 3:
                CreateParticleBurst(_timeline, _director, _particles, _spots, _beams);
                break;
            default:
                CreateFinale(_timeline, _director, _particles, _spots, _beams);
                break;
        }
    }

    /// <summary>
    /// 開幕向けにSpotLight、Beam、Particleの順で演出します。
    /// </summary>
    private static void CreateOpening(
        TimelineAsset _timeline,
        PlayableDirector _director,
        List<GameObject> _particles,
        List<GameObject> _spots,
        List<GameObject> _beams)
    {
        AddStaggered(_timeline, _director, _spots, "SpotLight", 0.0, 0.8, 2.8);
        AddStaggered(_timeline, _director, _beams, "Beam", 3.5, 0.25, 1.5);
        AddStaggered(_timeline, _director, _particles, "Particle", 7.0, 0.5, 3.0);
    }

    /// <summary>
    /// 色が流れるようにSpotLightとBeamを交互に演出します。
    /// </summary>
    private static void CreateColorWave(
        TimelineAsset _timeline,
        PlayableDirector _director,
        List<GameObject> _spots,
        List<GameObject> _beams)
    {
        AddStaggered(_timeline, _director, _spots, "SpotLight Wave", 0.0, 0.45, 1.8);
        AddStaggered(_timeline, _director, _beams, "Beam Wave", 0.2, 0.3, 1.2);
    }

    /// <summary>
    /// Beamを高速で展開して最後にSpotLightを重ねます。
    /// </summary>
    private static void CreateLaserRush(
        TimelineAsset _timeline,
        PlayableDirector _director,
        List<GameObject> _spots,
        List<GameObject> _beams)
    {
        AddStaggered(_timeline, _director, _beams, "Laser Rush", 0.0, 0.12, EShortDuration);
        AddSimultaneous(_timeline, _director, _spots, "SpotLight Accent", 2.0, 2.5);
    }

    /// <summary>
    /// Particleを中心にSpotLightとBeamをアクセントとして重ねます。
    /// </summary>
    private static void CreateParticleBurst(
        TimelineAsset _timeline,
        PlayableDirector _director,
        List<GameObject> _particles,
        List<GameObject> _spots,
        List<GameObject> _beams)
    {
        AddStaggered(_timeline, _director, _particles, "Particle Burst", 0.0, 0.6, 3.5);
        AddSimultaneous(_timeline, _director, _spots, "SpotLight", 1.0, 3.0);
        AddStaggered(_timeline, _director, _beams, "Beam Accent", 2.0, 0.2, 1.4);
    }

    /// <summary>
    /// 全種類を段階的に増やしてフィナーレを作ります。
    /// </summary>
    private static void CreateFinale(
        TimelineAsset _timeline,
        PlayableDirector _director,
        List<GameObject> _particles,
        List<GameObject> _spots,
        List<GameObject> _beams)
    {
        AddStaggered(_timeline, _director, _spots, "SpotLight Build", 0.0, 0.5, 9.0);
        AddStaggered(_timeline, _director, _beams, "Beam Build", 1.0, 0.18, 7.5);
        AddStaggered(_timeline, _director, _particles, "Particle Finale", 3.0, 0.35, 4.0);
    }

    /// <summary>
    /// 対象を時間差で点灯するTrack群を作成します。
    /// </summary>
    private static void AddStaggered(
        TimelineAsset _timeline,
        PlayableDirector _director,
        List<GameObject> _objects,
        string _groupname,
        double _start,
        double _interval,
        double _duration)
    {
        GroupTrack group = _timeline.CreateTrack<GroupTrack>(null, _groupname); //分類
        for (int i = 0; i < _objects.Count; ++i)
        {
            AddActivation(
                _timeline,
                _director,
                group,
                _objects[i],
                _start + (_interval * i),
                _duration);
        }
    }

    /// <summary>
    /// 対象を同時点灯するTrack群を作成します。
    /// </summary>
    private static void AddSimultaneous(
        TimelineAsset _timeline,
        PlayableDirector _director,
        List<GameObject> _objects,
        string _groupname,
        double _start,
        double _duration)
    {
        AddStaggered(
            _timeline,
            _director,
            _objects,
            _groupname,
            _start,
            0.0,
            _duration);
    }

    /// <summary>
    /// 一つの対象を指定時間だけ有効にするTrackを追加します。
    /// </summary>
    private static void AddActivation(
        TimelineAsset _timeline,
        PlayableDirector _director,
        GroupTrack _group,
        GameObject _object,
        double _start,
        double _duration)
    {
        ActivationTrack track =
            _timeline.CreateTrack<ActivationTrack>(_group, _object.name); //表示Track
        track.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
        TimelineClip clip = track.CreateDefaultClip(); //表示期間
        clip.displayName = _object.name;
        clip.start = _start;
        clip.duration = _duration;
        _director.SetGenericBinding(track, _object);
        AddRotation(
            _timeline,
            _director,
            _group,
            _object,
            _start,
            _duration);
    }

    /// <summary>
    /// 点灯期間へ対象に応じた回転Animation Trackを追加します。
    /// </summary>
    private static void AddRotation(
        TimelineAsset _timeline,
        PlayableDirector _director,
        GroupTrack _group,
        GameObject _object,
        double _start,
        double _duration)
    {
        Animator animator = _object.GetComponent<Animator>(); //Animation適用先
        if (animator == null)
        {
            animator = _object.AddComponent<Animator>();
        }

        bool b_beam = IsChildOf(_object, EBeamRootName); //Beam判定
        bool b_particle = IsChildOf(_object, EParticleRootName); //Particle判定
        float sweepAngle = b_beam
            ? EBeamSweepAngle
            : b_particle
                ? EParticleSweepAngle
                : ESpotSweepAngle; //左右振り幅
        float tiltAngle = b_beam
            ? EBeamTiltAngle
            : b_particle
                ? EParticleTiltAngle
                : ESpotTiltAngle; //上下振り幅
        int cycleCount = b_beam
            && (_timeline.name.Contains("LaserRush")
                || _timeline.name.Contains("Finale"))
            ? EFastRotationCycles
            : EDefaultRotationCycles; //往復回数

        AnimationClip animationClip = GetOrCreateRotationClip(
            _timeline.name,
            _object,
            (float)_duration,
            sweepAngle,
            tiltAngle,
            cycleCount); //回転Animation
        AnimationTrack animationTrack =
            _timeline.CreateTrack<AnimationTrack>(
                _group,
                $"{_object.name} Rotation"); //回転Track
        animationTrack.trackOffset = TrackOffset.ApplyTransformOffsets;
        TimelineClip timelineClip =
            animationTrack.CreateClip<AnimationPlayableAsset>(); //回転Clip
        AnimationPlayableAsset animationAsset =
            timelineClip.asset as AnimationPlayableAsset; //Clip Asset
        if (animationAsset != null)
        {
            animationAsset.clip = animationClip;
        }

        timelineClip.displayName = $"{_object.name} Sweep";
        timelineClip.start = _start;
        timelineClip.duration = _duration;
        _director.SetGenericBinding(animationTrack, animator);
    }

    /// <summary>
    /// 左右と上下へ往復する回転AnimationClipを取得または生成します。
    /// </summary>
    private static AnimationClip GetOrCreateRotationClip(
        string _showname,
        GameObject _object,
        float _duration,
        float _sweepangle,
        float _tiltangle,
        int _cyclecount)
    {
        string clipPath =
            $"{EAnimationFolder}/{_showname}_{_object.name}_Rotation.anim"; //保存先
        AnimationClip clip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath); //既存Clip
        if (clip == null)
        {
            clip = new AnimationClip();
            clip.name = $"{_showname}_{_object.name}_Rotation";
            AssetDatabase.CreateAsset(clip, clipPath);
        }

        AnimationCurve[] rotationCurves = CreateWorldRotationCurves(
            _object.transform,
            _duration,
            _sweepangle,
            _tiltangle,
            _cyclecount); //ワールドY基準の回転Curve群
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "localEulerAnglesRaw.x",
            null);
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "localEulerAnglesRaw.y",
            null);
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "m_LocalRotation.x",
            rotationCurves[0]);
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "m_LocalRotation.y",
            rotationCurves[1]);
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "m_LocalRotation.z",
            rotationCurves[2]);
        clip.SetCurve(
            string.Empty,
            typeof(Transform),
            "m_LocalRotation.w",
            rotationCurves[3]);
        clip.EnsureQuaternionContinuity();
        EditorUtility.SetDirty(clip);
        return clip;
    }

    /// <summary>
    /// 基準姿勢を維持し、ワールドY軸を中心に往復するQuaternion Curveを生成します。
    /// </summary>
    private static AnimationCurve[] CreateWorldRotationCurves(
        Transform _transform,
        float _duration,
        float _sweepangle,
        float _tiltangle,
        int _cyclecount)
    {
        const int keysPerCycle = 4; //一往復内の区間数
        int keyCount = (_cyclecount * keysPerCycle) + 1; //生成Key数
        Keyframe[] xKeys = new Keyframe[keyCount]; //Quaternion X Key群
        Keyframe[] yKeys = new Keyframe[keyCount]; //Quaternion Y Key群
        Keyframe[] zKeys = new Keyframe[keyCount]; //Quaternion Z Key群
        Keyframe[] wKeys = new Keyframe[keyCount]; //Quaternion W Key群
        Quaternion baseWorldRotation = _transform.rotation; //基準ワールド姿勢
        Quaternion parentWorldRotation = _transform.parent != null
            ? _transform.parent.rotation
            : Quaternion.identity; //親のワールド姿勢
        for (int i = 0; i < keyCount; ++i)
        {
            float normalizedTime = (float)i / (keyCount - 1); //全体内の時刻
            float phase = normalizedTime
                * _cyclecount
                * Mathf.PI
                * 2.0f; //往復位相
            float yawAngle = Mathf.Sin(phase) * _sweepangle; //ワールドY回転角
            float pitchAngle = Mathf.Sin(phase * 0.5f) * _tiltangle; //上下回転角
            Quaternion worldRotation =
                Quaternion.AngleAxis(yawAngle, Vector3.up)
                * baseWorldRotation
                * Quaternion.AngleAxis(pitchAngle, Vector3.right); //目標ワールド姿勢
            Quaternion localRotation =
                Quaternion.Inverse(parentWorldRotation)
                * worldRotation; //Animationへ記録するローカル姿勢
            float time = normalizedTime * _duration; //Key時刻
            xKeys[i] = new Keyframe(time, localRotation.x);
            yKeys[i] = new Keyframe(time, localRotation.y);
            zKeys[i] = new Keyframe(time, localRotation.z);
            wKeys[i] = new Keyframe(time, localRotation.w);
        }

        AnimationCurve[] curves =
        {
            new AnimationCurve(xKeys),
            new AnimationCurve(yKeys),
            new AnimationCurve(zKeys),
            new AnimationCurve(wKeys)
        }; //Quaternion Curve群
        for (int i = 0; i < curves.Length; ++i)
        {
            for (int j = 0; j < keyCount; ++j)
            {
                AnimationUtility.SetKeyLeftTangentMode(
                    curves[i],
                    j,
                    AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(
                    curves[i],
                    j,
                    AnimationUtility.TangentMode.Linear);
            }
        }

        return curves;
    }

    /// <summary>
    /// 対象が指定した管理Object配下にあるか確認します。
    /// </summary>
    private static bool IsChildOf(GameObject _object, string _rootname)
    {
        Transform current = _object.transform.parent; //確認中の親
        while (current != null)
        {
            if (current.name == _rootname)return true;

            current = current.parent;
        }

        return false;
    }

    /// <summary>
    /// 生成した演出を現在のEffectSystemへ追加または更新します。
    /// </summary>
    private static void RegisterEffects(
        List<TimelineAsset> _timelines,
        List<PlayableDirector> _directors)
    {
        EffectSystem effectSystem =
            Object.FindFirstObjectByType<EffectSystem>(); //登録対象
        if (effectSystem == null)
        {
            Debug.LogWarning("EffectSystemが見つからないため、Timelineの自動登録を省略しました。");
            return;
        }

        SerializedObject serializedSystem = new SerializedObject(effectSystem); //編集対象
        SerializedProperty effects =
            serializedSystem.FindProperty("m_effectDatas"); //EffectData配列
        for (int i = 0; i < EShowCount; ++i)
        {
            int effectIndex = FindEffectIndex(effects, m_showNames[i]); //登録位置
            if (effectIndex < 0)
            {
                effectIndex = effects.arraySize;
                effects.InsertArrayElementAtIndex(effectIndex);
            }

            SerializedProperty effect = effects.GetArrayElementAtIndex(effectIndex); //登録項目
            effect.FindPropertyRelative("m_effectName").stringValue = m_showNames[i];
            effect.FindPropertyRelative("m_playDelaySeconds").floatValue = 0.0f;
            effect.FindPropertyRelative("m_timeline").objectReferenceValue = _timelines[i];
            effect.FindPropertyRelative("m_director").objectReferenceValue = _directors[i];
        }

        serializedSystem.ApplyModifiedProperties();
        EditorUtility.SetDirty(effectSystem);
    }

    /// <summary>
    /// EffectData配列から指定名の位置を検索します。
    /// </summary>
    private static int FindEffectIndex(
        SerializedProperty _effects,
        string _effectname)
    {
        for (int i = 0; i < _effects.arraySize; ++i)
        {
            SerializedProperty effect = _effects.GetArrayElementAtIndex(i); //確認項目
            if (effect.FindPropertyRelative("m_effectName").stringValue == _effectname)return i;
        }

        return -1;
    }

    /// <summary>
    /// 指定名のTimelineを取得または生成します。
    /// </summary>
    private static TimelineAsset GetOrCreateTimeline(string _name)
    {
        string path = $"{ETimelineFolder}/{_name}.playable"; //Asset保存先
        TimelineAsset timeline =
            AssetDatabase.LoadAssetAtPath<TimelineAsset>(path); //既存Timeline
        if (timeline != null)return timeline;

        timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.editorSettings.frameRate = EFrameRate;
        AssetDatabase.CreateAsset(timeline, path);
        return timeline;
    }

    /// <summary>
    /// 指定Timelineの既存Trackを消去します。
    /// </summary>
    private static void ClearTracks(TimelineAsset _timeline)
    {
        List<TrackAsset> tracks =
            new List<TrackAsset>(_timeline.GetRootTracks()); //消去対象
        for (int i = 0; i < tracks.Count; ++i)
        {
            _timeline.DeleteTrack(tracks[i]);
        }
    }

    /// <summary>
    /// 演出専用Directorを取得または生成します。
    /// </summary>
    private static PlayableDirector GetOrCreateDirector(
        GameObject _root,
        string _name)
    {
        Transform child = _root.transform.Find(_name); //既存配置
        GameObject directorObject = child != null ? child.gameObject : null; //Director Object
        if (directorObject == null)
        {
            directorObject = new GameObject(_name);
            directorObject.transform.SetParent(_root.transform, false);
        }

        PlayableDirector director =
            directorObject.GetComponent<PlayableDirector>(); //専用Director
        if (director == null)
        {
            director = directorObject.AddComponent<PlayableDirector>();
        }

        return director;
    }

    /// <summary>
    /// 指定した親の直下をHierarchy順に取得します。
    /// </summary>
    private static List<GameObject> CollectChildren(GameObject _root)
    {
        List<GameObject> objects = new List<GameObject>(); //取得結果
        if (_root == null)return objects;

        for (int i = 0; i < _root.transform.childCount; ++i)
        {
            objects.Add(_root.transform.GetChild(i).gameObject);
        }

        return objects;
    }

    /// <summary>
    /// Timeline再生前の対象を非表示にします。
    /// </summary>
    private static void SetInitialInactive(List<GameObject> _objects)
    {
        for (int i = 0; i < _objects.Count; ++i)
        {
            _objects[i].SetActive(false);
        }
    }

    /// <summary>
    /// InGameシーンを取得します。
    /// </summary>
    private static Scene GetInGameScene()
    {
        Scene scene = SceneManager.GetActiveScene(); //現在のシーン
        if (scene.path == EScenePath)return scene;

        bool b_openScene = EditorUtility.DisplayDialog(
            "Build Live Show Timelines",
            "InGameシーンを開いてライブ演出を生成します。",
            "InGameを開く",
            "キャンセル");
        if (!b_openScene)return default;

        return EditorSceneManager.OpenScene(EScenePath);
    }

    /// <summary>
    /// シーン直下から指定名を検索します。
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
    /// シーン直下の管理用Objectを取得または生成します。
    /// </summary>
    private static GameObject GetOrCreateRoot(Scene _scene, string _name)
    {
        GameObject root = FindRoot(_scene, _name); //既存Object
        if (root != null)return root;

        root = new GameObject(_name);
        SceneManager.MoveGameObjectToScene(root, _scene);
        return root;
    }

    /// <summary>
    /// Timeline保存フォルダを生成します。
    /// </summary>
    private static void EnsureFolders()
    {
        const string parentFolder = "Assets/EffectSystem/EffectTimeLine"; //親フォルダ
        if (!AssetDatabase.IsValidFolder(ETimelineFolder))
        {
            AssetDatabase.CreateFolder(parentFolder, "LiveShows");
        }

        if (AssetDatabase.IsValidFolder(EAnimationFolder))return;

        AssetDatabase.CreateFolder(ETimelineFolder, "Animations");
    }
}
