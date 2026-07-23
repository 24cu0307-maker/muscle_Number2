/*━━━━━━━━━
@file RankingDisplay.cs
@brief TextMeshProへランキングを表示
@author 24CU0000 Name
@date 2026/07/02
最終更新日 2026/07/02
@remarks Canvas UI用
━━━━━━━━━*/

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace CrossProjectRanking
{
    /// <summary>
    /// RankingDataLoaderが読み込んだ記録をTextMeshProへ表示するクラスです。
    /// Canvasを使った本番UIを作る場合はこちらを使います。
    /// </summary>
    public sealed class RankingDisplay : MonoBehaviour
    {
        private const int DefaultMaximumRows = 10;             //TextMeshPro表示で初期状態に表示するランキング件数
        private const int MillisecondsPerMinute = 60000;       //ミリ秒を分に変換するための値。タイム表示の分部分を作る
        private const int MillisecondsPerSecond = 1000;        //ミリ秒を秒に変換するための値。タイム表示の秒・ミリ秒部分を作る
        private const string LineBreak = "\n";                 //TextMeshProに複数行ランキングを表示するための改行文字
        private const string EmptyMessage = "ランキングデータがありません"; //CSVに記録がない時、空欄ではなく状態を伝えるための表示文

        [FormerlySerializedAs("loader")]
        [SerializeField] private RankingDataLoader m_loader;                   //CSVを読み込むコンポーネント。Loadedイベントを受け取って表示を更新する
        [FormerlySerializedAs("rankingText")]
        [SerializeField] private TMP_Text m_rankingText;                       //ランキング文字列を書き込むTextMeshProコンポーネント
        [FormerlySerializedAs("maximumRows")]
        [SerializeField, Min(1)] private int m_maximumRows = DefaultMaximumRows;   //画面に表示する最大行数。CSV件数が多くても上位だけ出すために使う
        [FormerlySerializedAs("emptyMessage")]
        [SerializeField] private string m_emptyMessage = EmptyMessage;         //表示対象が0件だった場合にTextMeshProへ入れるメッセージ

        private void OnEnable()
        {
            //Loaderの読み込み完了イベントを受け取り、CSV更新時に表示を更新する。
            if (m_loader != null)
            {
                m_loader.Loaded += Render;
            }
        }

        private void Start()
        {
            //Start時点ですでに読み込み済みの記録があれば、すぐ画面に表示する。
            if (m_loader != null && m_loader.Records.Length > 0)
            {
                Render(m_loader.Records);
            }
            else if (m_rankingText != null)
            {
                //記録がない場合は空メッセージを表示する。
                m_rankingText.text = m_emptyMessage;
            }
        }

        private void OnDisable()
        {
            //無効化時にイベント登録を解除し、不要な呼び出しを防ぐ。
            if (m_loader != null)
            {
                m_loader.Loaded -= Render;
            }
        }

        public void Render(RankingRecord[] _records)
        {
            //表示先TextMeshProが未設定なら何もできないため終了する。
            if (m_rankingText == null) { return; }

            //表示する記録がない場合は、ランキングなしのメッセージを出す。
            if (_records == null || _records.Length == 0)
            {
                m_rankingText.text = m_emptyMessage;
                return;
            }

            //各記録を1行ずつ文字列化し、最後に改行で結合してTextMeshProへ入れる。
            List<string> rows = new List<string>();    //表示行
            int count = Mathf.Min(m_maximumRows, _records.Length); //表示件数

            for (int i = 0; i < count; ++i)
            {
                RankingRecord record = _records[i];    //現在の記録
                rows.Add($"{i + 1}. {record.m_playerName}  {FormatTime(record.m_timeMilliseconds)}");
            }

            m_rankingText.text = string.Join(LineBreak, rows);
        }

        private static string FormatTime(long _milliseconds)
        {
            //ミリ秒の整数値を 00:00.000 形式へ変換する。
            long minutes = _milliseconds / MillisecondsPerMinute;                  //分
            long seconds = _milliseconds % MillisecondsPerMinute / MillisecondsPerSecond; //秒
            long remainder = _milliseconds % MillisecondsPerSecond;                //ミリ秒

            return $"{minutes:00}:{seconds:00}.{remainder:000}";
        }
    }
}
