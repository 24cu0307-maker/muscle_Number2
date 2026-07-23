/*━━━━━━━━━
@file        CameraSequence.cs
@brief       複数のCameraShotPresetを順番に再生するための設定アセット
@author      24CU0139 ラヤマジ プラシャント
@date 作成日 2026/07/12
最終更新日   2026/07/12
@remarks     再生順と演出単位を設定するためのアセット
━━━━━━━━━*/
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

//複数のCameraShotPresetを順番に再生する設定
[CreateAssetMenu(fileName = "Sequence_New", menuName = "Camera/CameraSequence")]
public sealed class CameraSequence : ScriptableObject
{
    private const int E_MEMO_MIN_LINE = 2; //Memoの最小行数
    private const int E_MEMO_MAX_LINE = 4; //Memoの最大行数
    private const string E_DEFAULT_MEMO = "どの場面で使う演出かを書きます。"; //Memoの初期値

    [Header("この演出の説明")]
    [TextArea(E_MEMO_MIN_LINE, E_MEMO_MAX_LINE)]
    [SerializeField]
    [FormerlySerializedAs("memo")]
    private string m_memo = E_DEFAULT_MEMO; //このSequenceの説明文

    [Header("再生設定")]
    [Tooltip("ONなら演出中にTime.timeScaleを0にします。")]
    [SerializeField]
    [FormerlySerializedAs("pauseGameTime")]
    private bool b_mPauseGameTime = true; //演出中にゲーム時間を止めるかどうか

    [Tooltip("ONなら演出終了後に通常カメラへ戻ります。")]
    [SerializeField]
    [FormerlySerializedAs("returnToGameplayCamera")]
    private bool b_mReturnToGameplayCamera = true; //終了時に通常カメラへ戻すかどうか

    [Tooltip("ONならStopSequenceが呼ばれるまで繰り返します。")]
    [SerializeField]
    [FormerlySerializedAs("loop")]
    private bool b_mLoop = false; //Sequenceをループ再生するかどうか

    [Header("再生するカット。上から順番に再生します")]
    [SerializeField]
    [FormerlySerializedAs("shots")]
    private List<CameraShotPreset> m_cameraShotPresetList = new List<CameraShotPreset>(); //再生するShot Presetの一覧

    //プロパティ
    public string Memo => m_memo;
    public bool PauseGameTime => b_mPauseGameTime;
    public bool ReturnToGameplayCamera => b_mReturnToGameplayCamera;
    public bool Loop => b_mLoop;

    //再生するShot Presetの一覧を返します。
    public IReadOnlyList<CameraShotPreset> Shots => m_cameraShotPresetList;
}
