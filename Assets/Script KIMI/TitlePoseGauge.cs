using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitlePoseGauge : MonoBehaviour
{
    [Header("ポーズ判定")]
    [SerializeField] private InputPose inputPose;
    [SerializeField] private int targetPoseID = 0;

    [Header("ゲージ設定")]
    [SerializeField] private float requiredHoldTime = 1.0f;
    [SerializeField] private float decreaseSpeed = 2.0f;

    [Header("ゲージ表示")]
    [SerializeField] private RectTransform fillMask;

    [Header("SE")]
    [SerializeField] private AudioSource seSource;
    [SerializeField] private AudioClip gaugeCompleteSE;

    [Header("シーン遷移")]
    [SerializeField] private string nextSceneName = "Tutorial";
    [SerializeField] private float sceneLoadDelay = 0.5f;

    private float currentHoldTime;
    private float maxHeight;
    private bool completed;


    private void Awake()
    {
        // 現在のFillMaskの高さを
        // 100%時の高さとして記録
        maxHeight = fillMask.rect.height;

        // 最初は0%
        SetGauge(0f);
    }


    private void LateUpdate()
    {
        // すでにゲージ完成済みなら
        // これ以上処理しない
        if (completed)
        {
            return;
        }

        bool isPoseActive =
            inputPose.GetisInputPose(targetPoseID);

        if (isPoseActive)
        {
            // ポーズ成立中
            currentHoldTime += Time.deltaTime;
        }
        else
        {
            // ポーズを崩したらゲージを減らす
            currentHoldTime -=
                Time.deltaTime * decreaseSpeed;
        }

        currentHoldTime = Mathf.Clamp(
            currentHoldTime,
            0f,
            requiredHoldTime
        );

        float progress =
            currentHoldTime / requiredHoldTime;

        SetGauge(progress);

        // =========================
        // ゲージ完成
        // =========================
        if (currentHoldTime >= requiredHoldTime)
        {
            completed = true;

            // 完成SE
            PlayCompleteSE();

            // 少し待ってからシーン遷移
            StartCoroutine(
                LoadNextScene()
            );
        }
    }


    /// <summary>
    /// ゲージ表示を更新します。
    /// </summary>
    private void SetGauge(float progress)
    {
        progress = Mathf.Clamp01(progress);

        float height =
            maxHeight * progress;

        fillMask.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );
    }


    /// <summary>
    /// ゲージが満タンになったときの
    /// SEを再生します。
    /// </summary>
    private void PlayCompleteSE()
    {
        if (seSource == null ||
            gaugeCompleteSE == null)
        {
            return;
        }

        seSource.PlayOneShot(
            gaugeCompleteSE
        );
    }


    /// <summary>
    /// SEを鳴らしたあと、
    /// 次のシーンへ遷移します。
    /// </summary>
    private IEnumerator LoadNextScene()
    {
        yield return new WaitForSeconds(
            sceneLoadDelay
        );

        SceneManager.LoadScene(
            nextSceneName
        );
    }
}