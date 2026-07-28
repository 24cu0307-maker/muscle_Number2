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

    [Header("終了の時間")]
    [SerializeField] private float m_endtimer;

    private UIState m_currentUIState = UIState.None;

    [SerializeField] private PoseCameraDirector poseCameraDirector;

    private float GameTimeSeconds;                  //現在のゲーム時間
    private int PoseMaxCount = 20;            //ポーズ数を設定

    private PoseFlow poseFlow;  　　　　  //ポーズ順の管理

    private CSVDataPoseFlow pose;

    private int m_SpecialFrame = -1;

    public Action<PoseFlow, CSVDataPoseFlow, float> PoseFrame;
    bool one = true;

    private InGameState m_state = InGameState.None;

    private bool m_isb;

    private float KeepGameTimeSeconds;

    public void Start()
    {

        //ゲームを開始する
        //GameManagerで管理している
        //Timerとスコアをリセット
        //Timerの開始と状態の切り替え
        m_gameManager.StartGame();

        // CSVのデータをPoseFlowへ渡す
        poseFlow = new PoseFlow(m_excelLoader.excelPoseTimeFlowLoader.GetCSVDatas());

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

        Debug.Log("pose.PoseID" + pose.PoseID);
        switch (m_state)
        {
            case InGameState.None:

                if(one)
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
                //m_effectSystem.PlayEffect("1");
                //m_uiController.UISet_thirdPerson(pose, GameTimeSeconds);


                KeepGameTimeSeconds = GameTimeSeconds;
                m_uiController.UISet_normal(pose, GameTimeSeconds);
                break;

            case InGameState.Active:

                //m_uiController.UIMove_thirdPerson(pose, GameTimeSeconds);
                //m_uiController.UIJudgeEnd_thirdPerson();
                m_uiController.UIMove_normal(pose, GameTimeSeconds);
                m_uiController.UIJudgeEnd_normal(pose);

                flowend(pose);

                break;

            case InGameState.End:
                Debug.Log("{END1}");
                m_effectSystem.IsEffectPlay();

                if (!m_effectSystem.IsPlayEffect())
                {
                    m_uiController.UIForcedQuit(poseFlow, pose, GameTimeSeconds);

                    m_state = InGameState.Start;
                }


                break;
        }

        /*
       

        //再生中かを調べる
        m_effectSystem.IsEffectPlay("Tesst");
        Debug.Log("aiueo" + m_effectSystem.IsPlayEffect());

        //再生中でなければ次へ実行
       
        */

        /*
        //UIフレームが表示されている間は判定を行う
        m_poseJudgeController.PoseJudge(pose.PoseID);
        //UIフレームの判定が重なったらスコアを計算し全体スコアに加算
        m_scoreController.PoseScoreJudge(pose.PoseID);

        //成功状態に遷移
        */
        //m_uiController.UIAnimation(poseFlow, pose, GameTimeSeconds);

    }


    /// <summary>
    /// 現在のゲーム時間の更新
    /// </summary>
    private void UpdateTime()
    {
        GameTimeSeconds = m_gameManager.GetTimeManager().GameTimeSeconds;
    }

    //オブザーバー
    private void OnEnable()
    {
        m_uiController.State += InGameflowState;
    }

    //オブザーバー
    private void OnDisable()
    {
        m_uiController.State -= InGameflowState;
    }


    public void InGameflowState(InGameState _state)
    {
        m_state = _state;
    }


    private void flowend(CSVDataPoseFlow _pose)
    {
        Debug.Log("{END0}");
        if ((KeepGameTimeSeconds + _pose.time) < GameTimeSeconds)
        {
            InGameflowState(InGameState.End);
            
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

}
