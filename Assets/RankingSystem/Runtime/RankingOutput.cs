/*━━━━━━━━━
@file RankingOutput.cs
@brief UIへランキング値を出力するコンポーネント
@author 24CU0000 Name
@date 2026/07/09
最終更新日 2026/07/09
@remarks 実機表示UIに順位・名前・時間を渡すために使用
━━━━━━━━━*/

using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace CrossProjectRanking
{
    /// <summary>
    /// RankingDataLoaderが読み込んだ記録を、UIで扱いやすい形へ変換する出力用スクリプトです。
    /// TextMeshProに直接書くのではなく、任意のUIへ順位・名前・時間を個別に渡したい場合に使います。
    /// </summary>
    [RequireComponent(typeof(RankingDataLoader))]
    public sealed class RankingOutput : MonoBehaviour
    {
        private const int DefaultMaximumRows = 10;             //初期状態でUIへ出力する最大ランキング件数
        private const int MinimumRows = 1;                     //Inspectorで0件以下が指定されないようにする最小件数
        private const int MillisecondsPerMinute = 60000;       //ミリ秒を分に変換するための値。時間表示の分を作る
        private const int MillisecondsPerSecond = 1000;        //ミリ秒を秒へ変換するための値。時間表示の秒とミリ秒を作る

        [FormerlySerializedAs("loader")]
        [SerializeField] private RankingDataLoader m_loader;   //CSVを読み込み、今日のランキングやTop件数の元データを持つコンポーネント
        [SerializeField, Min(MinimumRows)] private int m_maximumRows = DefaultMaximumRows;  //UIへ出力する最大件数。トップ何位まで出すかを決める
        [SerializeField] private bool m_bOutputOnStart = true; //Start時に現在のランキングを一度UI用データへ変換するか

        public RankingOutputRow[] Rows { get; private set; } = Array.Empty<RankingOutputRow>(); //UIへ渡す現在のランキング行
        public event Action<RankingOutputRow[]> Outputted;    //ランキング出力が更新された時にUIへ通知するイベント

        private void Awake()
        {
            if (m_loader == null)
            {
                m_loader = GetComponent<RankingDataLoader>();
            }
        }

        private void OnEnable()
        {
            if (m_loader != null)
            {
                m_loader.Loaded += OnLoaded;
            }
        }

        private void Start()
        {
            if (m_bOutputOnStart && m_loader != null)
            {
                BuildRows(m_loader.Records);
            }
        }

        private void OnDisable()
        {
            if (m_loader != null)
            {
                m_loader.Loaded -= OnLoaded;
            }
        }

        public void Reload()
        {
            //UI側から手動でランキングを更新したい場合に呼ぶ。
            //Reload後はRankingDataLoader.Loadedイベント経由でRowsが更新される。
            if (m_loader == null) { return; }

            m_loader.Reload();
        }

        public void SetMaximumRows(int _maximumrows)
        {
            //セットアップ用スクリプトから、トップ何位まで出力するかを変更する。
            m_maximumRows = Mathf.Max(MinimumRows, _maximumrows);
            BuildRows(m_loader == null ? Array.Empty<RankingRecord>() : m_loader.Records);
        }

        public RankingOutputRow[] GetRows()
        {
            //現在UIへ渡せるランキング配列を返す。
            //配列を書き換えられないようコピーして返す。
            return (RankingOutputRow[])Rows.Clone();
        }

        public int GetCount()
        {
            //現在出力されているランキング行数を返す。
            return Rows.Length;
        }

        public int GetRank(int _index)
        {
            //UI側が個別Textへ順位を入れたい場合に使う。
            if (!IsValidIndex(_index)) { return 0; }

            return Rows[_index].m_rank;
        }

        public string GetPlayerName(int _index)
        {
            //UI側が個別Textへ名前を入れたい場合に使う。
            if (!IsValidIndex(_index)) { return string.Empty; }

            return Rows[_index].m_playerName;
        }

        public string GetTimeText(int _index)
        {
            //UI側が個別Textへ時間を入れたい場合に使う。
            if (!IsValidIndex(_index)) { return string.Empty; }

            return Rows[_index].m_timeText;
        }

        public long GetTimeMilliseconds(int _index)
        {
            //UI側で独自フォーマット表示を行う場合に使う。
            if (!IsValidIndex(_index)) { return 0; }

            return Rows[_index].m_timeMilliseconds;
        }

        public string GetId(int _index)
        {
            //表示には使わないが、同じ内容の記録を識別したい場合に使う。
            if (!IsValidIndex(_index)) { return string.Empty; }

            return Rows[_index].m_id;
        }

        private void OnLoaded(RankingRecord[] _records)
        {
            //CSVが読み込まれたら、UI用の行データへ変換する。
            BuildRows(_records);
        }

        private void BuildRows(RankingRecord[] _records)
        {
            RankingRecord[] records = _records ?? Array.Empty<RankingRecord>();    //変換元のランキング記録
            int count = Mathf.Min(m_maximumRows, records.Length);                  //実際にUIへ出力する件数
            RankingOutputRow[] rows = new RankingOutputRow[count];                 //UIへ渡す行データ

            for (int i = 0; i < count; ++i)
            {
                rows[i] = new RankingOutputRow
                {
                    m_rank = i + 1,
                    m_id = records[i].m_id,
                    m_playerName = records[i].m_playerName,
                    m_timeMilliseconds = records[i].m_timeMilliseconds,
                    m_timeText = FormatTime(records[i].m_timeMilliseconds)
                };
            }

            Rows = rows;
            Outputted?.Invoke(Rows);
        }

        private bool IsValidIndex(int _index)
        {
            //指定された行番号がRowsの範囲内か確認する。
            return _index >= 0 && _index < Rows.Length;
        }

        private static string FormatTime(long _milliseconds)
        {
            //ミリ秒の整数値をUI表示用の 00:00.000 形式に変換する。
            long minutes = _milliseconds / MillisecondsPerMinute;                         //分
            long seconds = _milliseconds % MillisecondsPerMinute / MillisecondsPerSecond;  //秒
            long remainder = _milliseconds % MillisecondsPerSecond;                       //ミリ秒

            return $"{minutes:00}:{seconds:00}.{remainder:000}";
        }
    }
}
