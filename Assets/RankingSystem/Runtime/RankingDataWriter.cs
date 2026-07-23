/*━━━━━━━━━
@file RankingDataWriter.cs
@brief UnityコンポーネントからランキングCSVへ保存
@author 24CU0000 Name
@date 2026/07/02
最終更新日 2026/07/02
@remarks 手入力デモやUnityEventから使用
━━━━━━━━━*/

using UnityEngine;
using UnityEngine.Serialization;

namespace CrossProjectRanking
{
    /// <summary>
    /// UnityのGameObjectに付けて使う保存用コンポーネントです。
    /// 手入力デモやUnityEventから呼ぶ場合はこのクラスを使います。
    /// ゲームコードから直接保存したい場合はRaceTimeRanking.Submitを使っても同じCSVへ保存できます。
    /// </summary>
    public sealed class RankingDataWriter : MonoBehaviour
    {
        private const float SecondsPerMillisecond = 1000.0f;   //ミリ秒を秒に変換するための値。WriteMillisecondsから秒指定のWriteへ渡す時に使う
        private const string DefaultPlayerName = "";           //名前未設定時は空文字にして、RaceTimeRanking側でNoName_日付_時間を自動生成する
        private const string LegacyDefaultPlayerName = "Player";   //以前の初期プレイヤー名。既存Sceneに残っている場合は未入力扱いへ移行する

        [Tooltip("空欄なら現在のプロジェクト内のRankingDataへ保存します。別プロジェクトと共有する場合は同じ絶対パスを指定します。")]
        [FormerlySerializedAs("sharedDirectoryOverride")]
        [SerializeField] private string m_sharedDirectoryOverride = "";     //CSV保存先フォルダを固定するためのパス。入力側と表示側で同じフォルダを指定する

        [Tooltip("Write(timeSeconds) を使う場合のプレイヤー名です。")]
        [FormerlySerializedAs("defaultPlayerName")]
        [SerializeField] private string m_defaultPlayerName = DefaultPlayerName;    //Write(float)だけで保存する時に使うプレイヤー名。空欄ならNoName_日付_時間になる

        private void Awake()
        {
            //過去版の初期値「Player」がSceneに残っている場合は、名前未入力として扱う。
            //これにより、実行環境からTimeだけ渡した時にNoName_日付_時間が入る。
            if (m_defaultPlayerName == LegacyDefaultPlayerName)
            {
                m_defaultPlayerName = DefaultPlayerName;
            }

            //Scene開始時に保存先フォルダを設定する。
            //空欄の場合はRankingStorage側でプロジェクト直下のRankingDataが使われる。
            RankingStorage.SetSharedDirectory(m_sharedDirectoryOverride);
        }

        public void SetSharedDirectoryOverride(string _shareddirectoryoverride)
        {
            //実行中に保存先フォルダを変更したい場合に使う。
            //セットアップ用スクリプトから呼び、入力側と表示側が同じRankingDataを見るようにする。
            m_sharedDirectoryOverride = _shareddirectoryoverride;
            RankingStorage.SetSharedDirectory(m_sharedDirectoryOverride);
        }

        public bool Write(float _timeseconds)
        {
            //名前をInspectorの初期値から使う簡易保存関数。
            //UnityEventなど、引数を1つだけ渡したい場合に使いやすい。
            return Write(m_defaultPlayerName, _timeseconds);
        }

        public bool Input(float _timeseconds)
        {
            //実機側のプログラムから時間だけを渡す場合の入力関数。
            //名前が空の場合はRaceTimeRanking側でNoName_日付_時間に変換される。
            return Write(_timeseconds);
        }

        public bool Input(
            string _playername,
            float _timeseconds)
        {
            //実機側のプログラムからプレイヤー名と時間を渡す場合の入力関数。
            //例: m_rankingDataWriter.Input(playerName, clearTime);
            return Write(_playername, _timeseconds);
        }

        public bool WriteMilliseconds(long _timemilliseconds)
        {
            //ミリ秒で受け取ったタイムを秒へ変換して保存する。
            //ゲーム側のタイマーがミリ秒管理の場合に使う。
            return Write(m_defaultPlayerName, _timemilliseconds / SecondsPerMillisecond);
        }

        public bool InputMilliseconds(
            string _playername,
            long _timemilliseconds)
        {
            //実機側のタイマーがミリ秒整数の場合に使う入力関数。
            //内部では秒へ変換してCSVへ保存する。
            return Write(_playername, _timemilliseconds / SecondsPerMillisecond);
        }

        public bool Write(
            string _playername,
            float _timeseconds)
        {
            //実際の保存処理はRaceTimeRankingへ集約する。
            //保存ルールを1か所にまとめることで、手入力とゲーム本体で挙動がズレないようにしている。
            return RaceTimeRanking.Submit(_playername, _timeseconds, null, m_sharedDirectoryOverride);
        }

        public bool Write(
            string _playername,
            float _timeseconds,
            string _unusedcoursename)
        {
            //旧コード互換用。現在のランキング表示ではコース名を使わないため、値は保存しない。
            return Write(_playername, _timeseconds);
        }
    }
}
