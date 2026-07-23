/*━━━━━━━━━
@file       CameraSequenceTrigger.cs
@brief      UI ButtonやゲームイベントからCameraSequenceを再生する入口
@author     24CU0139 ラヤマジ プラシャント
@date       作成日 2026/07/12
最終更新日  2026/07/12
@remarks    イベント接続用
━━━━━━━━━*/
using UnityEngine;
using UnityEngine.Serialization;

//UI ButtonやゲームイベントからCameraSequenceを再生するための入口
public sealed class CameraSequenceTrigger : MonoBehaviour
{
    [SerializeField]
    [FormerlySerializedAs("director")]
    private PoseCameraDirector m_director;  //Sequenceを再生するDirector

    [SerializeField]
    [FormerlySerializedAs("sequence")]
    private CameraSequence m_sequence;      //再生したいSequence

    [Tooltip("テスト用通常はOFFにする")]
    [SerializeField]
    [FormerlySerializedAs("playOnStart")]
    private bool b_mPlayOnStart = false;    //Start時に自動再生するかどうか

    //Start時に必要ならSequenceを再生する
    private void Start()
    {
        if (b_mPlayOnStart) { Play(); }
    }

    //設定されたCameraSequenceを再生する
    public void Play()
    {
        if (m_director == null || m_sequence == null)
        {
            Debug.LogError("CameraSequenceTriggerの設定が不足しています。", this);
            return;
        }

        //DirectorにSequenceを再生させる
        m_director.PlaySequence(m_sequence);
    }

    //再生中のCameraSequenceを停止する
    public void Stop()
    {
        //Directorが設定されていない場合は何もしない
        if (m_director == null){ return; }

        //DirectorにSequenceの停止を指示する
        m_director.StopSequence();
    }
}
