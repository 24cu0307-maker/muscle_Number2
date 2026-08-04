/*━━━━━━━━━*
*@file VoltageAutoJudgeTester.cs*
*@brief 設定した成功率で成功失敗を自動生成するDebug機能*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks VenueVoltageSystemと観客演出の動作確認専用*
*━━━━━━━━━*/

using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 指定成功率に基づいてVoltageへ成功または失敗を自動登録します。
/// </summary>
[RequireComponent(typeof(VenueVoltageSystem))]
public sealed class VoltageAutoJudgeTester : MonoBehaviour
{
    private const float EMinimumSuccessRate = 0.0f; //成功率最小値
    private const float EMaximumSuccessRate = 100.0f; //成功率最大値
    private const float EMinimumIntervalSeconds = 0.1f; //最短判定間隔
    private const int EMinimumScoreGain = 0; //成功時スコア最小値

    [Header("Auto Judge")]
    [SerializeField] private bool b_m_enableAutoJudge; //自動判定を有効にするか
    [Range(EMinimumSuccessRate, EMaximumSuccessRate)]
    [SerializeField] private float m_successRatePercent = 70.0f; //成功率
    [Min(EMinimumIntervalSeconds)]
    [SerializeField] private float m_judgeIntervalSeconds = 2.0f; //判定間隔
    [SerializeField] private Vector2Int m_successScoreRange =
        new Vector2Int(1000, 5000); //成功時の仮獲得スコア範囲
    [SerializeField] private bool b_m_useUnscaledTime = true; //TimeScaleの影響を受けないか
    [SerializeField] private bool b_m_logResults; //判定結果をConsoleへ表示するか

    [Header("Runtime Results")]
    [SerializeField] private int m_totalCount; //総判定回数
    [SerializeField] private int m_successCount; //成功回数
    [SerializeField] private int m_failureCount; //失敗回数

    private VenueVoltageSystem m_voltageSystem; //判定通知先
    private float m_nextJudgeTime; //次の自動判定時刻

    /// <summary>
    /// Voltage参照と最初の判定時刻を準備します。
    /// </summary>
    private void Awake()
    {
        m_voltageSystem = GetComponent<VenueVoltageSystem>();
        ScheduleNextJudge();
    }

    /// <summary>
    /// 設定間隔ごとに確率判定を実行します。
    /// </summary>
    private void Update()
    {
        if (!b_m_enableAutoJudge)return;

        float currentTime = GetCurrentTime(); //現在の判定用時刻
        if (currentTime < m_nextJudgeTime)return;

        RunSingleJudge();
        ScheduleNextJudge();
    }

    /// <summary>
    /// Inspector設定値を安全な範囲へ補正します。
    /// </summary>
    private void OnValidate()
    {
        m_successRatePercent = Mathf.Clamp(
            m_successRatePercent,
            EMinimumSuccessRate,
            EMaximumSuccessRate);
        m_judgeIntervalSeconds = Mathf.Max(
            EMinimumIntervalSeconds,
            m_judgeIntervalSeconds);
        m_successScoreRange.x = Mathf.Max(
            EMinimumScoreGain,
            m_successScoreRange.x);
        m_successScoreRange.y = Mathf.Max(
            m_successScoreRange.x,
            m_successScoreRange.y);
    }

    /// <summary>
    /// 現在の成功率で一回だけ自動判定します。
    /// </summary>
    [ContextMenu("Run Single Judge")]
    public void RunSingleJudge()
    {
        if (m_voltageSystem == null)
        {
            m_voltageSystem = GetComponent<VenueVoltageSystem>();
        }

        float randomPercent = Random.Range(
            EMinimumSuccessRate,
            EMaximumSuccessRate); //今回の抽選値
        bool b_success = randomPercent < m_successRatePercent; //成功判定
        ++m_totalCount;
        if (b_success)
        {
            int scoreGain = Random.Range(
                m_successScoreRange.x,
                m_successScoreRange.y + 1); //今回の仮獲得スコア
            ++m_successCount;
            m_voltageSystem.RegisterSuccess(scoreGain);
        }
        else
        {
            ++m_failureCount;
            m_voltageSystem.RegisterFailure();
        }

        if (b_m_logResults)
        {
            Debug.Log(
                $"Auto Judge: {(b_success ? "Success" : "Failure")}"
                + $" / Success Rate {m_successRatePercent:F1}%"
                + $" / Total {m_totalCount}",
                this);
        }
    }

    /// <summary>
    /// 必ず成功を一回発生させます。
    /// </summary>
    [ContextMenu("Run Success")]
    public void RunSuccess()
    {
        if (m_voltageSystem == null)
        {
            m_voltageSystem = GetComponent<VenueVoltageSystem>();
        }

        int scoreGain = Random.Range(
            m_successScoreRange.x,
            m_successScoreRange.y + 1); //今回の仮獲得スコア
        ++m_totalCount;
        ++m_successCount;
        m_voltageSystem.RegisterSuccess(scoreGain);
    }

    /// <summary>
    /// 必ず失敗を一回発生させます。
    /// </summary>
    [ContextMenu("Run Failure")]
    public void RunFailure()
    {
        if (m_voltageSystem == null)
        {
            m_voltageSystem = GetComponent<VenueVoltageSystem>();
        }

        ++m_totalCount;
        ++m_failureCount;
        m_voltageSystem.RegisterFailure();
    }

    /// <summary>
    /// Debug集計値を初期化します。
    /// </summary>
    [ContextMenu("Reset Results")]
    public void ResetResults()
    {
        m_totalCount = 0;
        m_successCount = 0;
        m_failureCount = 0;
    }

    /// <summary>
    /// 次回判定時刻を現在の設定から計算します。
    /// </summary>
    private void ScheduleNextJudge()
    {
        m_nextJudgeTime =
            GetCurrentTime()
            + Mathf.Max(
                EMinimumIntervalSeconds,
                m_judgeIntervalSeconds);
    }

    /// <summary>
    /// TimeScale設定に対応した現在時刻を返します。
    /// </summary>
    private float GetCurrentTime()
    {
        return b_m_useUnscaledTime
            ? Time.unscaledTime
            : Time.time;
    }
}
