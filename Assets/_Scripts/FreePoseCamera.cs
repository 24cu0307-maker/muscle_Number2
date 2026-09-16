using System.Collections;
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

    [Header("判定")]
    [SerializeField]
    private PoseJudgeManager m_judgeManager;

   [Header("最大ズームZ座標")]
    [SerializeField] private float m_maxZoomZ = -10.0f;

    [Header("判定許容範囲")]
    [SerializeField] private float m_positionTolerance = 0.05f;

    private bool m_hasCaptured = false;

    [Header("判定後の何秒後に撮影のシステム")]
    [SerializeField] private bool m_isCaptureSystemOn = false;

    [Header("判定後の何秒後に撮影")]
    [SerializeField] private float m_captureDelay = 2.0f;

    [SerializeField]
    string folderPath = @"D:\MyGame\ScreenShots";

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
        if (currentZ >= m_maxZoomZ - m_positionTolerance && !m_isCaptureSystemOn)
        {
            m_hasCaptured = true;

            Debug.Log("CameraFree +++ 最大ズーム到達");

            Capture();

           
        }

        if(m_isCaptureSystemOn && m_judgeManager.LastGrade == EPoseMatchGrade.Great || m_judgeManager.LastGrade == EPoseMatchGrade.Perfect)
        {
            m_hasCaptured = true;
            StartCoroutine(CaptureAfterDelay());

        
        }
    }


    private void Capture()
    {
        

        string path;
        path = Path.Combine(
           folderPath,
           "ScreenShot.png");

        Debug.Log("★ 撮影 ★");

        m_imageGeneratorTest.SaveScreen();

        //指定したファイルパスの画像を読み込んで、Unityで使える Texture2D に変換して返す処理
        //元画像（ゲーム内で撮影した画像の読み込み）
        //m_imageGeneratorTest.LoadImage(path);

        //画像作成
        //m_imageGeneratorTest.CreateImage();
    }

    /// <summary>
    /// 撮影処理
    /// </summary>
    private void TimeCapture()
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
        //m_imageGeneratorTest.LoadImage(path);

        //画像作成
        //m_imageGeneratorTest.CreateImage();
    }

    private IEnumerator CaptureAfterDelay()
    {
        string folderPath = @"D:\MyGame\ScreenShots";

        string path;
        path = Path.Combine(
           folderPath,
           "ScreenShot.png");

        // 2秒待つ
        yield return new WaitForSeconds(m_captureDelay);

        // 撮影処理
        TimeCapture();

        // スクリーンショットの保存完了を待つ
        yield return new WaitForSeconds(1);

        m_imageGeneratorTest.LoadImage(path);

        // スクリーンショットの保存完了を待つ
        yield return new WaitForSeconds(1);
        m_imageGeneratorTest.CreateImage();
    }

   
}
