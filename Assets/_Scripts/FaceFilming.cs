using System.Collections;
using System.IO;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FaceFilming : MonoBehaviour
{


    bool once = true;



    [SerializeField] private RectTransform targetPanel;
    [SerializeField] private Camera uiCamera;

    [SerializeField] private Vector3[] Face = new Vector3[478];
    [SerializeField] private Vector2[] screenFace = new Vector2[478];


    // カウント表示用
    [SerializeField] private TextMeshProUGUI countText;

    // カウント中かどうか
    private bool isCounting = false;


    private void Update()
    {
        if (PositionDataManager.Instance == null)
            return;

        Face = PositionDataManager.Instance.positionData.Face;

        if (Face == null || Face.Length < 478)
            return;

        if (IsFaceInsidePanel(Face) && once && !isCounting)
        {
            StartCoroutine(PhotoCountDown());
            Debug.Log("顔がPanelの中に入りました！");
            
        }

     
    }



    private bool IsFaceInsidePanel(Vector3[] face)
    {
        int[] checkPoints =
        {
        10,   // 額付近
        127,
        152,  // 顎
        234,  // 左頬
        356,
        454,  // 右頬
        1     // 鼻付近
    };

        foreach (int index in checkPoints)
        {
            
            Vector2 screenPos = new Vector2(
                face[index].x * Screen.width,
                (1.0f + face[index].y) * Screen.height
            );

            if (!RectTransformUtility.RectangleContainsScreenPoint(
                targetPanel,
                screenPos,
                uiCamera))
            {
                return false;
            }
            
          
        }

        return true;
    }

    // 3秒カウントして撮影
    private IEnumerator PhotoCountDown()
    {
        isCounting = true;

        // 3秒カウント
        for (int count = 3; count > 0; count--)
        {
            countText.text = count.ToString();

            // 1秒間、毎フレーム顔の位置を確認
            float timer = 0f;

            while (timer < 1f)
            {
                // 顔がPanelから外れた
                if (!IsFaceInsidePanel(Face))
                {
                    Debug.Log("顔がPanelから外れました。カウントをやり直します。");

                    countText.text = "";
                    isCounting = false;

                    yield break;
                }

                timer += Time.deltaTime;
                yield return null;
            }
        }

        // カウント終了
        countText.text = "";

        // 撮影
        SaveScreen();

        Debug.Log("撮影しました！");

        // 一度だけ撮影
        once = false;

        isCounting = false;
    }


    private void SaveScreen()
    {
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

        ScreenCapture.CaptureScreenshot(path);

        Debug.Log("スクリーンショットを保存しました：" + path);

        UnityEngine.SceneManagement.SceneManager.LoadScene("Tutorial");
    }
    /*
    private void SaveScreen()
    {
        string path = Path.Combine(
            Application.dataPath,
            "..",
            "ScreenShot.png"
        );

        ScreenCapture.CaptureScreenshot(path);

        Debug.Log("スクリーンショットを保存しました：" + path);
    }


    */





    /*
    [SerializeField] private Vector3[] _face = new Vector3[478];

    [SerializeField] private Vector2[] _face2D = new Vector2[478];

    [SerializeField] private Camera targetCamera;


    private void Update()
    {
        _face = PositionDataManager.Instance.positionData.Face;

        for (int i = 0; i < 478; i++)
        {
            WorldToScreen(_face[i] ,i);

        }
    }


    private Vector2 WorldToScreen(Vector3 worldPosition,int num)
    {
        Vector3 screenPosition =
            targetCamera.WorldToScreenPoint(worldPosition);

        _face2D[num] = new Vector2(
            screenPosition.x,
            screenPosition.y
        );

        return _face2D[num];
    }
    */
}
/*
public class FaceFilming : MonoBehaviour
{
    [SerializeField] private Vector3[] _face = new Vector3[478];

    [SerializeField] private RectTransform targetCircle;

    void Update()
    {
        if (PositionDataManager.Instance == null) return;

        _face = PositionDataManager.Instance.positionData.Face;

        if (_face == null || _face.Length == 0) return;

        if (IsFaceInsideCircle(_face))
        {
            Debug.Log("顔が円の中に入りました！");
        }
    }

    public bool IsFaceInsideCircle(Vector3[] face)
    {
        // 円の中心をScreen座標にする
        Vector2 circleCenter = RectTransformUtility.WorldToScreenPoint(
            null,
            targetCircle.position
        );

        // 円の半径
        float radius = targetCircle.rect.width * 0.5f;

        for (int i = 0; i < face.Length; i++)
        {
            // MediaPipeの正規化座標 → Screen座標
            Vector2 screenPos = new Vector2(
                face[i].x * Screen.width,
                (1.0f - face[i].y) * Screen.height
            );

            float distance = Vector2.Distance(
                screenPos,
                circleCenter
            );

            if (distance > radius)
            {
                return false;
            }
        }

        return true;
    }
}

public class FaceFilming : MonoBehaviour
{
    //座標を格納する箱
    [SerializeField] private Vector3[] face = new Vector3[478];

    [SerializeField] private Vector2[] screenface = new Vector2[478];

    [SerializeField] private RectTransform targetCircle;
    [SerializeField] private Camera targetCamera;

    void Update()
    {
        if (!PositionDataManager.Instance) return;

        //座標を取得
        face = PositionDataManager.Instance.positionData.Face;
        for (int i = 0; i < 478; i++)
        {
            // MediaPipeの正規化座標 → 画面座標
            screenface[i] = new Vector2(face[i].x * Screen.width, (1.0f - face[i].y) * Screen.height);


        }

        if (IsFaceInsideCircle(face))
        {
            Debug.Log("顔が円の中に入りました！_000");
        }

        if (IsFaceInsidePanel(face))
        {
            Debug.Log("顔が円の中に入りました！_001");
        }
    }

    public bool IsFaceInsideCircle(Vector3[] _face)
    {
        Vector2 circleCenter = targetCircle.position;
        float radius = targetCircle.rect.width * 0.5f;

        for (int i = 0; i < _face.Length; i++)
        {
            // MediaPipeの正規化座標 → 画面座標
            Vector2 screenPos = new Vector2(
                _face[i].x * Screen.width,
                (1.0f - _face[i].y) * Screen.height
            );

            float distance = Vector2.Distance(screenPos, circleCenter);

            // 1点でも円の外に出たら失敗
            if (distance > radius)
            {
                return false;
            }
        }

        return true;
    }

    [SerializeField] private RectTransform targetPanel;
    [SerializeField] private Vector2[] screenFace = new Vector2[478];

    private bool IsFaceInsidePanel(Vector3[] face)
    {
        for (int i = 0; i < 478; i++)
        {
            // MediaPipe → 画面座標
            screenFace[i] = new Vector2(
                face[i].x * Screen.width,
                (1.0f - face[i].y) * Screen.height
            );

            // 画面座標 → Panelのローカル座標
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                targetPanel,
                screenFace[i],
                null,
                out localPoint
            );

            Debug.Log(
    $"Face[{i}] Screen={screenFace[i]} Local={localPoint} " +
    $"Panel={targetPanel.rect}"
);
            //Debug.Log("Pxxx" + targetPanel.rect.x + ("Pxxy" + targetPanel.rect.y));

            // PanelのRect内に入っているか
            if (!targetPanel.rect.Contains(localPoint))
            {
                return false;
            }
        }

        return true;
    }
}
*/
