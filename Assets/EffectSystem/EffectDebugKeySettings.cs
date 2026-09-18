/*━━━━━━━━━*
*@file EffectDebugKeySettings.cs*
*@brief Gameplayで使用するDebugキーとDebug操作を一括管理する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/09/18*
*@remarks InspectorからすべてのDebugキーを変更可能*
*━━━━━━━━━*/

using GameFlowTemplate;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

/// <summary>
/// EffectSystem関連のDebug表示に使用するキー設定を管理します。
/// </summary>
[DisallowMultipleComponent]
public sealed class EffectDebugKeySettings : MonoBehaviour
{
    private const string EPersistentObjectName = "GlobalDebugKeys";
    private const string EResultScenePath = "Assets/Scenes/GameFlow/Result_Anime.unity";
    private static EffectDebugKeySettings m_instance;
    private bool b_m_resultRequested; //F10によるResult遷移の多重実行防止
    private bool b_m_restartRequested; //Restartの多重実行防止

    public static bool ForceAllSuccess { get; private set; } //全成功Debug状態

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        m_instance = null;
        ForceAllSuccess = false;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(
        UnityEngine.SceneManagement.Scene _scene,
        UnityEngine.SceneManagement.LoadSceneMode _mode)
    {
        EnsurePersistentSettings(null);
        m_instance.b_m_resultRequested = false;
        m_instance.b_m_restartRequested = false;
    }

    private void Awake()
    {
        if (m_instance == this) { return; }
        EnsurePersistentSettings(this);
        //Effect一式のObjectを永続化せず、キー管理だけを専用Objectへ移します。
        enabled = false;
    }

    private static void EnsurePersistentSettings(EffectDebugKeySettings _sceneSettings)
    {
        if (m_instance == null)
        {
            GameObject owner = new GameObject(EPersistentObjectName);
            owner.SetActive(false);
            m_instance = owner.AddComponent<EffectDebugKeySettings>();
            DontDestroyOnLoad(owner);
            owner.SetActive(true);
        }
        if (_sceneSettings == null || _sceneSettings == m_instance) { return; }
        m_instance.m_voltageToggleInputKey = _sceneSettings.m_voltageToggleInputKey;
        m_instance.m_exitDebugInputKey = _sceneSettings.m_exitDebugInputKey;
        m_instance.m_restartInputKey = _sceneSettings.m_restartInputKey;
        m_instance.m_forceSuccessToggleInputKey = _sceneSettings.m_forceSuccessToggleInputKey;
        m_instance.m_cameraRetargetInputKey = _sceneSettings.m_cameraRetargetInputKey;
        m_instance.m_returnTitleInputKey = _sceneSettings.m_returnTitleInputKey;
        m_instance.m_skipSceneInputKey = _sceneSettings.m_skipSceneInputKey;
        m_instance.m_skipFallbackSceneName = _sceneSettings.m_skipFallbackSceneName;
    }

    [SerializeField] private Key m_voltageToggleInputKey =
        Key.F8; //Voltage Debug Panel表示切替Key
    [SerializeField] private Key m_exitDebugInputKey =
        Key.F10; //Debug再生終了Key
    [SerializeField] private Key m_restartInputKey =
        Key.F5; //現在のGameplayを最初から再読込するKey
    [SerializeField] private Key m_forceSuccessToggleInputKey =
        Key.F6; //全判定成功の切替Key
    [SerializeField] private Key m_cameraRetargetInputKey =
        Key.F9; //Camera注視対象を再設定するKey
    [SerializeField] private Key m_returnTitleInputKey = Key.F2; //タイトルへ戻るKey
    [SerializeField] private Key m_skipSceneInputKey = Key.F3; //現在のシーンを飛ばすKey
    [SerializeField] private string m_skipFallbackSceneName = GameSession.GameplayScene; //専用シーンのスキップ先

    public Key VoltageToggleKey
    {
        get
        {
            return m_voltageToggleInputKey;
        }
    }

    public Key ExitDebugKey
    {
        get
        {
            return m_exitDebugInputKey;
        }
    }

    public Key RestartKey => m_restartInputKey;
    public Key ForceSuccessToggleKey => m_forceSuccessToggleInputKey;
    public Key CameraRetargetKey => m_cameraRetargetInputKey;

    /// <summary>
    /// Debug表示Componentの有効状態に関係なく、Result遷移キーを監視します。
    /// </summary>
    private void Update()
    {
        if (SceneFadeTransition.IsTransitioning) { return; }
        if (IsKeyDown(m_returnTitleInputKey))
        {
            ReturnToTitle();
            return;
        }
        if (IsKeyDown(m_skipSceneInputKey))
        {
            SkipCurrentScene();
            return;
        }
        if (!b_m_restartRequested && IsKeyDown(m_restartInputKey))
        {
            RestartCurrentScene();
            return;
        }

        if (IsKeyDown(m_forceSuccessToggleInputKey))
        {
            ForceAllSuccess = !ForceAllSuccess;
            string forceSuccessState = "OFF";
            if (ForceAllSuccess)
            {
                forceSuccessState = "ON";
            }

            Debug.Log($"[DebugKey] 全成功判定: {forceSuccessState}", this);
        }

        if (IsKeyDown(m_cameraRetargetInputKey))
        {
            RetargetCamera();
        }

        if (!b_m_resultRequested && IsKeyDown(m_exitDebugInputKey))
        {
            MoveToResult();
        }
    }

    private void RestartCurrentScene()
    {
        b_m_restartRequested = true;
        ForceAllSuccess = false;
        UnityEngine.SceneManagement.Scene activeScene =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        SceneFadeTransition.LoadScene(activeScene.path);
    }

    private void ReturnToTitle()
    {
        ForceAllSuccess = false;
        SceneFadeTransition.LoadScene(GameSession.TitleScene);
    }

    private void SkipCurrentScene()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        //Build順には認識専用シーンも含まれるため、展示の画面フローで判断します。
        switch (sceneName)
        {
            case GameSession.TitleScene:
            case "Title":
            case GameSession.FilmingScene:
                GameSession.StartNewGame();
                break;
            case GameSession.TutorialScene:
                SceneFadeTransition.LoadScene(GameSession.GameplayScene);
                break;
            case GameSession.GameplayScene:
                MoveToResult();
                break;
            case "Result_Anime":
            case GameSession.ResultScene:
                ReturnToTitle();
                break;
            default:
                SceneFadeTransition.LoadScene(m_skipFallbackSceneName);
                break;
        }
    }

    private void RetargetCamera()
    {
        PoseCameraDirector cameraDirector = FindFirstObjectByType<PoseCameraDirector>();
        if (cameraDirector == null)
        {
            Debug.LogWarning(
                "[DebugKey] PoseCameraDirectorが見つからないため再ターゲットできません。",
                this);
            return;
        }

        cameraDirector.RetargetCurrentFocus();
        Debug.Log("[DebugKey] カメラのフォーカス対象を再設定しました。", this);
    }

    private void MoveToResult()
    {
        int resultIndex = UnityEngine.SceneManagement.SceneUtility.GetBuildIndexByScenePath(EResultScenePath);
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().path == EResultScenePath) { return; }
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            if (resultIndex >= 0)
            {
                b_m_resultRequested = true;
                SceneFadeTransition.LoadScene(EResultScenePath);
                return;
            }
            Debug.LogWarning(
                "[EffectDebugKeySettings] GameManagerが見つからないためResultへ遷移できません。",
                this);
            return;
        }

        b_m_resultRequested = true;
        gameManager.FinishGame();
    }

    /// <summary>
    /// シーン内の設定を取得し、存在しなければ指定Objectへ追加します。
    /// </summary>
    public static EffectDebugKeySettings GetOrCreate(GameObject _owner)
    {
        EnsurePersistentSettings(null);
        return m_instance;
    }

    /// <summary>
    /// 現在のInput方式に合わせて指定キーの押下を判定します。
    /// </summary>
    public static bool IsKeyDown(Key _inputSystemKey)
    {
        Keyboard keyboard = Keyboard.current; //現在接続中のKeyboard
        if (keyboard == null)return false;

        KeyControl keyControl = keyboard[_inputSystemKey];
        return keyControl != null && keyControl.wasPressedThisFrame;
    }
}
