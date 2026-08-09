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

    private readonly Dictionary<AudienceReaction, Vector3> m_preferences =
        new Dictionary<AudienceReaction, Vector3>(); //観客別好み
    private bool b_m_initialized; //好み作成済みか
    private bool b_m_evaluated; //評価済みか
    private Coroutine m_initializeCoroutine;

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
