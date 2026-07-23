/*━━━━━━━━━
@file ScoreRankingService.cs
@brief スコア保存と自分の順位計算
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks ゲーム終了時に呼び出す中心処理
━━━━━━━━━*/

using System;

namespace CrossProjectScoreRanking
{
    /// <summary>
    /// スコアをCSVへ保存し、保存した記録が何位かを計算するクラスです。
    /// </summary>
    public static class ScoreRankingService
    {
        private const string NoNamePrefix = "NoName_";             //名前未入力時に付ける接頭辞
        private const string NoNameDateFormat = "yyyyMMdd_HHmmss"; //名前未入力時に使う日時形式

        public static ScoreRankingResult Submit(
            string _playername,
            int _score,
            string _shareddirectoryoverride)
        {
            //保存先フォルダを設定してから、今回のスコアをCSVへ追記する。
            ScoreRankingStorage.SetSharedDirectory(_shareddirectoryoverride);

            ScoreRankingRecord record = new ScoreRankingRecord
            {
                m_id = Guid.NewGuid().ToString("N"),
                m_playerName = ResolvePlayerName(_playername),
                m_score = _score,
                m_recordedAtUtc = DateTime.UtcNow.ToString("O")
            };

            bool succeeded = ScoreRankingStorage.Append(record); //CSV保存に成功したか

            if (!succeeded)
            {
                return new ScoreRankingResult
                {
                    m_rank = 0,
                    m_totalCount = 0,
                    m_record = record,
                    m_bSucceeded = false
                };
            }

            return CreateResult(record);
        }

        public static ScoreRankingResult Submit(int _score)
        {
            //名前なしでスコアだけ保存する簡易関数。
            return Submit(string.Empty, _score, string.Empty);
        }

        public static ScoreRankingResult Submit(
            string _playername,
            int _score)
        {
            //名前とスコアだけを指定して保存する簡易関数。
            return Submit(_playername, _score, string.Empty);
        }

        private static ScoreRankingResult CreateResult(ScoreRankingRecord _savedrecord)
        {
            ScoreRankingRecord[] records = ScoreRankingSorter.HighestScoreFirst(ScoreRankingStorage.Load()); //保存後の全記録
            int rank = FindRank(records, _savedrecord.m_id); //今回保存した記録の順位

            return new ScoreRankingResult
            {
                m_rank = rank,
                m_totalCount = records.Length,
                m_record = _savedrecord,
                m_bSucceeded = rank > 0
            };
        }

        private static int FindRank(
            ScoreRankingRecord[] _records,
            string _id)
        {
            //IDで今回保存した記録を探す。
            //同じ名前・同じスコアが複数あっても、IDで自分の記録だけを見つけられる。
            for (int i = 0; i < _records.Length; ++i)
            {
                if (_records[i].m_id == _id)
                {
                    return i + 1;
                }
            }

            return 0;
        }

        private static string ResolvePlayerName(string _playername)
        {
            //名前が空ならNoName_日付_時間を自動生成する。
            if (!string.IsNullOrWhiteSpace(_playername))
            {
                return _playername.Trim();
            }

            return NoNamePrefix + DateTime.Now.ToString(NoNameDateFormat);
        }
    }
}
