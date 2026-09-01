/*━━━━━━━━━*
*@file MusicNodeSequence.cs*
*@brief BGMとPose Nodeのタイミングを保存するData Asset*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Music Node Editorから作成、編集する*
*━━━━━━━━━*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// BGM上へ配置するPose Node情報です。
/// </summary>
[Serializable]
public struct SMusicNodeEvent
{
    public int m_nodeNumber; //外部CSVと共有するNode番号
    public float m_time; //BGM開始からの秒数
    public int m_poseId; //Pose ID
    public string m_eventName; //表示名
    [FormerlySerializedAs("m_successEffectName")]
    public string m_successEffectNames; //成功時に固定再生する演出名。複数指定は|区切り
    public string m_failureEffectNames; //失敗時に固定再生する演出名。複数指定は|区切り
}

/// <summary>
/// 条件成立時に再生する一つのEffectと、その配置設定です。
/// </summary>
[Serializable]
public sealed class ConditionalEffectEntry
{
    public string m_effectName; //EffectListに登録されている演出名
    public bool b_m_overridePosition; //Node Editor指定位置を使用するか
    public Vector3 m_position; //演出のWorld座標
    [Min(0.0f)] public float m_delaySeconds; //条件成立後の追加待ち時間
}

/// <summary>
/// 条件式と、成立時にまとめて開始するEffect一覧です。
/// </summary>
[Serializable]
public sealed class ConditionalEffectEvent
{
    public string m_eventName = "Effect Event"; //Editor上の識別名
    public bool b_m_enabled = true; //条件評価を有効にするか
    public bool b_m_useTimelineTime; //Timeline上の配置時刻を発火条件に含めるか
    [Min(0.0f)] public float m_timelineTime; //BGM開始からの配置秒数
    public string m_conditionExpression = "time >= 0"; //発火条件
    public bool b_m_triggerOnce = true; //一度だけ発火するか
    [Min(0.0f)] public float m_repeatIntervalSeconds = 1.0f; //再発火の最短間隔
    public List<ConditionalEffectEntry> m_effectsList =
        new List<ConditionalEffectEntry>(); //同時に予約するEffect一覧
}

[Serializable]
public sealed class MusicBranchNode
{
    public bool b_m_enabled = true;
    public int m_nodeNumber = 1;
    [Min(0.0f)] public float m_time;
    public string m_branchName = "BGM Branch";
    public string m_conditionExpression = "time >= 0";
    public bool b_m_transitionOnSuccess = true;
    public string m_successEffectNames;
    public string m_failureEffectNames;
    public MusicNodeSequence m_targetSequence;
    [Min(0.0f)] public float m_crossFadeSeconds = 2.0f;
}

public enum EMusicEventType
{
    AudienceChoice,
    SpecialNodeBranch
}

/// <summary>
/// 特定Node成功後に開くEvent Sceneと専用Node一覧です。
/// </summary>
[Serializable]
public sealed class MusicEventSceneData
{
    public const string AudienceDecisionEventName = "Decision";
    public const string AudienceEndEventName = "End";

    public bool b_m_enabled = true; //Event遷移を使用するか
    public string m_eventName = "Event"; //Event識別名
    public int m_triggerNodeNumber = 1; //遷移を発生させる通常Node番号
    public EMusicEventType m_eventType = EMusicEventType.AudienceChoice;
    public int m_minimumBonusScore = 100;
    public int m_maximumBonusScore = 1000;
    public List<SMusicNodeEvent> m_eventNodesList =
        new List<SMusicNodeEvent>(); //Event Scene専用Node一覧

    /// <summary>EVENTNAMEがDecisionまたは決定の制御Node時刻を返します。</summary>
    public float GetAudienceChoiceDecisionTime()
    {
        return GetControlNodeTime(AudienceDecisionEventName, "決定", 0.0f);
    }

    /// <summary>EVENTNAMEがEndまたは終了の制御Node時刻を返します。</summary>
    public float GetAudienceChoiceEndTime()
    {
        return GetControlNodeTime(AudienceEndEventName, "終了", GetAudienceChoiceDecisionTime());
    }

    /// <summary>候補Poseではなく、受付開始・終了時刻を表す制御Nodeか判定します。</summary>
    public bool IsAudienceChoiceControlNode(SMusicNodeEvent _node)
    {
        return IsControlName(_node.m_eventName, AudienceDecisionEventName, "決定")
            || IsControlName(_node.m_eventName, AudienceEndEventName, "終了");
    }

    /// <summary>
    /// 制御Nodeを除いたPose候補だけを数え、指定された候補番号の設定を取得します。
    /// Editor上の並び順をそのまま一つ目、二つ目、三つ目として扱います。
    /// </summary>
    public bool TryGetAudienceChoiceCandidate(int _candidateindex, out SMusicNodeEvent _candidate)
    {
        int foundIndex = 0;
        for (int i = 0; i < m_eventNodesList.Count; ++i)
        {
            SMusicNodeEvent node = m_eventNodesList[i];
            if (IsAudienceChoiceControlNode(node))continue;
            if (foundIndex == _candidateindex)
            {
                _candidate = node;
                return true;
            }

            ++foundIndex;
        }

        _candidate = default;
        return false;
    }

    /// <summary>
    /// EVENTNAMEを英語名・日本語名の両方で検索し、見つからない場合は安全な代替時刻を返します。
    /// </summary>
    private float GetControlNodeTime(string _englishname, string _japanesename, float _fallback)
    {
        for (int i = 0; i < m_eventNodesList.Count; ++i)
        {
            SMusicNodeEvent node = m_eventNodesList[i];
            if (IsControlName(node.m_eventName, _englishname, _japanesename))
            {
                return Mathf.Max(0.0f, node.m_time);
            }
        }

        return _fallback;
    }

    private static bool IsControlName(
        string _eventname,
        string _englishname,
        string _japanesename)
    {
        return string.Equals(
                _eventname?.Trim(),
                _englishname,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                _eventname?.Trim(),
                _japanesename,
                StringComparison.Ordinal);
    }
}

/// <summary>
/// BGMと時系列Nodeをまとめて保存します。
/// </summary>
[CreateAssetMenu(
    fileName = "MusicNodeSequence",
    menuName = "Effect System/Music Node Sequence")]
public sealed class MusicNodeSequence : ScriptableObject
{
    [SerializeField] private AudioClip m_bgmClip; //編集対象BGM
    [SerializeField] private float m_manualTimelineDuration = 60.0f; //BGM未設定時の編集時間
    [SerializeField] private List<SMusicNodeEvent> m_eventsList =
        new List<SMusicNodeEvent>(); //Node一覧
    [SerializeField] private List<MusicEventSceneData> m_eventScenesList =
        new List<MusicEventSceneData>(); //Event Scene設定一覧
    [SerializeField] private List<ConditionalEffectEvent> m_conditionalEffectsList =
        new List<ConditionalEffectEvent>(); //条件で発火する演出一覧
    [SerializeField] private List<MusicBranchNode> m_musicBranchesList =
        new List<MusicBranchNode>(); //別Sequenceへ切り替えるBGM分岐Node一覧

    public AudioClip BgmClip
    {
        get
        {
            return m_bgmClip;
        }
        set
        {
            m_bgmClip = value;
        }
    }

    public float ManualTimelineDuration
    {
        get
        {
            return Mathf.Max(1.0f, m_manualTimelineDuration);
        }
        set
        {
            m_manualTimelineDuration = Mathf.Max(1.0f, value);
        }
    }

    public float TimelineDuration
    {
        get
        {
            return m_bgmClip != null
                ? Mathf.Max(1.0f, m_bgmClip.length)
                : ManualTimelineDuration;
        }
    }

    public List<SMusicNodeEvent> EventsList
    {
        get
        {
            return m_eventsList;
        }
    }

    public List<MusicEventSceneData> EventScenesList
    {
        get
        {
            return m_eventScenesList;
        }
    }

    public List<ConditionalEffectEvent> ConditionalEffectsList
    {
        get
        {
            if (m_conditionalEffectsList == null)
            {
                m_conditionalEffectsList = new List<ConditionalEffectEvent>();
            }
            return m_conditionalEffectsList;
        }
    }

    public List<MusicBranchNode> MusicBranchesList
    {
        get
        {
            if (m_musicBranchesList == null)
            {
                m_musicBranchesList = new List<MusicBranchNode>();
            }
            return m_musicBranchesList;
        }
    }
}
