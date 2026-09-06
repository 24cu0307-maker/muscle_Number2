using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>画面Canvas上へ一時的な観客コメントを表示します。</summary>
[RequireComponent(typeof(RectTransform))]
public sealed class AudienceReactionCommentBubble : MonoBehaviour
{
    [Header("Canvas Comment Visual")]
    [SerializeField] private Image m_bubbleImage;
    [SerializeField] private Text m_commentText;
    [SerializeField, Min(1)] private int m_fontSize = 40;
    [SerializeField] private float m_textRotationDegrees;
    [SerializeField] private Color m_textColor = Color.black;
    [SerializeField] private Vector2 m_textAnchorMin = new Vector2(0.12f, 0.2f);
    [SerializeField] private Vector2 m_textAnchorMax = new Vector2(0.88f, 0.82f);

    private Coroutine m_hideCoroutine;

    public bool IsVisible => gameObject.activeSelf;

    private void Awake()
    {
        EnsureVisuals();
        gameObject.SetActive(false);
    }

    private void OnValidate()
    {
        ApplyVisualSettings();
    }

    /// <summary>観客個体ではなく、このRectTransformの画面位置へコメントを表示します。</summary>
    public void Show(
        string _comment,
        Sprite _bubbleSprite,
        float _durationSeconds)
    {
        EnsureVisuals();
        if (m_bubbleImage != null)
        {
            m_bubbleImage.sprite = _bubbleSprite;
            m_bubbleImage.preserveAspect = _bubbleSprite != null;
        }
        if (m_commentText != null)
        {
            m_commentText.text = _comment;
        }

        ApplyVisualSettings();
        gameObject.SetActive(true);
        if (m_hideCoroutine != null)StopCoroutine(m_hideCoroutine);
        m_hideCoroutine = StartCoroutine(HideAfterDelay(_durationSeconds));
    }

    /// <summary>Sceneに子UIが未配置でも実行時に補完します。</summary>
    private void EnsureVisuals()
    {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect == null)return;

        if (m_bubbleImage == null)
        {
            GameObject imageObject = new GameObject(
                "BubbleImage",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            RectTransform imageRect = imageObject.GetComponent<RectTransform>();
            imageRect.SetParent(rootRect, false);
            imageRect.anchorMin = Vector2.zero;
            imageRect.anchorMax = Vector2.one;
            imageRect.offsetMin = Vector2.zero;
            imageRect.offsetMax = Vector2.zero;
            m_bubbleImage = imageObject.GetComponent<Image>();
            m_bubbleImage.raycastTarget = false;
        }

        if (m_commentText == null)
        {
            GameObject textObject = new GameObject(
                "Comment",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.SetParent(rootRect, false);
            m_commentText = textObject.GetComponent<Text>();
            m_commentText.font =
                Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            m_commentText.fontStyle = FontStyle.Bold;
            m_commentText.alignment = TextAnchor.MiddleCenter;
            m_commentText.raycastTarget = false;
        }
    }

    /// <summary>Inspectorで編集した文字角度・サイズ・余白を生成済みUIへ反映します。</summary>
    private void ApplyVisualSettings()
    {
        if (m_commentText == null)return;

        m_commentText.fontSize = Mathf.Max(1, m_fontSize);
        m_commentText.color = m_textColor;
        RectTransform textRect = m_commentText.rectTransform;
        textRect.anchorMin = m_textAnchorMin;
        textRect.anchorMax = m_textAnchorMax;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        textRect.localRotation = Quaternion.Euler(0.0f, 0.0f, m_textRotationDegrees);
    }

    private IEnumerator HideAfterDelay(float _durationSeconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, _durationSeconds));
        gameObject.SetActive(false);
        m_hideCoroutine = null;
    }
}
