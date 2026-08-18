using UnityEngine;

public class PoseFlowDataManager : MonoBehaviour
{
    [Header("UIの保存場所")]
    [SerializeField] private ExcelLoader m_excelLoader;

    private PoseFlow m_poseFlow;  　　　　 
    private CSVDataPoseFlow m_pose;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_poseFlow = new PoseFlow(m_excelLoader.excelPoseTimeFlowLoader.GetCSVDatas());
    }

    // Update is called once per frame
    void Update()
    {
        //現在のポーズを取得
        m_pose = m_poseFlow.CurrentPose();

    }

    public CSVDataPoseFlow GetPose()
    {
        return m_pose;
    }

    public PoseFlow GetposeFlow()
    {
        return m_poseFlow;
    }
}
