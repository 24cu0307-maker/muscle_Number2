using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


// ============================================================
// 重ねる画像の設定
// ============================================================
[Serializable]
public class OverlayImageSetting
{
    [Header("画像")]
    public Texture2D image;

    [Header("位置")]
    public int x;
    public int y;

    [Header("サイズ")]
    public int width = 100;
    public int height = 100;
}

// ============================================================
// 数字画像の設定
// ============================================================
[Serializable]
public class OverlayImageSettingNumbers
{

    // 数字アトラス
    [Header("数字アトラス")]
    [SerializeField]
    public Texture2D numberAtlas;


    // 数字表示設定
    [Header("数字表示設定")]

    // 数字を表示する開始位置
    [SerializeField]
    public int numberX = 500;

    [SerializeField]
    public int numberY = 700;


    // 1文字の表示サイズ
    [SerializeField]
    public int numberWidth = 100;

    [SerializeField]
    public int numberHeight = 150;


    // 数字同士の間隔
    [SerializeField]
    public int numberSpacing = 10;

    //表示する数値
    [SerializeField]
    public int number = 0;

    // ========================================================
    // アトラス設定
    // ========================================================
    [Header("アトラス設定")]

    // 0～9なので10分割
    [SerializeField]
    public int atlasColumns = 10;
}

// ============================================================
// アトラス画像の設定
// ============================================================
[Serializable]
public class Atlas
{

    // 数字アトラス
    [Header("アトラス画像")]
    [SerializeField]
    public Texture2D atlas;


    // 数字表示設定
    [Header("画像表示設定")]

    // 数字を表示する開始位置
    [SerializeField]
    public int numberX = 0;

    [SerializeField]
    public int numberY = 0;


    // 1文字の表示サイズ
    [SerializeField]
    public int numberWidth = 100;

    [SerializeField]
    public int numberHeight = 100;


    // 複数表示する場合の間隔
    [SerializeField]
    public int numberSpacing = 10;

    //表示する画像
    [SerializeField]
    public int texture = 0;

    // ========================================================
    // アトラス設定
    // ========================================================
    [Header("アトラス設定")]

    // 0～9なので10分割
    [SerializeField]
    public int atlasColumnsWidth = 10;

    // 0～9なので10分割
    [SerializeField]
    public int atlasColumnsHeight = 10;

    
}


// ============================================================
// 画像生成
// ============================================================
public class ImageGenerator : MonoBehaviour
{
    [SerializeField]
    private ExcelRankingLoader excelRankingLoader;

    List<RnakingData> RankingList;

    RnakingData data;

 
    // 生成画像サイズ
    [Header("生成画像サイズ")]
    [SerializeField]
    private int imageWidth = 1080;

    [SerializeField]
    private int imageHeight = 1080;


    // 重ねる画像
    [Header("重ねる画像")]
    [SerializeField]
    private List<OverlayImageSetting> overlayImages =
        new List<OverlayImageSetting>();


    // 数字画像の設定
    [Header("数字画像の設定")]
    [SerializeField]
    private List<OverlayImageSettingNumbers> overlayImageSettingNumbers =
        new List<OverlayImageSettingNumbers>();

    // アトラス画像の設定
    [Header("アトラス画像の設定")]
    [SerializeField]
    private List<Atlas> atlasSetting =
        new List<Atlas>();



    private void Awake()
    {
        RankingList = excelRankingLoader.GetCSVDatas();
    }

    // ========================================================
    // 画像生成
    // ========================================================
    /// <summary>
    /// 画像を生成する
    /// </summary>
    /// <param name="baseImagePath">元画像のパス</param>
    /// <param name="savePath">生成画像の保存先</param>
    /// <param name="score">表示するスコア</param>
    /// <returns>生成した画像のパス</returns>
    public string GenerateImage(
        string baseImagePath,
        string savePath
        )
    {
        data.Texture = savePath;

        // 元画像の存在確認
        if (!File.Exists(baseImagePath))
        {
            Debug.LogError(
                "元画像が存在しません : "
                + baseImagePath
            );

            return null;
        }


        // 元画像を読み込む
        byte[] baseBytes =
            File.ReadAllBytes(baseImagePath);

        Texture2D baseTexture =
            new Texture2D(
                2,
                2,
                TextureFormat.RGBA32,
                false
            );


        if (!baseTexture.LoadImage(baseBytes))
        {
            Debug.LogError(
                "元画像の読み込みに失敗しました : "
                + baseImagePath
            );

            Destroy(baseTexture);

            return null;
        }

        //結果画像を作成
        Texture2D result =
            new Texture2D(
                imageWidth,
                imageHeight,
                TextureFormat.RGBA32,
                false
            );


        //結果画像を透明で初期化
        Color[] pixels =
            new Color[
                imageWidth * imageHeight
            ];


        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.clear;
        }


        result.SetPixels(pixels);


        // 元画像を配置
        DrawTexture(
            result,
            baseTexture,
            0,
            0,
            imageWidth,
            imageHeight
        );


        // 元画像はもう必要ない
        Destroy(baseTexture);

        int index = 0;

        //Inspectorで設定した画像を重ねる
        foreach (
            OverlayImageSetting setting
            in overlayImages)
        {
            /*
            // 画像が設定されていなければスキップ
            if (setting.image == null)
            {
                continue;
            }
            */
            if(index == 1)
            {
                setting.image = GetRandomNumberTexture(atlasSetting[0]);

            }
            index++;

            DrawTexture(
                result,
                setting.image,
                setting.x,
                setting.y,
                setting.width,
                setting.height
            );
        }


        //Scoreを数字画像として表示
        DrawNumber(
            result

        );


        //PNGに変換
        byte[] pngData =
            result.EncodeToPNG();


        //保存先フォルダを作成
        string directory =
            Path.GetDirectoryName(savePath);


        if (!string.IsNullOrEmpty(directory))
        {
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }


        //PNG保存
        File.WriteAllBytes(
            savePath,
            pngData
        );


        Debug.Log(
            "画像を保存しました : "
            + savePath
        );


        //結果画像を削除
        Destroy(result);


        //保存先を返す
        return savePath;
    }


    // ========================================================
    // 数字を画像として表示
    // ========================================================
    /// <summary>
    /// 数字をアトラスから取得して表示する
    /// </summary>
    private void DrawNumber(
        Texture2D destination
       )
    {
        int index = 0;

        foreach (
            OverlayImageSettingNumbers setting
            in overlayImageSettingNumbers)
        {
            // アトラスが設定されているか確認
            if (setting.numberAtlas == null)
            {
                Debug.LogError(
                    "数字アトラスが設定されていません。"
                );

                return;
            }

           

            data = RankingList[RankingList.Count - 1];


            if(index == 0)
            {
                setting.number = data.Score;

            }
            else if (index == 1)
            {
                setting.number = data.Number;

            }

            // マイナス値の場合
            if (setting.number < 0)
            {
                Debug.LogWarning(
                    "マイナスの数字には対応していません。"
                );

                return;
            }

            // 数字を文字列にする
            string numberString =
                setting.number.ToString();

            // 1文字ずつ処理
            for (
                int i = 0;
                i < numberString.Length;
                i++)
            {
                // 文字を数字に変換
                int digit =
                    numberString[i] - '0';


                // アトラスから数字を取得
                Texture2D digitTexture =
                    GetNumberTexture(setting, digit);


                if (digitTexture == null)
                {
                    continue;
                }


                // 表示位置
                int x =
                    setting.numberX
                    + i *
                    (
                        setting.numberWidth
                        + setting.numberSpacing
                    );

                // 数字を描画
                DrawTexture(
                    destination,
                    digitTexture,
                    x,
                    setting.numberY,
                    setting.numberWidth,
                    setting.numberHeight
                );

                // 一時Textureを削除
                Destroy(digitTexture);
                index++;
            }
        }


    }


    // ========================================================
    // アトラスから数字を切り出す
    // ========================================================
    /// <summary>
    /// 数字アトラスから0～9の数字を切り出す
    /// </summary>
    private Texture2D GetNumberTexture(
        OverlayImageSettingNumbers setting,
        int number)
    {


        // 数字チェック
        if (number < 0 || number > 9)
        {
            Debug.LogError(
                "数字は0～9で指定してください。"
            );

            return null;
        }

        // アトラスの1マスのサイズ
        int cellWidth =
            setting.numberAtlas.width
            / setting.atlasColumns;


        int cellHeight =
            setting.numberAtlas.height;

        // 数字の位置
        int startX =
            number * cellWidth;


        int startY = 0;

        // アトラスからピクセル取得
        Color[] pixels =
            setting.numberAtlas.GetPixels(
                startX,
                startY,
                cellWidth,
                cellHeight
            );

        // 切り出した数字用Textureを作成
        Texture2D numberTexture =
            new Texture2D(
                cellWidth,
                cellHeight,
                TextureFormat.RGBA32,
                false
            );


        numberTexture.SetPixels(pixels);

        numberTexture.Apply();


        return numberTexture;
    }


    // ========================================================
    // Texture2Dを描画
    // ========================================================
    /// <summary>
    /// Texture2Dを指定位置・サイズで描画する
    /// </summary>
    private void DrawTexture(
        Texture2D destination,
        Texture2D source,
        int x,
        int y,
        int width,
        int height)
    {
        // サイズチェック
        if (width <= 0 ||
            height <= 0)
        {
            Debug.LogWarning(
                "画像サイズが不正です。"
            );

            return;
        }


        // 元画像のピクセル取得
        Color[] sourcePixels =
            source.GetPixels();


        // 指定サイズに拡大・縮小
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                // 元画像の座標
                int sourceX =
                    px * source.width / width;


                int sourceY =
                    py * source.height / height;


                Color color =
                    sourcePixels[
                        sourceY * source.width
                        + sourceX
                    ];


                // 描画先の座標
                int destX =
                    x + px;


                int destY =
                    y + py;


                // 範囲外なら無視
                if (
                    destX < 0 ||
                    destX >= destination.width ||
                    destY < 0 ||
                    destY >= destination.height
                )
                {
                    continue;
                }


                // 現在の色
                Color current =
                    destination.GetPixel(
                        destX,
                        destY
                    );


                // 透明度を考慮して合成
                Color resultColor =
                    Color.Lerp(
                        current,
                        color,
                        color.a
                    );


                destination.SetPixel(
                    destX,
                    destY,
                    resultColor
                );
            }
        }


        destination.Apply();
    }

    

    


    private Texture2D GetRandomNumberTexture(Atlas setting)
    {
        if (setting.atlas == null)
        {
            Debug.LogError("アトラス画像が設定されていません。");
            return null;
        }

        // 0～27からランダム
        int number = UnityEngine.Random.Range(0, 28);

        Debug.Log("ランダム番号：" + number);

        Texture2D atlas = setting.atlas;

        // 1画像分のサイズ
        int width = atlas.width / setting.atlasColumnsWidth;
        int height = atlas.height / setting.atlasColumnsHeight;

        // --------------------------------
        // 横・縦の何番目かを計算
        // --------------------------------

        int column = number % setting.atlasColumnsWidth;
        int row = number / setting.atlasColumnsWidth;

        // --------------------------------
        // アトラス上の位置
        // --------------------------------

        int x = column * width;
        int y = row * height;

        Debug.Log(
            $"番号={number} " +
            $"列={column} 行={row} " +
            $"x={x} y={y} " +
            $"width={width} height={height}"
        );

        // --------------------------------
        // 範囲チェック
        // --------------------------------

        if (x + width > atlas.width ||
            y + height > atlas.height)
        {
            Debug.LogError("ランダム番号：" + number);

            Debug.LogError(
                $"アトラス範囲外です。" +
                $"Atlas={atlas.width}x{atlas.height}, " +
                $"x={x}, y={y}, " +
                $"width={width}, height={height}"
            );

            return null;
        }

        // --------------------------------
        // ピクセル取得
        // --------------------------------

        Color[] pixels = atlas.GetPixels(
            x,
            y,
            width,
            height
        );

        // --------------------------------
        // 新しいTextureを作成
        // --------------------------------

        Texture2D result = new Texture2D(
            width,
            height,
            TextureFormat.RGBA32,
            false
        );

        result.SetPixels(pixels);
        result.Apply();

        Debug.Log("生成した画像：" + number);

        return result;
    }
}