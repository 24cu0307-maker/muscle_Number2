/*━━━━━━━━━
@file RankingOutputRow.cs
@brief UI表示用ランキング1行データ
@author 24CU0000 Name
@date 2026/07/09
最終更新日 2026/07/09
@remarks 表示側UIに順位・名前・時間を渡すために使用
━━━━━━━━━*/

using System;

namespace CrossProjectRanking
{
    /// <summary>
    /// 表示側UIへ渡すための1行分のランキングデータです。
    /// IDは画面に表示しませんが、同じ名前・同じ時間の記録を区別するために保持します。
    /// </summary>
    [Serializable]
    public struct RankingOutputRow
    {
        public int m_rank;                 //表示する順位。1位から始まる
        public string m_id;                //内部識別用ID。同じ名前・同じ時間でも別記録として扱うために使う
        public string m_playerName;        //UIに表示するプレイヤー名
        public long m_timeMilliseconds;    //比較用のミリ秒タイム。必要ならUI側で独自表示に使う
        public string m_timeText;          //UIにそのまま表示できる 00:00.000 形式のタイム文字列
    }
}
