using System.Collections;
using Mediapipe.Unity.Sample.Holistic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MediaPipe用シーンのロードとHolistic初期化を一括管理します。
///
/// シーンロード完了だけではWebカメラや推論グラフはまだ利用できないため、
/// <see cref="HolisticTrackingSolution.IsInitializationComplete"/>まで待機します。
/// Gameplay側は<see cref="IsReady"/>だけを確認すればよく、MediaPipe固有の
/// 初期化手順を知る必要がありません。
/// </summary>
public sealed class ScenesLoad : MonoBehaviour
{
    private const string EDefaultMediaPipeSceneName = "Holistic";

    [Header("MediaPipe Scene")]
    [SerializeField] private string m_mediaPipeSceneName =
        EDefaultMediaPipeSceneName;

    [Header("Failure Handling")]
    [Tooltip("0以下の場合はタイムアウトせず、初期化完了まで待機します。")]
    [SerializeField] private float m_initializationTimeoutSeconds = 30.0f;

    /// <summary>MediaPipeが推論を開始できる状態かどうか。</summary>
    public bool IsReady { get; private set; }

    /// <summary>ロードまたは初期化が失敗したかどうか。</summary>
    public bool HasFailed { get; private set; }

    /// <summary>失敗理由。正常時は空文字です。</summary>
    public string FailureReason { get; private set; } = string.Empty;

    private IEnumerator Start()
    {
        yield return LoadAndInitializeMediaPipe();
    }

    /// <summary>
    /// Holisticシーンを必要な場合だけAdditiveロードし、推論グラフの準備を待ちます。
    /// 既にロード済みの場合は重複ロードせず、そのシーン内のSolutionを再利用します。
    /// </summary>
    private IEnumerator LoadAndInitializeMediaPipe()
    {
        IsReady = false;
        HasFailed = false;
        FailureReason = string.Empty;

        if (string.IsNullOrWhiteSpace(m_mediaPipeSceneName))
        {
            Fail("MediaPipeシーン名が設定されていません。");
            yield break;
        }

        Scene mediaPipeScene = SceneManager.GetSceneByName(m_mediaPipeSceneName);
        if (!mediaPipeScene.isLoaded)
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                m_mediaPipeSceneName,
                LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Fail($"MediaPipeシーン「{m_mediaPipeSceneName}」をロードできませんでした。");
                yield break;
            }

            yield return loadOperation;
        }

        HolisticTrackingSolution solution =
            FindFirstObjectByType<HolisticTrackingSolution>();
        if (solution == null)
        {
            Fail("HolisticTrackingSolutionがMediaPipeシーン内に見つかりません。");
            yield break;
        }

        float waitStartedAt = Time.realtimeSinceStartup;
        while (!solution.IsInitializationComplete)
        {
            if (solution.HasInitializationFailed)
            {
                Fail("WebカメラまたはMediaPipeグラフの初期化に失敗しました。");
                yield break;
            }

            if (m_initializationTimeoutSeconds > 0.0f
                && Time.realtimeSinceStartup - waitStartedAt
                    >= m_initializationTimeoutSeconds)
            {
                Fail($"MediaPipeの初期化が{m_initializationTimeoutSeconds:0.#}秒以内に完了しませんでした。");
                yield break;
            }

            yield return null;
        }

        IsReady = true;
    }

    /// <summary>失敗状態と理由を一か所で設定し、Consoleにも出力します。</summary>
    private void Fail(string _reason)
    {
        HasFailed = true;
        FailureReason = _reason;
        Debug.LogError(_reason, this);
    }
}
