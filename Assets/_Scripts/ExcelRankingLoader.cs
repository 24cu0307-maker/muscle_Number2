using GameFlowTemplate;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class ExcelRankingLoader : MonoBehaviour
{
    private ScoreManager scoreManager;

    public List<RnakingData> RankingList = new List<RnakingData>();

    private string FileName = "ExcelRanking.csv";

    public List<RnakingData> GetCSVDatas()
    {
        return RankingList;
    }


    private void Awake()
    {
        scoreManager = FindFirstObjectByType<ScoreManager>();

        // CSVを読み込む
        LoadCsv();


        //これを呼び出せばよい
        //AddRankingData();
    }


    /// <summary>
    /// CSV読み込み
    /// </summary>
    public void LoadCsv()
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            FileName
        );

        // 初回起動
        if (!File.Exists(path))
        {
            Debug.Log("保存データがないため、Resourcesから初期CSVを作成します");

            TextAsset csv = Resources.Load<TextAsset>("ExcelRanking");

            if (csv == null)
            {
                Debug.LogError("Resources/ExcelRanking.csv が見つかりません");
                return;
            }

            File.WriteAllText(
                path,
                csv.text,
                Encoding.UTF8
            );
        }


        // CSV読み込み
        RankingList.Clear();

        string[] lines = File.ReadAllLines(
            path,
            Encoding.UTF8
        );


        // 1行目はヘッダー
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i]))
                continue;

            string[] cells = lines[i].Trim().Split(',');

            if (cells.Length < 4)
                continue;


            RnakingData ranking = new RnakingData();

            ranking.Number = int.Parse(cells[0]);
            ranking.Score = int.Parse(cells[1]);
            ranking.RankingNumber = int.Parse(cells[2]);
            ranking.Texture = cells[3];


            RankingList.Add(ranking);
        }


        Debug.Log(
            $"ランキング読み込み完了：{RankingList.Count}件"
        );

        Debug.Log(
            $"読み込み先：{path}"
        );
    }


    /// <summary>
    /// ランキングデータ追加
    /// </summary>
    public void AddRankingData()
    {
        RnakingData data = new RnakingData();


        // 番号
        if (RankingList.Count == 0)
        {
            data.Number = 1;
        }
        else
        {
            data.Number = RankingList[RankingList.Count - 1].Number + 1;
        }


        // スコア
        data.Score = FindScore();


        // 仮順位
        data.RankingNumber = -1;


        // 画像
        data.Texture = "face" + data.Number;


        // Listに追加
        RankingList.Add(data);


        // ランキング更新
        RankingUpdate();


        // CSVを保存
        SaveCsv();


        Debug.Log(
            $"ランキング追加：Number={data.Number}, Score={data.Score}"
        );
    }


    /// <summary>
    /// CSV保存
    /// </summary>
    private void SaveCsv()
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            FileName
        );


        StringBuilder builder = new StringBuilder();


        // ヘッダー
        builder.AppendLine(
            "Number,Score,RankingNumber,Texture"
        );


        // 全データを書き込む
        for (int i = 0; i < RankingList.Count; i++)
        {
            RnakingData data = RankingList[i];


            builder.AppendLine(
                $"{data.Number}," +
                $"{data.Score}," +
                $"{data.RankingNumber}," +
                $"{data.Texture}"
            );
        }


        File.WriteAllText(
            path,
            builder.ToString(),
            Encoding.UTF8
        );


        Debug.Log(
            $"ランキング保存完了：{path}"
        );
    }


    /// <summary>
    /// スコア取得
    /// </summary>
    public int FindScore()
    {
        if (scoreManager == null)
        {
            Debug.LogError("ScoreManagerが見つかりません");
            return 0;
        }

        return scoreManager.CurrentScore;
    }


    /// <summary>
    /// ランキング更新
    /// </summary>
    public void RankingUpdate()
    {
        for (int i = 0; i < RankingList.Count; i++)
        {
            RnakingData data = RankingList[i];

            int rank = 1;


            // 自分よりスコアが高いデータを数える
            for (int j = 0; j < RankingList.Count; j++)
            {
                if (RankingList[j].Score > data.Score)
                {
                    rank++;
                }
            }


            data.RankingNumber = rank;

            RankingList[i] = data;
        }
    }
}