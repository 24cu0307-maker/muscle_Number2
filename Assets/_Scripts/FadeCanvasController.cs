/*━━━━━━━━━*
*@file FadeCanvasController.cs*
*@brief 黒画像の透明度を外部から制御する画面フェード*
*@date 2026/09/18*
*━━━━━━━━━*/

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed class FadeCanvasController : MonoBehaviour
{
    private const float EDefaultDurationSeconds = 0.5f;
    private const float ETransparentAlpha = 0.0f;
    private const float EOpaqueAlpha = 1.0f;

    [SerializeField] private Image m_blackImage; //FadeCanvas内の黒画像
    [Min(0.0f)] [SerializeField] private float m_durationSeconds = EDefaultDurationSeconds;
    [SerializeField] private bool b_m_startBlack; //起動時に画面を黒く覆うか

    private Coroutine m_fadeCoroutine;

    public float DurationSeconds
    {
        get
        {
            return m_durationSeconds;
        }
        set
        {
            m_durationSeconds = Mathf.Max(0.0f, value);
        }
    }

    public bool IsFading
    {
        get
        {
            return m_fadeCoroutine != null;
        }
    }

    private void Awake()
    {
        if (m_blackImage == null)
        {
            m_blackImage = GetComponentInChildren<Image>(true);
        }
        if (m_blackImage == null)
        {
            Debug.LogWarning("[FadeCanvasController] 黒画像のImageを設定してください。", this);
            return;
        }
        float initialAlpha = ETransparentAlpha;
        if (b_m_startBlack)
        {
            initialAlpha = EOpaqueAlpha;
        }
        SetAlpha(initialAlpha);
    }

    /// <summary>trueで黒く覆い、falseで黒を消して画面を表示します。</summary>
    public void Fade(bool _toBlack)
    {
        if (m_blackImage == null || !isActiveAndEnabled) { return; }
        if (m_fadeCoroutine != null)
        {
            StopCoroutine(m_fadeCoroutine);
            m_fadeCoroutine = null;
        }
        float targetAlpha = ETransparentAlpha;
        if (_toBlack)
        {
            targetAlpha = EOpaqueAlpha;
        }
        m_blackImage.gameObject.SetActive(true);
        if (m_durationSeconds <= 0.0f)
        {
            SetAlpha(targetAlpha);
            return;
        }
        m_fadeCoroutine = StartCoroutine(FadeRoutine(targetAlpha));
    }

    private IEnumerator FadeRoutine(float _targetAlpha)
    {
        float startAlpha = m_blackImage.color.a;
        float elapsedSeconds = 0.0f;
        float durationSeconds = m_durationSeconds;
        while (elapsedSeconds < durationSeconds)
        {
            //ゲームの一時停止中もフェードを継続できるよう、実時間を使います。
            elapsedSeconds += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(
                startAlpha,
                _targetAlpha,
                Mathf.Clamp01(elapsedSeconds / durationSeconds)));
            yield return null;
        }
        SetAlpha(_targetAlpha);
        m_fadeCoroutine = null;
    }

    private void SetAlpha(float _alpha)
    {
        Color color = m_blackImage.color;
        color.a = _alpha;
        m_blackImage.color = color;
    }

    private void OnDisable()
    {
        if (m_fadeCoroutine != null)
        {
            StopCoroutine(m_fadeCoroutine);
            m_fadeCoroutine = null;
        }
    }
}
