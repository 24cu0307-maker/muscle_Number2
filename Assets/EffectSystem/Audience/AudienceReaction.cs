/*━━━━━━━━━*
*@file AudienceReaction.cs*
*@brief 観客Objectへ複数種類のリアクションを適用する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks AnimatorがなくてもTransform Animationで動作*
*━━━━━━━━━*/

using System.Collections;
using UnityEngine;

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

    private Vector3 m_baseLocalPosition; //基準位置
    private Quaternion m_baseLocalRotation; //基準回転
    private Vector3 m_baseLocalScale; //基準Scale
    private Coroutine m_reactionCoroutine; //現在の動作

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
        if (m_reactionCoroutine != null)
        {
            StopCoroutine(m_reactionCoroutine);
        }

        ResetTransform();
        m_reactionCoroutine = StartCoroutine(
            PlayReactionRoutine(
                _reaction,
                Mathf.Max(0.0f, _strength)));
    }

    /// <summary>
    /// リアクションを時間補間して再生します。
    /// </summary>
    private IEnumerator PlayReactionRoutine(
        EAudienceReaction _reaction,
        float _strength)
    {
        float duration = Mathf.Max(EMinimumDuration, m_duration); //安全な動作時間
        float elapsedSeconds = 0.0f; //経過時間
        while (elapsedSeconds < duration)
        {
            elapsedSeconds += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedSeconds / duration); //進行率
            float wave = Mathf.Sin(progress * EFullCycleRadians); //周期波形
            ApplyReaction(
                _reaction,
                progress,
                wave,
                _strength);
            yield return null;
        }

        ResetTransform();
        m_reactionCoroutine = null;
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
