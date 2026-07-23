/*━━━━━━━━━
@file TimeManager.cs
@brief ゲーム時間と演出停止時間の管理
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks インゲーム時間計測・演出中停止・表示出力を担当
━━━━━━━━━*/

using System;
using TMPro;
using UnityEngine;

namespace GameFlowTemplate
{
    /// <summary>
    /// インゲーム中の経過時間を管理するマネージャーです。
    /// 演出中はゲーム時間だけを止め、Unity全体のTime.timeScaleには触らない設計です。
    /// </summary>
    public sealed class TimeManager : MonoBehaviour
    {
        private const float InitialTimeSeconds = 0.0f;       //ゲーム時間の初期値
        private const int SecondsPerMinute = 60;             //秒を分へ変換するための値
        private const int MillisecondsPerSecond = 1000;      //秒をミリ秒へ変換するための値
        private const string TimeTextFormat = "{0:00}:{1:00}.{2:000}"; //表示用時間フォーマット

        [SerializeField] private TMP_Text m_timeText;        //ゲーム中時間を表示するText
        [SerializeField] private bool m_bUseUnscaledDeltaTime = false; //Time.timeScaleの影響を受けずに計測するか

        public float GameTimeSeconds { get; private set; }   //インゲームの経過時間。演出停止中は増えない
        public bool IsTimerRunning { get; private set; }     //ゲーム時間を計測中か
        public bool IsDirectionPaused { get; private set; }  //演出などでゲーム時間を止めているか
        public event Action<float> TimeChanged;              //ゲーム時間が変化した時に通知するイベント

        private void Start()
        {
            UpdateTimeText();
        }

        private void Update()
        {
            //ゲーム時間が動いていない、または演出停止中なら加算しない。
            if (!IsTimerRunning || IsDirectionPaused) { return; }

            float deltaTime = m_bUseUnscaledDeltaTime ? Time.unscaledDeltaTime : Time.deltaTime; //今回加算する時間
            GameTimeSeconds += deltaTime;
            UpdateTimeText();
            TimeChanged?.Invoke(GameTimeSeconds);
        }

        public void ResetTimer()
        {
            //ゲーム開始前やリトライ時に時間を初期化する。
            GameTimeSeconds = InitialTimeSeconds;
            IsTimerRunning = false;
            IsDirectionPaused = false;
            UpdateTimeText();
            TimeChanged?.Invoke(GameTimeSeconds);
        }

        public void StartGameTimer()
        {
            //インゲーム時間の計測を開始する。
            IsTimerRunning = true;
            IsDirectionPaused = false;
        }

        public void PauseForDirection()
        {
            //演出中など、ゲーム時間だけを止めたい時に呼ぶ。
            IsDirectionPaused = true;
        }

        public void ResumeFromDirection()
        {
            //演出が終わり、ゲーム時間の計測を再開したい時に呼ぶ。
            IsDirectionPaused = false;
        }

        public float StopGameTimer()
        {
            //ゲーム終了時に計測を止め、最終時間を返す。
            IsTimerRunning = false;
            IsDirectionPaused = false;
            return GameTimeSeconds;
        }

        public string GetTimeText()
        {
            //現在時間を 00:00.000 形式へ変換する。
            int totalMilliseconds = Mathf.FloorToInt(GameTimeSeconds * MillisecondsPerSecond); //秒から変換した合計ミリ秒
            int minutes = totalMilliseconds / (SecondsPerMinute * MillisecondsPerSecond);      //分
            int seconds = totalMilliseconds / MillisecondsPerSecond % SecondsPerMinute;        //秒
            int milliseconds = totalMilliseconds % MillisecondsPerSecond;                      //ミリ秒

            return string.Format(TimeTextFormat, minutes, seconds, milliseconds);
        }

        private void UpdateTimeText()
        {
            //Textが設定されている場合だけ、ゲーム中表示を更新する。
            if (m_timeText == null) { return; }

            m_timeText.text = GetTimeText();
        }
    }
}
