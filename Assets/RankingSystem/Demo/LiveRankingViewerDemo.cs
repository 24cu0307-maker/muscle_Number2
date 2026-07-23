/*━━━━━━━━━
@file LiveRankingViewerDemo.cs
@brief ランキング表示専用デモ
@author 24CU0000 Name
@date 2026/07/02
最終更新日 2026/07/02
@remarks 表示側Unityの負荷を抑えるため低FPSで動作
━━━━━━━━━*/

using UnityEngine;
using UnityEngine.Serialization;

namespace CrossProjectRanking
{
    /// <summary>
    /// Canvasを使わずに、すぐランキング表示を確認するためのIMGUIデモです。
    /// 表示専用Unityで動かす想定なので、FPSを下げてPC負荷を抑えます。
    /// </summary>
    [RequireComponent(typeof(RankingDataLoader))]
    public sealed class LiveRankingViewerDemo : MonoBehaviour
    {
        private const int DefaultMaximumRows = 20;             //ランキング画面に初期表示する最大行数。記録が多い時でも画面を埋めすぎないために使う
        private const int DefaultTargetFrameRate = 10;         //draw側Unityの初期FPS。ランキング表示だけなので低めにしてPC負荷を抑える
        private const int MinimumValue = 1;                    //Inspectorで0以下が入らないようにする最小値。表示件数やFPSの下限に使う
        private const float ScreenPadding = 40.0f;             //画面右端・下端に余白を残すために、画面サイズから引く値
        private const float AreaPadding = 20.0f;               //ランキング表示枠を画面左上から少し離すための余白
        private const float MaximumWidth = 760.0f;             //ランキング表示枠の最大横幅。横に広がりすぎて読みにくくなるのを防ぐ
        private const int TitleFontSize = 24;                  //ランキングタイトルの文字サイズ。通常行より目立たせるために使う
        private const int MillisecondsPerMinute = 60000;       //ミリ秒を分に変換するための値。タイム表示の分部分を作る
        private const int MillisecondsPerSecond = 1000;        //ミリ秒を秒に変換するための値。タイム表示の秒・ミリ秒部分を作る

        [FormerlySerializedAs("maximumRows")]
        [SerializeField, Min(MinimumValue)] private int m_maximumRows = DefaultMaximumRows;     //実際に画面へ表示する最大ランキング件数。Inspectorで調整できる
        [Tooltip("表示専用Unityの負荷を下げるため、ランキング画面のFPSを低めに固定します。")]
        [FormerlySerializedAs("targetFrameRate")]
        [SerializeField, Min(MinimumValue)] private int m_targetFrameRate = DefaultTargetFrameRate; //低負荷化のためにApplication.targetFrameRateへ設定するFPS
        [FormerlySerializedAs("reduceCpuLoad")]
        [SerializeField] private bool m_bReduceCpuLoad = true;                                  //ONならFPSを下げる。OFFにするとUnity通常設定で動作する

        private RankingDataLoader m_loader;                                                     //CSVを読み込み、更新検知するコンポーネント。表示するRecordsを取得する
        private Vector2 m_scrollPosition;                                                       //ランキング一覧のスクロール位置。件数が多い時に表示位置を保持する

        private void Awake()
        {
            //ランキング表示専用画面は高FPSで動かす必要がないため、FPSを下げて負荷を減らす。
            if (m_bReduceCpuLoad)
            {
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = m_targetFrameRate;
            }

            //同じGameObjectに付いているRankingDataLoaderを取得する。
            m_loader = GetComponent<RankingDataLoader>();
        }

        private void OnGUI()
        {
            //画面サイズに合わせてランキング枠の横幅を決める。
            float width = Mathf.Min(MaximumWidth, Screen.width - ScreenPadding);                 //表示幅

            GUILayout.BeginArea(new Rect(AreaPadding, AreaPadding, width, Screen.height - ScreenPadding), GUI.skin.box);
            GUILayout.Label("ライブランキング", new GUIStyle(GUI.skin.label) { fontSize = TitleFontSize, fontStyle = FontStyle.Bold });
            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);

            RankingRecord[] records = m_loader.Records;                                         //表示する記録

            //CSVに記録がない場合は、空表示にする。
            if (records.Length == 0)
            {
                GUILayout.Label("記録はまだありません。");
            }
            else
            {
                //最大表示件数を超えない範囲でランキング行を作る。
                int count = Mathf.Min(m_maximumRows, records.Length);                           //表示件数

                for (int i = 0; i < count; ++i)
                {
                    RankingRecord record = records[i];                                          //現在の記録
                    GUILayout.Label($"{i + 1,2}位  {record.m_playerName}  {FormatTime(record.m_timeMilliseconds)}");
                }
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private static string FormatTime(long _milliseconds)
        {
            //ミリ秒をランキング表示用の 00:00.000 形式へ変換する。
            return $"{_milliseconds / MillisecondsPerMinute:00}:{_milliseconds % MillisecondsPerMinute / MillisecondsPerSecond:00}.{_milliseconds % MillisecondsPerSecond:000}";
        }
    }
}
