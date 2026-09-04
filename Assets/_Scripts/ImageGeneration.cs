using System.IO;
using UnityEngine;
using UnityEngine.Audio;

public class ImageGeneration : MonoBehaviour
{
    [Header("生成する画像の横幅")]
    [Range(0, 1080)]
    [SerializeField]
    private int width;

    [Header("生成する画像の縦幅")]
    [Range(0, 1080)]
    [SerializeField]
    private int height;




    //ゲーム中に撮影した画像
    Texture2D musclePoseTexture;

    //枠組み
    Texture2D frameworkTexture;

    //お言葉 （アトラス画像で管理）
    Texture2D textTexture;

    //数字   （アトラス画像で管理）
    Texture2D numberTexture;


    public void TextureGeneration()
    {
        //画像生成
        Texture2D texture = new Texture2D(width, height);






        string overlayPath =
            @"C:\Images\character.png";

        byte[] data = File.ReadAllBytes(overlayPath);

        Texture2D overlay = new Texture2D(
            2,
            2,
            TextureFormat.RGBA32,
            false
        );



        overlay.LoadImage(data);

    
       

    }

   
}
