//
//InGameの流れをここで行う
//
//
//準備　　　InGame入って一度飲み使用
//始め　　　UI、エフェクトのセット　　　
//実行中　　UIの縮小、判定
//終わり　　ポーズごとに終了時間がきたら、又は、成功-失敗
//成功　　　ポーズを成功した場合、その後終わりへ
//失敗　　　ポーズを失敗した場合、その後終わりへ
//



using GameFlowTemplate;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public sealed class InGameManager : MonoBehaviour
{

    [Header("gameManager")]
    [SerializeField] private GameManager m_gameManager;

    [Header("po")]
    [SerializeField] private PoseJudgeManager m_poseJudgeManager;

    [Header("UI")]
    [SerializeField] private UIManager m_uIManager;

    [Header("ポーズデータ")]
    [SerializeField] private PoseFlowDataManager m_poseFlowDataManager;

    [Header("Startup Readiness")]
    [SerializeField] private AudienceAreaSpawner m_audienceSpawner;
    [SerializeField] private ScenesLoad m_mediaPipeLoader;

    [Header("Startup Loading Screen")]
    [SerializeField] private bool b_m_hideSceneUntilReady = true;
    [SerializeField] private Color m_loadingScreenColor = Color.black;
    [SerializeField, Min(0.0f)] private float m_loadingScreenFadeSeconds = 0.25f;
    [Tooltip("ロード中にTimeline、Particle、Animator、通常のUpdateを停止します。")]
    [SerializeField] private bool b_m_pauseSceneDuringStartup = true;

    [Header("Startup Timeline")]
    [Tooltip("ロード完了後、ゲーム開始前にTimelineを再生します。")]
    [SerializeField] private bool b_m_playStartupTimeline;
    [SerializeField] private PlayableDirector m_startupTimelineDirector;


    private InGameState m_inGameState = InGameState.Start;
    private float GameTimeSeconds;                  //現在のゲーム時間
    private bool b_m_gameStarted;
    private CanvasGroup m_loadingScreen;
    private float m_timeScaleBeforeStartup;
    private bool b_m_audioPauseBeforeStartup;
    private bool b_m_startupPauseApplied;
    public float GetCurrentTIme() { return GameTimeSeconds; }

    [Header("終了の時間")]
    [SerializeField] private float m_endtimer;

    /*
    [Header("UIの操作")]
    [SerializeField] private UIController m_uiController;

    [Header("ポーズの判定")]
    [SerializeField] private PoseJudgeController m_poseJudgeController;

    [Header("スコア判定")]
    [SerializeField] private ScoreController m_scoreController;

    [Header("UIの保存場所")]
    [SerializeField] private ExcelLoader m_excelLoader;

    [Header("エフェクトシステム")]
    [SerializeField] private EffectSystem m_effectSystem;

    [Header("カメラ")]
    [SerializeField] private PoseCameraDirector poseCameraDirector;

    [Header("イベントディレクター")]
    [SerializeField]
    private EventSceneVisualDirector m_eventSceneVisualDirector;

    [Header("熱量")]
    [SerializeField] private VenueVoltageSystem m_venueVoltageSystem;
    */


    private void Awake()
    {
        // 他ComponentのStartより前に時間を止め、TimelineやParticleがロード画面の
        // 裏側で先行再生されることを防ぎます。CoroutineとSceneManagerの非同期
        // ロードはTime.timeScaleが0でも進行します。
        ApplyStartupPause();

        // Awakeで作成することで、Gameplayシーンが最初に描画されるフレームより前に
        // ステージ、観客、MediaPipe映像、通常UIを確実に覆います。
        CreateStartupLoadingScreen();

        if (m_poseJudgeManager == null)
        {
            m_poseJudgeManager = FindFirstObjectByType<PoseJudgeManager>();
        }
        if (m_uIManager == null)
        {
            m_uIManager = FindFirstObjectByType<UIManager>();
        }
        if (m_poseFlowDataManager == null)
        {
            m_poseFlowDataManager =
                FindFirstObjectByType<PoseFlowDataManager>();
        }
        if (m_audienceSpawner == null)
        {
            m_audienceSpawner = FindFirstObjectByType<AudienceAreaSpawner>();
        }
        if (m_mediaPipeLoader == null)
        {
            m_mediaPipeLoader = FindFirstObjectByType<ScenesLoad>();
        }
    }

    private void OnEnable()
    {
        if (m_poseJudgeManager != null)
        {
            m_poseJudgeManager.setState += SetState;
        }
        if (m_uIManager != null)
        {
            m_uIManager.setState += SetState;
        }
    }

    private void OnDisable()
    {
        if (m_poseJudgeManager != null)
        {
            m_poseJudgeManager.setState -= SetState;
        }
        if (m_uIManager != null)
        {
            m_uIManager.setState -= SetState;
        }
    }

    /// <summary>
    /// 初期化失敗中に別Sceneへ移動した場合でも、Time.timeScaleや音声停止を
    /// 次のSceneへ持ち越さないよう必ず元の状態へ戻します。
    /// </summary>
    private void OnDestroy()
    {
        ReleaseStartupPause();
    }

    private IEnumerator Start()
    {
        // スコアと時計をリセットしたReady状態へ先に移します。
        // この時点ではタイマー、ポーズ判定、UI進行はまだ開始しません。
        m_gameManager?.PrepareGame();

        // 重い準備処理は各Componentが複数フレームに分けて実行します。
        // InGameManagerは各処理の詳細を持たず、公開された完了状態だけを待ちます。
        while (!AreStartupTasksComplete())
        {
            if (m_mediaPipeLoader != null && m_mediaPipeLoader.HasFailed)
            {
                Debug.LogError(
                    "MediaPipeの準備に失敗したため、ゲーム開始を中止します。理由: "
                    + m_mediaPipeLoader.FailureReason,
                    this);
                yield break;
            }

            yield return null;
        }

        // 完成した画面が少なくとも一度描画されてからロード画面を外します。
        // これにより最後の観客生成と同じフレームの未完成な描画を見せません。
        yield return new WaitForEndOfFrame();
        yield return HideStartupLoadingScreen();

        // 黒画面が完全に消えてからScene時間と音声を再開します。
        // Timeline、Particle、Animatorの最初のフレームは、この次の描画から始まります。
        ReleaseStartupPause();

        // ゲーム時間を開始する前に、専用Timelineの終了を待ちます。
        yield return PlayStartupTimeline();

        // すべての準備と開始演出が完了してから時計を0秒で開始します。
        m_gameManager?.StartGame();
        b_m_gameStarted = true;
    }

    /// <summary>ロード完了後の開始Timelineを終了まで再生します。</summary>
    private IEnumerator PlayStartupTimeline()
    {
        if (!b_m_playStartupTimeline || m_startupTimelineDirector == null
            || m_startupTimelineDirector.playableAsset == null)yield break;

        m_startupTimelineDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        m_startupTimelineDirector.extrapolationMode = DirectorWrapMode.Hold;
        m_startupTimelineDirector.time = 0.0d;
        m_startupTimelineDirector.Evaluate();
        m_startupTimelineDirector.Play();
        while (m_startupTimelineDirector.state == PlayState.Playing)
        {
            yield return null;
        }
    }

    /// <summary>
    /// 起動前の時間・音声状態を保存し、演出が暗転中に進まないよう一時停止します。
    /// Time.timeScaleを直接1へ戻さず保存値へ復元するため、別システムが設定した
    /// Slow Motionなどの状態も壊しません。
    /// </summary>
    private void ApplyStartupPause()
    {
        if (!b_m_pauseSceneDuringStartup || b_m_startupPauseApplied)return;

        m_timeScaleBeforeStartup = Time.timeScale;
        b_m_audioPauseBeforeStartup = AudioListener.pause;
        Time.timeScale = 0.0f;
        AudioListener.pause = true;
        b_m_startupPauseApplied = true;
    }

    /// <summary>起動待機前に保存した時間倍率と音声停止状態を復元します。</summary>
    private void ReleaseStartupPause()
    {
        if (!b_m_startupPauseApplied)return;

        Time.timeScale = m_timeScaleBeforeStartup;
        AudioListener.pause = b_m_audioPauseBeforeStartup;
        b_m_startupPauseApplied = false;
    }

    /// <summary>
    /// 他のCanvasやAdditiveロードされたMediaPipe Cameraより前面に表示される、
    /// 起動専用のScreen Space Overlayを実行時に作成します。
    /// ImageのRaycast Targetを有効にし、準備中のButton操作も遮断します。
    /// </summary>
    private void CreateStartupLoadingScreen()
    {
        if (!b_m_hideSceneUntilReady || m_loadingScreen != null)return;

        GameObject root = new GameObject(
            "StartupLoadingScreen",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasGroup),
            typeof(GraphicRaycaster));
        root.transform.SetParent(transform, false);

        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = short.MaxValue;

        m_loadingScreen = root.GetComponent<CanvasGroup>();
        m_loadingScreen.alpha = 1.0f;
        m_loadingScreen.interactable = true;
        m_loadingScreen.blocksRaycasts = true;

        GameObject background = new GameObject(
            "Background",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        background.transform.SetParent(root.transform, false);
        RectTransform rectTransform = background.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = background.GetComponent<Image>();
        image.color = m_loadingScreenColor;
        image.raycastTarget = true;
    }

    /// <summary>
    /// ロード画面をunscaled timeでフェードアウトして破棄します。
    /// ゲーム時計を開始する前でも、Time.timeScaleの影響を受けず終了できます。
    /// </summary>
    private IEnumerator HideStartupLoadingScreen()
    {
        if (m_loadingScreen == null)yield break;

        float duration = Mathf.Max(0.0f, m_loadingScreenFadeSeconds);
        if (duration > 0.0f)
        {
            float elapsed = 0.0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                m_loadingScreen.alpha = 1.0f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
        }

        m_loadingScreen.blocksRaycasts = false;
        Destroy(m_loadingScreen.gameObject);
        m_loadingScreen = null;
    }

    /// <summary>
    /// Gameplay開始に必要な非同期準備がすべて完了したかを返します。
    /// 待機条件をここへ集約することで、新しい準備処理を追加する場合も
    /// Start Coroutineへ個別のwhile文を増やさずに済みます。
    /// </summary>
    private bool AreStartupTasksComplete()
    {
        bool b_audienceReady =
            m_audienceSpawner == null || m_audienceSpawner.IsSpawnComplete;
        bool b_mediaPipeReady =
            m_mediaPipeLoader == null || m_mediaPipeLoader.IsReady;
        return b_audienceReady && b_mediaPipeReady;
    }

    private void Update()
    {
        // UnityのUpdate自体はシーンロード直後から呼ばれるため、準備完了までは
        // ゲーム時計、判定、UI、終了判定を一切進めません。
        if (!b_m_gameStarted)return;

        UpdateTime();

        VoltageBgmSystem bgmSystem = m_gameManager?.GetVoltageBgmSystem();
        float bgmDuration = 0.0f;
        if (bgmSystem != null)
        {
            bgmDuration = bgmSystem.DurationSeconds;
        }
        float manualDuration = 0.0f;
        if (m_poseFlowDataManager != null)
        {
            manualDuration = m_poseFlowDataManager.TimelineDuration;
        }
        float finishTime = m_endtimer;
        if (manualDuration > 0.0f)
        {
            finishTime = manualDuration;
        }
        if (bgmDuration > 0.0f)
        {
            finishTime = Mathf.Max(0.0f, bgmDuration - 0.1f);
        }
        if (finishTime > 0.0f && GameTimeSeconds >= finishTime)
        {
            m_gameManager?.FinishGame();
            return;
        }

        if (m_gameManager != null
            && m_gameManager.CurrentState == GameState.DirectionPause)return;
        if (m_poseFlowDataManager == null
            || !m_poseFlowDataManager.IsInitialized)return;

        bool hadActivePose = m_poseFlowDataManager.HasActivePose;
        CSVDataPoseFlow previousPose = hadActivePose
            ? m_poseFlowDataManager.GetPose()
            : default;
        bool nodeChanged =
            m_poseFlowDataManager.SynchronizeToBgmTime(GameTimeSeconds);
        if (nodeChanged)
        {
            if (hadActivePose)
            {
                m_uIManager?.FinishCurrentPose(previousPose);
            }

            SetState(m_poseFlowDataManager.HasActivePose
                ? InGameState.Start
                : InGameState.None);
        }

        if (!m_poseFlowDataManager.HasActivePose)return;

        CSVDataPoseFlow currentPose = m_poseFlowDataManager.GetPose();
        if (m_inGameState == InGameState.End)
        {
            FinishCurrentPose(currentPose);
            return;
        }

        m_uIManager?.UIManagerUpdate(
            m_inGameState,
            currentPose,
            GameTimeSeconds);
        m_poseJudgeManager?.PoseJudgeManagerUpdate(
            m_inGameState,
            currentPose);
    }

    private void UpdateTime()
    {
        TimeManager timeManager = m_gameManager?.GetTimeManager();
        VoltageBgmSystem bgmSystem = m_gameManager?.GetVoltageBgmSystem();
        if (bgmSystem != null && bgmSystem.IsPlaybackReady)
        {
            GameTimeSeconds = bgmSystem.CurrentTimeSeconds;
            timeManager?.SynchronizeExternalClock(GameTimeSeconds);
            return;
        }

        if (timeManager != null)
        {
            GameTimeSeconds = timeManager.GameTimeSeconds;
        }
    }

    public void SetState(InGameState _state)
    {
        m_inGameState = _state;
    }

    /// <summary>
    /// 各Managerへ終了処理を依頼し、次のポーズへ進行します。
    /// </summary>
    private void FinishCurrentPose(CSVDataPoseFlow _pose)
    {
        m_uIManager?.FinishCurrentPose(_pose);
        SetState(InGameState.None);
    }

}
