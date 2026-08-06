/*━━━━━━━━━*
*@file VenueVoltageSystem.cs*
*@brief スコアと連続成功に応じて会場の色と成功音を変化させる*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Gameplay_EffectWork専用の独立追加システム*
*━━━━━━━━━*/

using System;
using UnityEngine;

/// <summary>
/// 会場ボルテージの色段階です。
/// </summary>
public enum EVoltageLevel
{
    Blue,
    Green,
    Yellow,
    Orange,
    Red
}

/// <summary>
/// スコア加算を成功として受け取り、会場の色と成功音を制御します。
/// </summary>
public sealed class VenueVoltageSystem : MonoBehaviour
{
    private const float EMinimumVoltage = 0.0f; //ボルテージ最小値
    private const float EMaximumVoltage = 100.0f; //ボルテージ最大値
    private const float EDefaultVoltage = 50.0f; //ゲーム開始時の基準値
    private const float ELevelRange = 20.0f; //五段階の各Voltage範囲
    private const float EBaseVoltageGain = 7.0f; //成功一回の基本上昇量
    private const float EComboVoltageGain = 1.8f; //連続成功一回ごとの追加量
    private const float EScoreGainUnit = 5000.0f; //スコアによる追加上昇の基準
    private const float EMaximumScoreBonus = 5.0f; //スコアによる最大追加量
    private const float EDefaultComboWindowSeconds = 8.0f; //連続成功として扱う時間
    private const float EDefaultFailureVoltageLoss = 12.0f; //失敗時の低下量
    private const float EMaximumLightIntensityMultiplier = 3.0f; //重なったSpotlightが白飛びしないための倍率上限
    private const float EMinimumSoundPitch = 0.9f; //最低音程
    private const float EMaximumSoundPitch = 1.6f; //最高音程
    private const float ESuccessToneFrequency = 440.0f; //生成する成功音の基準周波数
    private const float ESuccessToneDuration = 0.16f; //成功音の長さ
    private const int EAudioSampleRate = 44100; //生成音のサンプルレート
    private const int EFirstComboCount = 1; //連続成功の初期回数
    private const int EFirstSampleIndex = 0; //音声サンプルの先頭番号

    [SerializeField] private float m_comboWindowSeconds = EDefaultComboWindowSeconds; //連続成功受付時間
    [Header("Voltage Change")]
    [SerializeField] private float m_initialVoltage = EDefaultVoltage;
    [SerializeField] private float m_baseVoltageGain = EBaseVoltageGain;
    [SerializeField] private float m_comboVoltageGain = EComboVoltageGain;
    [SerializeField] private float m_scoreGainUnit = EScoreGainUnit;
    [SerializeField] private float m_maximumScoreBonus = EMaximumScoreBonus;
    [SerializeField] private float m_failureVoltageLoss =
        EDefaultFailureVoltageLoss; //失敗時の低下量
    [Header("Success Sound")]
    [SerializeField] private AudioClip m_successSound; //任意の差し替え成功音
    [SerializeField, Range(0.0f, 1.0f)] private float m_minimumSuccessSoundVolume = 0.2f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_maximumSuccessSoundVolume = 0.8f;
    [SerializeField] private float m_minimumSuccessSoundPitch = EMinimumSoundPitch;
    [SerializeField] private float m_maximumSuccessSoundPitch = EMaximumSoundPitch;

    [Header("Success Effect Amount")]
    [SerializeField, Min(0)] private int m_minimumSuccessEffectCount = 1;
    [SerializeField, Min(0)] private int m_maximumSuccessEffectCount = 4;

    [Header("Effect Light Intensity")]
    [SerializeField, Range(0.0f, EMaximumLightIntensityMultiplier)]
    private float m_minimumLightIntensityMultiplier = 0.7f;
    [SerializeField, Range(0.0f, EMaximumLightIntensityMultiplier)]
    private float m_maximumLightIntensityMultiplier = 2.0f;

    [Header("Voltage Colors")]
    [SerializeField] private Color m_blueColor =
        new Color(0.1f, 0.35f, 1.0f, 0.025f); //青段階の色と濃さ
    [SerializeField] private Color m_greenColor =
        new Color(0.1f, 1.0f, 0.35f, 0.05f); //緑段階の色と濃さ
    [SerializeField] private Color m_yellowColor =
        new Color(1.0f, 0.9f, 0.1f, 0.08f); //黄段階の色と濃さ
    [SerializeField] private Color m_orangeColor =
        new Color(1.0f, 0.42f, 0.05f, 0.11f); //オレンジ段階の色と濃さ
    [SerializeField] private Color m_redColor =
        new Color(1.0f, 0.08f, 0.04f, 0.14f); //赤段階の色と濃さ

    [Header("Runtime State")]
    [SerializeField] private float m_voltage = EDefaultVoltage; //現在のボルテージ
    [SerializeField] private float m_presentedVoltage; //画面へ表示中のボルテージ

    private AudioSource m_audioSource; //成功音再生元
    private int m_comboCount; //現在の連続成功回数
    private float m_lastSuccessTime; //最後に成功した時刻

    public event Action<float> m_audienceSuccess; //成功時の観客通知
    public event Action m_audienceFailure; //失敗時の観客通知

    public float Voltage
    {
        get
        {
            return m_voltage;
        }
    }

    public int ComboCount
    {
        get
        {
            return m_comboCount;
        }
    }

    public EVoltageLevel VoltageLevel
    {
        get
        {
            return GetVoltageLevel(m_voltage);
        }
    }

    public float NormalizedVoltage
    {
        get
        {
            return Mathf.InverseLerp(
                EMinimumVoltage,
                EMaximumVoltage,
                m_voltage);
        }
    }

    public Color CurrentVoltageColor
    {
        get
        {
            Color color = EvaluateVoltageColor(m_voltage); //現在の演出色
            color.a = 1.0f;
            return color;
        }
    }

    /// <summary>
    /// 画面色演出と成功音を準備します。
    /// </summary>
    private void Awake()
    {
        CreateAudioSource();
        ConnectEffectTargets();
        EnsureDebugPanel();
        m_voltage = Mathf.Clamp(m_initialVoltage, EMinimumVoltage, EMaximumVoltage);
        m_presentedVoltage = m_voltage;
    }

    /// <summary>
    /// Inspectorで変更した色と設定値を再生中の表示へ反映します。
    /// </summary>
    private void OnValidate()
    {
        m_comboWindowSeconds = Mathf.Max(0.0f, m_comboWindowSeconds);
        m_initialVoltage = Mathf.Clamp(m_initialVoltage, EMinimumVoltage, EMaximumVoltage);
        m_baseVoltageGain = Mathf.Max(0.0f, m_baseVoltageGain);
        m_comboVoltageGain = Mathf.Max(0.0f, m_comboVoltageGain);
        m_scoreGainUnit = Mathf.Max(1.0f, m_scoreGainUnit);
        m_maximumScoreBonus = Mathf.Max(0.0f, m_maximumScoreBonus);
        m_failureVoltageLoss = Mathf.Max(0.0f, m_failureVoltageLoss);
        m_minimumSuccessEffectCount = Mathf.Max(0, m_minimumSuccessEffectCount);
        m_maximumSuccessEffectCount = Mathf.Max(m_minimumSuccessEffectCount, m_maximumSuccessEffectCount);
        m_minimumLightIntensityMultiplier = Mathf.Clamp(
            m_minimumLightIntensityMultiplier,
            0.0f,
            EMaximumLightIntensityMultiplier);
        m_maximumLightIntensityMultiplier = Mathf.Clamp(
            m_maximumLightIntensityMultiplier,
            m_minimumLightIntensityMultiplier,
            EMaximumLightIntensityMultiplier);
        m_voltage = Mathf.Clamp(
            m_voltage,
            EMinimumVoltage,
            EMaximumVoltage);

    }

    /// <summary>
    /// 成功を登録し、基本値・連続成功数・獲得Scoreからボルテージ上昇量を決定します。
    /// 更新後は成功音を再生し、観客側へ正規化済みボルテージを通知します。
    /// </summary>
    public void RegisterSuccess(int _scoregain)
    {
        bool b_isCombo =
            Time.unscaledTime - m_lastSuccessTime <= m_comboWindowSeconds; //連続成功判定
        m_comboCount = b_isCombo
            ? m_comboCount + EFirstComboCount
            : EFirstComboCount;
        m_lastSuccessTime = Time.unscaledTime;

        float scoreBonus = Mathf.Min(
            m_maximumScoreBonus,
            Mathf.Max(0, _scoregain) / m_scoreGainUnit); //獲得スコアによる追加量
        float comboBonus =
            Mathf.Max(0, m_comboCount - EFirstComboCount)
            * m_comboVoltageGain; //連続成功による追加量
        m_voltage = Mathf.Clamp(
            m_voltage + m_baseVoltageGain + comboBonus + scoreBonus,
            EMinimumVoltage,
            EMaximumVoltage);

        PlaySuccessSound();
        m_audienceSuccess?.Invoke(NormalizedVoltage);
    }

    /// <summary>
    /// 失敗を登録し、コンボを切ってボルテージを下げます。
    /// </summary>
    public void RegisterFailure()
    {
        m_comboCount = 0;
        m_voltage = Mathf.Max(
            EMinimumVoltage,
            m_voltage - m_failureVoltageLoss);
        m_presentedVoltage = m_voltage;
        m_audienceFailure?.Invoke();
    }

    /// <summary>
    /// Debug機能からだけボルテージを直接設定します。
    /// </summary>
    public void SetVoltageForDebug(float _voltage)
    {
        m_voltage = Mathf.Clamp(
            _voltage,
            EMinimumVoltage,
            EMaximumVoltage);
    }

    /// <summary>
    /// Debug機能からだけボルテージを増減します。
    /// </summary>
    public void AddVoltageForDebug(float _amount)
    {
        SetVoltageForDebug(m_voltage + _amount);
    }

    /// <summary>
    /// ボルテージと連続成功を初期化します。
    /// </summary>
    public void ResetVoltageForDebug()
    {
        ResetVoltage();
    }

    /// <summary>
    /// ボルテージと連続成功を内部で初期化します。
    /// </summary>
    private void ResetVoltage()
    {
        m_voltage = Mathf.Clamp(m_initialVoltage, EMinimumVoltage, EMaximumVoltage);
        m_presentedVoltage = m_voltage;
        m_comboCount = 0;
        m_lastSuccessTime = Time.unscaledTime;
    }

    /// <summary>
    /// 現在のボルテージを、成功時に同時再生するEffect数へ変換します。
    /// Inspectorで設定した最小・最大数の間を0～1の正規化値で補間します。
    /// </summary>
    public int GetSuccessEffectCount()
    {
        return Mathf.RoundToInt(Mathf.Lerp(
            m_minimumSuccessEffectCount,
            m_maximumSuccessEffectCount,
            NormalizedVoltage));
    }

    /// <summary>
    /// LightおよびSpotlightConeへ適用する明るさ倍率を返します。
    /// 元のTimeline強度は変更せず、この倍率を後段で掛ける設計です。
    /// </summary>
    public float GetLightIntensityMultiplier()
    {
        float maximumMultiplier = Mathf.Clamp(
            m_maximumLightIntensityMultiplier,
            m_minimumLightIntensityMultiplier,
            EMaximumLightIntensityMultiplier);
        return Mathf.Lerp(
            m_minimumLightIntensityMultiplier,
            maximumMultiplier,
            NormalizedVoltage);
    }

    /// <summary>
    /// 青、緑、黄、オレンジ、赤の間を滑らかに補間します。
    /// </summary>
    private Color EvaluateVoltageColor(float _voltage)
    {
        Color[] colors =
        {
            m_blueColor,
            m_greenColor,
            m_yellowColor,
            m_orangeColor,
            m_redColor
        }; //会場ボルテージの五段階色

        float clampedVoltage = Mathf.Clamp(
            _voltage,
            EMinimumVoltage,
            EMaximumVoltage); //安全な現在値
        int colorIndex = Mathf.Min(
            Mathf.FloorToInt(clampedVoltage / ELevelRange),
            colors.Length - 2); //補間開始色
        float colorProgress =
            (clampedVoltage - colorIndex * ELevelRange)
            / ELevelRange; //次の色までの進行率
        return Color.Lerp(
            colors[colorIndex],
            colors[colorIndex + 1],
            colorProgress);
    }

    /// <summary>
    /// 現在値に対応する色段階を返します。
    /// </summary>
    private static EVoltageLevel GetVoltageLevel(float _voltage)
    {
        if (_voltage < ELevelRange)return EVoltageLevel.Blue;
        if (_voltage < ELevelRange * 2.0f)return EVoltageLevel.Green;
        if (_voltage < ELevelRange * 3.0f)return EVoltageLevel.Yellow;
        if (_voltage < ELevelRange * 4.0f)return EVoltageLevel.Orange;

        return EVoltageLevel.Red;
    }

    /// <summary>
    /// ボルテージに応じて音程を上げて成功音を再生します。
    /// </summary>
    private void PlaySuccessSound()
    {
        if (m_audioSource == null || m_successSound == null)return;

        float normalizedVoltage =
            Mathf.InverseLerp(EMinimumVoltage, EMaximumVoltage, m_voltage); //音程の進行率
        m_audioSource.pitch = Mathf.Lerp(
            m_minimumSuccessSoundPitch,
            m_maximumSuccessSoundPitch,
            normalizedVoltage);
        m_audioSource.PlayOneShot(
            m_successSound,
            Mathf.Lerp(
                m_minimumSuccessSoundVolume,
                m_maximumSuccessSoundVolume,
                normalizedVoltage));
    }

    /// <summary>
    /// 成功音の再生元と確認用の短い基準音を生成します。
    /// </summary>
    private void CreateAudioSource()
    {
        m_audioSource = gameObject.AddComponent<AudioSource>();
        m_audioSource.playOnAwake = false;
        m_audioSource.loop = false;
        m_audioSource.spatialBlend = 0.0f;

        if (m_successSound == null)
        {
            m_successSound = CreateSuccessTone();
        }
    }

    /// <summary>
    /// 別スクリプトのDebug操作Panelを同じObjectへ追加します。
    /// </summary>
    private void EnsureDebugPanel()
    {
        if (GetComponent<VoltageDebugPanel>() != null)return;

        gameObject.AddComponent<VoltageDebugPanel>();
    }

    /// <summary>
    /// EffectSystem配下のAudioSourceとLight、およびシーン内のSpotlightConeへVoltage連携を追加します。
    /// 既に連携Componentが存在する対象は除外するため、複数回呼ばれても重複追加されません。
    /// </summary>
    private void ConnectEffectTargets()
    {
        EffectSystem effectSystem = FindFirstObjectByType<EffectSystem>(); //対象EffectSystem
        if (effectSystem == null)return;

        Transform effectRoot = effectSystem.transform; //追加検索範囲
        AudioSource[] audioSources =
            effectRoot.GetComponentsInChildren<AudioSource>(true); //Timelineを含む音源
        ConnectAudioSources(audioSources);

        Light[] lights = effectRoot.GetComponentsInChildren<Light>(true); //Timelineを含むLight
        for (int i = 0; i < lights.Length; ++i)
        {
            AddVoltageLight(lights[i]);
        }

        SpotlightConeController[] spotlightCones =
            FindObjectsByType<SpotlightConeController>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        for (int i = 0; i < spotlightCones.Length; ++i)
        {
            AddVoltageSpotlightCone(spotlightCones[i]);
        }
    }

    /// <summary>
    /// EffectSystem登録AudioSourceへVoltage連携を追加します。
    /// </summary>
    private static void ConnectAudioSources(AudioSource[] _audiosources)
    {
        if (_audiosources == null)return;

        for (int i = 0; i < _audiosources.Length; ++i)
        {
            AudioSource audioSource = _audiosources[i]; //対象音源
            if (audioSource == null)continue;
            if (audioSource.GetComponent<VoltageAudioEffect>() != null)continue;

            audioSource.gameObject.AddComponent<VoltageAudioEffect>();
        }
    }

    /// <summary>
    /// LightへVoltage連携がなければ追加します。
    /// </summary>
    private static void AddVoltageLight(Light _light)
    {
        if (_light == null)return;
        if (_light.GetComponent<VoltageLightEffect>() != null)return;

        _light.gameObject.AddComponent<VoltageLightEffect>();
    }

    /// <summary>
    /// SpotlightConeへボルテージ発光補正が未登録の場合だけComponentを追加します。
    /// 非アクティブなTimeline用Objectも起動前に接続できるよう、呼び出し側でシーン全体を検索します。
    /// </summary>
    private static void AddVoltageSpotlightCone(
        SpotlightConeController _spotlightcone)
    {
        if (_spotlightcone == null)return;
        if (_spotlightcone.GetComponent<VoltageSpotlightConeEffect>() != null)
        {
            return;
        }

        _spotlightcone.gameObject.AddComponent<VoltageSpotlightConeEffect>();
    }

    /// <summary>
    /// 外部音源がなくても確認できる短い成功音を生成します。
    /// </summary>
    private static AudioClip CreateSuccessTone()
    {
        int sampleCount = Mathf.CeilToInt(
            EAudioSampleRate * ESuccessToneDuration); //生成サンプル数
        float[] samples = new float[sampleCount]; //生成音声データ
        for (int i = EFirstSampleIndex; i < sampleCount; ++i)
        {
            float time = (float)i / EAudioSampleRate; //現在サンプルの秒数
            float envelope = 1.0f - (float)i / sampleCount; //末尾へ向けた減衰
            samples[i] =
                Mathf.Sin(2.0f * Mathf.PI * ESuccessToneFrequency * time)
                * envelope;
        }

        AudioClip successTone = AudioClip.Create(
            "GeneratedVoltageSuccess",
            sampleCount,
            1,
            EAudioSampleRate,
            false); //確認用成功音
        successTone.SetData(samples, EFirstSampleIndex);
        return successTone;
    }
}
