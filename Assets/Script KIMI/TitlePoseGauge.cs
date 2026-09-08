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

    [Header("シーン遷移")]
    [SerializeField] private string nextSceneName = "Tutorial";

    private float currentHoldTime;
    private float maxHeight;
    private bool completed;

    private void Awake()
    {
        // 現在のFillMaskの高さを100%時の高さとして記録
        maxHeight = fillMask.rect.height;

        // 最初は0%
        SetGauge(0f);
    }

    private void LateUpdate()
    {
        if (completed)
            return;

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
            currentHoldTime -= Time.deltaTime * decreaseSpeed;
        }

        currentHoldTime = Mathf.Clamp(
            currentHoldTime,
            0f,
            requiredHoldTime
        );

        float progress =
            currentHoldTime / requiredHoldTime;

        SetGauge(progress);

        if (currentHoldTime >= requiredHoldTime)
        {
            completed = true;

            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void SetGauge(float progress)
    {
        progress = Mathf.Clamp01(progress);

        float height = maxHeight * progress;

        fillMask.SetSizeWithCurrentAnchors(
            RectTransform.Axis.Vertical,
            height
        );
    }
}