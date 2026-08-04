/*━━━━━━━━━*
*@file EventSpecialNodePlayer.cs*
*@brief 特殊Event中に専用Node一覧を時間順に表示する*
*@author 24cu0312 久場洸太*
*@date 2026/08/03*
*最終更新日 2026/08/03*
*@remarks 通常InGame進行を一時停止しEvent Nodeを独立再生する*
*━━━━━━━━━*/

using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

/// <summary>
/// Runtime ContextまたはMusicNodeSequenceの特殊Event Nodeを順番に表示します。
/// </summary>
public sealed class EventSpecialNodePlayer : MonoBehaviour
{
    public event Action EventNodesCompleted; //特殊Node完了通知

    private const float EMinimumNodeDuration = 0.25f; //Node最低表示時間
    private const float EDefaultLastNodeDuration = 2.0f; //最終Node表示時間

    [SerializeField] private MusicNodeSequence m_sequence; //確認用Event設定
    [SerializeField] private int m_eventIndex; //確認に使うEvent番号
    [SerializeField] private UIController m_uiController; //既存Node表示制御
    [SerializeField] private InGameManager m_inGameManager; //通常進行制御
    [SerializeField] private float m_lastNodeDuration = EDefaultLastNodeDuration; //最終Node表示時間
    CSVDataPoseFlow pose;
    private Coroutine m_playCoroutine; //Event Node再生処理
    private bool b_m_normalFlowSuspended; //通常進行を休止中か
    private bool b_m_wasInGameEnabled; //開始前の通常進行状態
    private bool b_m_hasEventFrames; //特殊Node表示中か

    /// <summary>
    /// EventNodeRuntimeContextを優先して特殊Node再生を開始します。
    /// </summary>
    [ContextMenu("Play Special Event Nodes")]
    public void PlayEventNodes()
    {
        IReadOnlyList<SMusicNodeEvent> sourceNodesList =
            GetEventNodes(); //再生元Node一覧
        if (sourceNodesList == null || sourceNodesList.Count == 0)
        {
            Debug.LogWarning(
                "特殊Event Nodeがありません。Music Node EditorのEvent ScenesへNodeを設定してください。");
            return;
        }

        FindReferences();
        if (m_uiController == null)
        {
            Debug.LogError("特殊Event Node再生にUIControllerが必要です。");
            return;
        }

        StopEventNodes();
        List<SMusicNodeEvent> nodesList =
            new List<SMusicNodeEvent>(sourceNodesList); //時間順に並べる複製
        nodesList.Sort(
            (_left, _right) => _left.m_time.CompareTo(_right.m_time));
        if (m_inGameManager != null)
        {
            b_m_wasInGameEnabled = m_inGameManager.enabled;
            m_inGameManager.enabled = false;
            b_m_normalFlowSuspended = true;
        }

        m_playCoroutine = StartCoroutine(PlayNodesRoutine(nodesList));
    }

    /// <summary>
    /// 特殊Nodeを停止して通常InGame進行を復帰します。
    /// </summary>
    [ContextMenu("Stop Special Event Nodes")]
    public void StopEventNodes()
    {
        if (m_playCoroutine != null)
        {
            StopCoroutine(m_playCoroutine);
            m_playCoroutine = null;
        }

        if (m_uiController != null && b_m_hasEventFrames)
        {
            ClearCurrentFrames();
        }

        ResumeNormalFlow();
    }

    /// <summary>
    /// Event Nodeを時間差に合わせて一つずつ表示します。
    /// </summary>
    private IEnumerator PlayNodesRoutine(
        List<SMusicNodeEvent> _nodesList)
    {
        float firstTime = _nodesList[0].m_time; //Event内基準時間
        float startedTime = Time.unscaledTime; //Event開始時刻
        for (int i = 0; i < _nodesList.Count; ++i)
        {
            SMusicNodeEvent node = _nodesList[i]; //今回Node
            float targetTime = Mathf.Max(0.0f, node.m_time - firstTime); //表示時刻
            while (Time.unscaledTime - startedTime < targetTime)
            {
                yield return null;
            }

            ClearCurrentFrames();
            float duration = GetNodeDuration(_nodesList, i); //今回表示時間
            pose = new CSVDataPoseFlow
            {
                FlowNumber = node.m_nodeNumber,
                PoseID = node.m_poseId,
                PoseName = node.m_eventName,
                time = duration
            }; //既存UIへ渡すNode Data
            m_uiController.UISet_normal(pose);
            b_m_hasEventFrames = true;

            float elapsedSeconds = 0.0f; //Node表示経過時間
            while (elapsedSeconds < duration)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                m_uiController.UIMove_normal(pose);
                yield return null;
            }
        }

        ClearCurrentFrames();
        ResumeNormalFlow();
        m_playCoroutine = null;
        EventNodesCompleted?.Invoke();
    }

    /// <summary>
    /// 次Nodeまでの間隔から現在Node表示時間を求めます。
    /// </summary>
    private float GetNodeDuration(
        List<SMusicNodeEvent> _nodesList,
        int _index)
    {
        if (_index >= _nodesList.Count - 1)
        {
            return Mathf.Max(EMinimumNodeDuration, m_lastNodeDuration);
        }

        return Mathf.Max(
            EMinimumNodeDuration,
            _nodesList[_index + 1].m_time - _nodesList[_index].m_time);
    }

    /// <summary>
    /// 遷移時Contextを優先し、なければ確認用SequenceからEvent Nodeを取得します。
    /// </summary>
    private IReadOnlyList<SMusicNodeEvent> GetEventNodes()
    {
        IReadOnlyList<SMusicNodeEvent> runtimeNodesList =
            EventNodeRuntimeContext.EventNodesList; //遷移元から渡されたNode
        if (runtimeNodesList != null && runtimeNodesList.Count > 0)
        {
            return runtimeNodesList;
        }

        if (m_sequence == null
            || m_eventIndex < 0
            || m_eventIndex >= m_sequence.EventScenesList.Count)return null;

        MusicEventSceneData eventData =
            m_sequence.EventScenesList[m_eventIndex]; //確認対象Event
        return eventData?.m_eventNodesList;
    }

    /// <summary>
    /// 未設定の既存Node表示参照をSceneから取得します。
    /// </summary>
    private void FindReferences()
    {
        if (m_uiController == null)
        {
            m_uiController = FindFirstObjectByType<UIController>();
        }

        if (m_inGameManager == null)
        {
            m_inGameManager = FindFirstObjectByType<InGameManager>();
        }
    }

    /// <summary>
    /// 既存UIControllerに表示中Frameがある場合だけ安全に削除します。
    /// </summary>
    private void ClearCurrentFrames()
    {
        if (m_uiController == null)return;


        List<GameObject> currentFrames = m_uiController.GetCurrentFrame(); //表示中Frame
        if (currentFrames == null)
        {
            b_m_hasEventFrames = false;
            return;
        }

        m_uiController.UIForcedQuit(pose);
        b_m_hasEventFrames = false;
    }

    /// <summary>
    /// 特殊Event中も進んだ現在時刻へ通常Node進行を復帰します。
    /// </summary>
    private void ResumeNormalFlow()
    {
        if (m_inGameManager == null || !b_m_normalFlowSuspended)return;

        m_inGameManager.enabled = b_m_wasInGameEnabled;
        b_m_normalFlowSuspended = false;
    }
}
