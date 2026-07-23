/*━━━━━━━━━
@file ScoreRankingResult.cs
@brief スコア保存後の自分の順位情報
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks ゲーム終了画面で自分の順位を表示するために使用
━━━━━━━━━*/

using System;

namespace CrossProjectScoreRanking
{
    /// <summary>
    /// スコア保存後に、自分の順位をゲーム画面へ返すための結果データです。
    /// </summary>
    [Serializable]
    public struct ScoreRankingResult
    {
        public int m_rank;                      //保存したスコアの順位。1位から始まる
        public int m_totalCount;                //CSV内の合計記録数
        public ScoreRankingRecord m_record;     //今回保存した自分の記録
        public bool m_bSucceeded;               //保存と順位計算に成功したか
    }
}
