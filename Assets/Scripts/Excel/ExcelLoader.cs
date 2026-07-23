using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


///<summary>
///CSVデータの読み込みと保存
///</summary>
[DefaultExecutionOrder(-300)]
public class ExcelLoader : MonoBehaviour
{
    //インスタンス化
    //public static ExcelLoader Instance { get; private set; }

    public ExcelPoseJudgeLoader excelPoseJudgeLoader;
    public ExcelPoseTimeFlowLoader excelPoseTimeFlowLoader;

    private void Awake()
    {
        //CSVファイルを読み込む為のクラス
        excelPoseJudgeLoader = new ExcelPoseJudgeLoader();
        excelPoseTimeFlowLoader = new ExcelPoseTimeFlowLoader();

        //CSVファイルを読み込み
        excelPoseJudgeLoader.LoadCsv();
        excelPoseTimeFlowLoader.LoadCsv();
    }



}

///<summary>
///ポーズの判定のCSVデータの読み込み
///</summary>
public class ExcelPoseJudgeLoader
{
    private List<CSVPoseData> poseList = new List<CSVPoseData>();

    public List<CSVPoseData> GetCSVDatas() { return poseList; }

    public void LoadCsv()
    {
        TextAsset csv =
            Resources.Load<TextAsset>("masslePoseJudge");

        if (csv == null)
        {
            Debug.LogError("CSVが見つかりません");
            return;
        }

        string[] lines = csv.text.Split('\n');

        // 1行目はヘッダーなので飛ばす
        for (int i = 1; i < lines.Length; i++)
        {
            //空行がある場合スキップ
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            //カンマで分割
            string[] cells = lines[i].Trim().Split(',');

            //CSVData作成
            CSVPoseData pose = new CSVPoseData();

            //名前を保存
            pose.PoseName = cells[0];

            pose.PoseID = int.Parse(cells[1]);

            //左肩を保存
            pose.LeftShoulderRotation = new Vector3(
                float.Parse(cells[2]),
                float.Parse(cells[3]));

            //左腕を保存
            pose.LeftelbowRotation = new Vector3(
                float.Parse(cells[4]),
                float.Parse(cells[5]));


            pose.RightShoulderRotation = new Vector3(
                float.Parse(cells[6]),
                float.Parse(cells[7]));

            pose.RightelbowRotation = new Vector3(
                float.Parse(cells[8]),
                float.Parse(cells[9]));

            //ポーズを追加
            poseList.Add(pose);

        }
    }

}

///<summary>
///ポーズの順番のCSVデータの読み込み
///</summary>
public class ExcelPoseTimeFlowLoader
{
    public List<CSVDataPoseFlow> poseList = new List<CSVDataPoseFlow>();

    public List<CSVDataPoseFlow> GetCSVDatas() { return poseList; }


    public void LoadCsv()
    {
        TextAsset csv =
            Resources.Load<TextAsset>("PoseTimeFlow");

        if (csv == null)
        {
            Debug.LogError("CSVが見つかりません");
            return;
        }

        string[] lines = csv.text.Split('\n');

        // 1行目はヘッダーなので飛ばす
        for (int i = 1; i < lines.Length; i++)
        {
            //空行がある場合スキップ
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            //カンマで分割
            string[] cells = lines[i].Trim().Split(',');

            //CSVData作成
            CSVDataPoseFlow pose = new CSVDataPoseFlow();

            //順番を保存
            pose.FlowNumber = int.Parse(cells[0]);

            pose.PoseID = int.Parse(cells[1]);

            //名前を保存
            pose.PoseName = cells[2];

            //始まる時間　終わる時間
            pose.start = float.Parse(cells[3]);
            pose.end = float.Parse(cells[4]);

            //ポーズを追加
            poseList.Add(pose);
        }
    }

}
