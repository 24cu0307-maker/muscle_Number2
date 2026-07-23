/*━━━━━━━━━
@file GameResultContainer.cs
@brief ゲーム終了時の結果データ格納用コンテナ
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks ScoreRankingSystemへ渡した結果も保持する
━━━━━━━━━*/

using System;

namespace GameFlowTemplate
{
    /// <summary>
    /// ゲーム終了時に、スコア・時間・ランキング結果をまとめて保持するコンテナです。
    /// 結果画面や次シーンへの受け渡しに使います。
    /// </summary>
    [Serializable]
    public sealed class GameResultContainer
    {
        public string m_playerName;             //結果を出したプレイヤー名
        public int m_finalScore;                //ゲーム終了時の最終スコア
        public float m_playTimeSeconds;         //ゲーム終了時のプレイ時間。秒単位
        public string m_playTimeText;           //表示用に整形されたプレイ時間
        public int m_rank;                      //ScoreRankingSystemへ保存した後の自分の順位
        public int m_totalRankingCount;         //ランキングCSV内の合計記録数
        public bool m_bRankingSubmitted;        //ランキング保存と順位取得に成功したか
    }
}
