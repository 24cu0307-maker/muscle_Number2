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
using UnityEngine.UIElements;

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

    [Header("終了の時間")]
    [SerializeField] private float m_endtimer;



    private UIState m_currentUIState = UIState.None;



    private float GameTimeSeconds;                  //現在のゲーム時間
    private int PoseMaxCount = 20;            //ポーズ数を設定

    private PoseFlow poseFlow;  　　　　  //ポーズ順の管理

    private PoseFlow poseFlow_SP;  　　　　  //ポーズ順の管理

    private CSVDataPoseFlow pose;

    private CSVDataPoseFlow pose_SP;

    private int m_SpecialFrame = -1;

    public Action<PoseFlow, CSVDataPoseFlow, float> PoseFrame;

    public Action<int> PoseJudgeFrame;

    bool one = true;
    bool isSP = true;

    private InGameState m_state = InGameState.Start;

    private InGameState_SP m_state_sp = InGameState_SP.None;

    private float m_keepGameTimeSeconds;

    bool check = true;

    public void Start()
    {

        //ゲームを開始する
        //GameManagerで管理している
        //Timerとスコアをリセット
        //Timerの開始と状態の切り替え
        m_gameManager.StartGame();

        // CSVのデータをPoseFlowへ渡す
        poseFlow = new PoseFlow(m_excelLoader.excelPoseTimeFlowLoader.GetCSVDatas());

        poseFlow_SP = new PoseFlow(m_excelLoader.excelPoseTimeFlowLoader_SP.GetCSVDatas());

        m_effectSystem.PlayEffect("Tesst");


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

        pose_SP = poseFlow_SP.CurrentPose();

        if (pose_SP.PoseName == pose.PoseName && isSP)
        {
            Debug.Log("Name" + pose_SP.PoseName);
            m_state = InGameState.None;
            m_state_sp = InGameState_SP.Start;

            isSP = false;

        }


        Debug.Log("pose.PoseID" + pose.PoseID);
        switch (m_state)
        {
            case InGameState.None:
                /*
                if (one)
                {
                    poseCameraDirector.Play();
                    one = false;
                }

                if (8 <= GameTimeSeconds)
                {
                    m_state = InGameState.Start;

                }
                */
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
                
                ForcedQuit(poseFlow, pose, GameTimeSeconds);

                break;
            case InGameState.Success:
                Success();

                break;
            case InGameState.Failure:
                Failure();

                break;
        }

        switch (m_state_sp)
        {
            case InGameState_SP.None:

                break;

            case InGameState_SP.Start:
                Set_SP(pose, GameTimeSeconds);
                break;

            case InGameState_SP.Active:
                Active_SP(pose, GameTimeSeconds);
                Judge_SP(pose);
                flowend(pose);
                break;

            case InGameState_SP.End:

                ForcedQuit(poseFlow, pose, GameTimeSeconds);

                break;
            case InGameState_SP.Success:

                break;
            case InGameState_SP.Failure:

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
        if ((m_keepGameTimeSeconds + _pose.time) < GameTimeSeconds)
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
       

        m_keepGameTimeSeconds = _seconds;
        //UI設定・表示
        m_uiController.UISet_normal(_pose);


        m_state = InGameState.Active;
    }

    //今フレームの設定・表示
    private void Set_SP(CSVDataPoseFlow _pose, float _seconds)
    {
        m_keepGameTimeSeconds = _seconds;


        if (pose_SP.PoseName == pose.PoseName)
        {
            //UI設定・表示
            //m_uiController.UISet_thirdPerson(_pose);

        }


        m_state_sp = InGameState_SP.Active;
    }


    //実行
    private void Active(CSVDataPoseFlow _pose, float _seconds)
    {

        // 縮小(通常フレーム)
        if (_seconds <= (_pose.time + m_keepGameTimeSeconds))
        {
            m_uiController.UIMove_normal(_pose);

            //イベント実行　当たり判定
            PoseJudgeFrame?.Invoke(_pose.PoseID);
        }
    }

    //実行
    private void Active_SP(CSVDataPoseFlow _pose, float _seconds)
    {

        if (pose_SP.PoseName == pose.PoseName)
        {
            // 縮小(通常フレーム)
            if (_seconds <= (_pose.time + m_keepGameTimeSeconds))
            {

                m_uiController.UIMove_thirdPerson();


                //イベント実行　当たり判定
                for (int i = 0; i < 3; i++)
                {
                    PoseJudgeFrame?.Invoke(i);
                }
            }
        }
       
    }

    //判定
    private void Judge(CSVDataPoseFlow _pose)
    {
        //通常成功時
        if (m_poseJudgeController.GetisPose(_pose.PoseID) &&
            m_poseJudgeController.PoseJudge_Normal(m_uiController.GetCurrentApproachingFrame(_pose), m_uiController.GetCurrentWatingFrame(_pose)))
        {
            m_uiController.UIJudgeEnd_normal(_pose);
            m_uiController.UIForcedQuit(_pose);
            m_state = InGameState.Success;

        }

        //完璧成功時
        if (m_poseJudgeController.GetisPose(_pose.PoseID) &&
            m_poseJudgeController.PoseJudge_Perfect(m_uiController.GetCurrentApproachingFrame(_pose), m_uiController.GetCurrentWatingFrame(_pose)))
        {
            m_uiController.UIJudgeEnd_normal(_pose);
            m_uiController.UIForcedQuit(_pose);
            m_state = InGameState.Success;

        }

        //失敗
        if (m_poseJudgeController.PoseJudge_Failure(m_uiController.GetCurrentApproachingFrame(_pose), m_uiController.GetCurrentWatingFrame(_pose)))
        {
            m_uiController.UIForcedQuit(_pose);
            m_state = InGameState.Failure;

        }



    }

    private void Judge_SP(CSVDataPoseFlow _pose)
    {
        for (int poseID = 0; poseID < 2; poseID++)
        {
            int index = poseID * 4 + 1;

            if (!check)
            {
                m_state = InGameState.Success;
                break;
            }
            
            //通常
            if (m_poseJudgeController.GetisPose(poseID) &&
                m_poseJudgeController.PoseJudge_Normal(
                  m_uiController.GetCurrentApproachingFrame(_pose), m_uiController.GetCurrentWatingFrame(_pose)))
            {
                m_uiController.UIJudge_thirdPerson();
                 check = false;

            }

            if (!check)
            {
                m_state = InGameState.Success;
                break;
            }

            //完璧
            if (m_poseJudgeController.GetisPose(poseID) &&
               m_poseJudgeController.PoseJudge_Perfect(
                   m_uiController.GetCurrentApproachingFrame(_pose), m_uiController.GetCurrentWatingFrame(_pose)))
            {
                m_uiController.UIJudge_thirdPerson();
                check = false;

            }
            
        }

    }

    //現在のフレームを終了してEffectの再生が終わり次第、次のフレームへ
    private void ForcedQuit(PoseFlow _poseFlow, CSVDataPoseFlow _pose, float seconds)
    {
        // 強制終了時間
        if (_poseFlow.HasNextPose())
        {
            m_uiController.UIForcedQuit(_pose);

            m_keepGameTimeSeconds = 0;
            _poseFlow.NextPose();

            m_state = InGameState.Start;
        }
    }

    //成功時
    private void Success()
    {
        m_gameManager.AddScore((int)m_scoreController.GetScore());
        m_effectSystem.PlayRandomEffect();


        m_venueVoltageSystem.RegisterSuccess(30);


        m_state = InGameState.End;
    }

    //終了時
    private void Failure()
    {
        m_venueVoltageSystem.RegisterFailure();
        m_state = InGameState.End;
    }
}
