/*━━━━━━━━━
@file ManualRankingDemo.cs
@brief 入力とランキング表示を同時に確認するデモ
@author 24CU0000 Name
@date 2026/07/02
最終更新日 2026/07/02
@remarks 単体プロジェクトでの動作確認用
━━━━━━━━━*/

using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.Serialization;

namespace CrossProjectRanking
{
    /// <summary>
    /// 入力とランキング表示を1画面で確認するためのIMGUIデモです。
    /// 保存後すぐにCSVを読み直し、ランキングが更新されることを確認できます。
    /// </summary>
    [RequireComponent(typeof(RankingDataWriter))]
    public sealed class ManualRankingDemo : MonoBehaviour
    {
        private const int DefaultMaximumRows = 20;              //初期最大表示件数。ランキング欄に表示する上位件数の初期値
        private const float AreaWidth = 620.0f;                 //デモ画面の横幅。入力欄とランキングが収まるようにする
        private const float AreaTop = 20.0f;                    //デモ画面を上端からどれだけ離して配置するか
        private const float AreaMargin = 10.0f;                 //画面端との最小余白。中央寄せ時に画面外へ出ないように使う
        private const float AreaHeightMargin = 40.0f;           //デモ画面の高さを決める時、画面下に残す余白
        private const float MinimumAreaHeight = 300.0f;         //画面が小さい時でも最低限確保するデモ画面の高さ
        private const float TitleSpace = 8.0f;                  //タイトルと入力欄の間に入れる余白
        private const float HeaderSpace = 6.0f;                 //保存ボタンとランキング見出しの間に入れる余白
        private const float ButtonHeight = 36.0f;               //保存ボタン・再読み込みボタンの高さ
        private const float FieldHeight = 28.0f;                //プレイヤー名・タイム入力欄の高さ
        private const float LabelWidth = 110.0f;                //入力ラベルの横幅。各入力欄の位置を揃えるために使う
        private const int TitleFontSize = 24;                   //デモ画面タイトルの文字サイズ
        private const int HeaderFontSize = 18;                  //ランキング見出しの文字サイズ
        private const int TimePartsSecondsOnly = 1;             //秒だけ入力された時のSplit結果数。秒入力かどうかの判定に使う
        private const int TimePartsMinutesSeconds = 2;          //分:秒で入力された時のSplit結果数。分:秒形式かどうかの判定に使う
        private const int MillisecondsPerMinute = 60000;        //ミリ秒を分へ変換するための値。ランキングのタイム表示に使う
        private const int MillisecondsPerSecond = 1000;         //ミリ秒を秒・ミリ秒へ分けるための値。ランキングのタイム表示に使う
        private const double MinimumTimeSeconds = 0.0;          //保存可能な最小秒数。0秒以下や変換失敗値を保存しないために使う
        private const double SecondsPerMinute = 60.0;           //分を秒へ変換するための値。分:秒入力を合計秒数へ直す時に使う

        [FormerlySerializedAs("maximumRows")]
        [SerializeField, Min(1)] private int m_maximumRows = DefaultMaximumRows;     //ランキング欄に表示する最大件数。CSVに多く記録があっても上位だけ表示する

        private RankingDataWriter m_writer;                                         //入力内容をCSVへ保存するコンポーネント。保存ボタン処理で使用する
        private RankingRecord[] m_records = Array.Empty<RankingRecord>();           //CSVから読み込んだランキング記録。FastestFirstで並べ替えた後に保持する
        private string m_playerName = "Player 1";                                   //画面で入力されたプレイヤー名。保存時にCSVのPlayerName列へ入れる
        private string m_timeText = "01:23.456";                                    //画面で入力されたタイム文字列。保存前に秒数へ変換する
        private Vector2 m_scrollPosition;                                           //ランキング一覧のスクロール位置。表示件数が多い時に位置を保持する

        private void Awake()
        {
            //同じGameObjectにある保存用コンポーネントを取得する。
            m_writer = GetComponent<RankingDataWriter>();
            //起動時点のCSV内容を読み、ランキングを表示できるようにする。
            Reload();
        }

        private void OnGUI()
        {
            //画面中央に入力とランキングをまとめて表示する。
            float left = Mathf.Max(AreaMargin, (Screen.width - AreaWidth) * 0.5f);  //表示左位置
            Rect area = new Rect(left, AreaTop, AreaWidth, Mathf.Max(MinimumAreaHeight, Screen.height - AreaHeightMargin)); //表示範囲

            GUILayout.BeginArea(area, GUI.skin.box);
            GUILayout.Label("レースタイム手動入力デモ", TitleStyle());
            GUILayout.Space(TitleSpace);

            m_playerName = LabeledTextField("プレイヤー名", m_playerName);
            m_timeText = LabeledTextField("タイム", m_timeText);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("保存して反映", GUILayout.Height(ButtonHeight)))
            {
                //入力内容をCSVへ保存し、その後ランキングを再読み込みする。
                SaveAndReload();
            }

            if (GUILayout.Button("CSVを再読み込み", GUILayout.Height(ButtonHeight)))
            {
                //CSVだけを読み直す。Excelなど外部で編集した後の確認にも使える。
                Reload();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(HeaderSpace);
            GUILayout.Label("ランキング（タイムが速い順）", HeaderStyle());

            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, GUI.skin.box);
            DrawRankingRows();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void SaveAndReload()
        {
            double seconds;                                                         //秒に変換したタイム

            //タイム入力が不正なら保存しない。
            if (!TryParseTime(m_timeText, out seconds))
            {
                Debug.LogWarning("タイムを読み取れません。");
                return;
            }

            //保存に成功した時だけランキングを再読み込みする。
            if (m_writer.Write(m_playerName, (float)seconds))
            {
                Reload();
            }
        }

        private void Reload()
        {
            //CSVを読み込み、タイムが速い順に並べて画面表示用配列へ保持する。
            m_records = RankingDateSorter.FastestFirst(RankingStorage.Load());
        }

        private void DrawRankingRows()
        {
            //記録がない場合は空表示にする。
            if (m_records.Length == 0)
            {
                GUILayout.Label("記録はまだありません。");
                return;
            }

            //最大表示件数を超えない範囲でランキングを描画する。
            int count = Mathf.Min(m_maximumRows, m_records.Length);                 //表示件数

            for (int i = 0; i < count; ++i)
            {
                RankingRecord record = m_records[i];                                //現在の記録
                GUILayout.Label(
                    $"{i + 1,2}位  {FormatTime(record.m_timeMilliseconds)}  " +
                    $"{record.m_playerName}");
            }
        }

        private static bool TryParseTime(string _value, out double _seconds)
        {
            //入力は「83.456」のような秒のみ、または「01:23.456」のような分:秒を受け付ける。
            _seconds = MinimumTimeSeconds;

            if (string.IsNullOrWhiteSpace(_value)) { return false; }

            string trimmed = _value.Trim();                                         //前後空白を除いた文字列
            string[] parts = trimmed.Split(':');                                    //分秒分割

            if (parts.Length == TimePartsSecondsOnly)
            {
                //秒だけの入力をそのまま数値へ変換する。
                return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _seconds) && _seconds > MinimumTimeSeconds;
            }

            double minutes;                                                         //分
            double remainingSeconds;                                                //秒

            //分:秒形式の入力をチェックする。
            if (parts.Length != TimePartsMinutesSeconds ||
                !double.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out remainingSeconds) ||
                minutes < MinimumTimeSeconds ||
                remainingSeconds < MinimumTimeSeconds ||
                remainingSeconds >= SecondsPerMinute)
            {
                return false;
            }

            //分と秒を合計秒数へ変換する。
            _seconds = minutes * SecondsPerMinute + remainingSeconds;
            return _seconds > MinimumTimeSeconds;
        }

        private static string LabeledTextField(string _label, string _value)
        {
            //ラベルと入力欄を横並びにして、入力後の文字列を返す。
            GUILayout.BeginHorizontal();
            GUILayout.Label(_label, GUILayout.Width(LabelWidth));
            string result = GUILayout.TextField(_value, GUILayout.Height(FieldHeight));  //入力結果
            GUILayout.EndHorizontal();
            return result;
        }

        private static string FormatTime(long _milliseconds)
        {
            //CSVではミリ秒整数で保持し、画面表示時だけ 00:00.000 形式へ変換する。
            long minutes = _milliseconds / MillisecondsPerMinute;                  //分
            long seconds = _milliseconds % MillisecondsPerMinute / MillisecondsPerSecond; //秒
            long remainder = _milliseconds % MillisecondsPerSecond;                //ミリ秒

            return $"{minutes:00}:{seconds:00}.{remainder:000}";
        }

        private static GUIStyle TitleStyle()
        {
            return new GUIStyle(GUI.skin.label) { fontSize = TitleFontSize, fontStyle = FontStyle.Bold };
        }

        private static GUIStyle HeaderStyle()
        {
            return new GUIStyle(GUI.skin.label) { fontSize = HeaderFontSize, fontStyle = FontStyle.Bold };
        }
    }
}
