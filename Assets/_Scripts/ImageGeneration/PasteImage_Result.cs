using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class PasteImage_Result : MonoBehaviour
{

    [Header("一番上に現在の写真、次に１個前の写真と続く")]
    [SerializeField]
    private List<Image> Resultimage;

    [Header("一番上に現在の写真、次に１個前の写真と続く")]
    [SerializeField]
    private List<Image> ResultScore;

    // Dドライブ直下の保存フォルダ
    string saveDirectory = @"D:\GeneratedImages";

    private void Start()
    {

        //ファイル内の画像をImageのリスト分を貼り付ける
        SetImage(Resultimage);


        SetImage(ResultScore);
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