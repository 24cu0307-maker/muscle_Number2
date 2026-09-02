using System;

using System.Collections.Generic;

using Unity.VisualScripting;

using UnityEngine;

using UnityEngine.SocialPlatforms.Impl;



[DefaultExecutionOrder(-0)]

public class PoseJudgeController : MonoBehaviour

{

    [Header("InGame")]

    [SerializeField] private UIManager m_uiManager;



    [Header("UIの保存場所")]

    [SerializeField] private ExcelLoader m_excelLoader;





    [Header("CSVのデータリスト")]

    private List<CSVPoseData> poseDatas;



    [Header("ポーズの判定")]

    private bool isPose = false;



    public Action<int> Score;





    ///<summary>

    ///現在のポーズが成功しているかの判定

    ///</summary>

    public bool GetisPose(int PoseID)
    {
        return isPose;
    }


    private void Awake()

    {

        poseDatas = m_excelLoader.excelPoseJudgeLoader.GetCSVDatas();



    }





    //オブザーバー

    private void OnEnable()

    {

        m_uiManager.PoseJudgeFrame += PoseJudge;

    }



    //オブザーバー

    private void OnDisable()

    {

        m_uiManager.PoseJudgeFrame -= PoseJudge;

    }

    public void PoseJudge(int poseID)
    {
        if (poseDatas == null
            || poseID < 0
            || poseID >= poseDatas.Count
            )
        {
            Debug.LogWarning($"[PoseJudge] Invalid PoseID: {poseID}");
            return;
        }

        ///指定されたポーズデータを入れる
        CSVPoseData pose = poseDatas[poseID];

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
            isPose = true;
            Score?.Invoke(poseID);


        }

        else
        {
            isPose = false;


        }





    }

    public void LogPoseDiagnostics(int poseID)
    {
        if (poseDatas == null
            || poseID < 0
            || poseID >= poseDatas.Count
            || AngleDataManager.Instance == null
            || AngleDataManager.Instance.angleData == null)
        {
            Debug.LogWarning(
                $"[AudiencePoseDebug] Pose {poseID}: pose or angle data is unavailable.");
            return;
        }

        CSVPoseData pose = poseDatas[poseID];
        float[] angles = AngleDataManager.Instance.angleData.angle;
        string poseResult = "FAIL";
        if (GetisPose(poseID))
        {
            poseResult = "PASS";
        }
        Debug.Log(
            $"[AudiencePoseDebug] Pose {poseID} ({pose.PoseName}) "
            + FormatJoint("LeftElbow", angles[0], pose.LeftelbowRotation)
            + FormatJoint("LeftShoulder", angles[1], pose.LeftShoulderRotation)
            + FormatJoint("RightElbow", angles[2], pose.RightelbowRotation)
            + FormatJoint("RightShoulder", angles[3], pose.RightShoulderRotation)
            + $"Result={poseResult}");
    }

    private static string FormatJoint(
        string jointName,
        float currentAngle,
        Vector3 expectedAngle)
    {
        bool valid = !float.IsNaN(currentAngle)
            && !float.IsInfinity(currentAngle);
        bool passed = valid
            && currentAngle >= expectedAngle[0] - expectedAngle[1]
            && currentAngle <= expectedAngle[0] + expectedAngle[1];
        string currentText = "INVALID";
        if (valid)
        {
            currentText = currentAngle.ToString("F1");
        }
        string jointResult = "NG";
        if (passed)
        {
            jointResult = "OK";
        }
        return $"| {jointName}={currentText} "
            + $"({expectedAngle[0]:F1}±{expectedAngle[1]:F1}) "
            + $"{jointResult} ";
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
