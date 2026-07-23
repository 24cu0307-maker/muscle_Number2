/*━━━━━━━━━
@file GameFlowSceneGenerator.cs
@brief ゲーム進行に必要な四つのシーンを生成する
@author 24CU○○○○ 氏名
@date 2026/07/14
最終更新日 2026/07/14
@remarks Unity Editor専用
━━━━━━━━━*/
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using CrossProjectScoreRanking;
using GameFlowTemplate;
using FlowSceneManager = GameFlowTemplate.SceneManager;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>
/// タイトルからリザルトまでの仮シーンとBuild Settingsを生成します。
/// </summary>
public static class GameFlowSceneGenerator
{
    private const string SceneFolder = "Assets/Scenes/GameFlow";      // シーンの保存先
    private const string CanvasName = "Canvas";                       // Canvas名
    private const string RuntimeFontName = "LegacyRuntime.ttf";       // 使用フォント名
    private const float DefaultAlpha = 1.0f;                           // 不透明度
    private const float CanvasMatch = 0.5f;                            // Canvasの縦横比追従値
    private const int MinimumFontSize = 22;                            // 最小フォントサイズ
    private const int BrandFontSize = 64;                              // ロゴ文字サイズ
    private const int HeadingFontSize = 48;                            // 見出し文字サイズ
    private const int GameplayHeadingFontSize = 42;                    // インゲーム見出し文字サイズ
    private const int TutorialFontSize = 27;                           // 説明文字サイズ
    private const int GameplayFontSize = 26;                           // インゲーム説明文字サイズ
    private const int ScoreFontSize = 52;                              // スコア文字サイズ
    private const int ButtonFontSize = 24;                             // ボタン文字サイズ
    private const float DefaultButtonWidth = 360.0f;                   // 標準ボタン幅
    private const float ResultButtonWidth = 260.0f;                    // リザルトボタン幅
    private const float ButtonHeight = 70.0f;                          // ボタン高さ
    private static readonly Vector2 m_referenceResolution = new Vector2(1920.0f, 1080.0f); // 基準解像度
    private static readonly Color m_background = new Color(0.035f, 0.055f, 0.09f, DefaultAlpha); // 背景色
    private static readonly Color m_accent = new Color(0.1f, 0.78f, 0.72f, DefaultAlpha);         // 強調色
    private static readonly Color m_panelColor = new Color(0.08f, 0.12f, 0.18f, 0.96f);           // パネル色
    private static readonly Vector2 m_brandPosition = new Vector2(0.0f, 120.0f);                   // ロゴ位置
    private static readonly Vector2 m_brandSize = new Vector2(900.0f, 100.0f);                     // ロゴ領域
    private static readonly Vector2 m_taglinePosition = new Vector2(0.0f, 50.0f);                  // タグライン位置
    private static readonly Vector2 m_taglineSize = new Vector2(700.0f, 50.0f);                    // タグライン領域
    private static readonly Vector2 m_startPosition = new Vector2(0.0f, -80.0f);                   // 開始ボタン位置
    private static readonly Vector2 m_quitPosition = new Vector2(0.0f, -160.0f);                   // 終了ボタン位置
    private static readonly Vector2 m_tutorialHeadingPosition = new Vector2(0.0f, 220.0f);         // チュートリアル見出し位置
    private static readonly Vector2 m_headingSize = new Vector2(900.0f, 80.0f);                    // 見出し領域
    private static readonly Vector2 m_tutorialPanelPosition = new Vector2(0.0f, 30.0f);            // 説明パネル位置
    private static readonly Vector2 m_tutorialPanelSize = new Vector2(850.0f, 270.0f);             // 説明パネル領域
    private static readonly Vector2 m_tutorialTextPosition = new Vector2(0.0f, 35.0f);             // 説明文位置
    private static readonly Vector2 m_tutorialTextSize = new Vector2(750.0f, 230.0f);              // 説明文領域
    private static readonly Vector2 m_playPosition = new Vector2(0.0f, -210.0f);                   // プレイボタン位置
    private static readonly Vector2 m_gameplayHeadingPosition = new Vector2(0.0f, 260.0f);         // インゲーム見出し位置
    private static readonly Vector2 m_gameplayHeadingSize = new Vector2(800.0f, 70.0f);            // インゲーム見出し領域
    private static readonly Vector2 m_gameplayPanelPosition = new Vector2(0.0f, 20.0f);            // インゲームパネル位置
    private static readonly Vector2 m_gameplayPanelSize = new Vector2(1000.0f, 360.0f);            // インゲームパネル領域
    private static readonly Vector2 m_gameplayTextPosition = new Vector2(0.0f, 30.0f);             // インゲーム説明位置
    private static readonly Vector2 m_gameplayTextSize = new Vector2(900.0f, 240.0f);              // インゲーム説明領域
    private static readonly Vector2 m_completePosition = new Vector2(0.0f, -240.0f);               // 仮完了ボタン位置
    private static readonly Vector2 m_resultHeadingPosition = new Vector2(0.0f, 240.0f);           // リザルト見出し位置
    private static readonly Vector2 m_resultHeadingSize = new Vector2(800.0f, 70.0f);              // リザルト見出し領域
    private static readonly Vector2 m_scorePosition = new Vector2(0.0f, 70.0f);                    // スコア位置
    private static readonly Vector2 m_scoreSize = new Vector2(700.0f, 170.0f);                     // スコア領域
    private static readonly Vector2 m_retryPosition = new Vector2(-150.0f, -160.0f);               // リトライボタン位置
    private static readonly Vector2 m_titlePosition = new Vector2(150.0f, -160.0f);                // タイトルボタン位置

    /// <summary>
    /// 仮シーン一式を生成します。
    /// </summary>
    [MenuItem("Tools/Game Flow/Generate Shell Scenes")]
    public static void Generate()
    {
        EnsureFolder("Assets/Scenes", "GameFlow");
        CreateTitle();
        CreateTutorial();
        CreateGameplay();
        CreateResult();
        UpdateBuildSettings();
        AssetDatabase.SaveAssets();
        Debug.Log("Game flow scenes generated: Title > Tutorial > Gameplay > Result");
    }

    private static void CreateTitle()
    {
        NewScene();
        CreateText("MASSLE", BrandFontSize, m_brandPosition, m_brandSize, FontStyle.Bold);
        CreateText("MOVE  /  PLAY  /  SCORE", MinimumFontSize, m_taglinePosition, m_taglineSize);
        CreateButton("START", m_startPosition, GameFlowButton.EAction.StartGame);
        CreateButton("QUIT", m_quitPosition, GameFlowButton.EAction.Quit);
        Save("Title");
    }

    private static void CreateTutorial()
    {
        NewScene();
        CreateText("HOW TO PLAY", HeadingFontSize, m_tutorialHeadingPosition, m_headingSize, FontStyle.Bold);
        CreatePanel(m_tutorialPanelPosition, m_tutorialPanelSize);
        CreateText("1. カメラに全身が映る位置に立ちます\n\n2. お手本に合わせて体を動かします\n\n3. 高得点を目指しましょう", TutorialFontSize, m_tutorialTextPosition, m_tutorialTextSize);
        CreateButton("PLAY", m_playPosition, GameFlowButton.EAction.OpenGameplay);
        Save("Tutorial");
    }

    private static void CreateGameplay()
    {
        NewScene();
        var flowSystem = new GameObject("Flow System", typeof(GameManager), typeof(ScoreRankingInputSystem), typeof(GameFlowResultBridge));
        var gameManager = flowSystem.GetComponent<GameManager>();
        var sceneManager = flowSystem.GetComponent<FlowSceneManager>();
        var sceneSerialized = new SerializedObject(sceneManager);
        sceneSerialized.FindProperty("m_titleSceneName").stringValue = GameSession.TitleScene;
        sceneSerialized.FindProperty("m_gameSceneName").stringValue = GameSession.GameplayScene;
        sceneSerialized.FindProperty("m_resultSceneName").stringValue = GameSession.ResultScene;
        sceneSerialized.FindProperty("m_bUseResultScene").boolValue = true;
        sceneSerialized.ApplyModifiedPropertiesWithoutUndo();
        CreateText("GAMEPLAY", GameplayHeadingFontSize, m_gameplayHeadingPosition, m_gameplayHeadingSize, FontStyle.Bold);
        CreatePanel(m_gameplayPanelPosition, m_gameplayPanelSize);
        CreateText("ここにメインゲームを配置します\n\nゲーム終了時に GameSession.SetScore(score);\nGameSession.FinishGame(); を呼び出してください", GameplayFontSize, m_gameplayTextPosition, m_gameplayTextSize);
        var button = CreateButton("COMPLETE (PREVIEW)", m_completePosition, GameFlowButton.EAction.FinishGameplay);
        Object.DestroyImmediate(button.GetComponent<GameFlowButton>());
        var placeholder = button.AddComponent<GameplayPlaceholder>();
        var placeholderSerialized = new SerializedObject(placeholder);
        placeholderSerialized.FindProperty("m_gameManager").objectReferenceValue = gameManager;
        placeholderSerialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(button.GetComponent<Button>().onClick, placeholder.CompletePreview);
        Save("Gameplay");
    }

    private static void CreateResult()
    {
        NewScene();
        CreateText("RESULT", HeadingFontSize, m_resultHeadingPosition, m_resultHeadingSize, FontStyle.Bold);
        var score = CreateText("SCORE", ScoreFontSize, m_scorePosition, m_scoreSize, FontStyle.Bold);
        var view = score.gameObject.AddComponent<ResultView>();
        var serialized = new SerializedObject(view);
        serialized.FindProperty("m_scoreText").objectReferenceValue = score;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        CreateButton("RETRY", m_retryPosition, GameFlowButton.EAction.Retry, ResultButtonWidth);
        CreateButton("TITLE", m_titlePosition, GameFlowButton.EAction.ReturnToTitle, ResultButtonWidth);
        Save("Result");
    }

    private static void NewScene()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var camera = new GameObject("Main Camera", typeof(Camera));
        camera.tag = "MainCamera";
        camera.GetComponent<Camera>().backgroundColor = m_background;
        camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        var canvas = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = m_referenceResolution;
        scaler.matchWidthOrHeight = CanvasMatch;
    }

    /// <summary>
    /// Canvas上にテキストを生成します。
    /// </summary>
    private static Text CreateText(
        string _value,
        int _size,
        Vector2 _position,
        Vector2 _dimensions,
        FontStyle _style = FontStyle.Normal)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(GameObject.Find(CanvasName).transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = _position;
        rect.sizeDelta = _dimensions;
        var text = go.GetComponent<Text>();
        text.text = _value;
        text.font = Resources.GetBuiltinResource<Font>(RuntimeFontName);
        text.fontSize = Mathf.Max(MinimumFontSize, _size);
        text.fontStyle = _style;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        return text;
    }

    /// <summary>
    /// Canvas上に画面遷移ボタンを生成します。
    /// </summary>
    private static GameObject CreateButton(
        string _label,
        Vector2 _position,
        GameFlowButton.EAction _action,
        float _width = DefaultButtonWidth)
    {
        var go = new GameObject(_label + " Button", typeof(RectTransform), typeof(Image), typeof(Button), typeof(GameFlowButton));
        go.transform.SetParent(GameObject.Find(CanvasName).transform, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = _position;
        rect.sizeDelta = new Vector2(_width, ButtonHeight);
        go.GetComponent<Image>().color = m_accent;
        var flow = go.GetComponent<GameFlowButton>();
        var serialized = new SerializedObject(flow);
        serialized.FindProperty("m_action").enumValueIndex = (int)_action;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEventTools.AddPersistentListener(go.GetComponent<Button>().onClick, flow.Execute);
        var text = CreateText(_label, ButtonFontSize, Vector2.zero, rect.sizeDelta, FontStyle.Bold);
        text.transform.SetParent(go.transform, false);
        return go;
    }

    /// <summary>
    /// Canvas上に背景パネルを生成します。
    /// </summary>
    private static void CreatePanel(Vector2 _position, Vector2 _size)
    {
        var go = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(GameObject.Find(CanvasName).transform, false);
        go.transform.SetAsFirstSibling();
        var rect = go.GetComponent<RectTransform>();
        rect.anchoredPosition = _position;
        rect.sizeDelta = _size;
        go.GetComponent<Image>().color = m_panelColor;
    }

    /// <summary>
    /// 現在のシーンを保存します。
    /// </summary>
    private static void Save(string _name)
    {
        EditorSceneManager.SaveScene(UnitySceneManager.GetActiveScene(), $"{SceneFolder}/{_name}.unity");
    }

    private static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene($"{SceneFolder}/Title.unity", true),
            new EditorBuildSettingsScene($"{SceneFolder}/Tutorial.unity", true),
            new EditorBuildSettingsScene($"{SceneFolder}/Gameplay.unity", true),
            new EditorBuildSettingsScene($"{SceneFolder}/Result.unity", true)
        };
        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (!existing.path.StartsWith(SceneFolder + "/")) scenes.Add(existing);
        }
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    /// <summary>
    /// 必要なフォルダーが存在しない場合に作成します。
    /// </summary>
    private static void EnsureFolder(string _parent, string _child)
    {
        var path = _parent + "/" + _child;
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(_parent, _child);
    }
}
