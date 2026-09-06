using System;
using UnityEngine;

public enum EPoseMatchGrade
{
    Perfect,
    Great,
    Miss
}

[Serializable]
public sealed class SPoseGradeFeedback
{
    public AudioClip m_sound;
    [Range(0.0f, 1.0f)] public float m_volume = 1.0f;
    public SPoseParticleEmission[] m_particles =
        Array.Empty<SPoseParticleEmission>();
}

[Serializable]
public sealed class SPoseParticleEmission
{
    public ParticleSystem m_particlePrefab;
    [Tooltip("プレイヤー原点を基準にした生成位置です。")]
    public Vector3 m_localPosition;
    [Tooltip("プレイヤーの向きを基準にした生成角度です。")]
    public Vector3 m_localEulerAngles;
    [Tooltip("ParticleSystemプレハブに掛ける倍率です。")]
    public Vector3 m_localScale = Vector3.one;
    [Tooltip("再生中もプレイヤーの移動へ追従させます。")]
    public bool b_m_followPlayer;
    [Min(0.1f)] public float m_destroyAfterSeconds = 5.0f;
}

/// <summary>Pose判定結果に応じた音とEffectの出力だけを担当します。</summary>
public sealed class PoseJudgementFeedbackPlayer : MonoBehaviour
{
    [SerializeField] private Transform m_playerOrigin;

    [Header("段階別の音とエフェクト")]
    [SerializeField] private SPoseGradeFeedback m_perfectFeedback =
        new SPoseGradeFeedback();
    [SerializeField] private SPoseGradeFeedback m_greatFeedback =
        new SPoseGradeFeedback();
    [SerializeField] private SPoseGradeFeedback m_missFeedback =
        new SPoseGradeFeedback();
    [SerializeField] private AudioSource m_audioSource;

    private void Awake()
    {
        FindPlayerOrigin();
        PrepareAudioSource();
    }

    public void Play(EPoseMatchGrade _grade)
    {
        switch (_grade)
        {
            case EPoseMatchGrade.Perfect:
                PlayFeedback(m_perfectFeedback);
                break;
            case EPoseMatchGrade.Great:
                PlayFeedback(m_greatFeedback);
                break;
            default:
                PlayFeedback(m_missFeedback);
                break;
        }
    }

    [ContextMenu("Test Perfect Feedback")]
    private void TestPerfectFeedback()
    {
        if (Application.isPlaying)Play(EPoseMatchGrade.Perfect);
    }

    [ContextMenu("Test Great Feedback")]
    private void TestGreatFeedback()
    {
        if (Application.isPlaying)Play(EPoseMatchGrade.Great);
    }

    [ContextMenu("Test Miss Feedback")]
    private void TestMissFeedback()
    {
        if (Application.isPlaying)Play(EPoseMatchGrade.Miss);
    }

    private void PlayFeedback(SPoseGradeFeedback _feedback)
    {
        if (_feedback == null)return;

        if (m_audioSource != null && _feedback.m_sound != null)
        {
            m_audioSource.PlayOneShot(
                _feedback.m_sound,
                Mathf.Clamp01(_feedback.m_volume));
        }

        PlayParticles(_feedback.m_particles);
    }

    /// <summary>プレイヤー座標を基準に設定されたParticleSystemを生成します。</summary>
    private void PlayParticles(SPoseParticleEmission[] _particles)
    {
        if (_particles == null || _particles.Length == 0)return;
        FindPlayerOrigin();
        if (m_playerOrigin == null)return;

        for (int i = 0; i < _particles.Length; ++i)
        {
            SPoseParticleEmission setting = _particles[i];
            if (setting == null || setting.m_particlePrefab == null)continue;

            ParticleSystem instance = Instantiate(setting.m_particlePrefab);
            Transform particleTransform = instance.transform;
            if (setting.b_m_followPlayer)
            {
                particleTransform.SetParent(m_playerOrigin, false);
                particleTransform.localPosition = setting.m_localPosition;
                particleTransform.localRotation =
                    Quaternion.Euler(setting.m_localEulerAngles);
            }
            else
            {
                particleTransform.position =
                    m_playerOrigin.TransformPoint(setting.m_localPosition);
                particleTransform.rotation = m_playerOrigin.rotation
                    * Quaternion.Euler(setting.m_localEulerAngles);
            }

            particleTransform.localScale = setting.m_localScale;
            instance.Play(true);
            Destroy(
                instance.gameObject,
                Mathf.Max(0.1f, setting.m_destroyAfterSeconds));
        }
    }

    private void FindPlayerOrigin()
    {
        if (m_playerOrigin != null)return;

        GameObject playerObject = GameObject.Find("Macho_Base_CLEAN");
        if (playerObject == null)
        {
            playerObject = GameObject.Find("macho_Rig");
        }
        if (playerObject != null)
        {
            m_playerOrigin = playerObject.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (m_playerOrigin == null)return;

        DrawParticleGizmos(m_perfectFeedback, Color.yellow);
        DrawParticleGizmos(m_greatFeedback, Color.cyan);
        DrawParticleGizmos(m_missFeedback, Color.red);
    }

    private void DrawParticleGizmos(
        SPoseGradeFeedback _feedback,
        Color _color)
    {
        if (_feedback?.m_particles == null)return;

        Gizmos.color = _color;
        for (int i = 0; i < _feedback.m_particles.Length; ++i)
        {
            SPoseParticleEmission setting = _feedback.m_particles[i];
            if (setting == null)continue;

            Vector3 position = m_playerOrigin.TransformPoint(setting.m_localPosition);
            Gizmos.DrawWireSphere(position, 0.25f);
            Gizmos.DrawLine(m_playerOrigin.position, position);
        }
    }

    private void PrepareAudioSource()
    {
        if (m_audioSource == null)
        {
            m_audioSource = gameObject.AddComponent<AudioSource>();
        }

        m_audioSource.playOnAwake = false;
        m_audioSource.spatialBlend = 0.0f;
    }
}
