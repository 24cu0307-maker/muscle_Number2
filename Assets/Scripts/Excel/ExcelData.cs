using System.Collections;
using System.Collections.Generic;
using UnityEngine;


///<summary>
///
///</summary>
public struct CSVPoseData
{
    public string PoseName;
    public int PoseID;       // 固定のポーズ番号
    public Vector3 RightelbowRotation;
    public Vector3 LeftelbowRotation;
    public Vector3 RightShoulderRotation;
    public Vector3 LeftShoulderRotation;

}

///<summary>
///
///</summary>
public struct CSVDataPoseFlow
{
    public int FlowNumber;
    public int PoseID;       // 固定のポーズ番号
    public string PoseName;
    public float start;
    public float end;

}