/*━━━━━━━━━
@file RaceTimeRanking.cs
@brief ゲーム側から走行タイムを1行で保存するAPI
@author 24CU0000 Name
@date 2026/07/02
最終更新日 2026/07/02
@remarks RaceTimeRanking.Submit(timeSeconds)だけでも保存可能
━━━━━━━━━*/

using System;
using UnityEngine;

namespace CrossProjectRanking
{
    /// <summary>
    /// ゲーム本体からランキング記録を保存するための窓口です。
    /// Sceneにコンポーネントを置かなくても
    /// RaceTimeRanking.Submit(timeSeconds) の1行でも保存できます。
    /// </summary>
    public static class RaceTimeRanking
    {
        private const string NoNamePrefix = "NoName_";         //名前が入力されていない場合に自動生成するプレイヤー名の先頭文字列
        private const string IdFormat = "N";                   //Guidをハイフンなし文字列にするための書式。CSVのID列を短めに保つ
        private const string DateTimeFormat = "o";             //日時をISO 8601形式で保存するための書式。別PCや別地域でも日時を復元しやすい
        private const string PlayerNameDateTimeFormat = "yyyyMMdd_HHmmss"; //NoNameに付ける日付とリアル時間の書式。例: 20260702_143000
        private const double MillisecondsPerSecond = 1000.0;   //秒をミリ秒へ変換するための値。ランキング比較用の整数タイム作成に使う

        public static void SetSharedDirectory(string _directorypath)
        {
            //入力側と表示側で同じCSVを使う場合に、保存先フォルダを明示的に指定する。
            //空文字を渡した場合は、各Unityプロジェクト直下のRankingDataを使う。
            RankingStorage.SetSharedDirectory(_directorypath);
        }

        public static bool Submit(float _timeseconds)
        {
            //実行環境からタイムだけ渡される場合の保存関数。
            //名前はResolvePlayerNameでNoName_日付_時間に自動変換される。
            return Submit(string.Empty, _timeseconds);
        }

        public static bool Submit(
            float _timeseconds,
            string _coursename)
        {
            //タイムとコース名だけ渡される場合の保存関数。
            //プレイヤー名は未入力扱いにして自動生成する。
            //現在のランキング表示ではコース名を使わないため、互換性のために引数だけ残している。
            return Submit(string.Empty, _timeseconds, _coursename, null);
        }

        public static bool Submit(string _playername, float _timeseconds)
        {
            //表示に必要なプレイヤー名とタイムだけを保存する。
            return Submit(_playername, _timeseconds, null, null);
        }

        public static bool Submit(
            string _playername,
            float _timeseconds,
            string _coursename)
        {
            //保存先フォルダ指定が必要ない通常ケース用。
            //現在のランキング表示ではコース名を使わないため、互換性のために引数だけ残している。
            return Submit(_playername, _timeseconds, _coursename, null);
        }

        public static bool Submit(
            string _playername,
            float _timeseconds,
            string _coursename,
            string _shareddirectoryoverride)
        {
            //保存先フォルダが指定されている場合だけ、CSVの保存先を上書きする。
            if (!string.IsNullOrWhiteSpace(_shareddirectoryoverride))
            {
                RankingStorage.SetSharedDirectory(_shareddirectoryoverride);
            }

            //タイムが0以下、NaN、Infinityの場合は異常値なので保存しない。
            if (_timeseconds <= 0.0f || float.IsNaN(_timeseconds) || float.IsInfinity(_timeseconds))
            {
                Debug.LogWarning("走行タイムが不正なため保存しませんでした。");
                return false;
            }

            string playerName = ResolvePlayerName(_playername);    //CSVへ保存するプレイヤー名。未入力ならNoName_日付_時間を入れる

            //CSVへ保存する1行分のデータを作成する。
            //タイムは秒ではなくミリ秒整数に変換して、ランキング比較を安定させる。
            RankingRecord record = new RankingRecord
            {
                m_id = Guid.NewGuid().ToString(IdFormat),
                m_playerName = playerName,
                m_timeMilliseconds = (long)Math.Round(_timeseconds * MillisecondsPerSecond),
                m_recordedAtUtc = DateTime.UtcNow.ToString(DateTimeFormat)
            };

            return RankingStorage.Append(record);
        }

        private static string ResolvePlayerName(string _playername)
        {
            //名前が入力されている場合は、前後の空白だけ取り除いてそのまま使う。
            if (!string.IsNullOrWhiteSpace(_playername))
            {
                return _playername.Trim();
            }

            //名前が入力されていない場合は、実行PCの現在日時から一意に近い名前を作る。
            //例: NoName_20260702_143000
            return NoNamePrefix + DateTime.Now.ToString(PlayerNameDateTimeFormat);
        }
    }
}
