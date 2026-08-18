using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    [Header("InGame")]
    [SerializeField] private InGameManager m_InGameManager;

    [Header("UIの操作")]
    [SerializeField] private UIController m_uiController;

    [Header("ポーズデータの管理")]
    [SerializeField] private PoseFlowDataManager m_poseFlowDataManager;

    private InGameState m_inGameState = InGameState.Start;

    private float m_keepGameTimeSeconds;

    public Action<int> PoseJudgeFrame;
    public Action<InGameState> setState;

    private void Awake()
    {
        
    }

 

    public void UIManagerUpdate()
    {
        switch (m_inGameState)
        {
            case InGameState.None:
              
                break;

            case InGameState.Start:
                Set(m_poseFlowDataManager.GetPose());
                break;

            case InGameState.Active:

                Active(m_poseFlowDataManager.GetPose(), m_InGameManager.GetCurrentTIme());
                //Judge(m_poseFlowDataManager.GetPose());
                flowend(m_poseFlowDataManager.GetPose());

                break;

            case InGameState.End:

                ForcedQuit(m_poseFlowDataManager.GetposeFlow(), m_poseFlowDataManager.GetPose());

                break;
            case InGameState.Success:
                //Success();

                break;
            case InGameState.Failure:
               // Failure();

                break;

            default:
                break;
        }
    }

    private void OnEnable()
    {
        m_InGameManager.m_UIManagerAction += State;
    }

    //オブザーバー
    private void OnDisable()
    {
        m_InGameManager.m_UIManagerAction -= State;
    }

    public void State(InGameState _state)
    {
        m_inGameState = _state;
    }

    //今フレームの設定・表示
    private void Set(CSVDataPoseFlow _pose)
    {

        m_keepGameTimeSeconds = m_InGameManager.GetCurrentTIme();
        //UI設定・表示
        m_uiController.UISet_normal(_pose);

        setState?.Invoke(InGameState.Active);
        //m_state = InGameState.Active;
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

    private void flowend(CSVDataPoseFlow _pose)
    {
        Debug.Log("{END0}");
        if ((m_keepGameTimeSeconds + _pose.time) < m_InGameManager.GetCurrentTIme())
        {
            setState?.Invoke(InGameState.End);

            //m_inGameState = InGameState.End;

        }
    }

    private void ForcedQuit(PoseFlow _poseFlow, CSVDataPoseFlow _pose)
    {
        // 強制終了時間
        if (_poseFlow.HasNextPose())
        {
            m_uiController.UIForcedQuit(_pose);

            m_keepGameTimeSeconds = 0;
            _poseFlow.NextPose();
            setState?.Invoke(InGameState.Start);

            //m_state = InGameState.Start;
        }
    }



}
