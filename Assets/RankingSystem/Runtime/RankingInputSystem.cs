/*━━━━━━━━━
@file RankingInputSystem.cs
@brief アタッチだけで使えるランキング入力セットアップ
@author 24CU0000 Name
@date 2026/07/09
最終更新日 2026/07/09
@remarks 実機側ゲームから名前と時間を渡してCSVへ保存する
━━━━━━━━━*/

using UnityEngine;

namespace CrossProjectRanking
{
    /// <summary>
    /// 入力側Unityに置くための実機向けセットアップスクリプトです。
    /// 空のGameObjectにこのスクリプトを付けるだけでRankingDataWriterも自動で用意されます。
    /// ゲーム本体からはInput(playerName, timeSeconds)を呼ぶだけでランキングへ保存できます。
    /// </summary>
    [RequireComponent(typeof(RankingDataWriter))]
    public sealed class RankingInputSystem : MonoBehaviour
    {
        private const string DefaultSharedDirectoryOverride = "../RankingData"; //親フォルダのRankingDataを共有フォルダとして使う初期設定

        [Tooltip("入力側と表示側で共有するRankingDataフォルダ。親フォルダ共有形式なら ../RankingData のままで使います。")]
        [SerializeField] private string m_sharedDirectoryOverride = DefaultSharedDirectoryOverride; //CSV保存先フォルダ。入力側と表示側で同じ値にする

        private RankingDataWriter m_writer; //実際にCSV保存を行うコンポーネント。このスクリプトと同じGameObjectに自動追加される

        private void Awake()
        {
            //同じGameObjectにあるRankingDataWriterを取得する。
            //RequireComponentにより、手動追加し忘れてもUnityが自動で付ける。
            m_writer = GetComponent<RankingDataWriter>();
            m_writer.SetSharedDirectoryOverride(m_sharedDirectoryOverride);
        }

        public bool Input(
            string _playername,
            float _timeseconds)
        {
            //ゲーム本体からプレイヤー名と秒タイムを渡して保存するための入口。
            //例: m_rankingInputSystem.Input(playerName, clearTime);
            if (m_writer == null)
            {
                m_writer = GetComponent<RankingDataWriter>();
                m_writer.SetSharedDirectoryOverride(m_sharedDirectoryOverride);
            }

            return m_writer.Input(_playername, _timeseconds);
        }

        public bool Input(float _timeseconds)
        {
            //名前がまだ決まっていない場合や、時間だけを保存したい場合の入口。
            //名前はNoName_日付_時間として自動生成される。
            if (m_writer == null)
            {
                m_writer = GetComponent<RankingDataWriter>();
                m_writer.SetSharedDirectoryOverride(m_sharedDirectoryOverride);
            }

            return m_writer.Input(_timeseconds);
        }

        public bool InputMilliseconds(
            string _playername,
            long _timemilliseconds)
        {
            //ゲーム本体のタイマーがミリ秒整数の場合の入口。
            //CSVには秒表示用と比較用ミリ秒の両方が保存される。
            if (m_writer == null)
            {
                m_writer = GetComponent<RankingDataWriter>();
                m_writer.SetSharedDirectoryOverride(m_sharedDirectoryOverride);
            }

            return m_writer.InputMilliseconds(_playername, _timemilliseconds);
        }
    }
}
