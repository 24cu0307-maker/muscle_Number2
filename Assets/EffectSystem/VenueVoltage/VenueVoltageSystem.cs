/*━━━━━━━━━*
*@file VenueVoltageSystem.cs*
*@brief スコアと連続成功に応じて会場の色と成功音を変化させる*
*@author 24CU0000 Name*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Gameplay_EffectWork専用の独立追加システム*
*━━━━━━━━━*/

using System;
using UnityEngine;
using UnityEngine.UI;

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
    private const string EOverlayObjectName = "VoltageColorOverlay"; //色演出Object名
    private const float EMinimumVoltage = 0.0f; //ボルテージ最小値
    private const float EMaximumVoltage = 100.0f; //ボルテージ最大値
    private const float ELevelRange = 20.0f; //五段階の各Voltage範囲
    private const float EBaseVoltageGain = 7.0f; //成功一回の基本上昇量
    private const float EComboVoltageGain = 1.8f; //連続成功一回ごとの追加量
    private const float EScoreGainUnit = 5000.0f; //スコアによる追加上昇の基準
    private const float EMaximumScoreBonus = 5.0f; //スコアによる最大追加量
    private const float EDefaultComboWindowSeconds = 8.0f; //連続成功として扱う時間
    private const float EDefaultDecayDelaySeconds = 5.0f; //自然減少開始までの時間
    private const float EDefaultDecayPerSecond = 1.25f; //一秒あたりの自然減少量
    private const float EDefaultFailureVoltageLoss = 12.0f; //失敗時の低下量
    private const float EDefaultVisualTransitionSpeed = 35.0f; //表示値の追従速度
    private const float EMinimumSoundPitch = 0.9f; //最低音程
    private const float EMaximumSoundPitch = 1.6f; //最高音程
    private const float ESuccessToneFrequency = 440.0f; //生成する成功音の基準周波数
    private const float ESuccessToneDuration = 0.16f; //成功音の長さ
    private const float ESuccessToneVolume = 0.3f; //成功音の音量
    private const int EAudioSampleRate = 44100; //生成音のサンプルレート
    private const int EFirstComboCount = 1; //連続成功の初期回数
    private const int EFirstSampleIndex = 0; //音声サンプルの先頭番号
    private const int EOverlaySortingOrder = 32000; //画面色演出の描画順

    [SerializeField] private float m_comboWindowSeconds = EDefaultComboWindowSeconds; //連続成功受付時間
    [SerializeField] private float m_decayDelaySeconds = EDefaultDecayDelaySeconds; //自然減少開始時間
    [SerializeField] private float m_decayPerSecond = EDefaultDecayPerSecond; //自然減少速度
    [SerializeField] private float m_failureVoltageLoss =
        EDefaultFailureVoltageLoss; //失敗時の低下量
    [SerializeField] private float m_visualTransitionSpeed =
        EDefaultVisualTransitionSpeed; //色変化の追従速度
    [SerializeField] private AudioClip m_successSound; //任意の差し替え成功音

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
    [SerializeField] private float m_voltage; //現在のボルテージ
    [SerializeField] private float m_presentedVoltage; //画面へ表示中のボルテージ

    private RawImage m_colorOverlay; //画面全体の薄い色
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
        CreateOverlay();
        CreateAudioSource();
        ConnectEffectTargets();
        EnsureDebugPanel();
        m_presentedVoltage = m_voltage;
        ApplyVoltagePresentation();
    }

    /// <summary>
    /// Inspectorで変更した色と設定値を再生中の表示へ反映します。
    /// </summary>
    private void OnValidate()
    {
        m_comboWindowSeconds = Mathf.Max(0.0f, m_comboWindowSeconds);
        m_decayDelaySeconds = Mathf.Max(0.0f, m_decayDelaySeconds);
        m_decayPerSecond = Mathf.Max(0.0f, m_decayPerSecond);
        m_failureVoltageLoss = Mathf.Max(0.0f, m_failureVoltageLoss);
        m_visualTransitionSpeed = Mathf.Max(0.0f, m_visualTransitionSpeed);
        m_voltage = Mathf.Clamp(
            m_voltage,
            EMinimumVoltage,
            EMaximumVoltage);

        if (Application.isPlaying)
        {
            ApplyVoltagePresentation();
        }
    }

    /// <summary>
    /// 成功が途切れた後、会場ボルテージをゆっくり下げます。
    /// </summary>
    private void Update()
    {
        if (m_voltage > EMinimumVoltage
            && Time.unscaledTime - m_lastSuccessTime >= m_decayDelaySeconds)
        {
            m_voltage = Mathf.Max(
                EMinimumVoltage,
                m_voltage - m_decayPerSecond * Time.unscaledDeltaTime);
        }

        float previousPresentedVoltage = m_presentedVoltage; //直前の表示値
        m_presentedVoltage = Mathf.MoveTowards(
            m_presentedVoltage,
            m_voltage,
            m_visualTransitionSpeed * Time.unscaledDeltaTime);
        if (!Mathf.Approximately(previousPresentedVoltage, m_presentedVoltage))
        {
            ApplyVoltagePresentation();
        }
    }

    /// <summary>
    /// 成功を登録し、コンボに応じて上昇量を増やします。
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
            EMaximumScoreBonus,
            Mathf.Max(0, _scoregain) / EScoreGainUnit); //獲得スコアによる追加量
        float comboBonus =
            Mathf.Max(0, m_comboCount - EFirstComboCount)
            * EComboVoltageGain; //連続成功による追加量
        m_voltage = Mathf.Clamp(
            m_voltage + EBaseVoltageGain + comboBonus + scoreBonus,
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
        m_voltage = EMinimumVoltage;
        m_presentedVoltage = EMinimumVoltage;
        m_comboCount = 0;
        m_lastSuccessTime = Time.unscaledTime;
        ApplyVoltagePresentation();
    }

    /// <summary>
    /// 現在値に対応する画面色と透明度を適用します。
    /// </summary>
    private void ApplyVoltagePresentation()
    {
        if (m_colorOverlay == null)return;

        m_colorOverlay.color = EvaluateVoltageColor(m_presentedVoltage);
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
            EMinimumSoundPitch,
            EMaximumSoundPitch,
            normalizedVoltage);
        m_audioSource.PlayOneShot(m_successSound, ESuccessToneVolume);
    }

    /// <summary>
    /// ほかのUI操作を妨げない全画面色レイヤーを生成します。
    /// </summary>
    private void CreateOverlay()
    {
        GameObject overlayObject = new GameObject(
            EOverlayObjectName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(RawImage)); //画面全体の色レイヤー
        overlayObject.transform.SetParent(transform, false);

        Canvas canvas = overlayObject.GetComponent<Canvas>(); //色演出Canvas
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = EOverlaySortingOrder;

        m_colorOverlay = overlayObject.GetComponent<RawImage>();
        m_colorOverlay.texture = Texture2D.whiteTexture;
        m_colorOverlay.raycastTarget = false;

        RectTransform overlayTransform =
            overlayObject.GetComponent<RectTransform>(); //全画面領域
        overlayTransform.anchorMin = Vector2.zero;
        overlayTransform.anchorMax = Vector2.one;
        overlayTransform.offsetMin = Vector2.zero;
        overlayTransform.offsetMax = Vector2.zero;
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
    /// EffectSystemが使用するAudioSourceとLightへVoltage連携を追加します。
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
