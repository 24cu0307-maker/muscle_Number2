/*━━━━━━━━━
@file RankingDataLoader.cs
@brief ランキングCSVの読み込みと自動更新
@author 24CU0000 Name
@date 2026/07/02
最終更新日 2026/07/02
@remarks 表示側Unityで1秒ごとの更新確認に使用
━━━━━━━━━*/

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Serialization;

namespace CrossProjectRanking
{
    /// <summary>
    /// ランキングCSVを読み込み、必要に応じて自動更新するコンポーネントです。
    /// 表示側Unityではこのクラスが1秒ごとにCSVの更新日時を確認します。
    /// </summary>
    public enum RankingSortMode
    {
        FastestTime,     //走行タイムが短い記録から表示する。通常のランキング表示で使用する
        NewestDate,      //保存日時が新しい記録から表示する。直近の入力確認に使用する
        OldestDate       //保存日時が古い記録から表示する。記録の蓄積順を確認する時に使用する
    }

    public sealed class RankingDataLoader : MonoBehaviour
    {
        private const float DefaultChangeCheckIntervalSeconds = 1.0f;  //CSVの更新日時を確認する初期秒数。ランキング更新はこの間隔で検知する
        private const float MinimumChangeCheckIntervalSeconds = 0.2f;  //確認間隔を短くしすぎてPC負荷が増えないようにするInspector上の最小値

        [FormerlySerializedAs("loadOnStart")]
        [SerializeField] private bool m_bLoadOnStart = true;                   //Play開始時にCSVを読み込むか。ONなら起動直後からランキングを表示できる
        [FormerlySerializedAs("sortMode")]
        [SerializeField] private RankingSortMode m_sortMode = RankingSortMode.FastestTime;  //読み込んだ記録の並び順。最速順、日付新しい順、日付古い順を切り替える
        [SerializeField] private bool m_bTodayOnly = true;                     //ONならPCの今日の日付に記録されたデータだけを表示する
        [Tooltip("入力側のRankingDataフォルダを絶対パスで指定します。")]
        [FormerlySerializedAs("sharedDirectoryOverride")]
        [SerializeField] private string m_sharedDirectoryOverride = "";        //入力側と表示側で同じCSVを見るためのフォルダパス。空欄なら自分のプロジェクト内を使う
        [FormerlySerializedAs("autoReloadWhenFileChanges")]
        [SerializeField] private bool m_bAutoReloadWhenFileChanges = true;     //CSVの更新日時が変わった時に自動でReloadするか。draw側のライブ更新に使う
        [FormerlySerializedAs("changeCheckIntervalSeconds")]
        [SerializeField, Min(MinimumChangeCheckIntervalSeconds)] private float m_changeCheckIntervalSeconds = DefaultChangeCheckIntervalSeconds;  //CSVを直接読む間隔ではなく、更新日時だけを確認する間隔
        [Tooltip("別Unity画面を操作中でも表示側の自動更新を続けます。")]
        [FormerlySerializedAs("runInBackground")]
        [SerializeField] private bool m_bRunInBackground = true;               //Unity画面が非アクティブでもUpdateを続けるか。2画面運用のdraw側で必要

        public RankingRecord[] Records { get; private set; } = Array.Empty<RankingRecord>();   //現在表示対象になっているランキング記録。Reload後に並び替え済みで保持する
        public event Action<RankingRecord[]> Loaded;                                           //CSV読み込み完了時に表示側へ通知するイベント

        private DateTime m_lastKnownWriteTimeUtc = DateTime.MinValue;          //前回確認したCSVの最終更新時刻。変化があったか判定するために保持する
        private float m_nextChangeCheckTime;                                   //次にCSV更新日時を確認するUnity時間。毎フレーム確認しないために使う

        private void Awake()
        {
            //Unity画面が非アクティブでもCSV確認を続けるために有効化する。
            //draw側Unityを裏に回してもランキング更新を止めないための設定。
            if (m_bRunInBackground)
            {
                Application.runInBackground = true;
            }

            //入力側プロジェクトと同じRankingDataフォルダを読むための設定。
            RankingStorage.SetSharedDirectory(m_sharedDirectoryOverride);
        }

        private void Start()
        {
            //起動直後にCSVを読み、最初のランキング表示を作る。
            if (m_bLoadOnStart)
            {
                Reload();
            }
        }

        private void Update()
        {
            //自動更新がOFF、または次の確認時刻になっていない場合は何もしない。
            //毎フレームCSVを読むと無駄なので、更新日時だけを一定間隔で確認する。
            if (!m_bAutoReloadWhenFileChanges || Time.unscaledTime < m_nextChangeCheckTime) { return; }

            m_nextChangeCheckTime = Time.unscaledTime + m_changeCheckIntervalSeconds;

            string path = RankingStorage.SharedFilePath;                       //CSVパス
            DateTime currentWriteTimeUtc = File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;  //現在の更新時刻

            //CSVの更新日時が前回と違う時だけ、CSV全体を読み直す。
            if (currentWriteTimeUtc != m_lastKnownWriteTimeUtc)
            {
                Reload();
            }
        }

        public void SetSharedDirectoryOverride(string _shareddirectoryoverride)
        {
            //実行中に読込元フォルダを変更したい場合に使う。
            //セットアップ用スクリプトから呼び、入力側と表示側が同じRankingDataを見るようにする。
            m_sharedDirectoryOverride = _shareddirectoryoverride;
            RankingStorage.SetSharedDirectory(m_sharedDirectoryOverride);
        }

        public void Reload()
        {
            //CSVを読み込んで、必要なら今日の記録だけに絞り込み、その後指定された順番に並べ替える。
            RankingRecord[] loaded = RankingStorage.Load();                    //読み込んだ記録

            //Today OnlyがONの場合、その日に保存された記録だけをランキング対象にする。
            if (m_bTodayOnly)
            {
                loaded = Array.FindAll(
                    loaded,
                    _record => _record.RecordedAtUtc.ToLocalTime().Date == DateTime.Now.Date);
            }

            //Inspectorで選んだSort Modeに応じて並び替える。
            //レースランキングでは通常FastestTimeを使う。
            Records = m_sortMode == RankingSortMode.FastestTime
                ? RankingDateSorter.FastestFirst(loaded)
                : m_sortMode == RankingSortMode.NewestDate
                    ? RankingDateSorter.NewestFirst(loaded)
                    : RankingDateSorter.OldestFirst(loaded);

            //次回Updateで変更検知できるよう、現在のCSV更新日時を保存する。
            m_lastKnownWriteTimeUtc = File.Exists(RankingStorage.SharedFilePath)
                ? File.GetLastWriteTimeUtc(RankingStorage.SharedFilePath)
                : DateTime.MinValue;

            //RankingDisplayなど、読み込み完了を待っている表示側へ通知する。
            Loaded?.Invoke(Records);
        }
    }
}
