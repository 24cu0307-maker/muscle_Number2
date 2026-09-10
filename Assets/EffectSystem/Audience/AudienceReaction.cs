/*━━━━━━━━━*
*@file AudienceReaction.cs*
*@brief 観客Objectへ複数種類のリアクションを適用する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks 弱いTransform Animationへ任意のAnimation Clipを重ねて再生可能*
*━━━━━━━━━*/

using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>
/// 観客が使用するリアクション種類です。
/// </summary>
public enum EAudienceReaction
{
    Jump,
    Sway,
    Cheer,
    Bounce,
    Disappointed
}

[System.Serializable]
public sealed class AudienceReactionAnimationSet
{
    [SerializeField] private AnimationClip m_jump;
    [SerializeField] private AnimationClip m_sway;
    [SerializeField] private AnimationClip m_cheer;
    [SerializeField] private AnimationClip m_bounce;
    [SerializeField] private AnimationClip m_disappointed;

    public AnimationClip GetClip(EAudienceReaction _reaction)
    {
        switch (_reaction)
        {
            case EAudienceReaction.Jump:
                return m_jump;
            case EAudienceReaction.Sway:
                return m_sway;
            case EAudienceReaction.Cheer:
                return m_cheer;
            case EAudienceReaction.Bounce:
                return m_bounce;
            case EAudienceReaction.Disappointed:
                return m_disappointed;
        }

        return null;
    }
}

/// <summary>
/// 観客Objectを簡易Animationさせます。
/// </summary>
public sealed class AudienceReaction : MonoBehaviour
{
    private const float EMinimumDuration = 0.1f; //最短動作時間
    private const float EFullCycleRadians = Mathf.PI * 2.0f; //一周期

    [SerializeField] private float m_jumpHeight = 0.35f; //Jump高さ
    [SerializeField] private float m_swayAngle = 12.0f; //横揺れ角度
    [SerializeField] private float m_scaleAmount = 0.08f; //拡縮量
    [SerializeField] private float m_duration = 0.75f; //一回の長さ

    [Header("Reaction Animation")]
    [SerializeField, Range(0.0f, 1.0f)]
    private float m_proceduralStrengthMultiplier = 0.35f;
    [SerializeField, Range(0.0f, 1.0f)] private float m_animationWeight = 1.0f;
    [SerializeField] private Animator m_animator;
    [SerializeField] private AudienceReactionAnimationSet m_animationSet =
        new AudienceReactionAnimationSet();

    private Vector3 m_baseLocalPosition; //基準位置
    private Quaternion m_baseLocalRotation; //基準回転
    private Vector3 m_baseLocalScale; //基準Scale
    private Coroutine m_reactionCoroutine; //現在の動作
    private PlayableGraph m_animationGraph; //現在のAnimation Clip再生Graph
    private static AudiencePreferenceSystem s_preferenceSystem; //全観客で共有する好み管理元

    /// <summary>
    /// 基準Transformを保存します。
    /// </summary>
    private void Awake()
    {
        CaptureCurrentTransform();
    }

    public void CaptureCurrentTransform()
    {
        m_baseLocalPosition = transform.localPosition;
        m_baseLocalRotation = transform.localRotation;
        m_baseLocalScale = transform.localScale;
    }

    /// <summary>生成元で設定した共通Animationをこの観客へ適用します。</summary>
    public void ConfigureAnimation(
        float _proceduralStrengthMultiplier,
        float _animationWeight,
        AudienceReactionAnimationSet _animationSet)
    {
        m_proceduralStrengthMultiplier =
            Mathf.Clamp01(_proceduralStrengthMultiplier);
        m_animationWeight = Mathf.Clamp01(_animationWeight);
        m_animationSet = _animationSet;
        if (m_animator == null)
        {
            m_animator = GetComponentInChildren<Animator>(true);
        }
    }

    /// <summary>
    /// 指定種類のリアクションを開始します。
    /// </summary>
    public void PlayReaction(EAudienceReaction _reaction)
    {
        PlayReaction(_reaction, 1.0f);
    }

    /// <summary>
    /// 指定種類のリアクションを強度付きで開始します。
    /// </summary>
    public void PlayReaction(
        EAudienceReaction _reaction,
        float _strength)
    {
        if (s_preferenceSystem == null)
        {
            s_preferenceSystem = FindFirstObjectByType<AudiencePreferenceSystem>();
        }
        if (s_preferenceSystem != null)
        {
            s_preferenceSystem.ApplyAmbientPreference(
                this,
                ref _reaction,
                ref _strength);
        }

        if (m_reactionCoroutine != null)
        {
            StopCoroutine(m_reactionCoroutine);
        }

        StopConfiguredAnimation();
        ResetTransform();
        float animationDuration = PlayConfiguredAnimation(
            _reaction,
            Mathf.Max(0.0f, _strength));
        m_reactionCoroutine = StartCoroutine(
            PlayReactionRoutine(
                _reaction,
                Mathf.Max(0.0f, _strength)
                    * m_proceduralStrengthMultiplier,
                animationDuration));
    }

    /// <summary>
    /// リアクションを時間補間して再生します。
    /// </summary>
    private IEnumerator PlayReactionRoutine(
        EAudienceReaction _reaction,
        float _strength,
        float _animationDuration)
    {
        float proceduralDuration =
            Mathf.Max(EMinimumDuration, m_duration); //従来動作の再生時間
        float duration = Mathf.Max(
            proceduralDuration,
            _animationDuration); //両方の動作が終わるまで待つ時間
        float elapsedSeconds = 0.0f; //経過時間
        bool b_proceduralComplete = false;
        while (elapsedSeconds < duration)
        {
            elapsedSeconds += Time.deltaTime;
            if (elapsedSeconds < proceduralDuration)
            {
                float progress =
                    Mathf.Clamp01(elapsedSeconds / proceduralDuration); //進行率
                float wave = Mathf.Sin(progress * EFullCycleRadians); //周期波形
                ApplyReaction(
                    _reaction,
                    progress,
                    wave,
                    _strength);
            }
            else if (!b_proceduralComplete)
            {
                ResetTransform();
                b_proceduralComplete = true;
            }
            yield return null;
        }

        ResetTransform();
        StopConfiguredAnimation();
        m_reactionCoroutine = null;
    }

    /// <summary>設定されたClipをAnimatorへ一度だけ出力します。</summary>
    private float PlayConfiguredAnimation(
        EAudienceReaction _reaction,
        float _strength)
    {
        if (m_animationSet == null)return 0.0f;

        AnimationClip clip = m_animationSet.GetClip(_reaction);
        if (clip == null)return 0.0f;
        if (m_animator == null)
        {
            m_animator = GetComponentInChildren<Animator>(true);
        }
        if (m_animator == null)return 0.0f;

        m_animationGraph = PlayableGraph.Create(
            $"AudienceReaction_{GetInstanceID()}");
        m_animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        AnimationClipPlayable clipPlayable =
            AnimationClipPlayable.Create(m_animationGraph, clip);
        clipPlayable.SetApplyFootIK(false);
        clipPlayable.SetApplyPlayableIK(false);
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(
            m_animationGraph,
            "Audience Reaction",
            m_animator);
        output.SetSourcePlayable(clipPlayable);
        output.SetWeight(m_animationWeight * Mathf.Clamp01(_strength));
        m_animationGraph.Play();
        return clip.length;
    }

    private void StopConfiguredAnimation()
    {
        if (!m_animationGraph.IsValid())return;

        m_animationGraph.Destroy();
    }

    /// <summary>
    /// リアクション種類に応じたTransformを適用します。
    /// </summary>
    private void ApplyReaction(
        EAudienceReaction _reaction,
        float _progress,
        float _wave,
        float _strength)
    {
        switch (_reaction)
        {
            case EAudienceReaction.Jump:
                transform.localPosition =
                    m_baseLocalPosition
                    + Vector3.up
                    * Mathf.Sin(_progress * Mathf.PI)
                    * m_jumpHeight
                    * _strength;
                break;
            case EAudienceReaction.Sway:
                transform.localRotation =
                    m_baseLocalRotation
                    * Quaternion.Euler(
                        0.0f,
                        0.0f,
                        _wave * m_swayAngle * _strength);
                break;
            case EAudienceReaction.Cheer:
                transform.localPosition =
                    m_baseLocalPosition
                    + Vector3.up
                    * Mathf.Abs(_wave)
                    * m_jumpHeight
                    * _strength;
                transform.localRotation =
                    m_baseLocalRotation
                    * Quaternion.Euler(
                        _wave * m_swayAngle * _strength,
                        0.0f,
                        0.0f);
                break;
            case EAudienceReaction.Bounce:
                transform.localScale =
                    m_baseLocalScale
                    * (1.0f + Mathf.Abs(_wave) * m_scaleAmount * _strength);
                break;
            case EAudienceReaction.Disappointed:
                float disappointment =
                    Mathf.Sin(_progress * Mathf.PI) * _strength; //落胆進行量
                transform.localPosition =
                    m_baseLocalPosition
                    + Vector3.down * m_jumpHeight * 0.2f * disappointment;
                transform.localRotation =
                    m_baseLocalRotation
                    * Quaternion.Euler(
                        m_swayAngle * 0.65f * disappointment,
                        0.0f,
                        0.0f);
                break;
        }
    }

    /// <summary>
    /// 画面外無効化などで動作が中断された場合にTransformを戻します。
    /// </summary>
    private void OnDisable()
    {
        if (m_reactionCoroutine != null)
        {
            StopCoroutine(m_reactionCoroutine);
            m_reactionCoroutine = null;
        }

        StopConfiguredAnimation();
        ResetTransform();
    }

    /// <summary>
    /// Transformを生成時の状態へ戻します。
    /// </summary>
    private void ResetTransform()
    {
        transform.localPosition = m_baseLocalPosition;
        transform.localRotation = m_baseLocalRotation;
        transform.localScale = m_baseLocalScale;
    }
}
