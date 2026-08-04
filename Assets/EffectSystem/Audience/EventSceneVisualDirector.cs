/*━━━━━━━━━*
*@file EventSceneVisualDirector.cs*
*@brief 外部の特殊Nodeから既存CameraSequenceとCanvasを一括操作する*
*@author 24cu0312 久場洸太*
*@date 2026/08/03*
*最終更新日 2026/08/03*
*@remarks 特殊Node実装へ依存しない公開関数を接続口として提供する*
*━━━━━━━━━*/

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 特殊Node側からEvent演出を開始・終了するための接続口です。
/// </summary>
public sealed class EventSceneVisualDirector : MonoBehaviour
{
    [SerializeField] private PoseCameraDirector m_cameraDirector; //既存Camera演出制御
    [SerializeField] private CameraSequence m_cameraSequence; //Event用旋回Sequence
    [SerializeField] private MusicNodeSequence m_musicNodeSequence; //特殊NodeとTrigger設定
    [SerializeField] private EventAudienceCanvasController m_canvasController; //観客Node Canvas制御
    [SerializeField] private EventSpecialNodePlayer m_specialNodePlayer; //特殊Event Node再生
    [SerializeField] private AudiencePreferenceSystem m_preferenceSystem;
    [SerializeField] private InGameManager m_inGameManager;
    [SerializeField] private float m_canvasDelaySeconds = 0.2f; //Canvas表示待機時間
    [SerializeField] private bool b_m_playOnStart = true; //Event Scene単体確認用
    [SerializeField] private UnityEvent m_onEventVisualStarted; //演出開始通知
    [SerializeField] private UnityEvent m_onEventVisualStopped; //演出終了通知

    private Coroutine m_playCoroutine; //表示待機処理
    private bool b_m_isPlaying; //Event演出中か
    private bool b_m_normalFlowSuspended;
    private bool b_m_wasInGameEnabled;
    private MusicEventSceneData m_currentEvent;

    /// <summary>
    /// 成功した通常Node番号に対応するEventを検索して開始します。
    /// </summary>
    public bool TryPlayEvent(int _nodeNumber)
    {
        if (b_m_isPlaying || m_musicNodeSequence == null)return false;

        for (int i = 0; i < m_musicNodeSequence.EventScenesList.Count; ++i)
        {
            MusicEventSceneData eventData =
                m_musicNodeSequence.EventScenesList[i]; //Event候補
            if (eventData == null || !eventData.b_m_enabled)continue;
            if (eventData.m_triggerNodeNumber != _nodeNumber)continue;

            EventNodeRuntimeContext.Begin(eventData);
            m_currentEvent = eventData;
            PlayEventVisual();
            return true;
        }

        return false;
    }

    /// <summary>
    /// 特殊Node完了通知を登録します。
    /// </summary>
    private void OnEnable()
    {
        FindReferences();
        if (m_specialNodePlayer != null)
        {
            m_specialNodePlayer.EventNodesCompleted -= OnEventNodesCompleted;
            m_specialNodePlayer.EventNodesCompleted += OnEventNodesCompleted;
        }

        if (m_preferenceSystem != null)
        {
            m_preferenceSystem.PreferenceEvaluated -= OnPreferenceEvaluated;
            m_preferenceSystem.PreferenceEvaluated += OnPreferenceEvaluated;
        }
    }

    /// <summary>
    /// 特殊Node完了通知を解除します。
    /// </summary>
    private void OnDisable()
    {
        if (m_specialNodePlayer != null)
        {
            m_specialNodePlayer.EventNodesCompleted -= OnEventNodesCompleted;
        }
        if (m_preferenceSystem != null)
        {
            m_preferenceSystem.PreferenceEvaluated -= OnPreferenceEvaluated;
        }
    }

    /// <summary>
    /// Event Scene単体でも確認できるよう必要に応じて自動再生します。
    /// </summary>
    private void Start()
    {
        FindReferences();
        if (b_m_playOnStart)
        {
            PlayEventVisual();
        }
    }

    /// <summary>
    /// 特殊Node成功後に呼び出すEvent演出開始関数です。
    /// </summary>
    [ContextMenu("Play Event Visual")]
    public void PlayEventVisual()
    {
        FindReferences();
        if (b_m_isPlaying)return;

        b_m_isPlaying = true;
        if (m_cameraDirector != null && m_cameraSequence != null)
        {
            m_cameraDirector.PlaySequence(m_cameraSequence);
        }

        bool b_specialNodeBranch = m_currentEvent != null
            && m_currentEvent.m_eventType
                == EMusicEventType.SpecialNodeBranch;
        if (m_playCoroutine != null)
        {
            StopCoroutine(m_playCoroutine);
        }

        if (b_specialNodeBranch)
        {
            if (m_specialNodePlayer != null)
            {
                m_specialNodePlayer.PlayEventNodes();
            }
        }
        else
        {
            if (m_preferenceSystem != null)
            {
                m_preferenceSystem.InitializePreferences();
            }

            SuspendNormalFlow();
            m_playCoroutine = StartCoroutine(ShowCanvasRoutine());
        }

        m_onEventVisualStarted?.Invoke();
    }

    /// <summary>
    /// Event終了時にCameraとCanvasを通常状態へ戻します。
    /// </summary>
    [ContextMenu("Stop Event Visual")]
    public void StopEventVisual()
    {
        if (m_playCoroutine != null)
        {
            StopCoroutine(m_playCoroutine);
            m_playCoroutine = null;
        }

        if (m_cameraDirector != null)
        {
            m_cameraDirector.StopSequence();
        }

        if (m_canvasController != null)
        {
            m_canvasController.ClearNodes();
        }

        if (m_specialNodePlayer != null)
        {
            m_specialNodePlayer.StopEventNodes();
        }

        b_m_isPlaying = false;
        ResumeNormalFlow();
        EventNodeRuntimeContext.Clear();
        m_currentEvent = null;
        m_onEventVisualStopped?.Invoke();
    }

    /// <summary>
    /// 観客生成とCamera切替を待ってからCanvas Nodeを表示します。
    /// </summary>
    private IEnumerator ShowCanvasRoutine()
    {
        float delaySeconds = Mathf.Max(0.0f, m_canvasDelaySeconds); //安全な待機時間
        if (delaySeconds > 0.0f)
        {
            yield return new WaitForSeconds(delaySeconds);
        }
        else
        {
            yield return null;
        }

        if (m_canvasController != null)
        {
            m_canvasController.ShowNodes();
        }

        m_playCoroutine = null;
    }

    /// <summary>
    /// 同じSceneにあるCamera制御とCanvas制御を自動取得します。
    /// </summary>
    private void FindReferences()
    {
        if (m_cameraDirector == null)
        {
            m_cameraDirector = FindFirstObjectByType<PoseCameraDirector>();
        }

        if (m_canvasController == null)
        {
            m_canvasController =
                FindFirstObjectByType<EventAudienceCanvasController>();
        }

        if (m_specialNodePlayer == null)
        {
            m_specialNodePlayer = FindFirstObjectByType<EventSpecialNodePlayer>();
        }

        if (m_preferenceSystem == null)
        {
            m_preferenceSystem =
                FindFirstObjectByType<AudiencePreferenceSystem>();
        }

        if (m_inGameManager == null)
        {
            m_inGameManager = FindFirstObjectByType<InGameManager>();
        }
    }

    /// <summary>
    /// 特殊Node終了に合わせてCameraと観客Canvasを終了します。
    /// </summary>
    private void OnEventNodesCompleted()
    {
        if (m_cameraDirector != null)
        {
            m_cameraDirector.StopSequence();
        }

        if (m_canvasController != null)
        {
            m_canvasController.ClearNodes();
        }

        b_m_isPlaying = false;
        EventNodeRuntimeContext.Clear();
        m_currentEvent = null;
        m_onEventVisualStopped?.Invoke();
    }

    private void OnPreferenceEvaluated(
        int _preferenceindex,
        float _averagepreference)
    {
        if (!b_m_isPlaying)return;
        if (m_currentEvent != null
            && m_currentEvent.m_eventType
                == EMusicEventType.SpecialNodeBranch)return;

        int minimumScore = m_currentEvent != null
            ? m_currentEvent.m_minimumBonusScore
            : 100;
        int maximumScore = m_currentEvent != null
            ? m_currentEvent.m_maximumBonusScore
            : 1000;
        int bonusScore = Mathf.RoundToInt(
            Mathf.Lerp(
                minimumScore,
                maximumScore,
                Mathf.Clamp01(_averagepreference)));
        if (m_inGameManager != null)
        {
            m_inGameManager.AddEventScore(bonusScore);
        }

        Debug.Log(
            $"Audience Choice {_preferenceindex + 1}: "
            + $"Preference {_averagepreference:P1}, "
            + $"Bonus {bonusScore}");
        StopEventVisual();
    }

    private void SuspendNormalFlow()
    {
        if (m_inGameManager == null || b_m_normalFlowSuspended)return;

        b_m_wasInGameEnabled = m_inGameManager.enabled;
        m_inGameManager.enabled = false;
        b_m_normalFlowSuspended = true;
    }

    private void ResumeNormalFlow()
    {
        if (m_inGameManager == null || !b_m_normalFlowSuspended)return;

        m_inGameManager.enabled = b_m_wasInGameEnabled;
        b_m_normalFlowSuspended = false;
    }
}
