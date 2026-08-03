using GameFlowTemplate;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;




[DefaultExecutionOrder(-0)]
public class UIController : MonoBehaviour
{
    [Header("UIの保存場所")]
    [SerializeField] private UIData m_uiData;

    [Header("UIの保存場所")]
    [SerializeField] private ExcelLoader m_excelLoader;

    [Header("CSVのデータリスト")]
    private List<CSVPoseData> poseDatas;

    //private FlameBase m_FrontDoubleBiceps;
    //private FlameBase m_Most;
    //private FlameBase m_Side;

    private FlameBase[] m_Frame;

    private GameObject[] m_currentFrame;

    public GameObject GetCurrentSuccessFrame(CSVDataPoseFlow pose) { return m_Frame[pose.PoseID].m_currentFrameSuccess; }
    public GameObject GetCurrentApproachingFrame(CSVDataPoseFlow pose) { return m_Frame[pose.PoseID].m_currentFrameApproaching; }
    public GameObject GetCurrentFailureFrame(CSVDataPoseFlow pose) { return m_Frame[pose.PoseID].m_currentFrameFailure; }
    public GameObject GetCurrentWatingFrame(CSVDataPoseFlow pose) { return m_Frame[pose.PoseID].m_currentFrameWating; }


    //通常用のキャンバス
    [SerializeField] private Transform m_canvas;

    //三人称用のキャンバス
    [SerializeField] private Transform m_thirdPersonCanvas;

    private const string m_currentFrameSuccess = "Success";
    private const string m_currentFrameApproaching = "Approaching";
    private const string m_currentFrameFailure = "Failure";
    private const string m_currentFrameWating = "Wating";

    private void Awake()
    {
        poseDatas = m_excelLoader.excelPoseJudgeLoader.GetCSVDatas();
        CSVPoseData pose = poseDatas[0];
        m_Frame = new FlameBase[pose.PoseMax];

    }

    /*
    public void UIAnimation(PoseFlow poseFlow, CSVDataPoseFlow pose, float seconds)
    {
       

        switch (pose.PoseID)
        {
            //3人称視点
            case 3:
                UISet_thirdPerson(pose, seconds);
                UIMove_thirdPerson( pose, seconds);
                UIJudgeEnd_thirdPerson();
                break;

            //溜めてタイミング
            case 4:

                break;

            //キープタイミング
            case 5:

                break;

            //通常フレーム
            case <= 2:
                UISet_normal(pose, seconds);
                UIMove_normal(pose, seconds);
                UIJudgeEnd_normal(pose);


                break;
        }

        //強制終了と終了時間
        UIForcedQuit(poseFlow, pose, seconds);

    }
    */

    public void UISet_normal(CSVDataPoseFlow pose)
    {


        m_Frame[pose.PoseID].m_currentFrameSuccess = CreateFrame(pose.PoseID, m_currentFrameSuccess, Vector2.zero, m_canvas, new Vector2(650, 650));
        m_Frame[pose.PoseID].m_currentFrameApproaching = CreateFrame(pose.PoseID, m_currentFrameApproaching, Vector2.zero, m_canvas, new Vector2(650, 650));
        m_Frame[pose.PoseID].m_currentFrameFailure = CreateFrame(pose.PoseID, m_currentFrameFailure, Vector2.zero, m_canvas, new Vector2(650, 650));
        m_Frame[pose.PoseID].m_currentFrameWating = CreateFrame(pose.PoseID, m_currentFrameWating, Vector2.zero, m_canvas, new Vector2(650, 650));
        Debug.Log("wafewggg"+m_Frame[pose.PoseID].m_currentFrameSuccess);
        Show(m_Frame[pose.PoseID].m_currentFrameApproaching);
        Show(m_Frame[pose.PoseID].m_currentFrameWating);
        //isPoseShown = true;

    }

    public void UIMove_normal(CSVDataPoseFlow pose)
    {

        ScaleDown(m_Frame[pose.PoseID].m_currentFrameApproaching);


    }


    public void UIJudgeEnd_normal(CSVDataPoseFlow pose)
    {

        Show(m_Frame[pose.PoseID].m_currentFrameSuccess);


        Hide(m_Frame[pose.PoseID].m_currentFrameApproaching);
        Hide(m_Frame[pose.PoseID].m_currentFrameWating);



    }

    public void UISet_thirdPerson(Vector2 _pos, Transform _canvas)
    {

    }


    public void UISet_thirdPerson(CSVDataPoseFlow pose, Vector2 _pos, Transform _canvas)
    {


        m_currentFrame = new GameObject[12];
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

            //m_currentFrame[i] = CreateFrame(poseID, addFrameID, pos, m_thirdPersonCanvas, new Vector2(1000, 1000));
        }

        for (int i = 0; i < 3; ++i)
        {
            /*
            m_poseFrame[i].m_currentFrameFailure = m_currentFrame[failure + i];
            m_poseFrame[i].m_currentFrameSuccess = m_currentFrame[success + i];
            m_poseFrame[i].m_currentFrameApproaching = m_currentFrame[approaching + i];
            m_poseFrame[i].m_currentFrameWating = m_currentFrame[wating + i];
            */
        }

        for (int i = 1; i < m_currentFrame.Length; i += 2)
        {
            Show(m_currentFrame[i]);
        }




    }

    public void UIMove_thirdPerson()
    {


        for (int i = 1; i < m_currentFrame.Length; i += 4)
        {
            Debug.Log("kyouhagyuuniku");
            ScaleDown(m_currentFrame[i]);
        }



    }


    /*
    public void UIJudgeEnd_thirdPerson()
    {
        for (int poseID = 0; poseID < 2; poseID++)
        {
            int index = poseID * 4 + 1;

            if (!check)
            {
                //State?.Invoke(InGameState.End);
                break;
            }
            /*
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
                //m_effectSystem.PlayRandomEffect();
                //m_gameManager.AddScore((int)m_scoreController.GetScore());

            }

            if (!check)
            {
                State?.Invoke(InGameState.End);
                break;
            }

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
                //m_effectSystem.PlayRandomEffect();
                //m_gameManager.AddScore((int)m_scoreController.GetScore());

            }
            
        }


    }
*/
    public void UIJudge_thirdPerson()
    {
        for (int poseID = 0; poseID < 2; poseID++)
        {
            int index = poseID * 4 + 1;

            Show(m_currentFrame[index - 1]);
            Hide(m_currentFrame[index]);
            Hide(m_currentFrame[index + 2]);
            //check = false;
        }

    }

    public void UIForcedQuit(CSVDataPoseFlow pose)
    {

        DeleteFrame(m_Frame[pose.PoseID].m_currentFrameApproaching);
        DeleteFrame(m_Frame[pose.PoseID].m_currentFrameFailure);
        DeleteFrame(m_Frame[pose.PoseID].m_currentFrameWating);
        DeleteFrame(m_Frame[pose.PoseID].m_currentFrameSuccess);


    }


    /*
    // 強制終了と終了時間
    public void UIForcedQuit(PoseFlow poseFlow, CSVDataPoseFlow pose, float seconds)
    {
        // 強制終了時間
        if (seconds >= (pose.time + m_currentTImte) && poseFlow.HasNextPose())
        {

            for (int i = 0; i < m_currentFrame.Length; i++)
            {
                DeleteFrame(m_currentFrame[i]);


            }

            m_currentTImte = 0;
            poseFlow.NextPose();

            // 次のポーズ用にリセット
            isPoseShown = false;
            //once = true;
            check = true;

        }
    }
    */



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


    public GameObject CreateFrame(int _frameID, string _addFrameID, Vector2 _pos, Transform _canvas, Vector2 _size)
    {
        //m_uiData.getUI(m_currentFrameSuccess, pose.PoseID);


        GameObject obj = Instantiate(
            m_uiData.getUI(_addFrameID, _frameID),
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
