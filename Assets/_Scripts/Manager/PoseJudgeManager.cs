using GameFlowTemplate;
using System;
using UnityEngine;

public class PoseJudgeManager : MonoBehaviour
{

    [Header("InGame")]
    [SerializeField] private InGameManager m_InGameManager;

    [Header("ポーズの判定")]
    [SerializeField] private PoseJudgeController m_poseJudgeController;

    [Header("スコア判定")]
    [SerializeField] private ScoreController m_scoreController;

    [Header("UIの操作")]
    [SerializeField] private UIController m_uiController;

    [Header("ポーズデータの管理")]
    [SerializeField] private PoseFlowDataManager m_poseFlowDataManager;


    [Header("熱量")]
    [SerializeField] private VenueVoltageSystem m_venueVoltageSystem;

    [Header("イベントディレクター")]
    [SerializeField]
    private EventSceneVisualDirector m_eventSceneVisualDirector;

    [Header("エフェクトシステム")]
    [SerializeField] private EffectSystem m_effectSystem;

    [Header("gameManager")]
    [SerializeField] private GameManager m_gameManager;

    private InGameState m_inGameState = InGameState.None;
    public Action<InGameState> setState;


    private void Awake()
    {
        
    }


    public void PoseJudgeManagerUpdate()
    {
        switch (m_inGameState)
        {
            case InGameState.None:

                break;

            case InGameState.Start:
                break;

            case InGameState.Active:

                Judge(m_poseFlowDataManager.GetPose());

                break;

            case InGameState.End:


                break;
            case InGameState.Success:
                Success();

                break;
            case InGameState.Failure:
                Failure();

                break;

            default :
                break;
        }
    }

    private void OnEnable()
    {
        m_InGameManager.m_PoseJudgeManagerAction += State;

    }

    //オブザーバー
    private void OnDisable()
    {
        m_InGameManager.m_PoseJudgeManagerAction -= State;
    }

    public void State(InGameState _state)
    {
        m_inGameState = _state;
    }

    private void Judge(CSVDataPoseFlow _pose)
    {
        //通常成功時
        if (m_poseJudgeController.GetisPose(_pose.PoseID) &&
            m_poseJudgeController.PoseJudge_Normal(m_uiController.GetCurrentApproachingFrame(_pose), m_uiController.GetCurrentWatingFrame(_pose)))
        {
            m_uiController.UIJudgeEnd_normal(_pose);
            m_uiController.UIForcedQuit(_pose);
            //m_state = InGameState.Success;
            setState?.Invoke(InGameState.Success);


        }

        //完璧成功時
        if (m_poseJudgeController.GetisPose(_pose.PoseID) &&
            m_poseJudgeController.PoseJudge_Perfect(m_uiController.GetCurrentApproachingFrame(_pose), m_uiController.GetCurrentWatingFrame(_pose)))
        {
            m_uiController.UIJudgeEnd_normal(_pose);
            m_uiController.UIForcedQuit(_pose);
            //m_state = InGameState.Success;
            setState?.Invoke(InGameState.Success);

        }

        //失敗
        if (m_poseJudgeController.PoseJudge_Failure(m_uiController.GetCurrentApproachingFrame(_pose), m_uiController.GetCurrentWatingFrame(_pose)))
        {
            m_uiController.UIForcedQuit(_pose);
            //m_state = InGameState.Failure;
            setState?.Invoke(InGameState.Failure);

        }



    }

    //成功時
    private void Success()
    {
        m_gameManager.AddScore((int)m_scoreController.GetScore());
        m_effectSystem.PlayRandomEffect();


        m_venueVoltageSystem.RegisterSuccess(30);

        m_eventSceneVisualDirector.TryPlayEvent(m_poseFlowDataManager.GetPose().PoseID);

        setState?.Invoke(InGameState.End);
        //m_state = InGameState.End;
    }

    //終了時
    private void Failure()
    {
        m_venueVoltageSystem.RegisterFailure();

        setState?.Invoke(InGameState.End);
        //m_state = InGameState.End;
    }

}
