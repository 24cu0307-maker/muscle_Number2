/*━━━━━━━━━*
*@file VoltageBgmSystem.cs*
*@brief Voltageに応じてBGMの音量、Layer、空間Audio Effectを連続制御する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Pitchを変えず複数AudioClipの同期再生と軽い音響変化を行う*
*━━━━━━━━━*/

using System;
using UnityEngine;

/// <summary>
/// Voltage別に重ねるBGM Layer設定です。
/// </summary>
[Serializable]
public struct SVoltageBgmLayer
{
    public AudioClip m_clip; //再生Clip
    [Range(0.0f, 1.0f)] public float m_startVoltage; //鳴り始める正規化Voltage
    [Range(0.01f, 1.0f)] public float m_fadeWidth; //Fade幅
    [Range(0.0f, 1.0f)] public float m_maximumVolume; //最大音量
}

/// <summary>
/// VenueVoltageSystemを参照してBGMを連続変化させます。
/// </summary>
public sealed class VoltageBgmSystem : MonoBehaviour
{
    private const float EMinimumFadeWidth = 0.01f; //最小Fade幅
    private const float EDefaultPitch = 1.0f; //基準Pitch
    private const float EMinimumCutoffFrequency = 12000.0f; //最低時Low Pass周波数
    private const float EMaximumCutoffFrequency = 22000.0f; //最高時Low Pass周波数
    private const float ELowPassResonance = 1.0f; //Low Pass共振値
    private const float EMinimumReverbLevel = -10000.0f; //最低時Reverb量
    private const float EMaximumReverbLevel = -3500.0f; //最高時Reverb量
    private const float EMaximumEchoWetMix = 0.06f; //最高時Echo混合率
    private const float EEchoDelayMilliseconds = 280.0f; //Echo遅延
    private const float EEchoDecayRatio = 0.12f; //Echo減衰率
    private const float EMaximumChorusWetMix = 0.025f; //最高時Chorus混合率
    private const float EChorusRate = 0.35f; //Chorus変調速度
    private const float EChorusDepth = 0.08f; //Chorus変調深度
    private const int EHighestAudioPriority = 0; //BGM Voice優先度
    private const double EPlaybackStartDelay = 0.05d; //同期再生待機秒数
    private const double ERestartToleranceSeconds = 0.5d; //停止判定猶予

    [SerializeField] private VenueVoltageSystem m_voltageSystem; //Voltage参照元
    [Tooltip("Music & Effect Editorで設定したBGMを空のLayerへ使用します。")]
    [SerializeField] private MusicNodeSequence m_musicNodeSequence; //Editor共通BGM
    [SerializeField] private SVoltageBgmLayer[] m_layers; //同期BGM Layer一覧
    [SerializeField] private AnimationCurve m_volumeCurve =
        AnimationCurve.Linear(0.0f, 0.9f, 1.0f, 1.0f); //全体音量変化
    [Header("Subtle Audio Effects")]
    [SerializeField] private AnimationCurve m_lowPassCutoffCurve =
        AnimationCurve.Linear(
            0.0f,
            EMinimumCutoffFrequency,
            1.0f,
            EMaximumCutoffFrequency); //Voltage別Low Pass
    [SerializeField] private AnimationCurve m_reverbLevelCurve =
        AnimationCurve.Linear(
            0.0f,
            EMinimumReverbLevel,
            1.0f,
            EMaximumReverbLevel); //Voltage別Reverb
    [SerializeField] private AnimationCurve m_echoWetMixCurve =
        AnimationCurve.Linear(
            0.0f,
            0.0f,
            1.0f,
            EMaximumEchoWetMix); //Voltage別Echo
    [SerializeField] private AnimationCurve m_chorusWetMixCurve =
        AnimationCurve.Linear(
            0.0f,
            0.0f,
            1.0f,
            EMaximumChorusWetMix); //Voltage別Chorus
    [SerializeField] private bool b_m_playOnStart = true; //開始時自動再生
    [SerializeField] private bool b_m_playAfterOpeningCamera = true; //最初のCamera演出後に再生
    [Min(0.0f)]
    [SerializeField] private float m_openingCameraMaximumWaitSeconds = 3.0f; //Camera未開始時の最大待機
    [SerializeField] private PoseCameraDirector m_cameraDirector; //開始待機対象Camera演出

    private AudioSource[] m_audioSources; //生成した同期音源一覧
    private AudioLowPassFilter[] m_lowPassFilters; //Layer別Low Pass一覧
    private AudioReverbFilter[] m_reverbFilters; //Layer別Reverb一覧
    private AudioEchoFilter[] m_echoFilters; //Layer別Echo一覧
    private AudioChorusFilter[] m_chorusFilters; //Layer別Chorus一覧
    private double m_scheduledStartTime; //直近の再生開始DSP時刻
    private bool b_m_shouldBePlaying; //BGMを継続再生するか
    private bool b_m_observedCameraPlaying; //Camera演出開始を確認したか
    private bool b_m_completedAutoStart; //自動再生処理が完了したか
    private float m_openingCameraWaitStartTime; //Camera開始待機を始めた時刻

    public float CurrentTimeSeconds
    {
        get
        {
            AudioSource source = GetClockSource();
            if (source == null)return 0.0f;
            return source.time;
        }
    }

    public bool IsPlaybackReady => GetClockSource() != null;
    public bool IsPlaying
    {
        get
        {
            AudioSource source = GetClockSource();
            return source != null && source.isPlaying;
        }
    }
    public float DurationSeconds
    {
        get
        {
            AudioSource source = GetClockSource();
            if (source == null || source.clip == null)return 0.0f;
            return source.clip.length;
        }
    }

    /// <summary>
    /// AudioSourceを作成し必要ならBGMを開始します。
    /// </summary>
    private void Start()
    {
        if (m_voltageSystem == null)
        {
            m_voltageSystem = FindFirstObjectByType<VenueVoltageSystem>();
        }

        if (m_cameraDirector == null)
        {
            m_cameraDirector = FindFirstObjectByType<PoseCameraDirector>();
        }

        CreateAudioSources();
        m_openingCameraWaitStartTime = Time.unscaledTime;
        if (b_m_playOnStart && !b_m_playAfterOpeningCamera)
        {
            Play();
            b_m_completedAutoStart = true;
        }
    }

    /// <summary>
    /// Voltageに応じて各Layerを滑らかに調整します。
    /// </summary>
    private void Update()
    {
        if (m_audioSources == null)return;

        UpdateOpeningCameraWait();
        EnsurePlayback();
        float voltage = m_voltageSystem != null
            ? m_voltageSystem.NormalizedVoltage
            : 0.0f; //正規化Voltage
        float masterVolume = Mathf.Clamp01(m_volumeCurve.Evaluate(voltage)); //全体音量
        for (int i = 0; i < m_audioSources.Length; ++i)
        {
            float fadeWidth =
                Mathf.Max(EMinimumFadeWidth, m_layers[i].m_fadeWidth); //安全なFade幅
            float layerWeight = Mathf.SmoothStep(
                0.0f,
                1.0f,
                Mathf.InverseLerp(
                    m_layers[i].m_startVoltage - fadeWidth,
                    m_layers[i].m_startVoltage,
                    voltage)); //Layer混合率
            m_audioSources[i].volume =
                masterVolume * layerWeight * m_layers[i].m_maximumVolume;
            m_audioSources[i].pitch = EDefaultPitch;
        }

        ApplyAudioEffects(voltage);
    }

    /// <summary>
    /// 最初のCamera演出開始と終了を検出し、終了時にBGMを開始します。
    /// </summary>
    private void UpdateOpeningCameraWait()
    {
        if (!b_m_playOnStart
            || !b_m_playAfterOpeningCamera
            || b_m_completedAutoStart)return;
        if (m_cameraDirector == null)
        {
            m_cameraDirector = FindFirstObjectByType<PoseCameraDirector>();
        }

        if (m_cameraDirector != null && m_cameraDirector.IsPlaying)
        {
            b_m_observedCameraPlaying = true;
            return;
        }

        float waitSeconds = Time.unscaledTime - m_openingCameraWaitStartTime;
        if (!b_m_observedCameraPlaying
            && waitSeconds < m_openingCameraMaximumWaitSeconds)return;

        Play();
        b_m_completedAutoStart = true;
    }

    /// <summary>
    /// 全Layerを同じDSP時刻から再生します。
    /// </summary>
    public void Play()
    {
        if (m_audioSources == null)
        {
            CreateAudioSources();
        }

        m_scheduledStartTime =
            AudioSettings.dspTime + EPlaybackStartDelay; //同期開始時刻
        b_m_shouldBePlaying = true;
        for (int i = 0; i < m_audioSources.Length; ++i)
        {
            if (m_audioSources[i].clip == null)continue;

            m_audioSources[i].Stop();
            m_audioSources[i].time = 0.0f;
            m_audioSources[i].PlayScheduled(m_scheduledStartTime);
        }
    }

    /// <summary>
    /// 全BGM Layerを停止します。
    /// </summary>
    public void Stop()
    {
        b_m_shouldBePlaying = false;
        if (m_audioSources == null)return;

        for (int i = 0; i < m_audioSources.Length; ++i)
        {
            m_audioSources[i].Stop();
        }
    }

    /// <summary>
    /// Layer数に合わせてAudioSourceを生成します。
    /// </summary>
    private void CreateAudioSources()
    {
        if (m_layers == null)
        {
            m_layers = Array.Empty<SVoltageBgmLayer>();
        }

        m_audioSources = new AudioSource[m_layers.Length];
        m_lowPassFilters = new AudioLowPassFilter[m_layers.Length];
        m_reverbFilters = new AudioReverbFilter[m_layers.Length];
        m_echoFilters = new AudioEchoFilter[m_layers.Length];
        m_chorusFilters = new AudioChorusFilter[m_layers.Length];
        for (int i = 0; i < m_layers.Length; ++i)
        {
            GameObject layerObject = new GameObject($"BGM_Layer_{i:00}"); //音源Object
            layerObject.transform.SetParent(transform, false);
            AudioSource audioSource =
                layerObject.AddComponent<AudioSource>(); //同期音源
            AudioClip layerClip = m_layers[i].m_clip;
            if (layerClip == null && m_musicNodeSequence != null)
            {
                layerClip = m_musicNodeSequence.BgmClip;
            }
            audioSource.clip = layerClip;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.0f;
            audioSource.priority = EHighestAudioPriority;
            audioSource.ignoreListenerPause = true;
            audioSource.pitch = EDefaultPitch;
            audioSource.volume = 0.0f;
            m_audioSources[i] = audioSource;
            CreateAudioEffects(layerObject, i);
        }
    }

    /// <summary>
    /// BGM Layerへ原音を保つ軽いAudio Effectを追加します。
    /// </summary>
    private void CreateAudioEffects(
        GameObject _layerobject,
        int _layerindex)
    {
        AudioLowPassFilter lowPassFilter =
            _layerobject.AddComponent<AudioLowPassFilter>(); //高域制御
        lowPassFilter.lowpassResonanceQ = ELowPassResonance;
        m_lowPassFilters[_layerindex] = lowPassFilter;

        AudioReverbFilter reverbFilter =
            _layerobject.AddComponent<AudioReverbFilter>(); //空間表現
        reverbFilter.reverbPreset = AudioReverbPreset.User;
        reverbFilter.dryLevel = 0.0f;
        reverbFilter.reverbLevel = EMinimumReverbLevel;
        m_reverbFilters[_layerindex] = reverbFilter;

        AudioEchoFilter echoFilter =
            _layerobject.AddComponent<AudioEchoFilter>(); //薄い反響
        echoFilter.dryMix = 1.0f;
        echoFilter.wetMix = 0.0f;
        echoFilter.delay = EEchoDelayMilliseconds;
        echoFilter.decayRatio = EEchoDecayRatio;
        m_echoFilters[_layerindex] = echoFilter;

        AudioChorusFilter chorusFilter =
            _layerobject.AddComponent<AudioChorusFilter>(); //広がり表現
        chorusFilter.dryMix = 1.0f;
        chorusFilter.wetMix1 = 0.0f;
        chorusFilter.wetMix2 = 0.0f;
        chorusFilter.wetMix3 = 0.0f;
        chorusFilter.rate = EChorusRate;
        chorusFilter.depth = EChorusDepth;
        m_chorusFilters[_layerindex] = chorusFilter;
    }

    /// <summary>
    /// Voltage値を各Audio Effectへ連続的に反映します。
    /// </summary>
    private void ApplyAudioEffects(float _voltage)
    {
        float cutoffFrequency = Mathf.Clamp(
            m_lowPassCutoffCurve.Evaluate(_voltage),
            EMinimumCutoffFrequency,
            EMaximumCutoffFrequency); //現在Low Pass周波数
        float reverbLevel = Mathf.Clamp(
            m_reverbLevelCurve.Evaluate(_voltage),
            EMinimumReverbLevel,
            EMaximumReverbLevel); //現在Reverb量
        float echoWetMix = Mathf.Clamp(
            m_echoWetMixCurve.Evaluate(_voltage),
            0.0f,
            EMaximumEchoWetMix); //現在Echo混合率
        float chorusWetMix = Mathf.Clamp(
            m_chorusWetMixCurve.Evaluate(_voltage),
            0.0f,
            EMaximumChorusWetMix); //現在Chorus混合率

        for (int i = 0; i < m_audioSources.Length; ++i)
        {
            m_lowPassFilters[i].cutoffFrequency = cutoffFrequency;
            m_reverbFilters[i].reverbLevel = reverbLevel;
            m_echoFilters[i].wetMix = echoWetMix;
            m_chorusFilters[i].wetMix1 = chorusWetMix;
        }
    }

    /// <summary>
    /// カメラ演出などの後にBGMが意図せず停止した場合は再開します。
    /// </summary>
    private void EnsurePlayback()
    {
        if (!b_m_shouldBePlaying)return;
        if (AudioSettings.dspTime
            <= m_scheduledStartTime + ERestartToleranceSeconds)return;

        for (int i = 0; i < m_audioSources.Length; ++i)
        {
            AudioSource audioSource = m_audioSources[i]; //確認対象Layer
            if (audioSource == null
                || audioSource.clip == null
                || audioSource.isPlaying)continue;

            audioSource.Play();
        }
    }

    public float GetBGMTime(int index)
    {
        if (m_audioSources == null
            || index < 0
            || index >= m_audioSources.Length
            || m_audioSources[index] == null)return 0.0f;

        return m_audioSources[index].time;
    }

    private AudioSource GetClockSource()
    {
        if (m_audioSources == null)return null;

        for (int i = 0; i < m_audioSources.Length; ++i)
        {
            if (m_audioSources[i] != null && m_audioSources[i].clip != null)
            {
                return m_audioSources[i];
            }
        }

        return null;
    }
}
