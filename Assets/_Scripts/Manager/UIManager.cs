using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{

    [Header("UIの操作")]
    [SerializeField] private UIController m_uiController;

    private float m_keepGameTimeSeconds; //現在Pose UIを表示開始したゲーム時刻

    public Action<int> PoseJudgeFrame; //判定可能時間中に対象PoseIDを判定側へ通知するCallback
    public Action<InGameState> setState; //UI進行に応じた次状態をInGameManagerへ通知するCallback

    private void Awake()
    {
        
    }

 

    /// <summary>
    /// InGame状態に合わせてPose UIの生成、縮小Animation、表示時間終了を進行します。
    /// BGM同期済みの現在時刻を受け取るため、Frame数ではなく秒数を基準に動作します。
    /// </summary>
    public void UIManagerUpdate(
        InGameState _state,
        CSVDataPoseFlow _pose,
        float _currenttimeseconds)
    {
        switch (_state)
        {
            case InGameState.None:
              
                break;

            case InGameState.Start:
                Set(_pose, _currenttimeseconds);
                break;

            case InGameState.Active:

                Active(_pose, _currenttimeseconds);
                FlowEnd(_pose, _currenttimeseconds);

                break;

            case InGameState.End:

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

    /// <summary>Pose UIを生成し、開始時刻を保存してActive状態へ遷移します。</summary>
    private void Set(
        CSVDataPoseFlow _pose,
        float _currenttimeseconds)
    {
        m_keepGameTimeSeconds = _currenttimeseconds;
        //UI設定・表示
        m_uiController.UISet_normal(_pose);

        setState?.Invoke(InGameState.Active);
        //m_state = InGameState.Active;
    }

    /// <summary>Poseの有効時間内にUIを動かし、毎FrameのPose判定を要求します。</summary>
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

    /// <summary>開始時刻とPose表示時間から終了時刻を計算し、超過時にNodeを終了します。</summary>
    private void FlowEnd(
        CSVDataPoseFlow _pose,
        float _currenttimeseconds)
    {
        Debug.Log("{END0}");
        if ((m_keepGameTimeSeconds + _pose.time) < _currenttimeseconds)
        {
            setState?.Invoke(InGameState.End);

            //m_inGameState = InGameState.End;

        }
    }

    /// <summary>Event開始等で通常Poseを中断する際、表示中UIと保持時刻を即座に初期化します。</summary>
    public void FinishCurrentPose(CSVDataPoseFlow _pose)
    {
        m_uiController.UIForcedQuit(_pose);
        m_keepGameTimeSeconds = 0;
    }



}
