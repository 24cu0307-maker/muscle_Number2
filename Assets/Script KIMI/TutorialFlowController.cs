using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class TutorialFlowController : MonoBehaviour
{
    [Header("Tutorial")]
    [SerializeField] private TutorialScene tutorialScene;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector startTimeline;
    [SerializeField] private PlayableDirector endTimeline;

    [Header("Explanation UI")]
    [SerializeField] private GameObject explanationRoot;
    [SerializeField] private Image explanationImage;

    [Header("Intro Images")]
    [SerializeField] private Sprite[] introImages;

    [Header("Practice Explanation Images")]
    [SerializeField] private Sprite[] practiceImages;

    [Header("Practice Active Image")]
    [SerializeField] private Sprite practiceActiveImage;

    [Header("Result Images")]
    [SerializeField] private Sprite successImage;
    [SerializeField] private Sprite failureImage;

    [Header("Outro Images")]
    [SerializeField] private Sprite[] outroImages;

    [Header("Audio Source")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource jingleSource;
    [SerializeField] private AudioSource seSource;

    [Header("BGM")]
    [SerializeField] private AudioClip tutorialBGM;
    [SerializeField] private AudioClip transitionJingle;

    [Header("SE")]
    [SerializeField] private AudioClip explanationSE;
    [SerializeField] private AudioClip poseStartSE;
    [SerializeField] private AudioClip successSE;
    [SerializeField] private AudioClip failureSE;
    [SerializeField] private AudioClip finishSE;

    [Header("Timing")]
    [SerializeField] private float imageDisplaySeconds = 2.5f;
    [SerializeField] private float resultDisplaySeconds = 1.5f;

    [Header("Audio Transition")]
    [SerializeField] private float bgmFadeOutSeconds = 0.4f;
    [SerializeField] private float jingleFadeInSeconds = 0.25f;

    private bool failureImagePlaying = false;


    private IEnumerator Start()
    {
        // 最初は説明画像を非表示
        explanationRoot.SetActive(false);

        // TutorialSceneから成功・失敗を受け取る
        tutorialScene.AttemptResolved += OnAttemptResolved;

        // チュートリアルBGM開始
        PlayBGM();

        // MediaPipeなどの準備完了を待つ
        while (!tutorialScene.IsReady)
        {
            yield return null;
        }

        // =========================
        // 1. 開始アニメーション
        // =========================
        yield return PlayTimeline(startTimeline);

        // =========================
        // 2. 導入説明
        // =========================
        yield return ShowImages(introImages);

        // =========================
        // 3. ポーズ操作説明
        // =========================
        yield return ShowImages(practiceImages);

        // =========================
        // 4. ポーズ練習
        // =========================

        // 練習中の説明画像を表示
        ShowPracticeActiveImage();

        // ポーズ開始SE
        PlaySE(poseStartSE);

        // ポーズ練習開始
        tutorialScene.StartPractice();

        // 成功するまで待つ
        while (!tutorialScene.PracticeCompleted)
        {
            yield return null;
        }

        // 失敗画像が表示中なら終了を待つ
        while (failureImagePlaying)
        {
            yield return null;
        }

        explanationRoot.SetActive(false);

        // =========================
        // 5. 成功画像
        // =========================
        yield return ShowSingleImage(
            successImage,
            resultDisplaySeconds
        );

        // =========================
        // 6. 本番前の説明
        // 最後の画像と同時に
        // BGM → ジングルへクロスフェード
        // =========================
        yield return ShowOutroImages();

        // =========================
        // 7. 終了アニメーション
        // =========================
        PlaySE(finishSE);

        yield return PlayTimeline(endTimeline);

        // =========================
        // 8. Gameplayへ
        // =========================
        GameSession.Load(
            GameSession.GameplayScene
        );
    }


    /// <summary>
    /// 通常の説明画像を順番に表示
    /// </summary>
    private IEnumerator ShowImages(Sprite[] images)
    {
        if (images == null || images.Length == 0)
        {
            yield break;
        }

        explanationRoot.SetActive(true);

        foreach (Sprite sprite in images)
        {
            if (sprite == null)
            {
                continue;
            }

            explanationImage.sprite = sprite;

            PlaySE(explanationSE);

            yield return new WaitForSeconds(
                imageDisplaySeconds
            );
        }

        explanationRoot.SetActive(false);
    }


    /// <summary>
    /// 1枚の画像を指定時間表示
    /// </summary>
    private IEnumerator ShowSingleImage(
        Sprite sprite,
        float seconds
    )
    {
        if (sprite == null)
        {
            yield break;
        }

        explanationRoot.SetActive(true);
        explanationImage.sprite = sprite;

        yield return new WaitForSeconds(seconds);

        explanationRoot.SetActive(false);
    }


    /// <summary>
    /// ポーズ練習中の画像を表示
    /// </summary>
    private void ShowPracticeActiveImage()
    {
        if (practiceActiveImage == null)
        {
            return;
        }

        explanationRoot.SetActive(true);
        explanationImage.sprite = practiceActiveImage;
    }


    /// <summary>
    /// TutorialSceneから成功・失敗を受け取る
    /// </summary>
    private void OnAttemptResolved(bool success)
    {
        if (success)
        {
            PlaySE(successSE);
        }
        else
        {
            PlaySE(failureSE);

            StartCoroutine(
                ShowFailureImage()
            );
        }
    }


    /// <summary>
    /// 失敗画像を表示したあと、
    /// 練習中の画像へ戻す
    /// </summary>
    private IEnumerator ShowFailureImage()
    {
        if (failureImagePlaying)
        {
            yield break;
        }

        failureImagePlaying = true;

        if (failureImage != null)
        {
            explanationRoot.SetActive(true);
            explanationImage.sprite = failureImage;

            yield return new WaitForSeconds(
                resultDisplaySeconds
            );
        }

        ShowPracticeActiveImage();

        failureImagePlaying = false;
    }


    /// <summary>
    /// 本番前の説明画像を順番に表示。
    /// 最後の画像が表示された瞬間に
    /// BGMからジングルへクロスフェードする。
    /// </summary>
    private IEnumerator ShowOutroImages()
    {
        if (outroImages == null ||
            outroImages.Length == 0)
        {
            yield break;
        }

        explanationRoot.SetActive(true);

        for (int i = 0; i < outroImages.Length; i++)
        {
            Sprite sprite = outroImages[i];

            if (sprite == null)
            {
                continue;
            }

            // 画像表示
            explanationImage.sprite = sprite;

            // 説明切り替えSE
            PlaySE(explanationSE);

            // 最後の画像が表示された瞬間
            if (i == outroImages.Length - 1)
            {
                StartAudioTransition();
            }

            yield return new WaitForSeconds(
                imageDisplaySeconds
            );
        }

        explanationRoot.SetActive(false);
    }


    /// <summary>
    /// チュートリアルBGMを再生
    /// </summary>
    private void PlayBGM()
    {
        if (bgmSource == null ||
            tutorialBGM == null)
        {
            return;
        }

        bgmSource.clip = tutorialBGM;
        bgmSource.loop = true;
        bgmSource.volume = 1.0f;
        bgmSource.Play();
    }


    /// <summary>
    /// BGMからジングルへの
    /// クロスフェードを開始
    /// </summary>
    private void StartAudioTransition()
    {
        StartCoroutine(
            FadeOutBGM()
        );

        StartCoroutine(
            FadeInJingle()
        );
    }


    /// <summary>
    /// チュートリアルBGMをフェードアウト
    /// </summary>
    private IEnumerator FadeOutBGM()
    {
        if (bgmSource == null)
        {
            yield break;
        }

        float startVolume = bgmSource.volume;
        float elapsedTime = 0.0f;

        // フェード時間が0以下なら即停止
        if (bgmFadeOutSeconds <= 0.0f)
        {
            bgmSource.Stop();
            yield break;
        }

        while (elapsedTime < bgmFadeOutSeconds)
        {
            elapsedTime += Time.deltaTime;

            float t =
                elapsedTime / bgmFadeOutSeconds;

            bgmSource.volume =
                Mathf.Lerp(
                    startVolume,
                    0.0f,
                    t
                );

            yield return null;
        }

        bgmSource.volume = 0.0f;
        bgmSource.Stop();

        // 次回再生用に戻しておく
        bgmSource.volume = startVolume;
    }


    /// <summary>
    /// ジングルを0からフェードイン
    /// </summary>
    private IEnumerator FadeInJingle()
    {
        if (jingleSource == null ||
            transitionJingle == null)
        {
            yield break;
        }

        // Inspectorで設定した本来の音量を保存
        float targetVolume = jingleSource.volume;

        // まず音量0にする
        jingleSource.volume = 0.0f;

        jingleSource.clip = transitionJingle;
        jingleSource.loop = false;

        // 音量0の状態で再生開始
        jingleSource.Play();

        // フェード時間が0以下なら即座に本来の音量へ
        if (jingleFadeInSeconds <= 0.0f)
        {
            jingleSource.volume = targetVolume;
            yield break;
        }

        float elapsedTime = 0.0f;

        while (elapsedTime < jingleFadeInSeconds)
        {
            elapsedTime += Time.deltaTime;

            float t =
                elapsedTime / jingleFadeInSeconds;

            jingleSource.volume =
                Mathf.Lerp(
                    0.0f,
                    targetVolume,
                    t
                );

            yield return null;
        }

        jingleSource.volume = targetVolume;
    }


    /// <summary>
    /// SEを再生
    /// </summary>
    private void PlaySE(AudioClip clip)
    {
        if (seSource == null ||
            clip == null)
        {
            return;
        }

        seSource.PlayOneShot(clip);
    }


    /// <summary>
    /// Timelineを再生し、
    /// 終了まで待つ
    /// </summary>
    private IEnumerator PlayTimeline(
        PlayableDirector director
    )
    {
        if (director == null)
        {
            yield break;
        }

        director.Play();

        while (director.state ==
               PlayState.Playing)
        {
            yield return null;
        }
    }


    /// <summary>
    /// イベント登録解除
    /// </summary>
    private void OnDestroy()
    {
        if (tutorialScene != null)
        {
            tutorialScene.AttemptResolved
                -= OnAttemptResolved;
        }
    }
}