/*━━━━━━━━━*
*@file EventSceneVisualDirector.cs*
*@brief 外部の特殊Nodeから既存CameraSequenceとCanvasを一括操作する*
*@author 24cu0312 久場洸太*
*@date 2026/08/03*
*最終更新日 2026/08/03*
*@remarks 特殊Node実装へ依存しない公開関数を接続口として提供する*
*━━━━━━━━━*/

using System.Collections;
using GameFlowTemplate;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
    [SerializeField] private AudiencePreferenceSystem m_preferenceSystem; //候補Poseごとの観客の好みと得点倍率を算出するSystem
    [SerializeField] private AudienceAreaSpawner m_audienceSpawner; //成功・失敗時の観客Animationと歓声音声を制御する生成元
    [FormerlySerializedAs("m_inGameManager")]
    [SerializeField] private GameManager m_gameManager; //ScoreManager等、ゲーム全体のManagerへ到達するための窓口
    [SerializeField] private PoseFlowDataManager m_poseFlowDataManager; //通常Nodeの進行をEvent中だけ停止・再開する対象
    [SerializeField] private UIController m_uiController; //通常Node UIをEvent中だけ停止・再開する対象
    [SerializeField] private PoseJudgeController m_poseJudgeController; //現在のプレイヤー姿勢が候補Poseと一致したか判定するController
    [SerializeField] private VenueVoltageSystem m_venueVoltageSystem; //成功時のボルテージ加算と会場演出通知を担当するSystem
    [SerializeField] private EffectSystem m_effectSystem; //選択成功時の派手なクリアEffectを名前で再生するSystem
    [SerializeField] private AudioSource m_choiceAudioSource; //候補決定音とクリア音を再生する専用AudioSource
    [SerializeField] private AudioClip m_choiceSound; //観客の好みが確定した瞬間に一度だけ鳴らす通知音
    [Header("Audience Choice Clear")]
    [SerializeField] private AudioClip m_clearSound; //候補Poseの成立時に再生する成功音
    [SerializeField] private float m_clearReactionIntervalSeconds = 0.2f; //成功後からEvent終了まで観客Animationを更新する間隔
    [SerializeField] private float m_clearFrameAnimationSeconds = 1.0f; //黄色い成功枠が拡大して消えるまでの時間
    [SerializeField] private float m_clearFrameScaleMultiplier = 1.65f; //成功枠Animation終了時の基準Scaleに対する倍率
    [SerializeField] private Color m_clearFrameColor =
        new Color(1.0f, 0.82f, 0.05f, 1.0f);
    [Header("Audience Choice Debug")]
    [SerializeField] private bool b_m_enablePoseDiagnostics; //候補Pose判定の詳細Logを一定間隔で出力するか
    [SerializeField] private float m_poseDiagnosticIntervalSeconds = 0.5f; //診断Logを連続出力し過ぎないための待機時間
    [SerializeField] private float m_canvasDelaySeconds = 0.2f; //Canvas表示待機時間
    [SerializeField] private bool b_m_playOnStart = true; //Event Scene単体確認用
    [SerializeField] private UnityEvent m_onEventVisualStarted; //演出開始通知
    [SerializeField] private UnityEvent m_onEventVisualStopped; //演出終了通知

    private Coroutine m_playCoroutine; //表示待機処理
    private bool b_m_isPlaying; //Event演出中か
    private bool b_m_normalFlowSuspended; //通常Node生成と判定を停止済みであることを示す復帰管理フラグ
    private bool b_m_audienceSelectionEnabled; //Decision時刻へ到達し、プレイヤーPoseを受け付けられる状態か
    private float m_manualClockOffset; //BGM未設定時にEvent相対時刻へ変換するための開始時刻
    private MusicEventSceneData m_currentEvent; //現在再生中のEvent種類・Trigger時刻・終了時刻を保持する設定

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

            PrepareManualClock();
            SuspendNormalFlow();
            b_m_audienceSelectionEnabled = false;
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
        m_audienceSpawner?.StopSequentialSuccessVoices();
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

        b_m_audienceSelectionEnabled = false;
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
            m_canvasController.SetPoseDetectionEnabled(false);
        }

        float decisionTime = m_currentEvent != null
            ? m_currentEvent.GetAudienceChoiceDecisionTime()
            : 0.0f;
        float endTime = m_currentEvent != null
            ? Mathf.Max(decisionTime, m_currentEvent.GetAudienceChoiceEndTime())
            : decisionTime;
        float nextPoseDiagnosticTime = Time.unscaledTime;
        while (GetPlaybackClock() < decisionTime)
        {
            UpdateCandidatePoseDiagnostics(ref nextPoseDiagnosticTime);
            yield return null;
        }

        b_m_audienceSelectionEnabled = true;
        m_canvasController?.SetPoseDetectionEnabled(true);
        Debug.Log(
            $"Audience Choice pose detection enabled: perform one of the "
            + $"displayed poses before End {endTime:F2}s.");
        while (b_m_isPlaying && GetPlaybackClock() < endTime)
        {
            if (TryCompleteDetectedCandidatePose())yield break;
            UpdateCandidatePoseDiagnostics(ref nextPoseDiagnosticTime);
            yield return null;
        }

        if (!b_m_isPlaying)yield break;

        Debug.Log("Audience Choice failed: no pose was selected before the end time.");
        b_m_audienceSelectionEnabled = false;
        m_playCoroutine = null;
        StopEventVisual();
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
        if (m_audienceSpawner == null)
        {
            m_audienceSpawner = FindFirstObjectByType<AudienceAreaSpawner>();
        }

        if (m_gameManager == null)
        {
            m_gameManager = FindFirstObjectByType<GameManager>();
        }
        if (m_poseFlowDataManager == null)
        {
            m_poseFlowDataManager =
                FindFirstObjectByType<PoseFlowDataManager>();
        }
        if (m_uiController == null)
        {
            m_uiController = FindFirstObjectByType<UIController>();
        }
        if (m_poseJudgeController == null)
        {
            m_poseJudgeController = FindFirstObjectByType<PoseJudgeController>();
        }
        if (m_venueVoltageSystem == null)
        {
            m_venueVoltageSystem = FindFirstObjectByType<VenueVoltageSystem>();
        }
        if (m_effectSystem == null)
        {
            m_effectSystem = FindFirstObjectByType<EffectSystem>();
        }
        if (m_choiceAudioSource == null)
        {
            m_choiceAudioSource = GetComponent<AudioSource>();
            if (m_choiceAudioSource == null)
            {
                m_choiceAudioSource = gameObject.AddComponent<AudioSource>();
            }

            m_choiceAudioSource.playOnAwake = false;
            m_choiceAudioSource.spatialBlend = 0.0f;
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

    /// <summary>
    /// 観客の好みの集計完了を受け取り、好みの強さから獲得予定Scoreを算出します。
    /// この時点ではまだ成功扱いにせず、選ばれた候補Poseの実演受付へ遷移します。
    /// </summary>
    private void OnPreferenceEvaluated(
        int _preferenceindex,
        float _averagepreference)
    {
        if (!b_m_isPlaying || !b_m_audienceSelectionEnabled)return;
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
        int selectedPoseId = GetAudienceChoicePoseId(_preferenceindex);

        Debug.Log(
            $"Audience Choice {_preferenceindex + 1}: "
            + $"Preference {_averagepreference:P1}, "
            + $"Pose {selectedPoseId}, "
            + $"Pending Bonus {bonusScore}");
        if (m_playCoroutine != null)
        {
            StopCoroutine(m_playCoroutine);
        }

        m_playCoroutine = StartCoroutine(
            ShowSelectedPoseNodeRoutine(
                _preferenceindex,
                _averagepreference,
                bonusScore,
                false));
    }

    /// <summary>
    /// 選択されたPose Nodeを終了時刻まで表示し、実際のPose成立または時間切れを待ちます。
    /// 成功時はScore・ボルテージ・Effect・観客反応を一度だけ確定し、終了時刻まで成功演出を継続します。
    /// </summary>
    /// <param name="_preferenceindex">三つの候補内で選ばれた番号です。</param>
    /// <param name="_preference">音量や得点演出へ使用する0～1の好みの強さです。</param>
    /// <param name="_bonusScore">Pose成立時に加算する確定Scoreです。</param>
    /// <param name="_forceclear">Debug確認時にPose判定を省略して成功させるかを示します。</param>
    private IEnumerator ShowSelectedPoseNodeRoutine(
        int _preferenceindex,
        float _preference,
        int _bonusScore,
        bool _forceclear)
    {
        m_canvasController?.ClearNodes();
        if (m_currentEvent == null
            || !m_currentEvent.TryGetAudienceChoiceCandidate(
                _preferenceindex,
                out SMusicNodeEvent candidate))
        {
            m_playCoroutine = null;
            StopEventVisual();
            yield break;
        }

        float eventEndTime = Mathf.Max(
            GetPlaybackClock(),
            m_currentEvent.GetAudienceChoiceEndTime());
        float displaySeconds = Mathf.Max(
            0.1f,
            eventEndTime - GetPlaybackClock());
        CSVDataPoseFlow selectedPose = new CSVDataPoseFlow
        {
            FlowNumber = candidate.m_nodeNumber,
            PoseID = candidate.m_poseId,
            PoseName = candidate.m_eventName,
            time = displaySeconds,
            SuccessEffectNames = candidate.m_successEffectNames,
            FailureEffectNames = candidate.m_failureEffectNames
        };
        m_uiController?.UISet_normal(selectedPose);

        bool b_poseSucceeded = _forceclear;
        float nextDiagnosticTime = Time.unscaledTime;
        while (!b_poseSucceeded && GetPlaybackClock() < eventEndTime)
        {
            m_poseJudgeController?.PoseJudge(candidate.m_poseId);
            if (b_m_enablePoseDiagnostics
                && m_poseJudgeController != null
                && Time.unscaledTime >= nextDiagnosticTime)
            {
                m_poseJudgeController.LogPoseDiagnostics(candidate.m_poseId);
                nextDiagnosticTime = Time.unscaledTime
                    + Mathf.Max(0.1f, m_poseDiagnosticIntervalSeconds);
            }
            if (IsSelectedPoseSuccessful(selectedPose))
            {
                b_poseSucceeded = true;
                break;
            }

            yield return null;
        }

        if (b_poseSucceeded)
        {
            m_uiController?.UIJudgeEnd_normal(selectedPose);
            GameObject clearFrame = m_uiController != null
                ? m_uiController.GetCurrentSuccessFrame(selectedPose)
                : null;
            Coroutine clearAnimation = clearFrame != null
                ? StartCoroutine(AnimateClearFrameRoutine(clearFrame))
                : null;
            m_poseFlowDataManager?.QueueNextPose(candidate.m_poseId);
            m_gameManager?.AddScore(_bonusScore);
            m_audienceSpawner?.StartSequentialSuccessVoices(_preference);
            m_venueVoltageSystem?.RegisterSuccess(_bonusScore);
            m_effectSystem?.PlayMusicNodeEffects(
                candidate.m_successEffectNames);

            PlayClearSound(_preference);
            Debug.Log(
                $"Audience Choice CLEAR: {candidate.m_eventName}, "
                + $"Pose {candidate.m_poseId}, Bonus {_bonusScore}");

            float reactionInterval = Mathf.Max(
                0.1f,
                m_clearReactionIntervalSeconds);
            float nextReactionTime = Time.unscaledTime + reactionInterval;
            while (GetPlaybackClock() < eventEndTime)
            {
                if (Time.unscaledTime >= nextReactionTime)
                {
                    float voltage = m_venueVoltageSystem != null
                        ? m_venueVoltageSystem.NormalizedVoltage
                        : 0.5f;
                    m_audienceSpawner?.PlaySuccessReactionVisual(voltage);
                    nextReactionTime = Time.unscaledTime + reactionInterval;
                }

                yield return null;
            }

            if (clearAnimation != null)
            {
                StopCoroutine(clearAnimation);
            }
            m_audienceSpawner?.StopSequentialSuccessVoices();
        }
        else
        {
            m_effectSystem?.PlayMusicNodeEffects(
                candidate.m_failureEffectNames);
            m_venueVoltageSystem?.RegisterFailure();
            Debug.Log(
                $"Audience Choice FAILED: Pose {candidate.m_poseId} "
                + "was not completed before End.");
        }

        m_uiController?.UIForcedQuit(selectedPose);
        m_playCoroutine = null;
        StopEventVisual();
    }

    /// <summary>
    /// 表示中の全候補Poseを順に判定し、最初に成立した候補の好み評価を開始します。
    /// 一Frame内で複数候補が成立してもEventを二重開始しないよう、成立時点で受付を閉じます。
    /// </summary>
    /// <returns>いずれかの候補Poseを検出して次の処理へ進んだ場合はtrueです。</returns>
    private bool TryCompleteDetectedCandidatePose()
    {
        if (!b_m_audienceSelectionEnabled
            || m_currentEvent == null
            || m_poseJudgeController == null
            || m_preferenceSystem == null)return false;

        int candidateIndex = 0;
        while (m_currentEvent.TryGetAudienceChoiceCandidate(
            candidateIndex,
            out SMusicNodeEvent candidate))
        {
            m_poseJudgeController.PoseJudge(candidate.m_poseId);
            if (m_poseJudgeController.GetisPose(candidate.m_poseId))
            {
                m_canvasController?.SetPoseDetectionEnabled(false);
                Debug.Log(
                    $"Audience Choice pose detected: candidate "
                    + $"{candidateIndex + 1}, Pose {candidate.m_poseId} "
                    + $"({candidate.m_eventName}).");
                m_preferenceSystem.EvaluatePreference(candidateIndex);
                b_m_audienceSelectionEnabled = false;
                return true;
            }

            ++candidateIndex;
        }

        return false;
    }

    /// <summary>
    /// Debug設定が有効な場合だけ、各候補Poseの関節判定結果を一定間隔で出力します。
    /// 毎Frame大量のLogが流れないよう、次回出力可能時刻を参照渡しで更新します。
    /// </summary>
    private void UpdateCandidatePoseDiagnostics(ref float _nextdiagnostictime)
    {
        if (!b_m_enablePoseDiagnostics
            || m_poseJudgeController == null
            || m_currentEvent == null
            || Time.unscaledTime < _nextdiagnostictime)return;

        int candidateIndex = 0;
        while (m_currentEvent.TryGetAudienceChoiceCandidate(
            candidateIndex,
            out SMusicNodeEvent candidate))
        {
            m_poseJudgeController.PoseJudge(candidate.m_poseId);
            m_poseJudgeController.LogPoseDiagnostics(candidate.m_poseId);
            ++candidateIndex;
        }

        _nextdiagnostictime = Time.unscaledTime
            + Mathf.Max(0.1f, m_poseDiagnosticIntervalSeconds);
    }

    /// <summary>
    /// 成功した候補枠を黄色に変更し、脈動しながら拡大・透明化して消去します。
    /// ゲーム停止中の演出確認にも対応するため、unscaledDeltaTimeで進行します。
    /// </summary>
    private IEnumerator AnimateClearFrameRoutine(GameObject _clearframe)
    {
        if (_clearframe == null)yield break;

        _clearframe.SetActive(true);
        Transform frameTransform = _clearframe.transform;
        Vector3 startScale = frameTransform.localScale;
        CanvasGroup canvasGroup = _clearframe.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = _clearframe.AddComponent<CanvasGroup>();
        }

        Graphic[] graphics = _clearframe.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; ++i)
        {
            Color color = m_clearFrameColor;
            color.a = graphics[i].color.a;
            graphics[i].color = color;
        }

        Graphic outlineTarget = _clearframe.GetComponent<Graphic>();
        if (outlineTarget != null)
        {
            Outline outline = _clearframe.GetComponent<Outline>();
            if (outline == null)
            {
                outline = _clearframe.AddComponent<Outline>();
            }
            outline.effectColor = m_clearFrameColor;
            outline.effectDistance = new Vector2(5.0f, -5.0f);
        }

        float duration = Mathf.Max(0.1f, m_clearFrameAnimationSeconds);
        float elapsed = 0.0f;
        while (elapsed < duration && _clearframe != null)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / duration);
            float easedTime = 1.0f
                - Mathf.Pow(1.0f - normalizedTime, 3.0f);
            float pulse = 1.0f
                + Mathf.Sin(normalizedTime * Mathf.PI * 6.0f)
                * 0.05f
                * (1.0f - normalizedTime);
            frameTransform.localScale = Vector3.Lerp(
                startScale,
                startScale * Mathf.Max(1.0f, m_clearFrameScaleMultiplier),
                easedTime) * pulse;
            canvasGroup.alpha = 1.0f
                - Mathf.SmoothStep(0.0f, 1.0f, normalizedTime);
            yield return null;
        }

        if (_clearframe != null)
        {
            canvasGroup.alpha = 0.0f;
            _clearframe.SetActive(false);
        }
    }

    /// <summary>
    /// PoseJudgeControllerが保持する最新結果から、選択Poseが成立しているかを取得します。
    /// </summary>
    private bool IsSelectedPoseSuccessful(CSVDataPoseFlow _pose)
    {
        return EffectDebugKeySettings.ForceAllSuccess
            || m_poseJudgeController != null
            && m_poseJudgeController.GetisPose(_pose.PoseID);
    }

    /// <summary>
    /// 観客の好みが強いほど音程と音量を上げ、候補決定の手応えを音で伝えます。
    /// Clip未設定時は確認用の短い電子音を実行時に生成します。
    /// </summary>
    private void PlayPreferenceSound(float _preference)
    {
        if (m_choiceAudioSource == null)return;

        float strength = Mathf.Clamp01(_preference);
        AudioClip clip = m_choiceSound != null
            ? m_choiceSound
            : CreatePreferenceTone(strength);
        m_choiceAudioSource.pitch = Mathf.Lerp(0.9f, 1.15f, strength);
        m_choiceAudioSource.PlayOneShot(
            clip,
            Mathf.Lerp(0.25f, 1.0f, strength));
    }

    /// <summary>
    /// Pose成功音を好みの強さに合わせた音程・音量で再生します。
    /// 専用Clipがない場合は候補決定音へ安全にFallbackします。
    /// </summary>
    private void PlayClearSound(float _preference)
    {
        if (m_choiceAudioSource != null && m_clearSound != null)
        {
            float strength = Mathf.Clamp01(_preference);
            m_choiceAudioSource.pitch = Mathf.Lerp(0.95f, 1.08f, strength);
            m_choiceAudioSource.PlayOneShot(
                m_clearSound,
                Mathf.Lerp(0.65f, 1.0f, strength));
            return;
        }

        PlayPreferenceSound(_preference);
    }

    /// <summary>
    /// 音源未設定でも動作確認できるよう、好みの強さを周波数へ変換した減衰音を生成します。
    /// </summary>
    private static AudioClip CreatePreferenceTone(float _strength)
    {
        const int sampleRate = 44100;
        const float duration = 0.32f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float frequency = Mathf.Lerp(440.0f, 880.0f, _strength);
        for (int i = 0; i < sampleCount; ++i)
        {
            float time = (float)i / sampleRate;
            float envelope = 1.0f - time / duration;
            samples[i] = Mathf.Sin(2.0f * Mathf.PI * frequency * time)
                * envelope * 0.35f;
        }

        AudioClip clip = AudioClip.Create(
            "AudienceChoiceTone",
            sampleCount,
            1,
            sampleRate,
            false);
        clip.SetData(samples, 0);
        return clip;
    }

    /// <summary>
    /// BGM再生中はBGM位置をEventの基準時計として返します。
    /// BGMがない場合は開始時に保存したOffsetから同じ形式の経過秒を算出します。
    /// </summary>
    private float GetPlaybackClock()
    {
        VoltageBgmSystem bgmSystem = m_gameManager?.GetVoltageBgmSystem();
        return bgmSystem != null && bgmSystem.IsPlaybackReady
            ? bgmSystem.CurrentTimeSeconds
            : Mathf.Max(0.0f, Time.unscaledTime - m_manualClockOffset);
    }

    /// <summary>
    /// BGM未設定時もNode時刻を維持できるよう、現在のゲーム時刻と実時間の差を保存します。
    /// </summary>
    private void PrepareManualClock()
    {
        TimeManager timeManager = m_gameManager?.GetTimeManager();
        float gameTime = 0.0f;
        if (timeManager != null)
        {
            gameTime = timeManager.GameTimeSeconds;
        }
        m_manualClockOffset = Time.unscaledTime - gameTime;
    }

    /// <summary>
    /// 候補番号に対応するPoseIDをEvent設定から取得します。
    /// 設定不足時もDebug処理を止めないよう、候補番号を安全な代替値として返します。
    /// </summary>
    private int GetAudienceChoicePoseId(int _preferenceindex)
    {
        if (m_currentEvent == null
            || !m_currentEvent.TryGetAudienceChoiceCandidate(
                _preferenceindex,
                out SMusicNodeEvent candidate))
        {
            return Mathf.Max(0, _preferenceindex);
        }

        return Mathf.Max(0, candidate.m_poseId);
    }

    /// <summary>
    /// Event用Nodeと通常Nodeが同時進行しないよう、GameManager経由で通常進行を一度だけ停止します。
    /// </summary>
    private void SuspendNormalFlow()
    {
        if (m_gameManager == null || b_m_normalFlowSuspended)return;

        m_gameManager.PauseForDirection();
        b_m_normalFlowSuspended = true;
    }

    /// <summary>
    /// Event終了時にGameManager配下の通常進行を再開し、重複Resumeを防ぐフラグも戻します。
    /// </summary>
    private void ResumeNormalFlow()
    {
        if (m_gameManager == null || !b_m_normalFlowSuspended)return;

        m_gameManager.ResumeFromDirection();
        b_m_normalFlowSuspended = false;
    }
}
