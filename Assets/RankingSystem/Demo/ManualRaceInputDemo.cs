/*━━━━━━━━━
@file ManualRaceInputDemo.cs
@brief レースタイム入力専用デモ
@author 24CU0000 Name
@date 2026/07/02
最終更新日 2026/07/02
@remarks 入力側UnityでCSV保存確認に使用
━━━━━━━━━*/

using System.Globalization;
using UnityEngine;

namespace CrossProjectRanking
{
    /// <summary>
    /// 入力側プロジェクトでCSV保存だけを確認するためのIMGUIデモです。
    /// プレイヤー名とタイムを手入力してCSVへ追記します。
    /// </summary>
    [RequireComponent(typeof(RankingDataWriter))]
    public sealed class ManualRaceInputDemo : MonoBehaviour
    {
        private const float AreaLeft = 20.0f;                   //入力画面を左端からどれだけ離すかを表す位置
        private const float AreaTop = 20.0f;                    //入力画面を上端からどれだけ離すかを表す位置
        private const float AreaWidth = 520.0f;                 //入力画面全体の横幅。ラベルと入力欄が収まるようにする
        private const float AreaHeight = 220.0f;                //入力画面全体の高さ。入力2項目と保存ボタンを収める
        private const float FieldHeight = 28.0f;                //プレイヤー名・タイム入力欄の高さ
        private const float ButtonHeight = 38.0f;               //CSV保存ボタンの高さ。押しやすい見た目にする
        private const float LabelWidth = 110.0f;                //入力項目名の横幅。各入力欄の開始位置を揃えるために使う
        private const int TitleFontSize = 22;                   //画面タイトルの文字サイズ。通常ラベルより大きく表示する
        private const int TimePartsSecondsOnly = 1;             //「83.456」のように秒だけ入力された時、Split結果が1個になることを判定する値
        private const int TimePartsMinutesSeconds = 2;          //「01:23.456」のように分:秒で入力された時、Split結果が2個になることを判定する値
        private const double MinimumTimeSeconds = 0.0;          //0秒以下のタイムを保存しないための下限値
        private const double SecondsPerMinute = 60.0;           //分を秒へ変換するための値。分:秒入力を合計秒数に直す時に使う

        private RankingDataWriter m_writer;                     //CSVへ記録を保存するコンポーネント。SaveでWriteを呼び出すために保持する
        private string m_playerName = "Player 1";               //画面で入力されたプレイヤー名。CSVのPlayerName列へ保存する
        private string m_timeText = "01:23.456";                //画面で入力されたタイム文字列。TryParseTimeで秒数へ変換してから保存する

        private void Awake()
        {
            //同じGameObjectに付いている保存用コンポーネントを取得する。
            m_writer = GetComponent<RankingDataWriter>();
        }

        private void OnGUI()
        {
            //Unity標準のIMGUIで簡易入力画面を作る。
            //本番UIではCanvasに置き換えても、保存処理はm_writer.Writeを呼ぶだけで同じ。
            GUILayout.BeginArea(new Rect(AreaLeft, AreaTop, AreaWidth, AreaHeight), GUI.skin.box);
            GUILayout.Label("レースタイム入力", new GUIStyle(GUI.skin.label) { fontSize = TitleFontSize, fontStyle = FontStyle.Bold });

            m_playerName = Field("プレイヤー名", m_playerName);
            m_timeText = Field("タイム", m_timeText);

            if (GUILayout.Button("CSVへ保存", GUILayout.Height(ButtonHeight)))
            {
                //ボタンを押した時だけCSVへ保存する。
                Save();
            }

            GUILayout.EndArea();
        }

        private void Save()
        {
            double seconds;                                     //秒に変換したタイム

            //入力文字列を秒数へ変換できない場合は保存しない。
            if (!TryParseTime(m_timeText, out seconds))
            {
                Debug.LogWarning("タイム形式が正しくありません。");
                return;
            }

            //変換できた秒数をCSVへ保存する。
            m_writer.Write(m_playerName, (float)seconds);
        }

        private static bool TryParseTime(string _value, out double _seconds)
        {
            //入力は「83.456」のような秒だけ、または「01:23.456」のような分:秒を受け付ける。
            _seconds = MinimumTimeSeconds;
            string[] parts = (_value ?? string.Empty).Trim().Split(':');       //分秒分割

            if (parts.Length == TimePartsSecondsOnly)
            {
                //秒だけの入力をそのままdoubleへ変換する。
                return double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _seconds) && _seconds > MinimumTimeSeconds;
            }

            double minutes;                                    //分
            double remainder;                                  //秒

            //分:秒形式として正しいか確認する。
            //秒部分が60以上の場合は時刻形式として不正なので保存しない。
            if (parts.Length != TimePartsMinutesSeconds ||
                !double.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out minutes) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out remainder) ||
                minutes < MinimumTimeSeconds ||
                remainder < MinimumTimeSeconds ||
                remainder >= SecondsPerMinute)
            {
                return false;
            }

            //分と秒を合計秒数へ変換する。
            _seconds = minutes * SecondsPerMinute + remainder;
            return _seconds > MinimumTimeSeconds;
        }

        private static string Field(string _label, string _value)
        {
            //ラベルと入力欄を横並びで表示し、入力後の文字列を返す。
            GUILayout.BeginHorizontal();
            GUILayout.Label(_label, GUILayout.Width(LabelWidth));
            string result = GUILayout.TextField(_value, GUILayout.Height(FieldHeight));          //入力結果
            GUILayout.EndHorizontal();
            return result;
        }
    }
}
