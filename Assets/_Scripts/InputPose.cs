using System;
using System.Collections.Generic;
using UnityEngine;

public class InputPose : MonoBehaviour
{
    [Header("UIの保存場所")]
    [SerializeField] private ExcelLoader m_excelLoader;


    [Header("CSVのデータリスト")]
    private List<CSVPoseData> poseDatas;

    [Header("ポーズの判定")]
    private bool[] isPose = new bool[3];



    //座標を格納する箱
    [SerializeField] private Vector3[] _Body = new Vector3[37];


    public Action<int> Score;


    ///<summary>
    ///現在のポーズが成功しているかの判定
    ///</summary>
    public bool GetisInputPose(int PoseID) { return isPose[PoseID]; }

    private void Awake()
    {
        poseDatas = m_excelLoader.excelPoseJudgeLoader.GetCSVDatas();

    }


    private void Update()
    {
        PoseJudge();
    }


    public void PoseJudge()
    {
        _Body = PositionDataManager.Instance.positionData.Body;

        ///指定されたポーズデータを入れる
        CSVPoseData pose = poseDatas[0];

        //Debug.Log("[posecheck]Per" + poseID);
        ///OKの時
        if (AngleDataManager.Instance &&
            AngleDataManager.Instance.angleData.angle[0] <= (pose.LeftelbowRotation[0] + pose.LeftelbowRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[0] >= (pose.LeftelbowRotation[0] - pose.LeftelbowRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[1] <= (pose.LeftShoulderRotation[0] + pose.LeftShoulderRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[1] >= (pose.LeftShoulderRotation[0] - pose.LeftShoulderRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[2] <= (pose.RightelbowRotation[0] + pose.RightelbowRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[2] >= (pose.RightelbowRotation[0] - pose.RightelbowRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[3] <= (pose.RightShoulderRotation[0] + pose.RightShoulderRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[3] >= (pose.RightShoulderRotation[0] - pose.RightShoulderRotation[1])
            )
        {
            Debug.Log("[posecheckaaa]true");
            isPose[0] = true;
            //Score?.Invoke(poseID);

        }
        else　if(
            _Body[13].x >= _Body[14].x &&
            _Body[15].x <= _Body[16].x &&
            _Body[13].y <= _Body[15].y &&
            _Body[14].y <= _Body[16].y 
            )
        {
            Debug.Log("[posecheckaaa]false");
            //isPose[poseID] = false;

        }


    }

    
}
