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
    public int PoseMax;

}

///<summary>
///
///</summary>
public struct CSVDataPoseFlow
{
    public int FlowNumber;
    public int PoseID;       // 固定のポーズ番号
    public string PoseName;
    public float time;
    public string SuccessEffectNames; //成功時に固定再生する演出名。複数指定は|区切り

}

public struct RnakingData
{ 
    public int Number;           //番号
    public int Score;            //スコア
    public int RankingNumber;    //ランキング
    public string Texture;    //画像データ取得
}
