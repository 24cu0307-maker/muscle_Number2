using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Animation : MonoBehaviour
{
    [Header("ポーズ準")]
    [SerializeField]
    private PoseFlowDataManager m_flowDataManager;

    [Header("判定")]
    [SerializeField]
    private PoseJudgeManager m_judgeManager;

    [Header("Animator")]
    [SerializeField]
    private Animator m_animator;

    [Header("判定")]
    [SerializeField]
    private MusicNodeSequence m_sequence;

    [Header("In")]
    [SerializeField]
    private InGameManager m_gameManager;

    [Header("Idleに戻るまでの待ち時間")]
    [SerializeField]
    private float m_idleDelay = 2.0f;

    [Header("AnimationCrip Front")]
    [SerializeField]
    private AnimationClip m_CripFront;

    [Header("AnimationCrip Side")]
    [SerializeField]
    private AnimationClip m_CripSide;

    // 再生中
    private bool m_isPlaying = false;

    // 今回の判定ですでに再生したか
    private bool m_hasPlayed = false;

    // 現在選択しているアニメーション
    private string m_currentAnimation = "";

    // アニメーションを一時停止しているか
    private bool m_isPaused = false;

    float curentTime;

    [Header("再生誤差")]
    [SerializeField]
    private float m_time = 2.5f;

    private void Start()
    {
        if (m_animator == null)
            return;

        // Animatorを無効化
        m_animator.enabled = false;

        // 再生速度を通常に戻す
        m_animator.speed = 1f;

        // 最初はIdle
        m_animator.Play(
            "Base Layer.Idle",
            0,
            0f
        );
    }


    private void Update()
    {
        if (m_animator == null)
            return;


        // ==========================================
        // キーボードテスト
        // ==========================================
        if (Keyboard.current != null)
        {
            // A → Front
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                if (!m_isPlaying)
                {
                    m_hasPlayed = true;
                    PlayFront();
                }
            }

            // S → Side
            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                if (!m_isPlaying)
                {
                    m_hasPlayed = true;
                    PlaySide();
                }
            }

            // Space → 一時停止 / 再開
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                TogglePause();
            }
        }



        // ==========================================
        // 実際のゲーム判定
        // ==========================================

    

        float a = m_sequence.m_eventsList[0].m_time;
        float b = m_sequence.m_eventsList[1].m_time;
      

        int poseID =
            m_flowDataManager.GetPose().PoseID;

        int FlowID = m_sequence.GetCurrentNodeNumber(m_gameManager.GetCurrentTIme()) - 1;



        if (poseID != 2 && FlowID >= 0
            && (m_gameManager.GetCurrentTIme() >= ((m_sequence.m_eventsList[FlowID].m_time + m_time) - m_CripFront.length))
            && (curentTime + 1 <= ((m_sequence.m_eventsList[FlowID].m_time + m_time) - m_CripFront.length)))
        {
            curentTime = m_gameManager.GetCurrentTIme();

            m_hasPlayed = true;
            PlayFront();
        }


        if (poseID == 2 && FlowID >= 0
            && (m_gameManager.GetCurrentTIme() >= ((m_sequence.m_eventsList[FlowID].m_time + m_time) - m_CripSide.length))
            && (curentTime + 1 <= ((m_sequence.m_eventsList[FlowID].m_time + m_time) - m_CripSide.length)))
        {
            curentTime = m_gameManager.GetCurrentTIme();

            m_hasPlayed = true;
            PlaySide();
        }


    }


    // =========================================================
    // Front
    // =========================================================
    public void PlayFront()
    {
        if (m_isPlaying)
            return;

        m_isPlaying = true;
        m_isPaused = false;

        m_currentAnimation = "Front";

        m_animator.enabled = true;
        m_animator.speed = 1f;

        // ★ Frontを実際に再生
        m_animator.Play(
            "Base Layer.Front",
            0,
            0f
        );

        StartCoroutine(WaitAnimation("Front"));
    }


    // =========================================================
    // Side
    // =========================================================
    public void PlaySide()
    {
        if (m_isPlaying)
            return;

        m_isPlaying = true;
        m_isPaused = false;

        m_currentAnimation = "Side";

        m_animator.enabled = true;
        m_animator.speed = 1f;

        // ★ Sideを実際に再生
        m_animator.Play(
            "Base Layer.Side",
            0,
            0f
        );

        StartCoroutine(WaitAnimation("Side"));
    }


    // =========================================================
    // アニメーション終了待ち
    // =========================================================
    private IEnumerator WaitAnimation(string animationName)
    {
        // AnimatorにPlayを反映
        yield return null;

        // ==========================================
        // 指定したアニメーションになるまで待つ
        // ==========================================
        yield return new WaitUntil(() =>
        {
            return m_animator
                .GetCurrentAnimatorStateInfo(0)
                .IsName(animationName);
        });

        // ==========================================
        // アニメーション終了まで待つ
        // ==========================================
        yield return new WaitUntil(() =>
        {
            if (m_isPaused)
                return false;

            AnimatorStateInfo state =
                m_animator.GetCurrentAnimatorStateInfo(0);

            return state.IsName(animationName)
                && state.normalizedTime >= 1.0f;
        });

        // ==========================================
        // ポーズを数秒維持
        // ==========================================
        yield return new WaitForSeconds(m_idleDelay);

        // ==========================================
        // Idleへ
        // ==========================================
        m_animator.speed = 1f;
        m_isPaused = false;

        m_animator.Play(
            "Base Layer.Idle",
            0,
            0f
        );

        yield return null;

        // ==========================================
        // Idle終了まで待つ
        // ==========================================
        yield return new WaitUntil(() =>
        {
            if (m_isPaused)
                return false;

            AnimatorStateInfo state =
                m_animator.GetCurrentAnimatorStateInfo(0);

            return state.IsName("Idle")
                && state.normalizedTime >= 1.0f;
        });

        // ==========================================
        // Animator停止
        // ==========================================
        m_animator.speed = 0f;
        m_animator.enabled = false;

        m_isPlaying = false;
        m_isPaused = false;
        m_currentAnimation = "";

    }



    // =========================================================
    // 一時停止 / 再開
    // =========================================================
    public void TogglePause()
    {
        if (!m_isPlaying)
            return;


        if (m_isPaused)
        {
            ResumeAnimation();
        }
        else
        {
            PauseAnimation();
        }
    }


    public void PauseAnimation()
    {
        m_isPaused = true;

        m_animator.speed = 0f;
    }


    public void ResumeAnimation()
    {
        m_isPaused = false;

        m_animator.speed = 1f;
    }



}