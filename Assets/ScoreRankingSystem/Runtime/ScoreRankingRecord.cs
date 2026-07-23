/*━━━━━━━━━
@file ScoreRankingRecord.cs
@brief スコアランキング1件分のデータ
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks IDで同じスコア・同じ名前の記録も別データとして識別
━━━━━━━━━*/

using System;

namespace CrossProjectScoreRanking
{
    /// <summary>
    /// CSVから読み書きするスコアランキングの1件分のデータです。
    /// 画面にはプレイヤー名とスコアを表示し、IDは内部識別に使います。
    /// </summary>
    [Serializable]
    public sealed class ScoreRankingRecord
    {
        public string m_id;             //内部識別用ID。同じ名前・同じスコアでも別記録として扱うために使う
        public string m_playerName;     //ランキングに表示するプレイヤー名
        public int m_score;             //ランキング比較に使うスコア。大きいほど上位
        public string m_recordedAtUtc;  //保存日時。ISO形式のUTC文字列でCSVへ保存する

        public DateTime RecordedAtUtc
        {
            get
            {
                DateTime parsed;        //CSV文字列から変換した日時

                if (DateTime.TryParse(m_recordedAtUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out parsed))
                {
                    return parsed;
                }

                return DateTime.MinValue;
            }
        }
    }
}
