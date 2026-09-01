using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>観客の頭上へ一時的な吹き出しコメントを表示します。</summary>
public sealed class AudienceReactionCommentBubble : MonoBehaviour
{
    private GameObject m_bubbleRoot;
    private Coroutine m_hideCoroutine;
    private Vector3 m_worldOffset;

    public void Show(
        string _comment,
        Sprite _bubbleSprite,
        Vector3 _worldOffset,
        Vector2 _bubbleSize,
        float _canvasScale,
        float _durationSeconds)
    {
        EnsureBubble(_bubbleSprite, _bubbleSize, _canvasScale);
        if (m_bubbleRoot == null)return;

        m_worldOffset = _worldOffset;
        m_bubbleRoot.transform.position = transform.position + m_worldOffset;
        Text commentText = m_bubbleRoot.GetComponentInChildren<Text>(true);
        if (commentText != null)commentText.text = _comment;
        m_bubbleRoot.SetActive(true);
        FaceCamera();

        if (m_hideCoroutine != null)StopCoroutine(m_hideCoroutine);
        m_hideCoroutine = StartCoroutine(HideAfterDelay(_durationSeconds));
    }

    private void LateUpdate()
    {
        if (m_bubbleRoot == null || !m_bubbleRoot.activeSelf)return;
        m_bubbleRoot.transform.position = transform.position + m_worldOffset;
        FaceCamera();
    }

    private void EnsureBubble(
        Sprite _bubbleSprite,
        Vector2 _bubbleSize,
        float _canvasScale)
    {
        if (m_bubbleRoot != null)return;

        m_bubbleRoot = new GameObject(
            "ReactionCommentBubble",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        RectTransform rootRect = m_bubbleRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = _bubbleSize;
        rootRect.localScale = Vector3.one * Mathf.Max(0.0001f, _canvasScale);
        Canvas canvas = m_bubbleRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;

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
        Image image = imageObject.GetComponent<Image>();
        image.sprite = _bubbleSprite;
        image.color = Color.white;
        image.preserveAspect = _bubbleSprite != null;
        image.raycastTarget = false;

        GameObject textObject = new GameObject(
            "Comment",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Text));
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.SetParent(rootRect, false);
        textRect.anchorMin = new Vector2(0.12f, 0.2f);
        textRect.anchorMax = new Vector2(0.88f, 0.82f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        Text text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 40;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.black;
        text.raycastTarget = false;
        m_bubbleRoot.SetActive(false);
    }

    private void FaceCamera()
    {
        Camera targetCamera = Camera.main;
        if (targetCamera != null)
        {
            m_bubbleRoot.transform.rotation = targetCamera.transform.rotation;
        }
    }

    private IEnumerator HideAfterDelay(float _durationSeconds)
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, _durationSeconds));
        if (m_bubbleRoot != null)m_bubbleRoot.SetActive(false);
        m_hideCoroutine = null;
    }

    private void OnDestroy()
    {
        if (m_bubbleRoot != null)Destroy(m_bubbleRoot);
    }
}
