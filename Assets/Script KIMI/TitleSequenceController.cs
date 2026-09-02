using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Playables;
using System.Collections;

public class TitleSequenceController : MonoBehaviour
{
    [Header("Timeline")]
    [SerializeField]
    private PlayableDirector opDirector;

    [SerializeField]
    private PlayableDirector idleDirector;


    [Header("BGM")]
    [SerializeField]
    private AudioSource bgmSource;

    // OP開始からBGM開始までの時間
    [SerializeField]
    private float bgmDelay = 0.8f;


    [Header("Idle")]
    // Idleを何秒間流してからOPへ戻るか
    [SerializeField]
    private float idleDuration = 50f;


    [Header("Black Fade")]
    // 先ほど作ったBlackFade
    [SerializeField]
    private Image blackFade;

    // 黒くなるまでの時間
    [SerializeField]
    private float fadeOutDuration = 0.25f;

    // 完全に黒い状態を維持する時間
    [SerializeField]
    private float blackDuration = 0.6f;

    // 黒から画面を表示するまでの時間
    [SerializeField]
    private float fadeInDuration = 0.25f;


    private Coroutine sequenceCoroutine;
    private Coroutine bgmCoroutine;

    private bool bgmStarted = false;
    private bool restarting = false;


    private void Start()
    {
        // AudioSource自身ではループさせない
        bgmSource.loop = false;

        // 最初はBlackFadeを透明にする
        SetBlackAlpha(0f);

        // 最初のサイクルを開始
        StartNewCycle();
    }


    private void Update()
    {
        // BGMが一度再生され、
        // その後再生が終了したらリスタート処理へ
        if (bgmStarted &&
            !bgmSource.isPlaying &&
            !restarting)
        {
            StartCoroutine(RestartWithBlackFade());
        }
    }


    // =====================================================
    // BGM終了時のブラックアウト＋リスタート
    // =====================================================

    private IEnumerator RestartWithBlackFade()
    {
        restarting = true;
        bgmStarted = false;


        // -----------------------------
        // 1. 徐々にブラックアウト
        // -----------------------------

        yield return FadeBlack(
            0f,
            1f,
            fadeOutDuration
        );


        // -----------------------------
        // 2. 完全に黒くなってから
        //    現在のアニメーションを停止
        // -----------------------------

        if (sequenceCoroutine != null)
        {
            StopCoroutine(sequenceCoroutine);
        }

        if (bgmCoroutine != null)
        {
            StopCoroutine(bgmCoroutine);
        }

        opDirector.Stop();
        idleDirector.Stop();

        opDirector.time = 0;
        idleDirector.time = 0;

        bgmSource.Stop();
        bgmSource.time = 0;


        // -----------------------------
        // 3. 黒画面を少し維持
        // -----------------------------

        yield return new WaitForSeconds(
            blackDuration
        );


        // -----------------------------
        // 4. OPを頭から開始
        // -----------------------------

        StartNewCycle();


        // -----------------------------
        // 5. 黒画面から徐々に戻す
        // -----------------------------

        yield return FadeBlack(
            1f,
            0f,
            fadeInDuration
        );


        restarting = false;
    }


    // =====================================================
    // 新しいタイトルサイクルを開始
    // =====================================================

    private void StartNewCycle()
    {
        opDirector.Stop();
        idleDirector.Stop();

        opDirector.time = 0;
        idleDirector.time = 0;

        bgmSource.Stop();
        bgmSource.time = 0;

        bgmStarted = false;


        // OP → Idle → OP の映像サイクル
        sequenceCoroutine =
            StartCoroutine(SequenceLoop());


        // BGMは0.8秒遅れて開始
        bgmCoroutine =
            StartCoroutine(StartBGM());
    }


    // =====================================================
    // BGM開始
    // =====================================================

    private IEnumerator StartBGM()
    {
        yield return new WaitForSeconds(
            bgmDelay
        );

        bgmSource.Play();

        bgmStarted = true;
    }


    // =====================================================
    // OP → Idle → OP の通常サイクル
    // =====================================================

    private IEnumerator SequenceLoop()
    {
        while (true)
        {
            // -------------------------
            // OP
            // -------------------------

            idleDirector.Stop();

            opDirector.time = 0;
            opDirector.Play();


            // OP終了まで待つ
            yield return new WaitUntil(
                () =>
                    opDirector.state
                    != PlayState.Playing
            );


            // -------------------------
            // Idle
            // -------------------------

            idleDirector.time = 0;
            idleDirector.Play();


            // Idleを指定時間流す
            yield return new WaitForSeconds(
                idleDuration
            );


            // -------------------------
            // Idle終了
            // -------------------------

            idleDirector.Stop();

            // while先頭に戻って
            // 再びOPを再生
        }
    }


    // =====================================================
    // BlackFadeのAlphaをアニメーション
    // =====================================================

    private IEnumerator FadeBlack(
        float startAlpha,
        float endAlpha,
        float duration
    )
    {
        float elapsedTime = 0f;


        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;

            float t =
                elapsedTime / duration;


            float alpha =
                Mathf.Lerp(
                    startAlpha,
                    endAlpha,
                    t
                );


            SetBlackAlpha(alpha);

            yield return null;
        }


        // 最後に確実に目標値へ合わせる
        SetBlackAlpha(endAlpha);
    }


    // =====================================================
    // BlackFadeのAlphaを変更
    // =====================================================

    private void SetBlackAlpha(float alpha)
    {
        Color color =
            blackFade.color;

        color.a = alpha;

        blackFade.color = color;
    }
}