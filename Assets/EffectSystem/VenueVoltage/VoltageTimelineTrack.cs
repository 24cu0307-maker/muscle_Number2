/*━━━━━━━━━*
*@file VoltageTimelineTrack.cs*
*@brief Timeline上で会場Voltageを連続制御する*
*@author 24CU0000 Name*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks VenueVoltageSystemをTrackへBindingして使用*
*━━━━━━━━━*/

using System;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

/// <summary>
/// 一つのVoltage Clipが保持する開始値と終了値です。
/// </summary>
[Serializable]
public sealed class VoltagePlayableBehaviour : PlayableBehaviour
{
    [Range(0.0f, 100.0f)] public float m_startVoltage; //Clip開始時Voltage
    [Range(0.0f, 100.0f)] public float m_endVoltage = 100.0f; //Clip終了時Voltage
}

/// <summary>
/// Timelineへ配置できるVoltage Clipです。
/// </summary>
[Serializable]
public sealed class VoltagePlayableAsset : PlayableAsset, ITimelineClipAsset
{
    [SerializeField]
    [Range(0.0f, 100.0f)]
    [NotKeyable]
    private float m_startVoltage; //Clip開始時Voltage

    [SerializeField]
    [Range(0.0f, 100.0f)]
    [NotKeyable]
    private float m_endVoltage = 100.0f; //Clip終了時Voltage

    public ClipCaps clipCaps
    {
        get
        {
            return ClipCaps.Blending;
        }
    }

    /// <summary>
    /// Clip設定を保持するPlayableを生成します。
    /// </summary>
    public override Playable CreatePlayable(
        PlayableGraph _graph,
        GameObject _owner)
    {
        ScriptPlayable<VoltagePlayableBehaviour> playable =
            ScriptPlayable<VoltagePlayableBehaviour>.Create(
            _graph,
            1); //Voltage処理Playable
        VoltagePlayableBehaviour behaviour = playable.GetBehaviour(); //実行設定
        behaviour.m_startVoltage = m_startVoltage;
        behaviour.m_endVoltage = m_endVoltage;
        return playable;
    }

    /// <summary>
    /// Template生成時に開始値と終了値を設定します。
    /// </summary>
    public void SetVoltageRange(
        float _startvoltage,
        float _endvoltage)
    {
        m_startVoltage = Mathf.Clamp(_startvoltage, 0.0f, 100.0f);
        m_endVoltage = Mathf.Clamp(_endvoltage, 0.0f, 100.0f);
    }
}

/// <summary>
/// 複数Voltage Clipを混合してVenueVoltageSystemへ反映します。
/// </summary>
public sealed class VoltageMixerBehaviour : PlayableBehaviour
{
    /// <summary>
    /// 現在時刻の全Clipを評価してVoltageへ反映します。
    /// </summary>
    public override void ProcessFrame(
        Playable _playable,
        FrameData _info,
        object _playerdata)
    {
        // Voltageは判定結果のゲーム状態です。
        // Effect Timelineから値を書き換えないため、Clipは互換表示専用です。
    }
}

/// <summary>
/// VenueVoltageSystemをBindingしてVoltage Clipを配置するTrackです。
/// </summary>
[TrackClipType(typeof(VoltagePlayableAsset))]
[TrackBindingType(typeof(VenueVoltageSystem))]
public sealed class VoltageTrack : TrackAsset
{
    /// <summary>
    /// Track内のVoltage Clipを処理するMixerを生成します。
    /// </summary>
    public override Playable CreateTrackMixer(
        PlayableGraph _graph,
        GameObject _gameobject,
        int _inputcount)
    {
        return ScriptPlayable<VoltageMixerBehaviour>.Create(
            _graph,
            _inputcount);
    }
}
