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
using UnityEngine;

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


    private InGameState m_inGameState = InGameState.Start;
    private float GameTimeSeconds;                  //現在のゲーム時間
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

    public void Start()
    {
        m_gameManager?.StartGame();
    }

    private void Update()
    {
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
