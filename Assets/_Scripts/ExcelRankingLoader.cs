using System.Collections.Generic;
using UnityEngine;

public class ExcelRankingLoader : MonoBehaviour
{
    public List<RnakingData> RankingList = new List<RnakingData>();

    public List<RnakingData> GetCSVDatas() { return RankingList; }

    private void Awake()
    {

        //CSVファイルを読み込み
        LoadCsv("ExcelRanking");
    }


    // Update is called once per frame
    void Update()
    {
        
    }

   


    public void LoadCsv(string _excelName)
    {
        TextAsset csv =
            Resources.Load<TextAsset>(_excelName);

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
            RnakingData ranking = new RnakingData();

            //進行ナンバー
            ranking.Number = int.Parse(cells[0]);

            //スコア
            ranking.Score = int.Parse(cells[1]);

            //順位
            ranking.RankingNumber = int.Parse(cells[2]);

            //画像
            ranking.Texture = cells[3];

            // リストに追加
            RankingList.Add(ranking);

        }
    }
}
