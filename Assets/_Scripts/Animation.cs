using UnityEngine;
using System.Collections;

public class Animation : MonoBehaviour
{
    [Header("ポーズ準")]
    [SerializeField]
    private PoseFlowDataManager m_flowDataManager;

    [Header("判定")]
    [SerializeField]
    private PoseJudgeManager m_judgeManager;

    [Header("Animation")]
    [SerializeField]
    private Animator m_animatorFront;

    [SerializeField]
    private Animator m_animatorSide;

    [Header("Idleに戻るまでの待ち時間")]
    [SerializeField]
    private float m_idleDelay = 2.0f;

    private bool m_isFrontPlaying = false;
    private bool m_isSidePlaying = false;

    // 今回のGreat / Perfectですでに再生したか
    private bool m_hasPlayed = false;


    private void Start()
    {
        // ゲーム開始時はAnimatorを止める
        if (m_animatorFront != null)
        {
            m_animatorFront.enabled = false;
        }

        if (m_animatorSide != null)
        {
            m_animatorSide.enabled = false;
        }
    }


    private void Update()
    {
        bool isGood =
            m_judgeManager.LastGrade == EPoseMatchGrade.Great ||
            m_judgeManager.LastGrade == EPoseMatchGrade.Perfect;


        // Great / Perfectではない
        if (!isGood)
        {
            // 次の判定で再生できるようにリセット
            m_hasPlayed = false;
            return;
        }


        // すでに今回の判定で再生済み
        if (m_hasPlayed)
            return;


        int poseID = m_flowDataManager.GetPose().PoseID;


        // PoseID 0 → Front
        if (poseID == 0 && !m_isFrontPlaying)
        {
            m_hasPlayed = true;

            PlayFront();
        }


        // PoseID 2 → Side
        else if (poseID == 2 && !m_isSidePlaying)
        {
            m_hasPlayed = true;

            PlaySide();
        }
    }


    // Front再生

    public void PlayFront()
    {
        m_isFrontPlaying = true;

        m_animatorFront.enabled = true;

        // Frontを最初から再生
        m_animatorFront.Play(
            "Base Layer.Front",
            0,
            0f
        );

        StartCoroutine(WaitFrontAnimation());
    }

    //待機して再生
    private IEnumerator WaitFrontAnimation()
    {
        // AnimatorにPlayを反映
        yield return null;

        // Frontの状態になるまで待つ
        yield return new WaitUntil(() =>
            m_animatorFront.GetCurrentAnimatorStateInfo(0).IsName("Front")
        );

        // Frontの再生終了まで待つ
        yield return new WaitUntil(() =>
            m_animatorFront.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        );

        // Idleに戻るまで待つ
        yield return new WaitForSeconds(m_idleDelay);

        // Idleへ戻す
        m_animatorFront.Play(
            "Base Layer.Idle",
            0,
            0f
        );

        // Idleの再生終了まで待つ
        yield return new WaitUntil(() =>
            m_animatorFront.GetCurrentAnimatorStateInfo(0).IsName("Idle")
        );

        yield return new WaitUntil(() =>
            m_animatorFront.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        );

        // Idle終了後にAnimatorを停止
        m_animatorFront.enabled = false;

        // Frontの再生可能状態を解除
        m_isFrontPlaying = false;
    }


    // Side再生
    public void PlaySide()
    {
        m_isSidePlaying = true;

        m_animatorSide.enabled = true;

        // Sideを最初から再生
        m_animatorSide.Play(
            "Base Layer.Side",
            0,
            0f
        );

        StartCoroutine(WaitSideAnimation());
    }

    //待機して再生
    private IEnumerator WaitSideAnimation()
    {
        // AnimatorにPlayを反映
        yield return null;

        // Sideの状態になるまで待つ
        yield return new WaitUntil(() =>
            m_animatorSide.GetCurrentAnimatorStateInfo(0).IsName("Side")
        );

        // Sideの再生終了まで待つ
        yield return new WaitUntil(() =>
            m_animatorSide.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        );

        // Idleに戻るまで待つ
        yield return new WaitForSeconds(m_idleDelay);

        // Idleへ戻す
        m_animatorSide.Play(
            "Base Layer.Idle",
            0,
            0f
        );

        // Idleの再生終了まで待つ
        yield return new WaitUntil(() =>
            m_animatorSide.GetCurrentAnimatorStateInfo(0).IsName("Idle")
        );

        yield return new WaitUntil(() =>
            m_animatorSide.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f
        );

        // Idle終了後にAnimatorを停止
        m_animatorSide.enabled = false;

        // Frontの再生可能状態を解除
        m_isSidePlaying = false;
    }
}