/*━━━━━━━━━*
*@file ConditionalEffectManager.cs*
*@brief 登録値と条件式から複数Effectの発火を一元管理する*
*@author 24cu0312 久場洸太*
*@date 2026/08/30*
*最終更新日 2026/08/30*
*@remarks MusicNodeSequenceの条件付き演出を実行する*
*━━━━━━━━━*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using GameFlowTemplate;
using UnityEngine;

/// <summary>
/// time、score、voltageと任意登録値を条件式から参照し、Effectを発火します。
/// </summary>
public sealed class ConditionalEffectManager : MonoBehaviour
{
    private const string ETimeValueName = "time";
    private const string EScoreValueName = "score";
    private const string EVoltageValueName = "voltage";
    private const float EMinimumRepeatIntervalSeconds = 0.0f;

    [SerializeField] private MusicNodeSequence m_sequence; //条件付き演出設定
    [SerializeField] private EffectSystem m_effectSystem; //演出再生先
    [SerializeField] private VoltageBgmSystem m_bgmSystem; //BGM基準時刻
    [SerializeField]
    [Tooltip("BGMが未再生または存在しない場合、シーン開始からの経過時間で演出条件を判定します。")]
    private bool b_m_useSceneTimeWithoutBgm = true; //BGMなしでの確認用
    [SerializeField] private ScoreManager m_scoreManager; //現在Score
    [SerializeField] private VenueVoltageSystem m_voltageSystem; //現在Voltage

    private readonly Dictionary<string, float> m_values =
        new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase); //条件値一覧
    private readonly HashSet<ConditionalEffectEvent> m_triggeredEvents =
        new HashSet<ConditionalEffectEvent>(); //一度だけ発火済みの設定
    private readonly Dictionary<ConditionalEffectEvent, float> m_nextTriggerTimes =
        new Dictionary<ConditionalEffectEvent, float>(); //繰り返し可能時刻
    private readonly HashSet<string> m_reportedErrors =
        new HashSet<string>(); //同じ式のError連続出力防止

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        UpdateBuiltInValues();
        EvaluateEvents();
    }

    /// <summary>条件に利用する任意の数値を登録または更新します。</summary>
    public void SetValue(string _name, float _value)
    {
        if (string.IsNullOrWhiteSpace(_name))return;
        m_values[_name.Trim()] = _value;
    }

    /// <summary>条件値を登録します。既に同名がある場合は新しい値へ更新します。</summary>
    public void RegisterValue(string _name, float _value)
    {
        SetValue(_name, _value);
    }

    /// <summary>登録済みの条件値を取得します。</summary>
    public bool TryGetValue(string _name, out float _value)
    {
        if (string.IsNullOrWhiteSpace(_name))
        {
            _value = 0.0f;
            return false;
        }
        return m_values.TryGetValue(_name.Trim(), out _value);
    }

    /// <summary>登録済み条件値を返し、未登録の場合は指定した代替値を返します。</summary>
    public float GetValue(string _name, float _fallbackValue = 0.0f)
    {
        return TryGetValue(_name, out float value) ? value : _fallbackValue;
    }

    /// <summary>指定名の条件値を加算します。未登録の場合は0から開始します。</summary>
    public void AddValue(string _name, float _amount)
    {
        TryGetValue(_name, out float currentValue);
        SetValue(_name, currentValue + _amount);
    }

    /// <summary>一度だけ発火した状態と繰り返し待機時間を初期化します。</summary>
    public void ResetTriggers()
    {
        m_triggeredEvents.Clear();
        m_nextTriggerTimes.Clear();
        m_reportedErrors.Clear();
    }

    private void ResolveReferences()
    {
        if (m_effectSystem == null)
        {
            m_effectSystem = FindFirstObjectByType<EffectSystem>();
        }
        if (m_bgmSystem == null)
        {
            m_bgmSystem = FindFirstObjectByType<VoltageBgmSystem>();
        }
        if (m_scoreManager == null)
        {
            m_scoreManager = FindFirstObjectByType<ScoreManager>();
        }
        if (m_voltageSystem == null)
        {
            m_voltageSystem = FindFirstObjectByType<VenueVoltageSystem>();
        }
    }

    private void UpdateBuiltInValues()
    {
        float timelineTime = 0.0f;
        if (m_bgmSystem != null && m_bgmSystem.IsPlaying)
        {
            timelineTime = m_bgmSystem.CurrentTimeSeconds;
        }
        else if (b_m_useSceneTimeWithoutBgm)
        {
            timelineTime = Time.timeSinceLevelLoad;
        }

        SetValue(
            ETimeValueName,
            timelineTime);
        SetValue(
            EScoreValueName,
            m_scoreManager != null ? m_scoreManager.CurrentScore : GameSession.Score);
        SetValue(
            EVoltageValueName,
            m_voltageSystem != null ? m_voltageSystem.Voltage : 0.0f);
    }

    private void EvaluateEvents()
    {
        if (m_sequence == null || m_effectSystem == null)return;

        List<ConditionalEffectEvent> eventsList = m_sequence.ConditionalEffectsList;
        if (eventsList == null)return;
        for (int i = 0; i < eventsList.Count; ++i)
        {
            ConditionalEffectEvent effectEvent = eventsList[i];
            if (effectEvent == null || !effectEvent.b_m_enabled)continue;
            if (effectEvent.b_m_triggerOnce && m_triggeredEvents.Contains(effectEvent))continue;
            if (m_nextTriggerTimes.TryGetValue(effectEvent, out float nextTime)
                && Time.unscaledTime < nextTime)continue;
            if (effectEvent.b_m_useTimelineTime
                && GetValue(ETimeValueName) < effectEvent.m_timelineTime)continue;

            if (!TryEvaluateCondition(
                effectEvent.m_conditionExpression,
                out bool b_conditionMet,
                out string error))
            {
                ReportConditionError(effectEvent, error);
                continue;
            }
            if (!b_conditionMet)continue;

            TriggerEffects(effectEvent);
            m_triggeredEvents.Add(effectEvent);
            m_nextTriggerTimes[effectEvent] = Time.unscaledTime
                + Mathf.Max(
                    EMinimumRepeatIntervalSeconds,
                    effectEvent.m_repeatIntervalSeconds);
        }
    }

    private void TriggerEffects(ConditionalEffectEvent _effectEvent)
    {
        if (_effectEvent.m_effectsList == null)return;
        for (int i = 0; i < _effectEvent.m_effectsList.Count; ++i)
        {
            ConditionalEffectEntry entry = _effectEvent.m_effectsList[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.m_effectName))continue;
            StartCoroutine(PlayEffectEntry(entry));
        }
    }

    private IEnumerator PlayEffectEntry(ConditionalEffectEntry _entry)
    {
        float delay = Mathf.Max(0.0f, _entry.m_delaySeconds);
        if (delay > 0.0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (_entry.b_m_overridePosition)
        {
            m_effectSystem.PlayConditionalEffectAt(
                _entry.m_effectName,
                _entry.m_position);
        }
        else
        {
            m_effectSystem.PlayConditionalEffect(_entry.m_effectName);
        }
    }

    private bool TryEvaluateCondition(
        string _expression,
        out bool _result,
        out string _error)
    {
        ConditionParser parser = new ConditionParser(_expression, m_values);
        return parser.TryEvaluate(out _result, out _error);
    }

    private void ReportConditionError(
        ConditionalEffectEvent _effectEvent,
        string _error)
    {
        string eventName = string.IsNullOrWhiteSpace(_effectEvent.m_eventName)
            ? "Effect Event"
            : _effectEvent.m_eventName;
        string message = $"{eventName}: 条件式を評価できません。{_error}";
        if (m_reportedErrors.Add(message))
        {
            Debug.LogWarning(message, this);
        }
    }

    /// <summary>&amp;を|より優先し、括弧を扱う条件式Parserです。</summary>
    private sealed class ConditionParser
    {
        private readonly string m_expression;
        private readonly IReadOnlyDictionary<string, float> m_values;
        private int m_index;
        private string m_error;

        public ConditionParser(
            string _expression,
            IReadOnlyDictionary<string, float> _values)
        {
            m_expression = _expression ?? string.Empty;
            m_values = _values;
        }

        public bool TryEvaluate(out bool _result, out string _error)
        {
            m_index = 0;
            m_error = string.Empty;
            _result = ParseOr();
            SkipWhitespace();
            if (string.IsNullOrEmpty(m_error) && m_index != m_expression.Length)
            {
                m_error = $"位置 {m_index + 1} 付近の記述を確認してください。";
            }
            _error = m_error;
            return string.IsNullOrEmpty(m_error);
        }

        private bool ParseOr()
        {
            bool result = ParseAnd();
            while (string.IsNullOrEmpty(m_error))
            {
                SkipWhitespace();
                if (!Match('|'))break;
                bool right = ParseAnd();
                result = result || right;
            }
            return result;
        }

        private bool ParseAnd()
        {
            bool result = ParseTerm();
            while (string.IsNullOrEmpty(m_error))
            {
                SkipWhitespace();
                if (!Match('&'))break;
                bool right = ParseTerm();
                result = result && right;
            }
            return result;
        }

        private bool ParseTerm()
        {
            SkipWhitespace();
            if (Match('('))
            {
                bool result = ParseOr();
                SkipWhitespace();
                if (!Match(')'))
                {
                    SetError("閉じ括弧がありません。");
                }
                return result;
            }
            return ParseComparison();
        }

        private bool ParseComparison()
        {
            if (!TryParseValue(out float left))return false;
            SkipWhitespace();
            string comparison = ParseComparisonOperator();
            if (string.IsNullOrEmpty(comparison))
            {
                SetError("比較演算子（>, >=, <, <=, ==, !=）が必要です。");
                return false;
            }
            if (!TryParseValue(out float right))return false;

            switch (comparison)
            {
                case ">": return left > right;
                case ">=": return left >= right;
                case "<": return left < right;
                case "<=": return left <= right;
                case "==": return Mathf.Approximately(left, right);
                case "!=": return !Mathf.Approximately(left, right);
                default: return false;
            }
        }

        private bool TryParseValue(out float _value)
        {
            SkipWhitespace();
            int startIndex = m_index;
            while (m_index < m_expression.Length)
            {
                char character = m_expression[m_index];
                if (char.IsLetterOrDigit(character)
                    || character == '_'
                    || character == '.'
                    || character == '-')
                {
                    ++m_index;
                    continue;
                }
                break;
            }

            string token = m_expression.Substring(startIndex, m_index - startIndex);
            if (float.TryParse(
                token,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out _value))return true;
            if (m_values.TryGetValue(token, out _value))return true;

            SetError(string.IsNullOrEmpty(token)
                ? "値または変数名が必要です。"
                : $"「{token}」は登録されていない条件値です。");
            return false;
        }

        private string ParseComparisonOperator()
        {
            string[] operators = { ">=", "<=", "==", "!=", ">", "<" };
            for (int i = 0; i < operators.Length; ++i)
            {
                if (m_expression.IndexOf(
                    operators[i],
                    m_index,
                    StringComparison.Ordinal) != m_index)continue;
                m_index += operators[i].Length;
                return operators[i];
            }
            return string.Empty;
        }

        private bool Match(char _character)
        {
            if (m_index >= m_expression.Length
                || m_expression[m_index] != _character)return false;
            ++m_index;
            return true;
        }

        private void SkipWhitespace()
        {
            while (m_index < m_expression.Length
                && char.IsWhiteSpace(m_expression[m_index]))
            {
                ++m_index;
            }
        }

        private void SetError(string _message)
        {
            if (!string.IsNullOrEmpty(m_error))return;
            m_error = $"{_message}（位置 {m_index + 1}）";
        }
    }
}
