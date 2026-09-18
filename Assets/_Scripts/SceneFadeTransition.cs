using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

/// <summary>画面切替を黒フェードで覆います。認識用のAdditiveロードには使用しません。</summary>
public sealed class SceneFadeTransition : MonoBehaviour
{
    private const string ECanvasName = "FadeCanvas";
    private const string EImageName = "BlackImage";
    private const int EOverlayOrder = short.MaxValue;
    private const string EStartupLoadingScreenName = "StartupLoadingScreen";
    private const float EOpaqueAlpha = 1.0f;
    private const float ETransparentAlpha = 0.0f;
    private const float ETransitionFadeSeconds = 1.0f;
    private static SceneFadeTransition m_instance;
    private FadeCanvasController m_fade;
    private Image m_image;
    private bool b_m_isTransitioning;
    private readonly List<TransitionAudio> m_transitionAudio = new List<TransitionAudio>();

    private sealed class TransitionAudio
    {
        public AudioSource m_source;
        public float m_initialVolume;
    }

    public static bool IsTransitioning
    {
        get
        {
            return m_instance != null && m_instance.b_m_isTransitioning;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        m_instance = null;
    }

    public static void LoadScene(string _sceneName)
    {
        if (string.IsNullOrWhiteSpace(_sceneName)) { return; }
        if (!Application.CanStreamedLevelBeLoaded(_sceneName))
        {
            Debug.LogWarning("[SceneFadeTransition] ビルド対象にないシーンです: " + _sceneName);
            return;
        }
        EnsureInstance();
        if (m_instance.b_m_isTransitioning) { return; }
        m_instance.StartCoroutine(m_instance.Transition(_sceneName));
    }

    private static void EnsureInstance()
    {
        if (m_instance != null) { return; }
        GameObject owner = new GameObject(ECanvasName);
        DontDestroyOnLoad(owner);
        Canvas canvas = owner.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = EOverlayOrder;
        owner.AddComponent<GraphicRaycaster>();
        GameObject black = new GameObject(
            EImageName,
            typeof(RectTransform),
            typeof(Image));
        black.transform.SetParent(owner.transform, false);
        RectTransform rect = black.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        Image image = black.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
        m_instance = owner.AddComponent<SceneFadeTransition>();
        m_instance.m_image = image;
        m_instance.m_fade = owner.AddComponent<FadeCanvasController>();
        m_instance.m_fade.DurationSeconds = ETransitionFadeSeconds;
    }

    private IEnumerator Transition(string _sceneName)
    {
        b_m_isTransitioning = true;
        m_image.raycastTarget = true;
        CaptureSceneAudio();
        m_fade.Fade(true);
        while (m_fade.IsFading)
        {
            yield return null;
        }
        ApplyAudioFade();
        AsyncOperation operation = null;
        try
        {
            operation = UnitySceneManager.LoadSceneAsync(_sceneName);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
        }
        if (operation != null)
        {
            yield return operation;
            //遷移元の音量だけを扱い、遷移先のBGM再生開始には干渉しません。
            RestoreSceneAudio();
            if (HasOpaqueStartupLoadingScreen())
            {
                //既存のロード画面が黒を保持し、初期化完了後のフェードも担当します。
                //共通側で再びフェードすると、開始Timelineを覆ってしまうため引き継ぎます。
                Color color = m_image.color;
                color.a = ETransparentAlpha;
                m_image.color = color;
                m_image.raycastTarget = false;
                b_m_isTransitioning = false;
                yield break;
            }
            //遷移先のStartが実行されてから黒を消します。
            yield return null;
        }
        m_fade.Fade(false);
        while (m_fade.IsFading)
        {
            yield return null;
        }
        m_image.raycastTarget = false;
        RestoreSceneAudio();
        b_m_isTransitioning = false;
    }

    private void CaptureSceneAudio()
    {
        m_transitionAudio.Clear();
        Scene activeScene = UnitySceneManager.GetActiveScene();
        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>())
            {
                if (!source.isPlaying) { continue; }
                m_transitionAudio.Add(new TransitionAudio
                {
                    m_source = source,
                    m_initialVolume = source.volume
                });
            }
        }
    }

    private void LateUpdate()
    {
        //BGM管理側のUpdateが音量を更新しても、暗転中に音量が戻らないよう後で適用します。
        ApplyAudioFade();
    }

    private void ApplyAudioFade()
    {
        if (m_image == null || m_transitionAudio.Count == 0) { return; }
        float volumeMultiplier = EOpaqueAlpha - Mathf.Clamp01(m_image.color.a);
        foreach (TransitionAudio audio in m_transitionAudio)
        {
            if (audio.m_source == null) { continue; }
            audio.m_source.volume = audio.m_initialVolume * volumeMultiplier;
        }
    }

    private void RestoreSceneAudio()
    {
        foreach (TransitionAudio audio in m_transitionAudio)
        {
            if (audio.m_source == null) { continue; }
            audio.m_source.volume = audio.m_initialVolume;
        }
        m_transitionAudio.Clear();
    }


    private static bool HasOpaqueStartupLoadingScreen()
    {
        Scene activeScene = UnitySceneManager.GetActiveScene();
        foreach (GameObject root in activeScene.GetRootGameObjects())
        {
            foreach (CanvasGroup group in root.GetComponentsInChildren<CanvasGroup>())
            {
                if (group.name == EStartupLoadingScreenName
                    && group.alpha >= EOpaqueAlpha
                    && group.blocksRaycasts)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
