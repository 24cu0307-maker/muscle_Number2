/*━━━━━━━━━*
*@file AudiencePreferenceSystem.cs*
*@brief 観客へ三種類の好みを割り当てEvent評価へ反映する*
*@author 24cu0312 久場洸太*
*@date 2026/08/02*
*最終更新日 2026/08/02*
*@remarks 大人数向けに好みを管理側で保持する*
*━━━━━━━━━*/

using System.Collections.Generic;
using UnityEngine;

// Audience event preference controller.
/// <summary>
/// 観客ごとの三種類の好み、頭上Node、評価Reactionを管理します。
/// </summary>
public sealed class AudiencePreferenceSystem : MonoBehaviour
{
    public event System.Action<int, float> PreferenceEvaluated;
    private const int EPreferenceCount = 3; //好み種類数
    private const float EMinimumPreferenceSeed = 0.05f; //抽選最低値
    private const float EMinimumReactionRatio = 0.08f; //最低反応率
    private const float EMaximumReactionRatio = 0.95f; //最大反応率
    private const float EMinimumReactionStrength = 0.7f; //最低反応強度
    private const float EMaximumReactionStrength = 3.5f; //最大反応強度

    [SerializeField] private AudienceAreaSpawner m_audienceSpawner; //観客生成元
    [SerializeField] private int m_maximumReactionCount = 1200; //同時Reaction上限

    [Header("Ambient Preference Reactions")]
    [Tooltip("通常時のReaction種類と強さへ、観客ごとの最も強い好みを反映します。")]
    [SerializeField] private bool b_m_useAmbientPreference = true;
    [Tooltip("好み0～1に対応する通常Reaction強度倍率です。")]
    [SerializeField] private Vector2 m_ambientStrengthMultiplierRange =
        new Vector2(0.8f, 1.8f);
    [Tooltip("最も好みの強いNode固有Reactionへ置き換える確率です。")]
    [SerializeField, Range(0.0f, 1.0f)] private float m_preferredReactionChance = 0.75f;

    [Header("Audience Reaction Comments")]
    [SerializeField] private bool b_m_showReactionComments = true;
    [SerializeField, Range(0.0f, 1.0f)] private float m_commentDisplayChance = 0.2f;
    [Tooltip("コメント再生終了後、次回再生を許可するまでの秒数Min/Maxです。")]
    [SerializeField] private Vector2 m_commentIntervalSecondsRange =
        new Vector2(2.0f, 5.0f);
    [Tooltip("一つのコメントを表示する秒数Min/Maxです。")]
    [SerializeField] private Vector2 m_commentDurationSecondsRange =
        new Vector2(1.2f, 2.2f);
    [SerializeField] private Sprite m_commentBubbleSprite;
    [Tooltip("観客個体ではなく、画面Canvas上で歓声を表示する複数の場所です。")]
    [SerializeField] private AudienceReactionCommentBubble[] m_commentDisplays;
    [SerializeField, Min(1)] private int m_minimumCommentWindows = 1;
    [SerializeField, Min(1)] private int m_maximumCommentWindows = 2;
    [Tooltip("高評価時にランダム選択されるコメント文の候補です。")]
    [SerializeField] private string[] m_positiveComments =
    {
        "いいぞ！",
        "そのポーズ好き！",
        "キレてる！"
    };
    [Tooltip("低評価時にランダム選択されるコメント文の候補です。")]
    [SerializeField] private string[] m_disappointedComments =
    {
        "惜しい！",
        "次に期待！"
    };

    private readonly Dictionary<AudienceReaction, Vector3> m_preferences =
        new Dictionary<AudienceReaction, Vector3>(); //観客別好み
    private bool b_m_initialized; //好み作成済みか
    private bool b_m_evaluated; //評価済みか
    private Coroutine m_initializeCoroutine;
    private float m_nextCommentTime; //次のコメント表示を許可する時刻

    /// <summary>
    /// Canvas表示用に指定観客の三種類の好みを返します。
    /// </summary>
    public bool TryGetPreferences(
        AudienceReaction _audience,
        out Vector3 _preferences)
    {
        _preferences = Vector3.zero;
        if (_audience == null)return false;
        if (!b_m_initialized)
        {
            InitializePreferences();
        }

        if (m_preferences.TryGetValue(_audience, out _preferences))return true;

        _preferences = CreateRandomPreferences();
        m_preferences[_audience] = _preferences;
        return true;
    }

    /// <summary>
    /// ゲーム開始時に全観客へ三種類の好みを割り当てます。
    /// </summary>
    private void Start()
    {
        FindAudienceSpawner();
        if (m_audienceSpawner != null && !m_audienceSpawner.IsSpawnComplete)
        {
            m_initializeCoroutine = StartCoroutine(InitializeAfterAudienceSpawn());
        }
        else
        {
            InitializePreferences();
        }
    }

    private System.Collections.IEnumerator InitializeAfterAudienceSpawn()
    {
        while (m_audienceSpawner != null && !m_audienceSpawner.IsSpawnComplete)
        {
            yield return null;
        }
        m_initializeCoroutine = null;
        InitializePreferences();
    }

    /// <summary>
    /// 生成済み観客へ正規化した三種類の好みを作成します。
    /// </summary>
    [ContextMenu("Initialize Audience Preferences")]
    public void InitializePreferences()
    {
        FindAudienceSpawner();
        m_preferences.Clear();
        if (m_audienceSpawner == null)return;

        IReadOnlyList<AudienceReaction> audiences =
            m_audienceSpawner.Audiences; //生成済み観客
        for (int i = 0; i < audiences.Count; ++i)
        {
            AudienceReaction audience = audiences[i]; //対象観客
            if (audience == null)continue;

            m_preferences[audience] = CreateRandomPreferences();
        }

        b_m_initialized = true;
        b_m_evaluated = false;
    }

    /// <summary>
    /// 選択種類への好みに比例して反応人数と動作強度を増やします。
    /// </summary>
    public float EvaluatePreference(int _preferenceIndex)
    {
        if (b_m_evaluated)return 0.0f;
        if (_preferenceIndex < 0
            || _preferenceIndex >= EPreferenceCount)return 0.0f;
        if (!b_m_initialized)
        {
            InitializePreferences();
        }

        b_m_evaluated = true;
        int reactionCount = 0; //今回反応人数
        float preferenceTotal = 0.0f; //全観客支持率合計
        foreach (KeyValuePair<AudienceReaction, Vector3> pair in m_preferences)
        {
            float preference = GetPreference(
                pair.Value,
                _preferenceIndex); //対象種類の好み
            preferenceTotal += preference;
            if (reactionCount >= m_maximumReactionCount)continue;
            float reactionRatio = Mathf.Lerp(
                EMinimumReactionRatio,
                EMaximumReactionRatio,
                preference);
            if (Random.value > reactionRatio)continue;
            if (pair.Key == null || !pair.Key.gameObject.activeInHierarchy)continue;

            float strength = Mathf.Lerp(
                EMinimumReactionStrength,
                EMaximumReactionStrength,
                preference);
            pair.Key.PlayReaction(GetPositiveReaction(), strength);
            ++reactionCount;
        }

        float averagePreference = m_preferences.Count > 0
            ? preferenceTotal / m_preferences.Count
            : 0.0f; //全体支持率
        Debug.Log(
            $"Event Node {_preferenceIndex + 1}: "
            + $"Audience Preference {averagePreference:P1}, "
            + $"Reactions {reactionCount}");
        PreferenceEvaluated?.Invoke(
            _preferenceIndex,
            averagePreference);
        return averagePreference;
    }

    /// <summary>通常Reactionへ観客個別の好みを反映し、必要ならコメントを表示します。</summary>
    public void ApplyAmbientPreference(
        AudienceReaction _audience,
        ref EAudienceReaction _reaction,
        ref float _strength)
    {
        if (!b_m_useAmbientPreference || _audience == null)return;
        if (!TryGetPreferences(_audience, out Vector3 preferences))return;

        int preferredIndex = GetPreferredIndex(preferences);
        float preference = GetPreference(preferences, preferredIndex);
        float minimumMultiplier = Mathf.Max(0.0f, m_ambientStrengthMultiplierRange.x);
        float maximumMultiplier = Mathf.Max(
            minimumMultiplier,
            m_ambientStrengthMultiplierRange.y);
        _strength *= Mathf.Lerp(minimumMultiplier, maximumMultiplier, preference);

        bool positiveReaction = _reaction != EAudienceReaction.Disappointed;
        if (positiveReaction && Random.value <= m_preferredReactionChance)
        {
            _reaction = GetPreferredReaction(preferredIndex);
        }

        if (!b_m_showReactionComments
            || Time.unscaledTime < m_nextCommentTime
            || Random.value > m_commentDisplayChance)return;

        if (ShowRandomCanvasComments(
            positiveReaction,
            out float longestDurationSeconds))
        {
            m_nextCommentTime = Time.unscaledTime
                + longestDurationSeconds
                + GetRandomRangeValue(m_commentIntervalSecondsRange, 0.0f);
        }
    }

    /// <summary>登録窓から毎回ランダムに一部を選び、候補文も個別に抽選します。</summary>
    private bool ShowRandomCanvasComments(
        bool _positive,
        out float _longestdurationseconds)
    {
        _longestdurationseconds = 0.0f;
        if (m_commentDisplays == null || m_commentDisplays.Length == 0)return false;

        List<int> availableIndexes = new List<int>(); //未表示窓を優先する候補
        for (int i = 0; i < m_commentDisplays.Length; ++i)
        {
            AudienceReactionCommentBubble display = m_commentDisplays[i];
            if (display != null && !display.IsVisible)availableIndexes.Add(i);
        }

        if (availableIndexes.Count == 0)
        {
            for (int i = 0; i < m_commentDisplays.Length; ++i)
            {
                if (m_commentDisplays[i] != null)availableIndexes.Add(i);
            }
        }
        if (availableIndexes.Count == 0)return false;

        for (int i = availableIndexes.Count - 1; i > 0; --i)
        {
            int swapIndex = Random.Range(0, i + 1);
            (availableIndexes[i], availableIndexes[swapIndex]) =
                (availableIndexes[swapIndex], availableIndexes[i]);
        }

        int minimumCount = Mathf.Clamp(
            m_minimumCommentWindows,
            1,
            availableIndexes.Count);
        int maximumCount = Mathf.Clamp(
            m_maximumCommentWindows,
            minimumCount,
            availableIndexes.Count);
        int displayCount = Random.Range(minimumCount, maximumCount + 1);
        bool b_displayed = false;
        for (int i = 0; i < displayCount; ++i)
        {
            string comment = GetRandomComment(_positive);
            if (string.IsNullOrWhiteSpace(comment))continue;
            float durationSeconds = GetRandomRangeValue(
                m_commentDurationSecondsRange,
                0.1f);
            m_commentDisplays[availableIndexes[i]].Show(
                comment,
                m_commentBubbleSprite,
                durationSeconds);
            _longestdurationseconds = Mathf.Max(
                _longestdurationseconds,
                durationSeconds);
            b_displayed = true;
        }
        return b_displayed;
    }

    [ContextMenu("Test Positive Canvas Comment")]
    private void TestPositiveCanvasComment()
    {
        if (!Application.isPlaying)return;
        ShowRandomCanvasComments(true, out _);
    }

    [ContextMenu("Test Disappointed Canvas Comment")]
    private void TestDisappointedCanvasComment()
    {
        if (!Application.isPlaying)return;
        ShowRandomCanvasComments(false, out _);
    }

    public void PreviewRandomComments(bool _positive)
    {
        if (Application.isPlaying)ShowRandomCanvasComments(_positive, out _);
    }

    /// <summary>順序が逆でも安全にMin/Maxを並べ、範囲内の値を返します。</summary>
    private static float GetRandomRangeValue(
        Vector2 _range,
        float _minimumvalue)
    {
        float minimum = Mathf.Max(_minimumvalue, Mathf.Min(_range.x, _range.y));
        float maximum = Mathf.Max(minimum, Mathf.Max(_range.x, _range.y));
        return Random.Range(minimum, maximum);
    }

    private static int GetPreferredIndex(Vector3 _preferences)
    {
        if (_preferences.x >= _preferences.y && _preferences.x >= _preferences.z)return 0;
        return _preferences.y >= _preferences.z ? 1 : 2;
    }

    private static EAudienceReaction GetPreferredReaction(int _preferredIndex)
    {
        switch (_preferredIndex)
        {
            case 0:
                return EAudienceReaction.Jump;
            case 1:
                return EAudienceReaction.Cheer;
            default:
                return EAudienceReaction.Bounce;
        }
    }

    private string GetRandomComment(bool _positive)
    {
        string[] comments = _positive
            ? m_positiveComments
            : m_disappointedComments;
        if (comments == null || comments.Length == 0)return string.Empty;

        return comments[Random.Range(0, comments.Length)];
    }

    /// <summary>
    /// 好みVectorから指定番号の値を返します。
    /// </summary>
    private static float GetPreference(
        Vector3 _preferences,
        int _index)
    {
        switch (_index)
        {
            case 0:
                return _preferences.x;
            case 1:
                return _preferences.y;
            default:
                return _preferences.z;
        }
    }

    /// <summary>
    /// 合計が一になる三種類の好みを作成します。
    /// </summary>
    private static Vector3 CreateRandomPreferences()
    {
        Vector3 seeds = new Vector3(
            Random.Range(EMinimumPreferenceSeed, 1.0f),
            Random.Range(EMinimumPreferenceSeed, 1.0f),
            Random.Range(EMinimumPreferenceSeed, 1.0f)); //好み抽選値
        float total = seeds.x + seeds.y + seeds.z; //正規化合計
        return seeds / total;
    }

    /// <summary>
    /// 高評価向けReactionをランダムに返します。
    /// </summary>
    private static EAudienceReaction GetPositiveReaction()
    {
        int reactionIndex = Random.Range(0, EPreferenceCount);
        switch (reactionIndex)
        {
            case 0:
                return EAudienceReaction.Jump;
            case 1:
                return EAudienceReaction.Cheer;
            default:
                return EAudienceReaction.Bounce;
        }
    }

    /// <summary>
    /// Scene上のAudienceAreaSpawnerを取得します。
    /// </summary>
    private void FindAudienceSpawner()
    {
        if (m_audienceSpawner != null)return;

        m_audienceSpawner = FindFirstObjectByType<AudienceAreaSpawner>();
    }
}
