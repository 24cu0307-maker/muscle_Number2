/*━━━━━━━━━*
*@file ThirdPersonExplanationUI.cs*
*@brief 三人称イベントの説明画像とTimelineの再生開始・停止を制御する*
*@date 2026/09/16*
*最終更新日 2026/09/16*
*━━━━━━━━━*/

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

[Serializable]
public sealed class ThirdPersonExplanationUI
{
    private const int EOverlaySortingOrder = 100;
    private const double ETimelineStartSeconds = 0.0d;
    private static readonly Vector2 EReferenceResolution = new Vector2(1920.0f, 1080.0f);
    private static readonly Vector2 EDefaultSize = new Vector2(1100.0f, 120.0f);
    private static readonly Vector2 EDefaultPosition = new Vector2(0.0f, -360.0f);

    [SerializeField] private bool b_m_enabled = true; //説明画像を使用するか
    [Tooltip("表示する説明画像を指定します。")]
    [SerializeField] private Sprite m_sprite;
    [Tooltip("編集用UIを作成すると自動設定されます。既存のUI Imageも指定できます。")]
    [SerializeField] private Image m_image;
    [Tooltip("画像の動きとフェードを設定するTimelineのDirector。画像UIの外側に配置してください。")]
    [SerializeField] private PlayableDirector m_motionDirector;

    private bool b_m_timelineStarted; //このUIが開始したTimelineだけを停止する

    public void PreparePreview(MonoBehaviour _owner)
    {
        PrepareUI(_owner);
        m_image.gameObject.SetActive(true);
    }

    public void Show(MonoBehaviour _owner)
    {
        StopMotionTimeline();
        if (!b_m_enabled)
        {
            HideImmediately(_owner);
            return;
        }
        PrepareUI(_owner);
        if (m_image.sprite == null)
        {
            m_image.gameObject.SetActive(false);
            Debug.LogWarning("[ThirdPersonExplanationUI] 表示用画像を設定してください。", _owner);
            return;
        }
        m_image.gameObject.SetActive(true);
        if (m_motionDirector != null && m_motionDirector.playableAsset != null)
        {
            //ゲーム時間停止中も、Timelineで設定した動きとフェードを進めます。
            m_motionDirector.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
            m_motionDirector.extrapolationMode = DirectorWrapMode.Hold;
            m_motionDirector.time = ETimelineStartSeconds;
            m_motionDirector.Play();
            m_motionDirector.Evaluate();
            b_m_timelineStarted = true;
        }
    }

    public void Hide(MonoBehaviour _owner)
    {
        HideImmediately(_owner);
    }

    public void HideImmediately(MonoBehaviour _owner)
    {
        StopMotionTimeline();
        if (m_image != null)
        {
            m_image.gameObject.SetActive(false);
        }
    }

    private void StopMotionTimeline()
    {
        if (b_m_timelineStarted && m_motionDirector != null)
        {
            m_motionDirector.Stop();
        }
        b_m_timelineStarted = false;
    }

    private void PrepareUI(MonoBehaviour _owner)
    {
        if (m_image == null)
        {
            GameObject canvasObject = new GameObject(
                "ThirdPersonExplanationCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));
            canvasObject.transform.SetParent(_owner.transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = EOverlaySortingOrder;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = EReferenceResolution;
            GameObject imageObject = new GameObject(
                "Explanation",
                typeof(RectTransform),
                typeof(CanvasGroup),
                typeof(Image),
                typeof(Animator));
            m_image = imageObject.GetComponent<Image>();
            m_image.rectTransform.SetParent(canvasObject.transform, false);
            m_image.rectTransform.anchoredPosition = EDefaultPosition;
            m_image.rectTransform.sizeDelta = EDefaultSize;
            imageObject.SetActive(false);
        }
        if (m_sprite != null)
        {
            m_image.sprite = m_sprite;
        }
        m_image.raycastTarget = false;
        m_image.preserveAspect = true;
        if (m_image.GetComponent<Animator>() == null)
        {
            m_image.gameObject.AddComponent<Animator>();
        }
        CanvasGroup group = m_image.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = m_image.gameObject.AddComponent<CanvasGroup>();
        }
        group.blocksRaycasts = false;
        group.interactable = false;
    }
}
