using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AngleJudge : MonoBehaviour
{
    [Header("UIの制御クラス")]
    [SerializeField] private UIController uiController;

    [Header("Excelの読み込みクラス")]
    [SerializeField] private ExcelLoader excelLoader;

    [Header("スコアの計算クラス")]
    private ScoreCalculator scoreCalculator;

    [Header("ポーズの判定")]
    private bool isPose;

    [Header("スコア")]
    private float Score;

    [Header("CSVのデータリスト")]
    private List<CSVPoseData> poseDatas;


    ///<summary>
    ///現在のポーズが成功しているかの判定
    ///</summary>
    public bool GetisPose() { return isPose; }


    private void Start()
    {
        scoreCalculator = new ScoreCalculator();
        poseDatas = excelLoader.excelPoseJudgeLoader.GetCSVDatas();
    }

    //オブザーバー
    private void OnEnable()
    {
        //uiController.action += Judge;
    }

    //オブザーバー
    private void OnDisable()
    {
        //uiController.action -= Judge;
    }

    ///<summary>
    ///指定されたポーズの判定
    ///ポーズのスコア計算
    ///のちのち分ける
    ///</summary>
    public void Judge(int poseID)
    {
        ///指定されたポーズデータを入れる
        CSVPoseData pose = poseDatas[poseID];

        ///ポーズのスコア計算
        Score = scoreCalculator.TotalScore(
            Mathf.Abs(pose.LeftShoulderRotation[0] - AngleDataManager.Instance.angleData.angle[1]), Mathf.Abs(pose.RightShoulderRotation[0] - AngleDataManager.Instance.angleData.angle[3]),
            Mathf.Abs(pose.LeftelbowRotation[0] - AngleDataManager.Instance.angleData.angle[0]), Mathf.Abs(pose.RightelbowRotation[0] - AngleDataManager.Instance.angleData.angle[2])
            );

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
            isPose = true;

        }
        else
        {
            isPose = false;

        }



        /*
       Debug.Log(angleDataManager.angleData.angle[0]);
        Debug.Log(angleDataManager.angleData.angle[1]);
        Debug.Log(angleDataManager.angleData.angle[2]);
        Debug.Log(angleDataManager.angleData.angle[3]);

        Debug.Log(pose.LeftelbowRotation[0] + pose.LeftelbowRotation[1]);
        Debug.Log(pose.LeftShoulderRotation[0] + pose.LeftShoulderRotation[1]);
        Debug.Log(pose.RightelbowRotation[0] + pose.RightelbowRotation[1]);
        Debug.Log(pose.RightShoulderRotation[0] + pose.RightShoulderRotation[1]);

        */


    }
}
