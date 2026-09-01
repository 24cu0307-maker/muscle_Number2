using GameFlowTemplate;
using System;
using UnityEngine;

public class PoseJudgeManager : MonoBehaviour
{

    [Header("ポーズの判定")]
    [SerializeField] private PoseJudgeController m_poseJudgeController;

    [Header("スコア判定")]
    [SerializeField] private ScoreController m_scoreController;

    [Header("UIの操作")]
    [SerializeField] private UIController m_uiController;

    [Header("熱量")]
    [SerializeField] private VenueVoltageSystem m_venueVoltageSystem;

    [Header("イベントディレクター")]
    [SerializeField]
    private EventSceneVisualDirector m_eventSceneVisualDirector;

    [Header("エフェクトシステム")]
    [SerializeField] private EffectSystem m_effectSystem;

    [Header("gameManager")]
    [SerializeField] private GameManager m_gameManager;

    public Action<InGameState> setState; //判定結果に応じた次のInGame状態を管理側へ通知するCallback


    private void Awake()
    {
        LiveStagePostProcess.GetOrCreate(gameObject);
    }


    /// <summary>
    /// InGameManagerが保持する現在状態に応じて、判定・成功確定・失敗確定の処理を振り分けます。
    /// 状態そのものは直接所有せず、setState通知で管理元に遷移を依頼します。
    /// </summary>
    public void PoseJudgeManagerUpdate(
        InGameState _state,
        CSVDataPoseFlow _pose)
    {
        switch (_state)
        {
            case InGameState.None:

                break;

            case InGameState.Start:
                break;

            case InGameState.Active:

                Judge(_pose);

                break;

            case InGameState.End:


                break;
            case InGameState.Success:
                Success(_pose);

                break;
            case InGameState.Failure:
                Failure(_pose);

                break;

            default :
                break;
        }
    }

    /// <summary>
    /// 現在Poseの身体判定とUI枠の時間判定を組み合わせ、Perfect・Normal・Failureを決定します。
    /// 成否確定時は対象UIを終了し、後続処理を行う状態へ遷移させます。
    /// </summary>
    private void Judge(CSVDataPoseFlow _pose)
    {
        if (EffectDebugKeySettings.ForceAllSuccess)
        {
            m_uiController.UIJudgeEnd_normal(_pose);
            m_uiController.UIForcedQuit(_pose);
            setState?.Invoke(InGameState.Success);
            return;
        }

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

    /// <summary>
    /// 通常Node成功時のScore加算、ボルテージ更新、Effect再生、特殊Event起動をまとめて確定します。
    /// </summary>
    private void Success(CSVDataPoseFlow _pose)
    {
        m_gameManager.AddScore((int)m_scoreController.GetScore());
        string fixedEffectNames = _pose.SuccessEffectNames?.Trim();
        if (!string.IsNullOrEmpty(fixedEffectNames))
        {
            m_effectSystem?.PlayMusicNodeEffects(fixedEffectNames);
        }


        m_venueVoltageSystem.RegisterSuccess(30);

        m_eventSceneVisualDirector.TryPlayEvent(_pose.FlowNumber);

        setState?.Invoke(InGameState.End);
        //m_state = InGameState.End;
    }

    /// <summary>失敗をボルテージへ通知し、現在Nodeの処理を終了状態へ進めます。</summary>
    private void Failure(CSVDataPoseFlow _pose)
    {
        string fixedEffectNames = _pose.FailureEffectNames?.Trim();
        if (!string.IsNullOrEmpty(fixedEffectNames))
        {
            m_effectSystem?.PlayMusicNodeEffects(fixedEffectNames);
        }

        m_venueVoltageSystem.RegisterFailure();

        setState?.Invoke(InGameState.End);
        //m_state = InGameState.End;
    }

}
