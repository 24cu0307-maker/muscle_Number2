using System.IO;
using UnityEngine;

public class FreePoseCamera : MonoBehaviour
{
    [Header("監視するカメラ")]
    [SerializeField] private Camera m_targetCamera;

    [Header("画像生成")]
    [SerializeField]
    private ImageGeneratorTest m_imageGeneratorTest;

    [Header("ポーズ準")]
    [SerializeField]
    private PoseFlowDataManager m_flowDataManager;

    [Header("最大ズームZ座標")]
    [SerializeField] private float m_maxZoomZ = -10.0f;

    [Header("判定許容範囲")]
    [SerializeField] private float m_positionTolerance = 0.05f;

    private bool m_hasCaptured = false;

    private void Start()
    {
        // カメラを指定していなければMainCameraを取得
        if (m_targetCamera == null)
        {
            m_targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        Debug.Log("PoseIDID" + m_flowDataManager.GetPose().PoseID);

        // カメラが取得できていない場合
        if (m_targetCamera == null)
        {
            return;
        }
        
        if(m_flowDataManager.GetPose().PoseID != 9)
        {
            return;
        }
        
        // 現在のカメラZ座標
        float currentZ = m_targetCamera.transform.position.z;

        Debug.Log("CameraFree Z : " + currentZ);

        // すでに撮影済みなら何もしない
        if (m_hasCaptured)
        {
            return;
        }

        // 最大ズーム位置に到達したか
        if (currentZ >= m_maxZoomZ - m_positionTolerance)
        {
            m_hasCaptured = true;

            Debug.Log("CameraFree +++ 最大ズーム到達");

            Capture();
        }
    }




    /// <summary>
    /// 撮影処理
    /// </summary>
    private void Capture()
    {
        string folderPath = @"D:\MyGame\ScreenShots";

        string path;
        path = Path.Combine(
           folderPath,
           "ScreenShot.png");

        Debug.Log("★ 撮影 ★");

        m_imageGeneratorTest.SaveScreen();

        //指定したファイルパスの画像を読み込んで、Unityで使える Texture2D に変換して返す処理
        //元画像（ゲーム内で撮影した画像の読み込み）
        m_imageGeneratorTest.LoadImage(path);

        //画像作成
        m_imageGeneratorTest.CreateImage();
    }
}