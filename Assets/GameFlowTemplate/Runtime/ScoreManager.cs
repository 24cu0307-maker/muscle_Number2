/*━━━━━━━━━
@file ScoreManager.cs
@brief ゲーム中スコアと結果スコアの管理
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks スコア加算・表示出力・結果コンテナ作成を担当
━━━━━━━━━*/

using System;
using TMPro;
using UnityEngine;

namespace GameFlowTemplate
{
    /// <summary>
    /// ゲーム中のスコアを管理するマネージャーです。
    /// スコアの加算、表示用Textへの出力、終了時の結果コンテナ作成を担当します。
    /// </summary>
    public sealed class ScoreManager : MonoBehaviour
    {
        private const int MinimumScore = 0;                 //スコアがマイナスにならないようにする下限
        private const string ScoreFormat = "{0}";           //表示用スコアの初期フォーマット

        [SerializeField] private TMP_Text m_scoreText;      //ゲーム中スコアを表示するText
        [SerializeField] private string m_scoreFormat = ScoreFormat; //スコア表示形式。例: Score: {0}

        public int CurrentScore { get; private set; }       //現在ゲーム中に加算されているスコア
        public event Action<int> ScoreChanged;              //スコアが変化した時に通知するイベント

        private void Start()
        {
            UpdateScoreText();
        }

        public void AddScore(int _score)
        {
            //ゲーム中に獲得した点数を現在スコアへ加算する。
            SetScore(CurrentScore + _score);
        }

        public void SetScore(int _score)
        {
            //現在スコアを直接設定する。
            //スコアはゲーム結果やランキングへ使うため、0未満にはしない。
            CurrentScore = Mathf.Max(MinimumScore, _score);
            UpdateScoreText();
            ScoreChanged?.Invoke(CurrentScore);
        }

        public void ResetScore()
        {
            //ゲーム開始時やリトライ時にスコアを初期化する。
            SetScore(MinimumScore);
        }

        public string GetScoreText()
        {
            //UI表示用に整形したスコア文字列を返す。
            return string.Format(m_scoreFormat, CurrentScore);
        }

        public GameResultContainer CreateResultContainer(
            string _playername,
            float _playtimeseconds,
            string _playtimetext)
        {
            //ゲーム終了時点の結果をコンテナへまとめる。
            return new GameResultContainer
            {
                m_playerName = _playername,
                m_finalScore = CurrentScore,
                m_playTimeSeconds = _playtimeseconds,
                m_playTimeText = _playtimetext,
                m_rank = 0,
                m_totalRankingCount = 0,
                m_bRankingSubmitted = false
            };
        }

        private void UpdateScoreText()
        {
            //Textが設定されている場合だけ、ゲーム中表示を更新する。
            if (m_scoreText == null) { return; }

            m_scoreText.text = GetScoreText();
        }
    }
}
