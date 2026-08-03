using GameFlowTemplate;
using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;

[DefaultExecutionOrder(-0)]
public class UIController : MonoBehaviour
{
    [Header("UIの保存場所")]
    [SerializeField] private UIData m_uiData;

    //一回表示の管理用
    private bool isPoseShown = false;

    bool check = true;


    private GameObject[] m_currentFrame;

    public GameObject[] GetCurrentFrame() { return m_currentFrame; }

 
    //通常用のキャンバス
    [SerializeField] private Transform m_canvas;

    //三人称用のキャンバス
    [SerializeField] private Transform m_thirdPersonCanvas;



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

        m_currentFrame = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            m_currentFrame[i] = CreateFrame(pose.PoseID, i * 3, Vector2.zero, m_canvas, new Vector2(650, 650));


        }

        Show(m_currentFrame[3]);
        Show(m_currentFrame[1]);
        isPoseShown = true;

    }

    public void UIMove_normal()
    {

        ScaleDown(m_currentFrame[1]);


    }


    public void UIJudgeEnd_normal()
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


    public void UISet_thirdPerson(CSVDataPoseFlow pose, float seconds)
    {
        if (!isPoseShown)
        {
            //m_currentTImte = seconds;
        }

        //開始時間
        //if (!isPoseShown && seconds <= (pose.time + m_currentTImte))
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

            //State?.Invoke(InGameState.Active);

        }


    }

    public void UIMove_thirdPerson(CSVDataPoseFlow pose, float seconds)
    {
        // 縮小(通常フレーム)
        //if (seconds <= (pose.time + m_currentTImte))
        {
            for (int i = 1; i < m_currentFrame.Length; i += 4)
            {
                ScaleDown(m_currentFrame[i]);
            }
            //イベント実行　当たり判定
            for (int i = 0; i < 3; i++)
            {
                //PoseJudgeFrame?.Invoke(i);
            }

        }
    }



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
            */
        }


    }

    public void UIForcedQuit()
    {

        for (int i = 0; i < m_currentFrame.Length; i++)
        {
            DeleteFrame(m_currentFrame[i]);

        }

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
