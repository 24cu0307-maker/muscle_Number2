/*━━━━━━━━━*
*@file EventNodeRuntimeContext.cs*
*@brief Event Sceneへ専用Node設定を引き継ぐ*
*@author 24cu0312 久場洸太*
*@date 2026/08/02*
*最終更新日 2026/08/02*
*@remarks Scene切替後のEvent専用処理から参照する*
*━━━━━━━━━*/

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 選択されたEvent設定をScene切替後まで保持します。
/// </summary>
public static class EventNodeRuntimeContext
{
    private const int EAudienceCandidateCount = 3;
    private static readonly List<SMusicNodeEvent> m_audienceCandidatesList =
        new List<SMusicNodeEvent>();

    public static MusicEventSceneData CurrentEvent { get; private set; }

    public static IReadOnlyList<SMusicNodeEvent> EventNodesList
    {
        get
        {
            if (CurrentEvent == null)return null;

            return CurrentEvent.m_eventNodesList;
        }
    }

    /// <summary>
    /// Event Sceneへ渡す設定を保存します。
    /// </summary>
    public static void Begin(MusicEventSceneData _eventData)
    {
        CurrentEvent = _eventData;
        PrepareRandomAudienceCandidates();
    }

    public static bool TryGetAudienceChoiceCandidate(
        int _candidateIndex,
        out SMusicNodeEvent _candidate)
    {
        if (_candidateIndex >= 0 && _candidateIndex < m_audienceCandidatesList.Count)
        {
            _candidate = m_audienceCandidatesList[_candidateIndex];
            return true;
        }

        _candidate = default;
        return false;
    }

    private static void PrepareRandomAudienceCandidates()
    {
        m_audienceCandidatesList.Clear();
        if (CurrentEvent == null
            || CurrentEvent.m_eventType != EMusicEventType.AudienceChoice)return;

        int candidateIndex = 0;
        while (CurrentEvent.TryGetAudienceChoiceCandidate(
            candidateIndex,
            out SMusicNodeEvent candidate))
        {
            m_audienceCandidatesList.Add(candidate);
            ++candidateIndex;
        }

        for (int i = m_audienceCandidatesList.Count - 1; i > 0; --i)
        {
            int swapIndex = Random.Range(0, i + 1);
            SMusicNodeEvent temporary = m_audienceCandidatesList[i];
            m_audienceCandidatesList[i] = m_audienceCandidatesList[swapIndex];
            m_audienceCandidatesList[swapIndex] = temporary;
        }

        if (m_audienceCandidatesList.Count > EAudienceCandidateCount)
        {
            m_audienceCandidatesList.RemoveRange(
                EAudienceCandidateCount,
                m_audienceCandidatesList.Count - EAudienceCandidateCount);
        }
    }

    /// <summary>
    /// Event終了後に保持設定を消去します。
    /// </summary>
    public static void Clear()
    {
        m_audienceCandidatesList.Clear();
        CurrentEvent = null;
    }
}
