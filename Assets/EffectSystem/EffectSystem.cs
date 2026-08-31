/*━━━━━━━━━*
*@file EffectSystem.cs*
*@brief 名前で管理された演出とTimelineを再生する*
*@author 24cu0312 久場洸太*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks 既存のPlayEffect APIとの互換性を維持*
*━━━━━━━━━*/

using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 名前ごとに再生する演出設定です。
/// </summary>
[System.Serializable]
public struct SEffectData
{
    [FormerlySerializedAs("s_meffectName")]
    [SerializeField] private string m_effectName;                    //演出を識別する名前

    [Header("Timing")]
    [Tooltip("PlayEffectが呼ばれてから、このエフェクトを再生するまでの待ち時間（秒）。")]
    [Min(0.0f)]
    [FormerlySerializedAs("m_playDelay")]
    [SerializeField] private float m_playDelaySeconds;               //再生開始までの待ち時間

    [FormerlySerializedAs("pa_mparticles")]
    [SerializeField] private ParticleSystem[] m_particles;           //同時再生するパーティクル群
    [FormerlySerializedAs("as_maudioSources")]
    [SerializeField] private AudioSource[] m_audioSources;           //同時再生する音声群
    [FormerlySerializedAs("lc_mlightControllers")]
    [SerializeField] private LightController[] m_lightControllers;   //同時再生するライト制御群

    [Header("Placement (Optional)")]
    [Tooltip("条件付き演出の位置指定で移動するRoot。未設定時はParticleのTransformを移動します。")]
    [SerializeField] private Transform m_positionRoot;               //位置指定時に移動するRoot

    [Header("Camera")]
    [Tooltip("このエフェクトと同時に再生するカメラシーケンス。不要な場合は未設定にします。")]
    [FormerlySerializedAs("m_cameraSequence")]
    [SerializeField] private CameraSequence m_cameraSequence;        //再生するカメラシーケンス

    [Header("Timeline (Optional)")]
    [Tooltip("設定すると、個別のParticle/Audio/Light/Cameraより優先してこのTimelineを再生します。")]
    [FormerlySerializedAs("m_timeline")]
    [SerializeField] private PlayableAsset m_timeline;               //優先再生するTimeline
    [Tooltip("このTimeline専用のDirector。未設定の場合はEffectSystem共通Directorを使用します。")]
    [SerializeField] private PlayableDirector m_director;             //Timeline専用Director

    [Header("Screen Strobe (Optional)")]
    [Tooltip("演出開始時に画面全体を点滅させます。")]
    [SerializeField] private bool b_m_useStrobe;
    [SerializeField] private Color m_strobeColor;
    [Min(0.05f)] [SerializeField] private float m_strobeDurationSeconds;
    [Min(1)] [SerializeField] private int m_strobeFlashCount;
    [Range(0.0f, 1.0f)] [SerializeField] private float m_strobeMaximumAlpha;

    [Header("Camera Shake (Optional)")]
    [Tooltip("演出開始時に現在のカメラへ加算式の揺れを適用します。")]
    [SerializeField] private bool b_m_useCameraShake;
    [Min(0.05f)] [SerializeField] private float m_cameraShakeDurationSeconds;
    [Min(0.0f)] [SerializeField] private float m_cameraShakeStrength;
    [Min(0.1f)] [SerializeField] private float m_cameraShakeFrequency;

    public string EffectName
    {
        get
        {
            return m_effectName;
        }
    }

    public float PlayDelay
    {
        get
        {
            return Mathf.Max(0.0f, m_playDelaySeconds);
        }
    }

    public ParticleSystem[] Particles
    {
        get
        {
            return m_particles;
        }
    }

    public AudioSource[] AudioSources
    {
        get
        {
            return m_audioSources;
        }
    }

    public LightController[] LightControllers
    {
        get
        {
            return m_lightControllers;
        }
    }

    public Transform PositionRoot
    {
        get
        {
            return m_positionRoot;
        }
    }

    public CameraSequence CameraSequence
    {
        get
        {
            return m_cameraSequence;
        }
    }

    public PlayableAsset Timeline
    {
        get
        {
            return m_timeline;
        }
    }

    public PlayableDirector Director
    {
        get
        {
            return m_director;
        }
    }

    public bool UsesStrobe => b_m_useStrobe;
    public Color StrobeColor => m_strobeColor;
    public float StrobeDuration => Mathf.Max(0.05f, m_strobeDurationSeconds);
    public int StrobeFlashCount => Mathf.Max(1, m_strobeFlashCount);
    public float StrobeMaximumAlpha => Mathf.Clamp01(m_strobeMaximumAlpha);
    public bool UsesCameraShake => b_m_useCameraShake;
    public float CameraShakeDuration => Mathf.Max(0.05f, m_cameraShakeDurationSeconds);
    public float CameraShakeStrength => Mathf.Max(0.0f, m_cameraShakeStrength);
    public float CameraShakeFrequency => Mathf.Max(0.1f, m_cameraShakeFrequency);
}

/// <summary>
/// パーティクル、サウンド、ライト、カメラ、Timelineを名前でまとめて再生します。
/// </summary>
public class EffectSystem : MonoBehaviour
{
    private const float EMinimumDelaySeconds = 0.0f;                 //待ち時間の最小値
    private const double ETimelineStartSeconds = 0.0;                //Timelineの再生開始位置

    [Header("Effects")]
    [Tooltip("個別エフェクトを一元管理するEffectList。")]
    [SerializeField] private EffectList m_effectList;
    [FormerlySerializedAs("m_effectDatas")]
    [FormerlySerializedAs("EffectDatas")]
    [SerializeField] private SEffectData[] m_legacyEffectDatas;      //旧Scene互換用

    [Header("Camera")]
    [Tooltip("Assets/newCamera の CameraSequence を再生するDirector。")]
    [FormerlySerializedAs("m_cameraDirector")]
    [SerializeField] private PoseCameraDirector m_cameraDirector;    //カメラ演出用Director

    [Header("Timeline")]
    [Tooltip("EffectDataに設定したTimelineを再生する共通Director。")]
    [FormerlySerializedAs("m_effectDirector")]
    [SerializeField] private PlayableDirector m_effectDirector;      //Timeline演出用Director

    [Tooltip("揺らすCamera。未設定時はMain Camera、次にScene内Cameraを自動取得します。")]
    [SerializeField] private Camera m_cameraShakeTarget;

    private bool b_m_isPlayEffect = true;                            //対象演出が再生を完了したか
    private readonly HashSet<PlayableDirector> m_playingDirectors =
        new HashSet<PlayableDirector>(); //現在再生中のDirector一覧

    private string m_nowplayEffectName = ""; //旧APIの再生状態確認で照合する、最後に要求されたEffect名
    private Coroutine m_strobeCoroutine;
    private Coroutine m_cameraShakeCoroutine;
    private Image m_strobeImage;
    private Camera m_activeShakeCamera;
    private Vector2 m_cameraProjectionOffset;
    private Matrix4x4 m_originalProjectionMatrix;
    private bool b_m_projectionOffsetApplied;
    private CinemachineBrain m_activeShakeBrain;
    private Vector3 m_appliedCinemachineShakeOffset;

    /// <summary>
    /// Scene読込時に自動再生された演出用Particleを、最初の描画前に停止します。
    /// Music Nodeから明示的に再生されたParticleは以降通常どおり動作します。
    /// </summary>
    private IEnumerator Start()
    {
        yield return null; //全ParticleSystemのPlay On Awake適用を待つ

        ParticleSystem[] sceneParticles = FindObjectsByType<ParticleSystem>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        for (int i = 0; i < sceneParticles.Length; ++i)
        {
            ParticleSystem particle = sceneParticles[i];
            if (particle == null || !particle.main.playOnAwake)continue;

            particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    /// <summary>
    /// 登録されている演出設定を取得します。
    /// </summary>
    public SEffectData[] GetEffectDatas()
    {
        if (m_effectList != null
            && m_effectList.Effects != null
            && m_effectList.Effects.Length > 0)
        {
            return m_effectList.Effects;
        }

        return m_legacyEffectDatas;
    }

    /// <summary>
    /// 既存コード向けの演出完了状態を取得します。
    /// </summary>
    public bool IsPlayEffect()
    {
        return b_m_isPlayEffect;
    }

    /// <summary>
    /// 旧再生入口です。演出経路を限定するため、現在は再生しません。
    /// </summary>
    public void PlayEffect(string _effectname)
    {
        Debug.LogWarning(
            $"「{_effectname}」の直接再生を停止しました。"
            + "MusicNodeまたはConditionalEffectManagerから設定してください。",
            this);
    }

    /// <summary>MusicNodeEditorで設定した|区切りの演出だけを再生します。</summary>
    public void PlayMusicNodeEffects(string _effectNames)
    {
        if (string.IsNullOrWhiteSpace(_effectNames))return;

        string[] effectNames = _effectNames.Split('|');
        for (int i = 0; i < effectNames.Length; ++i)
        {
            string effectName = effectNames[i].Trim();
            if (!string.IsNullOrEmpty(effectName))
            {
                PlayNamedEffect(effectName);
            }
        }
    }

    /// <summary>条件付き演出管理クラスから指定された演出を再生します。</summary>
    public void PlayConditionalEffect(string _effectName)
    {
        PlayNamedEffect(_effectName);
    }

    /// <summary>許可された経路から渡された名前付き演出を再生します。</summary>
    private void PlayNamedEffect(string _effectname)
    {
        SEffectData[] effectDatas = GetEffectDatas();
        if (effectDatas == null)return;

        foreach (SEffectData effectData in effectDatas)
        {
            if (effectData.EffectName != _effectname)continue;

            ScheduleEffect(effectData);

            m_nowplayEffectName = effectData.EffectName;
            return;
        }

        Debug.LogWarning($"EffectSystemに「{_effectname}」という名前の演出がありません。", this);
    }

    /// <summary>
    /// 指定したWorld座標へ演出を配置して再生します。
    /// </summary>
    public void PlayConditionalEffectAt(string _effectname, Vector3 _position)
    {
        SEffectData[] effectDatas = GetEffectDatas();
        if (effectDatas == null)return;

        for (int i = 0; i < effectDatas.Length; ++i)
        {
            if (effectDatas[i].EffectName != _effectname)continue;

            ApplyEffectPosition(effectDatas[i], _position);
            ScheduleEffect(effectDatas[i]);
            m_nowplayEffectName = effectDatas[i].EffectName;
            return;
        }

        Debug.LogWarning($"EffectSystemに「{_effectname}」という名前の演出がありません。", this);
    }

    /// <summary>専用Rootを優先し、未設定時はParticle群の位置を揃えます。</summary>
    private static void ApplyEffectPosition(
        SEffectData _effect,
        Vector3 _position)
    {
        if (_effect.PositionRoot != null)
        {
            _effect.PositionRoot.position = _position;
            return;
        }

        ParticleSystem[] particles = _effect.Particles;
        if (particles == null)return;
        for (int i = 0; i < particles.Length; ++i)
        {
            if (particles[i] != null)
            {
                particles[i].transform.position = _position;
            }
        }
    }

    /// <summary>
    /// 指定された演出が再生中か確認します。
    /// </summary>
    public void IsEffectPlay(string _effectname)
    {
        b_m_isPlayEffect = true;
        SEffectData[] effectDatas = GetEffectDatas();
        if (effectDatas == null)return;

        foreach (SEffectData effectData in effectDatas)
        {
            if (effectData.EffectName != _effectname)continue;

            b_m_isPlayEffect = !HasPlayingEffect(effectData);
            return;
        }
    }

    /// <summary>
    /// 最後に再生要求したEffectの現在状態を確認し、互換用フラグへ反映します。
    /// 引数付き関数と戻り値の意味が異なる既存仕様のため、呼び出し側との互換性を優先しています。
    /// </summary>
    public void IsEffectPlay()
    {
        SEffectData[] effectDatas = GetEffectDatas();
        if (effectDatas == null)return;
        
        if (m_nowplayEffectName == "")
        {
            b_m_isPlayEffect = false; 
            
            return;
        }
        foreach (SEffectData effectData in effectDatas)
        {
            if (effectData.EffectName != m_nowplayEffectName) continue;
            b_m_isPlayEffect = HasPlayingEffect(effectData);
            return;
        }
    }


    /// <summary>
    /// 再生中のカメラ演出を停止します。
    /// </summary>
    public void StopCameraEffect()
    {
        if (m_cameraDirector == null)return;

        m_cameraDirector.StopSequence();
    }

    /// <summary>
    /// EffectSystemが再生しているTimelineを停止します。
    /// </summary>
    public void StopEffectTimeline()
    {
        foreach (PlayableDirector director in m_playingDirectors)
        {
            if (director != null)
            {
                director.Stop();
            }
        }
        m_playingDirectors.Clear();

        if (m_effectDirector != null)
        {
            m_effectDirector.Stop();
        }
    }

    /// <summary>確認中を含む、EffectSystem管理下の演出をすべて停止します。</summary>
    public void StopAllEffects()
    {
        StopEffectTimeline();
        StopCameraEffect();

        if (m_strobeCoroutine != null)
        {
            StopCoroutine(m_strobeCoroutine);
            m_strobeCoroutine = null;
        }
        if (m_strobeImage != null)m_strobeImage.color = Color.clear;

        if (m_cameraShakeCoroutine != null)
        {
            StopCoroutine(m_cameraShakeCoroutine);
            m_cameraShakeCoroutine = null;
        }
        CinemachineCore.CameraUpdatedEvent.RemoveListener(
            ApplyCinemachineCameraShake);
        RestoreCinemachineCameraShake();
        Camera.onPreCull -= ApplyCameraProjectionShake;
        Camera.onPostRender -= RestoreCameraProjection;
        RestoreCameraProjection();
        m_activeShakeBrain = null;
        m_activeShakeCamera = null;
        m_cameraProjectionOffset = Vector2.zero;

        SEffectData[] effectDatas = GetEffectDatas();
        if (effectDatas == null)return;
        for (int i = 0; i < effectDatas.Length; ++i)
        {
            ParticleSystem[] particles = effectDatas[i].Particles;
            if (particles != null)
            {
                for (int particleIndex = 0;
                    particleIndex < particles.Length;
                    ++particleIndex)
                {
                    particles[particleIndex]?.Stop(
                        true,
                        ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }

            AudioSource[] audioSources = effectDatas[i].AudioSources;
            if (audioSources == null)continue;
            for (int audioIndex = 0;
                audioIndex < audioSources.Length;
                ++audioIndex)
            {
                audioSources[audioIndex]?.Stop();
            }
        }
    }

    /// <summary>
    /// 設定された待ち時間を適用して演出を予約します。
    /// </summary>
    private void ScheduleEffect(SEffectData _effects)
    {
        if (_effects.PlayDelay <= EMinimumDelaySeconds)
        {
            EffectPlay(_effects);
            return;
        }

        StartCoroutine(PlayEffectAfterDelay(_effects));
    }

    /// <summary>
    /// 指定時間待ってから演出を再生します。
    /// </summary>
    private IEnumerator PlayEffectAfterDelay(SEffectData _effects)
    {
        yield return new WaitForSeconds(_effects.PlayDelay);
        EffectPlay(_effects);
    }

    /// <summary>
    /// Timelineを優先しつつ、CameraSequenceはTimelineと同時に再生します。
    /// Timeline内に配置済みのParticle、Audio、Lightは二重再生を避けるため個別には開始しません。
    /// </summary>
    private void EffectPlay(SEffectData _effects)
    {
        PlayStrobe(_effects);
        PlayCameraShake(_effects);

        if (_effects.Timeline != null)
        {
            PlayTimeline(_effects.Timeline, _effects.Director);
            PlayCamera(_effects.CameraSequence);
            return;
        }

        PlayParticles(_effects.Particles);
        PlayAudioSources(_effects.AudioSources);
        PlayLights(_effects.LightControllers);
        PlayCamera(_effects.CameraSequence);
    }

    /// <summary>
    /// 指定されたTimelineを先頭から再生します。
    /// Effectごとの専用Directorを優先することで、異なるDirectorのTimelineは同時再生できます。
    /// </summary>
    private void PlayTimeline(
        PlayableAsset _timeline,
        PlayableDirector _director)
    {
        PlayableDirector director = m_effectDirector;
        if (_director != null)
        {
            director = _director;
        }
        if (director == null)
        {
            Debug.LogWarning(
                $"「{_timeline.name}」を再生できません。EffectSystemのEffect Directorを設定してください。",
                this);
            return;
        }

        m_playingDirectors.RemoveWhere(
            playingDirector => playingDirector == null
                || playingDirector.state != PlayState.Playing);
        director.Stop();
        director.playableAsset = _timeline;
        director.time = ETimelineStartSeconds;
        director.Play();
        m_playingDirectors.Add(director);
    }

    /// <summary>
    /// 登録されたパーティクル群を再生します。
    /// </summary>
    private static void PlayParticles(ParticleSystem[] _particles)
    {
        if (_particles == null)return;

        foreach (ParticleSystem particle in _particles)
        {
            if (particle == null)continue;

            particle.Play();
        }
    }

    /// <summary>
    /// 登録された音声群を再生します。
    /// </summary>
    private static void PlayAudioSources(AudioSource[] _audiosources)
    {
        if (_audiosources == null)return;

        foreach (AudioSource audioSource in _audiosources)
        {
            if (audioSource == null)continue;

            audioSource.Play();
        }
    }

    /// <summary>
    /// 登録されたライト制御群を開始します。
    /// </summary>
    private static void PlayLights(LightController[] _lightcontrollers)
    {
        if (_lightcontrollers == null)return;

        foreach (LightController lightController in _lightcontrollers)
        {
            if (lightController == null)continue;

            lightController.Illumination();
        }
    }

    /// <summary>
    /// パーティクル、音声、Timelineのいずれかが再生中か確認します。
    /// </summary>
    private bool HasPlayingEffect(SEffectData _effects)
    {
        if (_effects.Timeline != null)
        {
            PlayableDirector director = m_effectDirector;
            if (_effects.Director != null)
            {
                director = _effects.Director;
            }
            if (director != null && director.state == PlayState.Playing)return true;
        }

        if (_effects.Particles != null)
        {
            foreach (ParticleSystem particle in _effects.Particles)
            {
                if (particle != null && particle.isPlaying)return true;
            }
        }

        if (_effects.AudioSources != null)
        {
            foreach (AudioSource audioSource in _effects.AudioSources)
            {
                if (audioSource != null && audioSource.isPlaying)return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 指定されたカメラシーケンスを再生します。
    /// </summary>
    private void PlayCamera(CameraSequence _camerasequence)
    {
        if (_camerasequence == null)return;

        if (m_cameraDirector == null)
        {
            Debug.LogWarning(
                $"「{_camerasequence.name}」を再生できません。EffectSystemのCamera Directorを設定してください。",
                this);
            return;
        }

        m_cameraDirector.PlaySequence(_camerasequence);
    }

    /// <summary>Effect設定に応じて画面全体のストロボを開始します。</summary>
    private void PlayStrobe(SEffectData _effects)
    {
        if (!_effects.UsesStrobe)return;

        EnsureStrobeImage();
        if (m_strobeImage == null)return;
        if (m_strobeCoroutine != null)StopCoroutine(m_strobeCoroutine);
        m_strobeCoroutine = StartCoroutine(StrobeRoutine(_effects));
    }

    private void EnsureStrobeImage()
    {
        if (m_strobeImage != null)return;

        GameObject canvasObject = new GameObject(
            "Effect Strobe Canvas",
            typeof(RectTransform),
            typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue - 10;

        GameObject imageObject = new GameObject(
            "Effect Strobe",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform imageRect = imageObject.GetComponent<RectTransform>();
        imageRect.anchorMin = Vector2.zero;
        imageRect.anchorMax = Vector2.one;
        imageRect.offsetMin = Vector2.zero;
        imageRect.offsetMax = Vector2.zero;
        m_strobeImage = imageObject.GetComponent<Image>();
        m_strobeImage.raycastTarget = false;
        m_strobeImage.color = Color.clear;
    }

    private IEnumerator StrobeRoutine(SEffectData _effects)
    {
        float elapsed = 0.0f;
        Color color = _effects.StrobeColor;
        while (elapsed < _effects.StrobeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / _effects.StrobeDuration);
            float pulse = Mathf.Abs(Mathf.Sin(
                progress * _effects.StrobeFlashCount * Mathf.PI));
            color.a = pulse * (1.0f - progress) * _effects.StrobeMaximumAlpha;
            m_strobeImage.color = color;
            yield return null;
        }

        m_strobeImage.color = Color.clear;
        m_strobeCoroutine = null;
    }

    /// <summary>Effect設定に応じて現在のカメラへ加算式の揺れを開始します。</summary>
    private void PlayCameraShake(SEffectData _effects)
    {
        if (!_effects.UsesCameraShake || _effects.CameraShakeStrength <= 0.0f)return;

        if (m_cameraShakeCoroutine != null)StopCoroutine(m_cameraShakeCoroutine);
        CinemachineCore.CameraUpdatedEvent.RemoveListener(
            ApplyCinemachineCameraShake);
        RestoreCinemachineCameraShake();
        Camera.onPreCull -= ApplyCameraProjectionShake;
        Camera.onPostRender -= RestoreCameraProjection;
        RestoreCameraProjection();
        m_cameraShakeCoroutine = StartCoroutine(CameraShakeRoutine(_effects));
    }

    private IEnumerator CameraShakeRoutine(SEffectData _effects)
    {
        m_activeShakeCamera = m_cameraShakeTarget != null
            ? m_cameraShakeTarget
            : Camera.main;
        if (m_activeShakeCamera == null)
        {
            m_activeShakeCamera = FindFirstObjectByType<Camera>();
        }
        if (m_activeShakeCamera == null)
        {
            m_cameraShakeCoroutine = null;
            yield break;
        }

        m_activeShakeBrain = m_activeShakeCamera.GetComponent<CinemachineBrain>();
        if (m_activeShakeBrain != null)
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(
                ApplyCinemachineCameraShake);
        }
        else
        {
            Camera.onPreCull += ApplyCameraProjectionShake;
            Camera.onPostRender += RestoreCameraProjection;
        }
        float elapsed = 0.0f;
        float seed = Random.Range(0.0f, 1000.0f);
        while (elapsed < _effects.CameraShakeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / _effects.CameraShakeDuration);
            float amplitude = _effects.CameraShakeStrength * (1.0f - progress);
            float sampleTime = seed + elapsed * _effects.CameraShakeFrequency;
            m_cameraProjectionOffset = new Vector2(
                Mathf.PerlinNoise(sampleTime, 0.0f) * 2.0f - 1.0f,
                Mathf.PerlinNoise(0.0f, sampleTime) * 2.0f - 1.0f)
                * amplitude;
            yield return null;
        }

        CinemachineCore.CameraUpdatedEvent.RemoveListener(
            ApplyCinemachineCameraShake);
        RestoreCinemachineCameraShake();
        Camera.onPreCull -= ApplyCameraProjectionShake;
        Camera.onPostRender -= RestoreCameraProjection;
        RestoreCameraProjection();
        m_activeShakeBrain = null;
        m_activeShakeCamera = null;
        m_cameraProjectionOffset = Vector2.zero;
        m_cameraShakeCoroutine = null;
    }

    /// <summary>
    /// Cinemachine BrainがCameraSequenceを反映した直後に、最終出力Cameraへ揺れを加算します。
    /// </summary>
    private void ApplyCinemachineCameraShake(CinemachineBrain _brain)
    {
        if (_brain == null
            || _brain != m_activeShakeBrain
            || m_activeShakeCamera == null)return;

        RestoreCinemachineCameraShake();
        const float positionScale = 10.0f; //従来の画面比率StrengthをWorld移動量へ変換
        Transform cameraTransform = m_activeShakeCamera.transform;
        m_appliedCinemachineShakeOffset =
            (cameraTransform.right * m_cameraProjectionOffset.x
                + cameraTransform.up * m_cameraProjectionOffset.y)
            * positionScale;
        cameraTransform.position += m_appliedCinemachineShakeOffset;
    }

    private void RestoreCinemachineCameraShake()
    {
        if (m_activeShakeCamera == null
            || m_appliedCinemachineShakeOffset == Vector3.zero)return;

        m_activeShakeCamera.transform.position -=
            m_appliedCinemachineShakeOffset;
        m_appliedCinemachineShakeOffset = Vector3.zero;
    }

    private void ApplyCameraProjectionShake(Camera _camera)
    {
        if (_camera == null || _camera != m_activeShakeCamera)return;

        m_originalProjectionMatrix = _camera.projectionMatrix;
        Matrix4x4 shakenProjection = m_originalProjectionMatrix;
        shakenProjection.m02 += m_cameraProjectionOffset.x;
        shakenProjection.m12 += m_cameraProjectionOffset.y;
        _camera.projectionMatrix = shakenProjection;
        b_m_projectionOffsetApplied = true;
    }

    private void RestoreCameraProjection(Camera _camera)
    {
        if (_camera == null || _camera != m_activeShakeCamera)return;
        RestoreCameraProjection();
    }

    private void RestoreCameraProjection()
    {
        if (!b_m_projectionOffsetApplied || m_activeShakeCamera == null)return;

        m_activeShakeCamera.projectionMatrix = m_originalProjectionMatrix;
        b_m_projectionOffsetApplied = false;
    }

    private void OnDisable()
    {
        CinemachineCore.CameraUpdatedEvent.RemoveListener(
            ApplyCinemachineCameraShake);
        RestoreCinemachineCameraShake();
        Camera.onPreCull -= ApplyCameraProjectionShake;
        Camera.onPostRender -= RestoreCameraProjection;
        RestoreCameraProjection();
        if (m_strobeImage != null)m_strobeImage.color = Color.clear;
    }
}
