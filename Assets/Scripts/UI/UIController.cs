using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
public class UIController : MonoBehaviour
{
    //エクセルデータ
    [SerializeField] private ExcelLoader excelLoader;
    //タイマー
    [SerializeField] private Timer timer;
    //UIオブジェクト
    [SerializeField] private UIData uIData;

    [SerializeField] private AngleJudge angleJudge;

    [SerializeField] private PoseCameraDirector poseCameraDirector;

    //UIアニメーション
    private UIAnimation uIAnimation;
    //UIの表示非表示
    private UIPresenter uIPresenter;
    //ポーズの成功判定
    private PoseJudge poseJudge;

    //ポーズ順
    private PoseFlow poseFlow;


    private　CSVDataPoseFlow pose;

    //一回表示の管理用
    private bool isPoseShown = false;

    public Action<int> action;

    //固定値
    private const int success = 0;
    private const int approaching = 3;
    private const int failure = 6;
    private const int wating = 9;

    private bool stop = true;
    // Start is called before the first frame update
    public void Start()
    {
        uIPresenter = new UIPresenter();
        uIAnimation = new UIAnimation();
        poseJudge = new PoseJudge();
        // CSVのデータをPoseFlowへ渡す
        poseFlow = new PoseFlow(excelLoader.excelPoseTimeFlowLoader.GetCSVDatas());
    }

    // Update is called once per frame
    void Update()
    {


        //現在
        pose = poseFlow.CurrentPose();

        //開始時間
        if (!isPoseShown &&
           timer.CurrentTime() >= pose.start &&
           timer.CurrentTime() < pose.end &&
           pose.PoseID >= 10)
        {
            poseCameraDirector.SetTestPlay(true);
            stop = false;
        }
        else if (!isPoseShown && timer.CurrentTime() >= pose.start && timer.CurrentTime() < pose.end)
        {
            uIPresenter.Show(uIData.getUI(pose.PoseID + wating));
            uIPresenter.Show(uIData.getUI(pose.PoseID + approaching));
            isPoseShown = true;

        }

        // 縮小
        if (stop && timer.CurrentTime() >= pose.start && timer.CurrentTime() < pose.end)
        {
            //イベント実行　当たり判定
            action?.Invoke(pose.PoseID);

            uIAnimation.ScaleDown(uIData.getUI(pose.PoseID + approaching));
        }


        //成功時
        if (angleJudge.GetisPose() && poseJudge.PoseJudge_Perfect(uIData.getUI(pose.PoseID + approaching), uIData.getUI(pose.PoseID + wating))
)
        {
            uIPresenter.Show(uIData.getUI(pose.PoseID + success));
            uIPresenter.Hide(uIData.getUI(pose.PoseID + wating));
            uIPresenter.Hide(uIData.getUI(pose.PoseID + approaching));
        }



        // 終了時間
        if (timer.CurrentTime() >= pose.end && poseFlow.HasNextPose())
        {
            uIPresenter.Hide(uIData.getUI(pose.PoseID + wating));
            uIPresenter.Hide(uIData.getUI(pose.PoseID + approaching));
            uIPresenter.Hide(uIData.getUI(pose.PoseID + success));
            uIAnimation.ScaleReset(uIData.getUI(pose.PoseID + approaching));
            poseFlow.NextPose();

            // 次のポーズ用にリセット
            isPoseShown = false;

            poseCameraDirector.SetTestPlay(false);
            stop = true;
        }
    }
}

*/