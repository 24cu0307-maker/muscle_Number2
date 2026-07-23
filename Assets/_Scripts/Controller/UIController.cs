using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;

[DefaultExecutionOrder(-200)]
public class UIController : MonoBehaviour
{
    [Header("UIの保存場所")]
    [SerializeField] private UIData m_uiData;

    [Header("ポーズを判定する")]
    [SerializeField] private PoseJudgeController m_poseJudgeController;

    //aa
    //一回表示の管理用
    private bool isPoseShown = false;

    //固定値
    private const int success = 0;
    private const int approaching = 3;
    private const int failure = 6;
    private const int wating = 9;

    public Action<int> PoseJudgeFrame;

    private GameObject[] m_currentFrame;

    private GameObject m_currentFrameSuccess;
    private GameObject m_currentFrameApproaching;
    private GameObject m_currentFrameFailure;
    private GameObject m_currentFrameWating;

    private bool once = true;
    private float time = 0;

    private const int size = 200;

    bool check = true;

    /*
    [SerializeField] private GameObject uiPrefab1;
    [SerializeField] private GameObject uiPrefab2;
    */

    [SerializeField] private Transform m_canvas;

    [SerializeField] private Transform m_thirdPersonCanvas;

    public void UIAnimation(PoseFlow poseFlow, CSVDataPoseFlow pose, float seconds)
    {

        //フレームごとの処理
        switch (pose.PoseID)
        {
            //3人称視点
            case 3:

                //開始時間
                if (!isPoseShown && seconds >= pose.start && seconds < pose.end)
                {
                    m_currentFrame = new GameObject[8];
                    for (int i = 0; i < m_currentFrame.Length; i++)
                    {
                        int poseID = i / 4;          // 0,0,0,0,1,1,1,1,2,2,2,2
                        int addFrameID = (i % 4) * 3; // 0,3,6,9

                        Vector2 pos = poseID switch
                        {
                            0 => new Vector2(100, 0),
                            1 => new Vector2(-100, 0),
                            2 => new Vector2(-500, 0),
                            _ => Vector2.zero
                        };

                        m_currentFrame[i] = CreateFrame(poseID, addFrameID, pos, m_thirdPersonCanvas, new Vector2(1000, 1000));
                    }

                    for (int i = 1; i < m_currentFrame.Length; i += 2)
                    {
                        Show(m_currentFrame[i]);
                    }
                    isPoseShown = true;


                }


                // 縮小(通常フレーム)
                if (seconds >= pose.start && seconds < pose.end)
                {
                    for (int i = 1; i < m_currentFrame.Length; i += 4)
                    {
                        ScaleDown(m_currentFrame[i]);
                    }
                    //イベント実行　当たり判定
                    for (int i = 0; i < 3; i++)
                    {
                        PoseJudgeFrame?.Invoke(i);
                    }

                }

                Debug.Log("[ace] " + check);
                if (!check) break;
                Debug.Log("[ace1] " + check);

                for (int poseID = 0; poseID < 2; poseID++)
                {
                    int index = poseID * 4 + 1;

                    Debug.Log("[ace2] " + check);
                    if (!check) break;
                    Debug.Log("[ace3] " + check);
                    //通常
                    if (m_poseJudgeController.GetisPose(poseID) &&
                        m_poseJudgeController.PoseJudge_Normal(
                            m_currentFrame[index],
                            m_currentFrame[index + 2]))
                    {
                        Show(m_currentFrame[index - 1]);
                        Hide(m_currentFrame[index]);
                        Hide(m_currentFrame[index + 2]);
                        check = false;
                    }

                    if (!check) break;

                    //完璧
                    if (m_poseJudgeController.GetisPose(poseID) &&
                       m_poseJudgeController.PoseJudge_Perfect(
                           m_currentFrame[index],
                           m_currentFrame[index + 2]))
                    {
                        Show(m_currentFrame[index - 1]);
                        Hide(m_currentFrame[index]);
                        Hide(m_currentFrame[index + 2]);
                        check = false;
                    }
                }

            
                break;

            //溜めてタイミング
            case 4:

                break;

            //キープタイミング
            case 5:

                break;

            //通常フレーム
            case <= 2:

                //開始時間
                if (!isPoseShown && seconds >= pose.start && seconds < pose.end)
                {
                    m_currentFrame = new GameObject[4];
                    for (int i = 0; i < 4; i++)
                    {
                        m_currentFrame[i] = CreateFrame(pose.PoseID, i * 3, Vector2.zero, m_canvas, new Vector2(650,650));


                    }
                    /*
                    m_currentFrameWating = CreateFrame(pose.PoseID, wating, Vector2.zero);
                    m_currentFrameApproaching = CreateFrame(pose.PoseID, approaching, Vector2.zero);
                    m_currentFrameFailure = CreateFrame(pose.PoseID, failure, Vector2.zero);
                    m_currentFrameSuccess = CreateFrame(pose.PoseID, success, Vector2.zero);
                    */
                    Show(m_currentFrame[3]);
                    Show(m_currentFrame[1]);
                    isPoseShown = true;
                }


                // 縮小(通常フレーム)
                if (seconds >= pose.start && seconds < pose.end)
                {
                    ScaleDown(m_currentFrame[1]);


                    //イベント実行　当たり判定
                    PoseJudgeFrame?.Invoke(pose.PoseID);
                }

                //完璧成功時
                if (m_poseJudgeController.GetisPose(pose.PoseID) &&
                    m_poseJudgeController.PoseJudge_Normal(m_currentFrame[1], m_currentFrame[3]))
                {


                    for (int i = 0; i < m_currentFrame.Length; i += 4)
                    {
                        Show(m_currentFrame[i]);

                    }

                    for (int i = 1; i < m_currentFrame.Length; i += 2)
                    {
                        Hide(m_currentFrame[i]);
                    }
                }

                //通常成功時
                if (m_poseJudgeController.GetisPose(pose.PoseID) &&
                    m_poseJudgeController.PoseJudge_Perfect(m_currentFrame[1], m_currentFrame[3]))
                {


                    for (int i = 0; i < m_currentFrame.Length; i += 4)
                    {
                        Show(m_currentFrame[i]);

                    }

                    for (int i = 1; i < m_currentFrame.Length; i += 2)
                    {
                        Hide(m_currentFrame[i]);
                    }
                }
                break;
        }



        /*
        Debug.Log("[数値1]" + pose.PoseID);
        //開始時間
        if (!isPoseShown && seconds >= pose.start && seconds < pose.end)
        {
           
            //if(specialFrame <= 2)
            {
                Show(pose.PoseID + wating);
                Show(pose.PoseID + approaching);
                isPoseShown = true;
                Debug.Log("[数値4]" + pose.PoseID);
            }
            
            
            if (pose.PoseID == 1)
            {
                GameObject obj1 = Instantiate(uiPrefab1, canvas);
                GameObject obj2 = Instantiate(uiPrefab2, canvas);

                RectTransform rect1 = obj1.GetComponent<RectTransform>();
                RectTransform rect2 = obj2.GetComponent<RectTransform>();
                rect1.anchoredPosition = new Vector2(200, 100);
                rect2.anchoredPosition = new Vector2(-200, 100);

                rect1.sizeDelta = new Vector2(400, 400);
                rect2.sizeDelta = new Vector2(400, 400);

                Debug.Log("[数値3]" + pose.PoseID);

               // obj1.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
              //  obj2.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

                obj1.SetActive(true);
                obj2.SetActive(true);

                isPoseShown = true;
            }
            Debug.Log("[数値2]" + pose.PoseID);
            
        }

       
        
      
        
        
        // 三人称(通常フレーム)
        if (pose.PoseID == 1 && seconds >= pose.start && seconds < pose.end)
        {
            /*
            if(once)
            {
                time = seconds;
                once = false;
            }


            if (seconds >= (time + 5.0f))
            {

            }
            //
          


            //イベント実行　当たり判定
            PoseJudgeFrame?.Invoke(pose.PoseID);
            //イベント実行　当たり判定
            PoseJudgeFrame?.Invoke(pose.PoseID);

        }
    */

      

        
       
             
        // 終了時間
        if (seconds >= pose.end && poseFlow.HasNextPose())
        {

            for (int i = 0; i < m_currentFrame.Length; i++)
            {
                DeleteFrame(m_currentFrame[i]);


            }

            /*
            Hide(m_currentFrameSuccess);
            Hide(m_currentFrameWating);
            Hide(m_currentFrameApproaching);
            ScaleReset(m_currentFrameApproaching);
            */
            poseFlow.NextPose();

            // 次のポーズ用にリセット
            isPoseShown = false;
            //once = true;
            check = true;

        }

    }


    /// <summary>
    ///サイズダウン
    /// <summary>
    public void ScaleDown(GameObject m_uiData)
    {
        m_uiData.transform.localScale -= Vector3.one * Time.deltaTime * 0.02f;
    }

    /// <summary>
    ///サイズリセット
    /// <summary>
    public void ScaleReset(GameObject m_uiData)
    {
        m_uiData.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);
    }

    /// <summary>
    ///表示
    /// <summary>
    public void Show(GameObject m_uiData)
    {
        m_uiData.SetActive(true);
    }

    /// <summary>
    ///非表示
    /// <summary>
    public void Hide(GameObject m_uiData)
    {
        m_uiData.SetActive(false);
    }


    public GameObject CreateFrame(int _frameID, int _addFrameID, Vector2 _pos, Transform _canvas, Vector2 _size)
    {
        GameObject obj = Instantiate(
            m_uiData.getUI(_frameID + _addFrameID),
            _canvas
        );

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchoredPosition = _pos;

        rect.sizeDelta = _size;

        return obj;
    }

    public void DeleteFrame(GameObject _uiFrame)
    {
        Destroy(_uiFrame);
    }

}
