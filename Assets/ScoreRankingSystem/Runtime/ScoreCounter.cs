/*━━━━━━━━━
@file ScoreCounter.cs
@brief ゲーム中スコアの加算と保存
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks ゲーム側からAddScoreとSubmitを呼ぶ
━━━━━━━━━*/

using System;
using UnityEngine;

namespace CrossProjectScoreRanking
{
    /// <summary>
    /// ゲーム中に加算されるスコアを保持し、ゲーム終了時にランキングへ保存するコンポーネントです。
    /// </summary>
    public sealed class ScoreCounter : MonoBehaviour
    {
        private const string DefaultPlayerName = "";                  //名前未設定時はNoName_日付_時間を自動生成する
        private const string DefaultSharedDirectoryOverride = "../RankingData"; //親フォルダのRankingDataを共有フォルダとして使う初期設定
        private const int MinimumScore = 0;                            //スコアがマイナスにならないようにする下限

        [SerializeField] private string m_sharedDirectoryOverride = DefaultSharedDirectoryOverride; //CSV保存先フォルダ
        [SerializeField] private string m_defaultPlayerName = DefaultPlayerName; //Submit()だけを呼んだ場合に使う名前
        [SerializeField] private bool m_bResetAfterSubmit = true;      //保存後に現在スコアを0へ戻すか

        public int CurrentScore { get; private set; }                  //現在ゲーム中に加算されているスコア
        public ScoreRankingResult LastResult { get; private set; }     //最後にSubmitした時の順位結果
        public event Action<int> ScoreChanged;                         //スコアが変わった時にUIへ通知するイベント
        public event Action<ScoreRankingResult> Submitted;             //保存完了時に自分の順位結果を通知するイベント

        private void Awake()
        {
            ScoreRankingStorage.SetSharedDirectory(m_sharedDirectoryOverride);
        }

        public void SetSharedDirectoryOverride(string _shareddirectoryoverride)
        {
            //セットアップ用スクリプトから保存先フォルダを反映する。
            m_sharedDirectoryOverride = _shareddirectoryoverride;
            ScoreRankingStorage.SetSharedDirectory(m_sharedDirectoryOverride);
        }

        public void AddScore(int _score)
        {
            //ゲーム中に獲得したスコアを加算する。
            SetScore(CurrentScore + _score);
        }

        public void SetScore(int _score)
        {
            //現在スコアを直接指定する。
            //マイナス値はランキングとして扱いにくいため0で止める。
            CurrentScore = Mathf.Max(MinimumScore, _score);
            ScoreChanged?.Invoke(CurrentScore);
        }

        public void ResetScore()
        {
            //ゲーム開始時やリトライ時に現在スコアを0へ戻す。
            SetScore(MinimumScore);
        }

        public ScoreRankingResult Submit()
        {
            //InspectorのDefault Player Nameを使って現在スコアを保存する。
            return Submit(m_defaultPlayerName);
        }

        public ScoreRankingResult Submit(string _playername)
        {
            //現在スコアをランキングへ保存し、自分の順位を返す。
            LastResult = ScoreRankingService.Submit(_playername, CurrentScore, m_sharedDirectoryOverride);
            Submitted?.Invoke(LastResult);

            if (m_bResetAfterSubmit)
            {
                ResetScore();
            }

            return LastResult;
        }
    }
}
