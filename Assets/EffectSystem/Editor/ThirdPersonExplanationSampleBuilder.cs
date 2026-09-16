/*━━━━━━━━━*
*@file ThirdPersonExplanationSampleBuilder.cs*
*@brief 三人称説明画像の仮設定とTimelineを作成する*
*@date 2026/09/16*
*最終更新日 2026/09/16*
*━━━━━━━━━*/

using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;

public static class ThirdPersonExplanationSampleBuilder
{
    private const string EScenePath = "Assets/Scenes/GameFlow/Gameplay.unity";
    private const string ESampleFolder = "Assets/EffectSystem/Samples/ThirdPersonExplanation";
    private const string ESpritePath = "Assets/Sprite/Tutorial/アセット 9@4x-8.png";
    private const string EClipPath = ESampleFolder + "/ExplanationMotion.anim";
    private const string ETimelinePath = ESampleFolder + "/ExplanationSample.playable";
    private const float EDuration = 5.0f;
    private const float EFadeInEnd = 0.3f;
    private const float EFadeOutStart = 4.7f;
    private const float EHiddenPositionY = -400.0f;
    private const float EVisiblePositionY = -360.0f;
    private const float EAlphaTolerance = 0.01f;
    private const int ESortingOrder = 100;
    private static readonly Vector2 EResolution = new Vector2(1920.0f, 1080.0f);
    private static readonly Vector2 ESize = new Vector2(1100.0f, 350.0f);

    public static void ConfigureGameplay()
    {
        EditorSceneManager.OpenScene(EScenePath);
        EventSceneVisualDirector owner = UnityEngine.Object.FindFirstObjectByType<EventSceneVisualDirector>();
        if (owner == null)
        {
            throw new InvalidOperationException("Gameplayの三人称イベント制御が見つかりません。");
        }
        Configure(owner);
        EditorSceneManager.SaveScene(owner.gameObject.scene);
        Debug.Log("[ExplanationSample] Gameplayへの仮設定と保存が完了しました。");
    }

    [MenuItem("CONTEXT/EventSceneVisualDirector/Set Up Explanation UI Sample")]
    private static void ConfigureFromInspector(MenuCommand _command)
    {
        if (Application.isPlaying) { return; }
        Configure(_command.context as EventSceneVisualDirector);
        Debug.Log("[ExplanationSample] 仮設定を適用しました。シーンを保存してください。");
    }

    private static void Configure(EventSceneVisualDirector _owner)
    {
        if (_owner == null) { return; }
        EnsureFolder("Assets/EffectSystem", "Samples");
        EnsureFolder("Assets/EffectSystem/Samples", "ThirdPersonExplanation");
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ESpritePath);
        if (sprite == null)
        {
            throw new InvalidOperationException("仮設定に使う説明画像を読み込めません。");
        }
        SerializedObject settings = new SerializedObject(_owner);
        SerializedProperty explanation = settings.FindProperty("m_explanationUI");
        Image image = explanation.FindPropertyRelative("m_image").objectReferenceValue as Image;
        //既に設定された画像やTimelineは置き換えません。
        if (image == null)
        {
            GameObject canvasObject = new GameObject(
                "ThirdPersonExplanationSample",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            Undo.RegisterCreatedObjectUndo(canvasObject, "Create Explanation Sample");
            canvasObject.transform.SetParent(_owner.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = ESortingOrder;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = EResolution;
            GameObject imageObject = new GameObject(
                "Explanation",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup),
                typeof(Animator));
            imageObject.transform.SetParent(canvasObject.transform, false);
            image = imageObject.GetComponent<Image>();
            image.rectTransform.anchoredPosition = new Vector2(0.0f, EVisiblePositionY);
            image.rectTransform.sizeDelta = ESize;
            image.preserveAspect = true;
            image.raycastTarget = false;
            image.sprite = sprite;
            imageObject.SetActive(false);
            explanation.FindPropertyRelative("m_image").objectReferenceValue = image;
        }
        if (explanation.FindPropertyRelative("m_sprite").objectReferenceValue == null)
        {
            explanation.FindPropertyRelative("m_sprite").objectReferenceValue = sprite;
        }
        if (explanation.FindPropertyRelative("m_motionDirector").objectReferenceValue == null)
        {
            GameObject directorObject = new GameObject("ExplanationTimeline", typeof(PlayableDirector));
            Undo.RegisterCreatedObjectUndo(directorObject, "Create Explanation Timeline");
            directorObject.transform.SetParent(image.transform.parent, false);
            PlayableDirector director = directorObject.GetComponent<PlayableDirector>();
            director.playOnAwake = false;
            director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            director.extrapolationMode = DirectorWrapMode.Hold;
            TimelineAsset timeline = CreateTimeline();
            director.playableAsset = timeline;
            Animator animator = image.GetComponent<Animator>();
            if (animator == null)
            {
                animator = Undo.AddComponent<Animator>(image.gameObject);
            }
            if (image.GetComponent<CanvasGroup>() == null)
            {
                Undo.AddComponent<CanvasGroup>(image.gameObject);
            }
            foreach (PlayableBinding output in timeline.outputs)
            {
                director.SetGenericBinding(output.sourceObject, animator);
            }
            explanation.FindPropertyRelative("m_motionDirector").objectReferenceValue = director;
            ValidateTimeline(director, image);
        }
        settings.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(_owner.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    private static TimelineAsset CreateTimeline()
    {
        TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(ETimelinePath);
        if (timeline != null)
        {
            return timeline;
        }
        AnimationClip clip = new AnimationClip();
        clip.name = "ExplanationMotion";
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(
                "",
                typeof(CanvasGroup),
                "m_Alpha"),
            new AnimationCurve(
                new Keyframe(0.0f, 0.0f),
                new Keyframe(EFadeInEnd, 1.0f),
                new Keyframe(EFadeOutStart, 1.0f),
                new Keyframe(EDuration, 0.0f)));
        AnimationUtility.SetEditorCurve(
            clip,
            EditorCurveBinding.FloatCurve(
                "",
                typeof(RectTransform),
                "m_AnchoredPosition.y"),
            new AnimationCurve(
                new Keyframe(0.0f, EHiddenPositionY),
                new Keyframe(EFadeInEnd, EVisiblePositionY),
                new Keyframe(EDuration, EVisiblePositionY)));
        AssetDatabase.CreateAsset(clip, EClipPath);
        timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, ETimelinePath);
        AnimationTrack track = timeline.CreateTrack<AnimationTrack>(null, "Explanation Image");
        TimelineClip timelineClip = track.CreateClip<AnimationPlayableAsset>();
        AnimationPlayableAsset animation = timelineClip.asset as AnimationPlayableAsset;
        animation.clip = clip;
        timelineClip.duration = EDuration;
        timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        timeline.fixedDuration = EDuration;
        EditorUtility.SetDirty(timeline);
        return timeline;
    }

    private static void ValidateTimeline(PlayableDirector _director, Image _image)
    {
        bool b_wasActive = _image.gameObject.activeSelf;
        _image.gameObject.SetActive(true);
        CanvasGroup group = _image.GetComponent<CanvasGroup>();
        _director.time = 0.0d;
        _director.Evaluate();
        float startAlpha = group.alpha;
        _director.time = EFadeInEnd;
        _director.Evaluate();
        float visibleAlpha = group.alpha;
        _director.time = EDuration;
        _director.Evaluate();
        float endAlpha = group.alpha;
        _director.Stop();
        _image.gameObject.SetActive(b_wasActive);
        if (startAlpha > EAlphaTolerance || visibleAlpha < 1.0f - EAlphaTolerance
            || endAlpha > EAlphaTolerance)
        {
            throw new InvalidOperationException("仮Timelineのフェードが想定通りではありません。");
        }
        Debug.Log("[ExplanationSample] TimelineのAlpha 0 → 1 → 0を確認しました。");
    }

    private static void EnsureFolder(string _parent, string _name)
    {
        if (!AssetDatabase.IsValidFolder(_parent + "/" + _name))
        {
            AssetDatabase.CreateFolder(_parent, _name);
        }
    }
}
