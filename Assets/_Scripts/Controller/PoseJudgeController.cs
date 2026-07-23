using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

[DefaultExecutionOrder(-100)]
public class PoseJudgeController : MonoBehaviour
{
    [Header("UIの保存場所")]
    [SerializeField] private UIController m_uiController;

    [Header("UIの保存場所")]
    [SerializeField] private ExcelLoader m_excelLoader;


    [Header("CSVのデータリスト")]
    private List<CSVPoseData> poseDatas;

    [Header("ポーズの判定")]
    private bool[] isPose = new bool[3];

    public Action<int> Score;


    ///<summary>
    ///現在のポーズが成功しているかの判定
    ///</summary>
    public bool GetisPose(int PoseID) { return isPose[PoseID]; }

    private void Awake()
    {
        poseDatas = m_excelLoader.excelPoseJudgeLoader.GetCSVDatas();

    }


    //オブザーバー
    private void OnEnable()
    {
        m_uiController.PoseJudgeFrame += PoseJudge;
    }

    //オブザーバー
    private void OnDisable()
    {
        m_uiController.PoseJudgeFrame -= PoseJudge;
    }
    public void PoseJudge(int poseID)
    {
        Debug.Log("[PoseID]" + poseID);
        ///指定されたポーズデータを入れる
        CSVPoseData pose = poseDatas[poseID];

        Debug.Log("[posecheck]Per" + poseID);
        ///ポーズの判定
        if (AngleDataManager.Instance.angleData.angle[0] <= (pose.LeftelbowRotation[0] + pose.LeftelbowRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[0] >= (pose.LeftelbowRotation[0] - pose.LeftelbowRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[1] <= (pose.LeftShoulderRotation[0] + pose.LeftShoulderRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[1] >= (pose.LeftShoulderRotation[0] - pose.LeftShoulderRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[2] <= (pose.RightelbowRotation[0] + pose.RightelbowRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[2] >= (pose.RightelbowRotation[0] - pose.RightelbowRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[3] <= (pose.RightShoulderRotation[0] + pose.RightShoulderRotation[1]) &&
            AngleDataManager.Instance.angleData.angle[3] >= (pose.RightShoulderRotation[0] - pose.RightShoulderRotation[1])
            )
        {
            Debug.Log("[posecheck]true");
            isPose[poseID] = true;
            Score?.Invoke(poseID);

        }
        else
        {
            Debug.Log("[posecheck]false");
            isPose[poseID] = false;

        }


    }

    /// <summary>
    ///パーフェクトのポーズ判定 
    /// <summary>
    public bool PoseJudge_Perfect(GameObject _uinumber_approaching, GameObject _uinumber_wating)
    {
        Debug.Log("[posecheck]Per");
        return _uinumber_wating.transform.localScale.x >= _uinumber_approaching.transform.localScale.x - 0.001f &&
               _uinumber_wating.transform.localScale.x <= _uinumber_approaching.transform.localScale.x + 0.001f;

    }

    /// <summary>
    ///通常のポーズ判定 
    /// <summary>
    public bool PoseJudge_Normal(GameObject _uinumber_approaching, GameObject _uinumber_wating)
    {
        Debug.Log("[posecheck]Guu");
        return _uinumber_wating.transform.localScale.x >= _uinumber_approaching.transform.localScale.x - 0.005f &&
                _uinumber_wating.transform.localScale.x <= _uinumber_approaching.transform.localScale.x + 0.005f;

    }

 
}
