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
using Unity.VisualScripting;
using UnityEngine;

public sealed class InGameManager : MonoBehaviour
{

    [Header("gameManager")]
    [SerializeField] private GameManager m_gameManager;

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


    [Header("カメラ")]
    [SerializeField] private VenueVoltageSystem m_venueVoltageSystem;

    [SerializeField] private EventSceneVisualDirector m_eventSceneVisualDirector;

    [Header("終了の時間")]
    [SerializeField] private float m_endtimer;



    private UIState m_currentUIState = UIState.None;



    private float GameTimeSeconds;                  //現在のゲーム時間
    private int PoseMaxCount = 20;            //ポーズ数を設定

    private PoseFlow poseFlow;  　　　　  //ポーズ順の管理

    private CSVDataPoseFlow pose;

    private int m_SpecialFrame = -1;

    public Action<PoseFlow, CSVDataPoseFlow, float> PoseFrame;

    public Action<int> PoseJudgeFrame;

    bool one = true;

    private InGameState m_state = InGameState.Start;


    private float KeepGameTimeSeconds;


    public void Start()
    {

        if (m_eventSceneVisualDirector == null)
        {
            m_eventSceneVisualDirector =
                FindFirstObjectByType<EventSceneVisualDirector>();
        }

        //ゲームを開始する
        //GameManagerで管理している
        //Timerとスコアをリセット
        //Timerの開始と状態の切り替え
        m_gameManager.StartGame();

        // CSVのデータをPoseFlowへ渡す
        poseFlow = new PoseFlow(m_excelLoader.excelPoseTimeFlowLoader.GetCSVDatas());

    }

    private void Update()
    {
        //ゲームを終了する
        if (m_endtimer <= GameTimeSeconds) { m_gameManager.FinishGame(); }


        //現在のゲーム時間の更新
        UpdateTime();

        Debug.Log("pose.PoseID" + pose.PoseID);

        //現在のポーズを取得
        pose = poseFlow.CurrentPose();

        Debug.Log("pose.PoseID" + pose.PoseID);
        switch (m_state)
        {
            case InGameState.None:

                if (one)
                {
                    poseCameraDirector.Play();
                    one = false;
                }

                if (8 <= GameTimeSeconds)
                {
                    m_state = InGameState.Start;

                }


                break;

            case InGameState.Start:
                Set(pose, GameTimeSeconds);
                break;

            case InGameState.Active:

                Active(pose, GameTimeSeconds);
                Judge(pose);
                flowend(pose);

                break;

            case InGameState.End:
                /*
                m_effectSystem.IsEffectPlay();
                if (!m_effectSystem.IsPlayEffect())
                {
                   

                }
                */
                ForcedQuit(poseFlow, pose, GameTimeSeconds);

                break;
            case InGameState.Success:
                Success();

                break;
            case InGameState.Failure:
                Failure();

                break;
        }


    }


    /// <summary>
    /// 現在のゲーム時間の更新
    /// </summary>
    private void UpdateTime()
    {
        GameTimeSeconds = m_gameManager.GetTimeManager().GameTimeSeconds;
    }




    private void flowend(CSVDataPoseFlow _pose)
    {
        Debug.Log("{END0}");
        if ((KeepGameTimeSeconds + _pose.time) < GameTimeSeconds)
        {
            m_state = InGameState.End;

        }
    }

    /// <summary>
    /// ゲームを継続するか
    /// </summary>
    private void ContinuingGame()
    {
        if (PoseMaxCount == 0)
        {
            m_gameManager.FinishGame();

        }

        PoseMaxCount--;
    }

    //今フレームの設定・表示
    private void Set(CSVDataPoseFlow _pose, float _seconds)
    {

        KeepGameTimeSeconds = _seconds;
        //UI設定・表示
        m_uiController.UISet_normal(_pose);


        m_state = InGameState.Active;
    }

    //実行
    private void Active(CSVDataPoseFlow _pose, float _seconds)
    {

        // 縮小(通常フレーム)
        if (_seconds <= (_pose.time + KeepGameTimeSeconds))
        {
            m_uiController.UIMove_normal();

            //イベント実行　当たり判定
            PoseJudgeFrame?.Invoke(_pose.PoseID);
        }
    }

    //判定
    private void Judge(CSVDataPoseFlow _pose)
    {
        //通常成功時
        if (m_poseJudgeController.GetisPose(_pose.PoseID) &&
            m_poseJudgeController.PoseJudge_Normal(m_uiController.GetCurrentFrame()[1], m_uiController.GetCurrentFrame()[3]))
        {
            m_uiController.UIJudgeEnd_normal();
            m_uiController.UIForcedQuit();
            m_state = InGameState.Success;

        }

        //完璧成功時
        if (m_poseJudgeController.GetisPose(_pose.PoseID) &&
            m_poseJudgeController.PoseJudge_Perfect(m_uiController.GetCurrentFrame()[1], m_uiController.GetCurrentFrame()[3]))
        {
            m_uiController.UIJudgeEnd_normal();
            m_uiController.UIForcedQuit();
            m_state = InGameState.Success;

        }

        //失敗
        if (m_poseJudgeController.PoseJudge_Failure(m_uiController.GetCurrentFrame()[1], m_uiController.GetCurrentFrame()[3]))
        {
            m_uiController.UIForcedQuit();
            m_state = InGameState.Failure;

        }



    }

    //現在のフレームを終了してEffectの再生が終わり次第、次のフレームへ
    private void ForcedQuit(PoseFlow poseFlow, CSVDataPoseFlow pose, float seconds)
    {
        // 強制終了時間
        if (poseFlow.HasNextPose())
        {
            m_uiController.UIForcedQuit();

            KeepGameTimeSeconds = 0;
            poseFlow.NextPose();

            m_state = InGameState.Start;
        }
    }

    //成功時
    private void Success()
    {
        m_gameManager.AddScore((int)m_scoreController.GetScore());
        m_venueVoltageSystem.RegisterSuccess(30);
        m_state = InGameState.End;

        if (m_eventSceneVisualDirector != null
            && m_eventSceneVisualDirector.TryPlayEvent(pose.FlowNumber))
        {
            return;
        }

        if (m_effectSystem != null)
        {
            m_effectSystem.PlayRandomEffect();
        }
    }

    public void AddEventScore(int _score)
    {
        if (_score <= 0 || m_gameManager == null)return;

        m_gameManager.AddScore(_score);
    }

    //終了時
    private void Failure()
    {
        m_venueVoltageSystem.RegisterFailure();
        m_state = InGameState.End;
    }
}
