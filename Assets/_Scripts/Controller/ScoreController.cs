using GameFlowTemplate;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

[DefaultExecutionOrder(-200)]
public class ScoreController : MonoBehaviour
{

    [Header("gameManager")]
    [SerializeField] private GameManager m_gameManager;

    [Header("UIの保存場所")]
    [SerializeField] private ExcelLoader m_excelLoader;

    [Header("スコア")]
    private float m_score;

    [Header("CSVのデータリスト")]
    private List<CSVPoseData> m_poseDatas;

    [Header("スコアの計算クラス")]
    private ScoreCalculator m_scoreCalculator;

    [Header("ポーズを判定する")]
    [SerializeField] private PoseJudgeController m_poseJudgeController;

    private void Start()
    {
        m_scoreCalculator = new ScoreCalculator();
        m_poseDatas = m_excelLoader.excelPoseJudgeLoader.GetCSVDatas();
    }


    //オブザーバー
    private void OnEnable()
    {
        m_poseJudgeController.Score += PoseScoreJudge;
    }

    //オブザーバー
    private void OnDisable()
    {
        m_poseJudgeController.Score -= PoseScoreJudge;
    }

    public void PoseScoreJudge(int _poseID)
    {

        ///指定されたポーズデータを入れる
        CSVPoseData pose = m_poseDatas[_poseID];

        ///ポーズのスコア計算
        m_score = m_scoreCalculator.TotalScore(
            Mathf.Abs(pose.LeftShoulderRotation[0] - AngleDataManager.Instance.angleData.angle[1]), Mathf.Abs(pose.RightShoulderRotation[0] - AngleDataManager.Instance.angleData.angle[3]),
            Mathf.Abs(pose.LeftelbowRotation[0] - AngleDataManager.Instance.angleData.angle[0]), Mathf.Abs(pose.RightelbowRotation[0] - AngleDataManager.Instance.angleData.angle[2])
            );

        m_gameManager.AddScore((int)m_score);
    }

  
}
