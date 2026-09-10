using UnityEngine;

public class PoseFlowDataManager : MonoBehaviour
{
    [Header("Music Node Sequence")]
    [SerializeField] private MusicNodeSequence m_sequence; //BGM時刻とPose Node設定を一括保持するSequence

    private int m_currentNodeIndex = -1; //現在のBGM時刻に該当する通常Node番号。開始前は-1
    public bool IsInitialized => m_sequence != null; //時刻同期に必要なSequenceが設定済みか
    public bool HasActivePose => m_currentNodeIndex >= 0; //現在表示・判定すべき通常Poseが存在するか
    public float TimelineDuration => m_sequence != null
        ? m_sequence.TimelineDuration
        : 0.0f;

    private void OnEnable()
    {
        VoltageBgmSystem bgmSystem = FindFirstObjectByType<VoltageBgmSystem>();
        if (bgmSystem == null)return;
        bgmSystem.SequenceChanged -= SetSequence;
        bgmSystem.SequenceChanged += SetSequence;
    }

    private void OnDisable()
    {
        VoltageBgmSystem bgmSystem = FindFirstObjectByType<VoltageBgmSystem>();
        if (bgmSystem != null)bgmSystem.SequenceChanged -= SetSequence;
    }

    public void SetSequence(MusicNodeSequence _sequence)
    {
        if (_sequence == null)return;
        m_sequence = _sequence;
        m_currentNodeIndex = -1;
    }

    /// <summary>
    /// BGMの絶対時刻から現在の通常Nodeを決定します。
    /// Nodeが切り替わったFrameだけtrueを返します。
    /// </summary>
    public bool SynchronizeToBgmTime(float _bgmtimeseconds)
    {
        if (m_sequence == null || m_sequence.EventsList.Count == 0)return false;

        int synchronizedIndex = -1;
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            if (m_sequence.EventsList[i].m_time > _bgmtimeseconds)break;
            synchronizedIndex = i;
        }

        if (synchronizedIndex == m_currentNodeIndex)return false;

        m_currentNodeIndex = synchronizedIndex;
        return true;
    }

    /// <summary>
    /// 現在Nodeを既存UI・判定処理が扱うCSVDataPoseFlow形式へ変換します。
    /// Event終了後もSequenceに設定されたPoseをそのまま使用します。
    /// </summary>
    public CSVDataPoseFlow GetPose()
    {
        if (!HasActivePose)return default;

        SMusicNodeEvent node = m_sequence.EventsList[m_currentNodeIndex];
        float duration = GetCurrentNodeDuration();
        return new CSVDataPoseFlow
        {
            FlowNumber = node.m_nodeNumber,
            PoseID = node.m_poseId,
            PoseName = node.m_eventName,
            time = duration,
            SuccessEffectNames = node.m_successEffectNames,
            FailureEffectNames = node.m_failureEffectNames
        };
    }

    /// <summary>現在位置より後ろに通常Nodeが登録されているかを返します。</summary>
    public bool HasNextPose()
    {
        return m_sequence != null
            && m_currentNodeIndex + 1 < m_sequence.EventsList.Count;
    }

    // 互換用です。通常進行はSynchronizeToBgmTimeでのみ切り替えます。
    public bool MoveNextPose()
    {
        return HasNextPose();
    }

    /// <summary>
    /// Event中に通過した通常Nodeを表示済みとして扱い、終了後の遅延表示を防ぎます。
    /// Event終了後は、この時刻より後の通常Node設定へそのまま進みます。
    /// </summary>
    public void SkipElapsedNodesAfterEvent(float _bgmtimeseconds)
    {
        if (m_sequence == null || m_sequence.EventsList.Count == 0)return;

        int elapsedNodeIndex = -1;
        for (int i = 0; i < m_sequence.EventsList.Count; ++i)
        {
            if (m_sequence.EventsList[i].m_time > _bgmtimeseconds)break;
            elapsedNodeIndex = i;
        }

        m_currentNodeIndex = elapsedNodeIndex;
    }

    /// <summary>
    /// 現在Nodeから次Nodeまでの時刻差を表示時間として返します。
    /// 最終NodeではBGM末尾までを使用し、BGM未設定なら0秒として安全に終了します。
    /// </summary>
    private float GetCurrentNodeDuration()
    {
        if (m_currentNodeIndex + 1 < m_sequence.EventsList.Count)
        {
            return Mathf.Max(
                0.0f,
                m_sequence.EventsList[m_currentNodeIndex + 1].m_time
                - m_sequence.EventsList[m_currentNodeIndex].m_time);
        }

        AudioClip bgmClip = m_sequence.BgmClip;
        return bgmClip != null
            ? Mathf.Max(0.0f, bgmClip.length - m_sequence.EventsList[m_currentNodeIndex].m_time)
            : 0.0f;
    }
}
