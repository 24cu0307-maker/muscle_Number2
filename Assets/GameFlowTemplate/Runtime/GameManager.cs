/*━━━━━━━━━
@file GameManager.cs
@brief 各マネージャーの管理とゲームフロー制御
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks ScoreRankingSystemと連携してゲーム終了時に順位を取得
━━━━━━━━━*/

using CrossProjectScoreRanking;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameFlowTemplate
{
    /// <summary>
    /// ゲーム全体の流れを管理する中心マネージャーです。
    /// ScoreManager、TimeManager、SceneManagerを束ね、ゲーム開始・終了・演出停止を制御します。
    /// </summary>
    [RequireComponent(typeof(ScoreManager))]
    [RequireComponent(typeof(TimeManager))]
    [RequireComponent(typeof(SceneManager))]
    public class GameManager : MonoBehaviour
    {
        //インスタンス化
        //public static GameManager Instance { get; private set; }

        private const string DefaultPlayerName = ""; //名前未設定時はScoreRankingSystem側でNoName_日付_時間にする
        private const float EmptyPlayTimeSeconds = 0.0f; //TimeManagerが未設定の場合に使うプレイ時間

        [Header("インゲームの管理を担当")]
        [SerializeField] private InGameManager m_gameManager;                         //インゲームの管理を担当
        [Header("スコア管理担当")]
        [SerializeField] private ScoreManager m_scoreManager;                         //スコア管理担当
        [Header("時間管理担当")]
        [SerializeField] private TimeManager m_timeManager;                           //時間管理担当
        [Header("シーン管理担当")]
        [SerializeField] private SceneManager m_sceneManager;                         //シーン管理担当
        [Header("スコアランキング保存担当。未設定ならランキング保存しない")]
        [SerializeField] private ScoreRankingInputSystem m_scoreRankingInputSystem;   //スコアランキング保存担当。未設定ならランキング保存しない
        [Header("ゲーム終了時にランキングへ保存する名前")]
        [SerializeField] private string m_playerName = DefaultPlayerName;             //ゲーム終了時にランキングへ保存する名前
        [Header("ゲーム終了時にScoreRankingSystemへ保存するか")]
        [SerializeField] private bool m_bSubmitRankingOnFinish = true;                //ゲーム終了時にScoreRankingSystemへ保存するか

        public GameState CurrentState { get; private set; } = GameState.Title;        //現在のゲーム状態
        public GameResultContainer LastResult { get; private set; }                   //最後に作成したゲーム結果
        public event Action<GameState> StateChanged;                                  //状態が変わった時に通知するイベント
        public event Action<GameResultContainer> GameFinished;                        //ゲーム終了結果が作られた時に通知するイベント

        public TimeManager GetTimeManager() { return m_timeManager; }                 //

        
        IEnumerator Start()
        {
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(
                "Holistic",
                LoadSceneMode.Additive);


            /*
            yield return SceneManager.LoadSceneAsync(
                "InGame",
                LoadSceneMode.Additive);
         */   
        }
        
        private void Awake()
        {


            //Inspector設定がない場合は、同じGameObject上の各マネージャーを自動取得する。
            if (m_gameManager == null)
            {
                m_gameManager = GetComponent<InGameManager>();
            }

            if (m_scoreManager == null)
            {
                m_scoreManager = GetComponent<ScoreManager>();
            }

            if (m_timeManager == null)
            {
                m_timeManager = GetComponent<TimeManager>();
            }

            if (m_sceneManager == null)
            {
                m_sceneManager = GetComponent<SceneManager>();
            }

            if (m_scoreRankingInputSystem == null)
            {
                m_scoreRankingInputSystem = GetComponent<ScoreRankingInputSystem>();
            }
        }

 

        public void PrepareGame()
        {
            //ゲーム開始前の準備状態にする。
            m_scoreManager?.ResetScore();
            m_timeManager?.ResetTimer();
            ChangeState(GameState.Ready);
        }

        public void StartGame()
        {
            //インゲームを開始する。
            m_scoreManager?.ResetScore();
            m_timeManager?.ResetTimer();
            m_timeManager?.StartGameTimer();
            ChangeState(GameState.Playing);
        }

        public void PauseForDirection()
        {
            //演出などでインゲーム時間を止める。
            if (CurrentState != GameState.Playing) { return; }

            m_timeManager?.PauseForDirection();
            ChangeState(GameState.DirectionPause);
        }

        public void ResumeFromDirection()
        {
            //演出終了後にインゲーム時間を再開する。
            if (CurrentState != GameState.DirectionPause) { return; }

            m_timeManager?.ResumeFromDirection();
            ChangeState(GameState.Playing);
        }

        public GameResultContainer FinishGame()
        {
            //ゲーム終了時に時間を止め、スコアと時間を結果コンテナへまとめる。
            float playTimeSeconds = m_timeManager == null ? EmptyPlayTimeSeconds : m_timeManager.StopGameTimer(); //最終プレイ時間
            string playTimeText = m_timeManager == null ? string.Empty : m_timeManager.GetTimeText(); //表示用時間

            LastResult = m_scoreManager == null
                ? new GameResultContainer()
                : m_scoreManager.CreateResultContainer(m_playerName, playTimeSeconds, playTimeText);

            SubmitRankingIfNeeded(LastResult);
            ChangeState(GameState.Result);
            GameFinished?.Invoke(LastResult);

            if (m_sceneManager != null)
            {
                m_sceneManager.LoadResultSceneIfNeeded();
            }

            return LastResult;
        }

        public void AddScore(int _score)
        {
            //GameManager経由でスコア加算したい場合に使う。
            m_scoreManager?.AddScore(_score);
        }

        public void SetPlayerName(string _playername)
        {
            //ゲーム開始前や名前入力後にプレイヤー名を設定する。
            m_playerName = _playername;
        }

        public void RetryGame()
        {
            //同じシーン内でリトライする場合の入口。
            PrepareGame();
            StartGame();
        }

        public void LoadGameScene()
        {
            //SceneManager経由でインゲームシーンへ遷移する。
            if (m_sceneManager == null) { return; }

            m_sceneManager.LoadGameScene();
        }

        public void LoadTitleScene()
        {
            //SceneManager経由でタイトルシーンへ遷移する。
            if (m_sceneManager == null) { return; }

            m_sceneManager.LoadTitleScene();
        }

        private void SubmitRankingIfNeeded(GameResultContainer _result)
        {
            //ScoreRankingSystemが設定されている場合だけ、最終スコアをランキングへ保存する。
            if (!ResultAvailable(_result) || !m_bSubmitRankingOnFinish || m_scoreRankingInputSystem == null) { return; }

            m_scoreRankingInputSystem.SetScore(_result.m_finalScore);
            ScoreRankingResult rankingResult = string.IsNullOrWhiteSpace(m_playerName)
                ? m_scoreRankingInputSystem.Submit()
                : m_scoreRankingInputSystem.Submit(m_playerName);

            _result.m_rank = rankingResult.m_rank;
            _result.m_totalRankingCount = rankingResult.m_totalCount;
            _result.m_bRankingSubmitted = rankingResult.m_bSucceeded;
        }

        private bool ResultAvailable(GameResultContainer _result)
        {
            //結果コンテナが存在するかを確認する。
            return _result != null;
        }

        private void ChangeState(GameState _state)
        {
            //ゲーム状態を変更し、必要なら外部UIへ通知する。
            if (CurrentState == _state) { return; }

            CurrentState = _state;
            StateChanged?.Invoke(CurrentState);
        }
    }
}
