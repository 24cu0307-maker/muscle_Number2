/*━━━━━━━━━
@file ScoreRankingSorter.cs
@brief スコアランキングの並び替え
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks スコアの高い順に並べる
━━━━━━━━━*/

using System;

namespace CrossProjectScoreRanking
{
    /// <summary>
    /// スコアランキングの並び替えだけを担当するクラスです。
    /// </summary>
    public static class ScoreRankingSorter
    {
        public static ScoreRankingRecord[] HighestScoreFirst(ScoreRankingRecord[] _records)
        {
            //元配列を直接並び替えないようコピーしてからソートする。
            ScoreRankingRecord[] records = _records == null
                ? Array.Empty<ScoreRankingRecord>()
                : (ScoreRankingRecord[])_records.Clone();

            Array.Sort(records, CompareHighestScoreFirst);
            return records;
        }

        private static int CompareHighestScoreFirst(
            ScoreRankingRecord _left,
            ScoreRankingRecord _right)
        {
            //スコアが高い方を上位にする。
            int scoreCompare = _right.m_score.CompareTo(_left.m_score); //スコア比較結果

            if (scoreCompare != 0)
            {
                return scoreCompare;
            }

            //同じスコアの場合は、先に記録した方を上位にする。
            return _left.RecordedAtUtc.CompareTo(_right.RecordedAtUtc);
        }
    }
}
