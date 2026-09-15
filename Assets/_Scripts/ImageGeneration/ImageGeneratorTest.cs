using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ImageGeneratorTest : MonoBehaviour
{
    // 現在のカメラ
    [SerializeField]
    Camera camera;

    [SerializeField]
    private ImageGenerator imageGenerator;

    [SerializeField]
    private ExcelRankingLoader excelRankingLoader;

    List<RnakingData> RankingList;

    RnakingData data;

    [SerializeField]
    private string folderPath = @"D:\MyGame\ScreenShots";

    string path;

    // Dドライブ直下の保存フォルダ
    [SerializeField]
    private string saveDirectory = @"D:\MyGame\GeneratedImages";

    private void Start()
    {
        if (camera == null)
        {
            camera = GameObject.Find("Camera").GetComponent<Camera>();

        }

        path = Path.Combine(
           folderPath,
           "ScreenShot.png");


        //撮影
        //SaveScreen();

        //指定したファイルパスの画像を読み込んで、Unityで使える Texture2D に変換して返す処理
        //元画像（ゲーム内で撮影した画像の読み込み）
        //LoadImage(path);

        //画像作成
        //CreateImage();
    }


    public void CreateImage()
    {
        excelRankingLoader.AddRankingData();

        RankingList = excelRankingLoader.GetCSVDatas();

        // ランキングデータが取得できているか確認
        if (RankingList == null || RankingList.Count == 0)
        {
            Debug.LogError("RankingListにデータがありません");
            return;
        }

        //data = RankingList[RankingList.Count - 1];

        

        //ファイルを生成
        //なかったら生成しない
        if (!Directory.Exists(saveDirectory))
        {
            Directory.CreateDirectory(saveDirectory);
        }

        int number = GetNextImageNumber(saveDirectory);

        string fileName = $"Result_{number:D3}.png";

        string savePath = Path.Combine(
            saveDirectory,
            fileName
        );



        // 画像生成
        imageGenerator.GenerateImage(
            path,
            savePath
        );
    }

    //ファイルの中を検索して "Result_*.png"の画像分カウントし次に進む
    private int GetNextImageNumber(string directory)
    {
        int maxNumber = 0;

        string[] files = Directory.GetFiles(
            directory,
            "Result_*.png"
        );

        foreach (string file in files)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);

            // Result_001 → 001
            string numberText = fileName.Replace("Result_", "");

            if (int.TryParse(numberText, out int number))
            {
                if (number > maxNumber)
                {
                    maxNumber = number;
                }
            }
        }

        return maxNumber + 1;
    }


    /// <summary>
    /// 撮影
    /// </summary>
    public void SaveScreen()
    {
        int width = 1080;

        int height = 1080;


        // フォルダが存在しなければ作成
        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string path = Path.Combine(
            Application.dataPath,
            folderPath,
            "ScreenShot.png"
        );

        // RenderTextureを作成
        RenderTexture renderTexture = new RenderTexture(
            width,
            height,
            24
            );

        // 元のRenderTextureを保存
        RenderTexture originalTarget = camera.targetTexture;

        // 撮影先を変更
        camera.targetTexture = renderTexture;

        // カメラで描画
        camera.Render();

        // RenderTextureを読み取る
        RenderTexture.active = renderTexture;

        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.RGB24,
            false
        );

        texture.ReadPixels(
            new Rect(0, 0, width, height),
            0,
            0
        );

        texture.Apply();

        // PNG化
        byte[] bytes = texture.EncodeToPNG();

        // 保存
        File.WriteAllBytes(path, bytes);

        // 元に戻す
        camera.targetTexture = originalTarget;
        RenderTexture.active = null;

        // メモリ解放
        Destroy(renderTexture);
        Destroy(texture);

        Debug.Log("保存しました：" + path);

    }

    //指定したファイルパスの画像を読み込んで、Unityで使える Texture2D に変換して返す処理
    public Texture2D LoadImage(string path)
    {
        Debug.Log("画像を読み込みましたなぜ：" + path);
        if (!File.Exists(path))
        {
            Debug.LogError("画像が見つかりません：" + path);
            return null;
        }

        //画像のファイルを読み込む
        byte[] bytes = File.ReadAllBytes(path);

        //Texture2Dを作成
        Texture2D texture = new Texture2D(2, 2);

        //PNG/JPGをTexture２Dに変換
        if (texture.LoadImage(bytes))
        {

            Debug.Log("画像を読み込みました：" + path);
            return texture;
        }

        Debug.LogError("画像の読み込みに失敗しました：" + path);

        return null;
    }

  

}