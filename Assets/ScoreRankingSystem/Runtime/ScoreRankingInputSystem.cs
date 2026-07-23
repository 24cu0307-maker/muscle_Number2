/*━━━━━━━━━
@file ScoreRankingInputSystem.cs
@brief アタッチだけで使えるスコア入力セットアップ
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks ゲーム側からスコア加算と保存を簡単に呼ぶ
━━━━━━━━━*/

using UnityEngine;

namespace CrossProjectScoreRanking
{
    /// <summary>
    /// 入力側Unityに置くための実機向けセットアップスクリプトです。
    /// 空のGameObjectに付けるだけで、スコア加算とランキング保存を使えるようにします。
    /// </summary>
    [RequireComponent(typeof(ScoreCounter))]
    public sealed class ScoreRankingInputSystem : MonoBehaviour
    {
        private const string DefaultSharedDirectoryOverride = "../RankingData"; //親フォルダのRankingDataを共有フォルダとして使う初期設定

        [Tooltip("入力側と表示側で共有するRankingDataフォルダ。親フォルダ共有形式なら ../RankingData のままで使います。")]
        [SerializeField] private string m_sharedDirectoryOverride = DefaultSharedDirectoryOverride; //CSV保存先フォルダ

        private ScoreCounter m_counter; //実際にスコアを保持し、CSV保存するコンポーネント

        public int CurrentScore
        {
            get
            {
                EnsureCounter();
                return m_counter.CurrentScore;
            }
        }

        private void Awake()
        {
            EnsureCounter();
        }

        public void AddScore(int _score)
        {
            //ゲーム中にスコアが増えた時に呼ぶ。
            EnsureCounter();
            m_counter.AddScore(_score);
        }

        public void SetScore(int _score)
        {
            //現在スコアを直接指定したい時に呼ぶ。
            EnsureCounter();
            m_counter.SetScore(_score);
        }

        public void ResetScore()
        {
            //ゲーム開始時やリトライ時に呼ぶ。
            EnsureCounter();
            m_counter.ResetScore();
        }

        public ScoreRankingResult Submit(string _playername)
        {
            //ゲーム終了時に名前付きで保存し、自分の順位を受け取る。
            EnsureCounter();
            return m_counter.Submit(_playername);
        }

        public ScoreRankingResult Submit()
        {
            //ゲーム終了時に名前なしで保存する。
            //名前はNoName_日付_時間として自動生成される。
            EnsureCounter();
            return m_counter.Submit();
        }

        private void EnsureCounter()
        {
            if (m_counter != null) { return; }

            m_counter = GetComponent<ScoreCounter>();
            m_counter.SetSharedDirectoryOverride(m_sharedDirectoryOverride);
        }
    }
}
