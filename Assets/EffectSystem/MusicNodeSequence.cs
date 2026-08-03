/*━━━━━━━━━*
*@file MusicNodeSequence.cs*
*@brief BGMとPose Nodeのタイミングを保存するData Asset*
*@author 24CU0000 Name*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Music Node Editorから作成、編集する*
*━━━━━━━━━*/

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BGM上へ配置するPose Node情報です。
/// </summary>
[Serializable]
public struct SMusicNodeEvent
{
    public int m_nodeNumber; //外部CSVと共有するNode番号
    public float m_time; //BGM開始からの秒数
    public int m_poseId; //Pose ID
    public string m_eventName; //表示名
}

/// <summary>
/// 特定Node成功後に開くEvent Sceneと専用Node一覧です。
/// </summary>
[Serializable]
public sealed class MusicEventSceneData
{
    public bool b_m_enabled = true; //Event遷移を使用するか
    public string m_eventName = "Event"; //Event識別名
    public int m_triggerNodeNumber = 1; //遷移を発生させる通常Node番号
    public List<SMusicNodeEvent> m_eventNodesList =
        new List<SMusicNodeEvent>(); //Event Scene専用Node一覧
}

/// <summary>
/// BGMと時系列Nodeをまとめて保存します。
/// </summary>
[CreateAssetMenu(
    fileName = "MusicNodeSequence",
    menuName = "Effect System/Music Node Sequence")]
public sealed class MusicNodeSequence : ScriptableObject
{
    [SerializeField] private AudioClip m_bgmClip; //編集対象BGM
    [SerializeField] private List<SMusicNodeEvent> m_eventsList =
        new List<SMusicNodeEvent>(); //Node一覧
    [SerializeField] private List<MusicEventSceneData> m_eventScenesList =
        new List<MusicEventSceneData>(); //Event Scene設定一覧

    public AudioClip BgmClip
    {
        get
        {
            return m_bgmClip;
        }
        set
        {
            m_bgmClip = value;
        }
    }

    public List<SMusicNodeEvent> EventsList
    {
        get
        {
            return m_eventsList;
        }
    }

    public List<MusicEventSceneData> EventScenesList
    {
        get
        {
            return m_eventScenesList;
        }
    }
}
