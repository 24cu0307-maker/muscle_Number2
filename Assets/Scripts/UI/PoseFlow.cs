using Mediapipe.Tasks.Components.Containers;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

///<summary>
///ポーズの順番を管理
///</summary>

public class PoseFlow
{
    ///<summary>
    ///CSVから読み込んだポーズの流れ
    ///</summary>
    private List<CSVDataPoseFlow> poseFlows;

    ///<summary>
    ///現在実行中のポーズのインデックス
    ///</summary>
    private int currentIndex = 0;

    ///<summary>
    ///ポーズの流れを受け取って初期化する
    ///</summary>
    public PoseFlow(List<CSVDataPoseFlow> poseFlows)
    {
        this.poseFlows = poseFlows;
    }

    ///<summary>
    ///現在のポーズ情報を取得する
    ///</summary>
    public CSVDataPoseFlow CurrentPose()
    {
        return poseFlows[currentIndex];
    }

    ///<summary>
    ///次のポーズへ進める
    ///最後のポーズの場合は進まない
    ///</summary>
    public void NextPose()
    {
        if (currentIndex < poseFlows.Count - 1) currentIndex++;
    }

    ///<summary>
    ///次のポーズが存在するかを判定する
    ///</summary>
    public bool HasNextPose()
    {
        return currentIndex < poseFlows.Count - 1;
    }

}


/*
//ポーズの種類
public enum PoseType
{
    FrontDoubleBiceps,
    SideChest,
    MostMuscular,

}

public class PoseFlow
{
    private List<CSVDataPoseFlow> poseFlows;

    public PoseFlow(List<CSVDataPoseFlow> poseFlows)
    {
        this.poseFlows = poseFlows;
    }

    //現在のポーズ
    private int _currentIndex = 0;

    //ポーズ順
    private PoseType[] _flow =
    {
        PoseType.FrontDoubleBiceps,
        PoseType.SideChest,
        PoseType.MostMuscular,
        PoseType.FrontDoubleBiceps,
        PoseType.SideChest,
        PoseType.FrontDoubleBiceps,
        PoseType.SideChest,
        PoseType.MostMuscular,
        PoseType.FrontDoubleBiceps,
        PoseType.SideChest,
    };

    //現在のポーズの順を返す
    public int CurrentPose() { return _currentIndex; }

    //現在のポーズを返す
    public int CurrentPoseID() { return (int)_flow[_currentIndex]; }
    //次のポーズに移行
    public void NextPose()
    {

        if (_currentIndex < _flow.Length - 1)
        {
            _currentIndex++;
        }
    }

   
   
}
*/