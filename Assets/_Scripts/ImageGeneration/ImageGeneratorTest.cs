using System.Collections.Generic;
using System.IO;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class ImageGeneratorTest : MonoBehaviour
{
    // 現在のカメラ
    [SerializeField]
    Camera camera;

    [Header("一番上に現在の写真、次に１個前の写真と続く")]
    [SerializeField]
    private List<Image> Resultimage;

    [Header("一番上に現在の写真、次に１個前の写真と続く")]
    [SerializeField]
    private List<Image> Titleimage;
    

    [SerializeField]
    private ImageGenerator imageGenerator;

    [SerializeField]
    private ExcelRankingLoader excelRankingLoader;

    List<RnakingData> RankingList;

    RnakingData data;

    string folderPath = @"D:\MyGame\ScreenShots";

    string path;

    // Dドライブ直下の保存フォルダ
    string saveDirectory = @"D:\GeneratedImages";

    private void Start()
    {
        if (camera == null)
        {
            camera = GameObject.Find("Camera").GetComponent<Camera>();

        }


        //ImageにTextureを設定
        //CreateSprite(LoadResultImage(1));

        //ファイル内のTextureを読み込む
        //引数：画像の番号
        //LoadResultImage(1);


        //ファイル内の画像をImageのリスト分を貼り付ける
        SetImage(Resultimage);
        SetImage(Titleimage);

        path = Path.Combine(
           folderPath,
           "ScreenShot.png");


        //撮影
        SaveScreen();

        //指定したファイルパスの画像を読み込んで、Unityで使える Texture2D に変換して返す処理
        //元画像（ゲーム内で撮影した画像の読み込み）
        LoadImage(path);

        //画像作成
        CreateImage();
    }

    private void Update()
    {
        if (Keyboard.current.aKey.wasReleasedThisFrame)
        {
            //撮影
            SaveScreen();

            //指定したファイルパスの画像を読み込んで、Unityで使える Texture2D に変換して返す処理
            //元画像（ゲーム内で撮影した画像の読み込み）
            LoadImage(path);

            //画像作成
            CreateImage();
        }
    }


    public void CreateImage()
    {
        RankingList = excelRankingLoader.GetCSVDatas();

        // ランキングデータが取得できているか確認
        if (RankingList == null || RankingList.Count == 0)
        {
            Debug.LogError("RankingListにデータがありません");
            return;
        }

        data = RankingList[RankingList.Count - 1];

       

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


    public Texture2D LoadResultImage(int number)
    {
        string saveDirectory = @"D:\GeneratedImages";

        string fileName = $"Result_{number:D3}.png";

        string path = Path.Combine(
            saveDirectory,
            fileName
        );

        if (!File.Exists(path))
        {
            Debug.LogError("画像がありません：" + path);
            return null;
        }

        byte[] bytes = File.ReadAllBytes(path);

        Texture2D texture = new Texture2D(2, 2);

        if (texture.LoadImage(bytes))
        {
            Debug.Log("画像を再取得しました：" + path);
            return texture;
        }

        Debug.LogError("画像の読み込みに失敗しました：" + path);
        return null;
    }


    public Sprite CreateSprite(Texture2D texture)
    {
        if (texture == null)
        {
            Debug.LogError("Texture2Dがありません");
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f)
        );

        return sprite;
    }


    /// <summary>
    /// 撮影
    /// </summary>
    public void SaveScreen()
    {
        int width = 1080;

        int height = 1080;


        string folderPath = @"D:\MyGame\ScreenShots";

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

    //
    public void SetImage(List<Image> image)
    {
        int number = GetNextImageNumber(saveDirectory) - 1;

        foreach (Image setting in image)
        {
            setting.sprite = CreateSprite(LoadResultImage(number));
            number--;
        }
    }

}