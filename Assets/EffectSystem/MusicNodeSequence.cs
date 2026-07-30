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
}
