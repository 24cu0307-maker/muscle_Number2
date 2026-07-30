using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

[DefaultExecutionOrder(-0)]
public class PoseJudgeController : MonoBehaviour
{
    [Header("InGame")]
    [SerializeField] private InGameManager m_InGameManager;

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
        m_InGameManager.PoseJudgeFrame += PoseJudge;
    }

    //オブザーバー
    private void OnDisable()
    {
        m_InGameManager.PoseJudgeFrame -= PoseJudge;
    }
    public void PoseJudge(int poseID)
    {
        Debug.Log("[PoseID]" + poseID);
        ///指定されたポーズデータを入れる
        CSVPoseData pose = poseDatas[poseID];

        Debug.Log("[posecheck]Per" + poseID);
        ///ポーズの判定
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
    

        float ratio = Mathf.Abs(_uinumber_wating.transform.localScale.x - _uinumber_approaching.transform.localScale.x)
              / _uinumber_approaching.transform.localScale.x;

        return ratio <= 0.01f;    // ±1%
    }

    /// <summary>
    ///通常のポーズ判定 
    /// <summary>
    public bool PoseJudge_Normal(GameObject _uinumber_approaching, GameObject _uinumber_wating)
    {
    
        float ratio = Mathf.Abs(_uinumber_wating.transform.localScale.x - _uinumber_approaching.transform.localScale.x)
                / _uinumber_approaching.transform.localScale.x;

        return ratio <= 0.03f;    // ±3%

    }

    /// <summary>
    ///失敗判定 
    /// <summary>
    public bool PoseJudge_Failure(GameObject _uinumber_approaching, GameObject _uinumber_wating)
    {

        // return _uinumber_wating.transform.localScale.x - 0.01f >= _uinumber_approaching.transform.localScale.x;


        float ratio = _uinumber_approaching.transform.localScale.x / _uinumber_wating.transform.localScale.x;
        return ratio < (1.0f - 0.07f);

    }

}
// 10 10 - 0.01