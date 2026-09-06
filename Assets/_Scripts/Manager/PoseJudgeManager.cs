using GameFlowTemplate;
using System;
using UnityEngine;

/// <summary>Pose判定とゲーム状態遷移を管理し、演出出力は専用Componentへ委譲します。</summary>
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

    [Header("一致率による3段階判定")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_perfectMatchRate = 0.95f;
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_greatMatchRate = 0.75f;

    [Header("判定演出出力")]
    [SerializeField] private PoseJudgementFeedbackPlayer m_feedbackPlayer;

    public Action<InGameState> setState; //判定結果に応じた次のInGame状態を管理側へ通知するCallback

    public EPoseMatchGrade LastGrade { get; private set; } = EPoseMatchGrade.Miss;
    public float LastMatchRate { get; private set; }


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
            CompleteJudge(_pose, EPoseMatchGrade.Perfect, 1.0f);
            return;
        }

        bool b_poseMatched = m_poseJudgeController.GetisPose(_pose.PoseID);
        bool b_inJudgeWindow = m_poseJudgeController.PoseJudge_Normal(
            m_uiController.GetCurrentApproachingFrame(_pose),
            m_uiController.GetCurrentWatingFrame(_pose));
        if (b_poseMatched && b_inJudgeWindow)
        {
            float matchRate = m_scoreController.GetMatchRate();
            CompleteJudge(_pose, GetGrade(matchRate), matchRate);
            return;
        }

        if (m_poseJudgeController.PoseJudge_Failure(
            m_uiController.GetCurrentApproachingFrame(_pose),
            m_uiController.GetCurrentWatingFrame(_pose)))
        {
            CompleteJudge(_pose, EPoseMatchGrade.Miss, 0.0f);
        }
    }

    /// <summary>一致率をInspectorで設定した閾値から3段階へ分類します。</summary>
    private EPoseMatchGrade GetGrade(float _matchrate)
    {
        float perfectRate = Mathf.Max(m_perfectMatchRate, m_greatMatchRate);
        float greatRate = Mathf.Min(m_perfectMatchRate, m_greatMatchRate);
        if (_matchrate >= perfectRate)return EPoseMatchGrade.Perfect;
        if (_matchrate >= greatRate)return EPoseMatchGrade.Great;
        return EPoseMatchGrade.Miss;
    }

    /// <summary>UIを閉じ、段階に対応するSuccessまたはFailure状態へ一度だけ進めます。</summary>
    private void CompleteJudge(
        CSVDataPoseFlow _pose,
        EPoseMatchGrade _grade,
        float _matchrate)
    {
        LastGrade = _grade;
        LastMatchRate = Mathf.Clamp01(_matchrate);
        if (_grade != EPoseMatchGrade.Miss)
        {
            m_uiController.UIJudgeEnd_normal(_pose);
        }
        m_uiController.UIForcedQuit(_pose);
        setState?.Invoke(
            _grade == EPoseMatchGrade.Miss
                ? InGameState.Failure
                : InGameState.Success);
    }

    /// <summary>
    /// 通常Node成功時のScore加算、ボルテージ更新、Effect再生、特殊Event起動をまとめて確定します。
    /// </summary>
    private void Success(CSVDataPoseFlow _pose)
    {
        m_gameManager.AddScore((int)m_scoreController.GetScore());
        m_feedbackPlayer?.Play(LastGrade);
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
        m_feedbackPlayer?.Play(EPoseMatchGrade.Miss);
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
