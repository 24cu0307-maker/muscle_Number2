/*━━━━━━━━━*
*@file EffectSystem.cs*
*@brief 名前で管理された演出とTimelineを再生する*
*@author 24CU0000 Name*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks 既存のPlayEffect APIとの互換性を維持*
*━━━━━━━━━*/

using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;

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
}

/// <summary>
/// パーティクル、サウンド、ライト、カメラ、Timelineを名前でまとめて再生します。
/// </summary>
public class EffectSystem : MonoBehaviour
{
    private const float EMinimumDelaySeconds = 0.0f;                 //待ち時間の最小値
    private const double ETimelineStartSeconds = 0.0;                //Timelineの再生開始位置
    private const int EEmptyEffectCount = 0;                         //演出が未登録の状態

    [Header("Effects")]
    [FormerlySerializedAs("EffectDatas")]
    [SerializeField] private SEffectData[] m_effectDatas;            //登録された演出設定群

    [Header("Camera")]
    [Tooltip("Assets/newCamera の CameraSequence を再生するDirector。")]
    [FormerlySerializedAs("m_cameraDirector")]
    [SerializeField] private PoseCameraDirector m_cameraDirector;    //カメラ演出用Director

    [Header("Timeline")]
    [Tooltip("EffectDataに設定したTimelineを再生する共通Director。")]
    [FormerlySerializedAs("m_effectDirector")]
    [SerializeField] private PlayableDirector m_effectDirector;      //Timeline演出用Director

    private bool b_m_isPlayEffect = true;                            //対象演出が再生を完了したか
    private PlayableDirector m_playingDirector;                      //現在再生中のDirector

    private string m_nowplayEffectName = "";

    /// <summary>
    /// 登録されている演出設定を取得します。
    /// </summary>
    public SEffectData[] GetEffectDatas()
    {
        return m_effectDatas;
    }

    /// <summary>
    /// 既存コード向けの演出完了状態を取得します。
    /// </summary>
    public bool IsPlayEffect()
    {
        return b_m_isPlayEffect;
    }

    /// <summary>
    /// 指定された名前の演出を再生します。
    /// </summary>
    public void PlayEffect(string _effectname)
    {
        if (m_effectDatas == null)return;

        foreach (SEffectData effectData in m_effectDatas)
        {
            if (effectData.EffectName != _effectname)continue;

            ScheduleEffect(effectData);

            m_nowplayEffectName = effectData.EffectName;
            return;
        }

        Debug.LogWarning($"EffectSystemに「{_effectname}」という名前の演出がありません。", this);
    }

    /// <summary>
    /// 登録済みの演出からランダムに一つ再生します。
    /// </summary>
    public void PlayRandomEffect()
    {
        if (m_effectDatas == null || m_effectDatas.Length == EEmptyEffectCount)return;

        int randomIndex = Random.Range(0, m_effectDatas.Length);     //ランダムに選択した演出番号
        ScheduleEffect(m_effectDatas[randomIndex]);

        m_nowplayEffectName =  m_effectDatas[randomIndex].EffectName;
    }

    /// <summary>
    /// 指定された演出が再生中か確認します。
    /// </summary>
    public void IsEffectPlay(string _effectname)
    {
        b_m_isPlayEffect = true;
        if (m_effectDatas == null)return;

        foreach (SEffectData effectData in m_effectDatas)
        {
            if (effectData.EffectName != _effectname)continue;

            b_m_isPlayEffect = !HasPlayingEffect(effectData);
            return;
        }
    }

    public void IsEffectPlay()
    {
        if (m_effectDatas == null) return;
        
        if (m_nowplayEffectName == "")
        {
            b_m_isPlayEffect = false; 
            
            return;
        }
        foreach (SEffectData effectData in m_effectDatas)
        {
            if (effectData.EffectName != m_nowplayEffectName) continue;
            Debug.Log("{+++}" + 
            HasPlayingEffect(effectData));

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
        if (m_playingDirector != null)
        {
            m_playingDirector.Stop();
            m_playingDirector = null;
            return;
        }

        if (m_effectDirector == null)return;

        m_effectDirector.Stop();
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
    /// Timelineを優先し、未設定の場合は従来形式の演出を再生します。
    /// </summary>
    private void EffectPlay(SEffectData _effects)
    {
        if (_effects.Timeline != null)
        {
            PlayTimeline(_effects.Timeline, _effects.Director);
            return;
        }

        PlayParticles(_effects.Particles);
        PlayAudioSources(_effects.AudioSources);
        PlayLights(_effects.LightControllers);
        PlayCamera(_effects.CameraSequence);
    }

    /// <summary>
    /// 指定されたTimelineを先頭から再生します。
    /// </summary>
    private void PlayTimeline(
        PlayableAsset _timeline,
        PlayableDirector _director)
    {
        PlayableDirector director =
            _director != null ? _director : m_effectDirector; //今回使用するDirector
        if (director == null)
        {
            Debug.LogWarning(
                $"「{_timeline.name}」を再生できません。EffectSystemのEffect Directorを設定してください。",
                this);
            return;
        }

        if (m_playingDirector != null && m_playingDirector != director)
        {
            m_playingDirector.Stop();
        }

        director.Stop();
        director.playableAsset = _timeline;
        director.time = ETimelineStartSeconds;
        director.Play();
        m_playingDirector = director;
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
        PlayableDirector director =
            _effects.Director != null
                ? _effects.Director
                : m_effectDirector; //確認対象Director
        if (director != null && director.state == PlayState.Playing)return true;

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
}
