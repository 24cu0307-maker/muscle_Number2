//
//InGameの流れをここで行う
//
//
//準備　　　InGame入って一度飲み使用
//始め　　　UI、エフェクトのセット　　　
//実行中　　UIの縮小、判定
//終わり　　ポーズごとに終了時間がきたら、又は、成功-失敗
//成功　　　ポーズを成功した場合、その後終わりへ
//失敗　　　ポーズを失敗した場合、その後終わりへ
//



using GameFlowTemplate;
using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class InGameManager : MonoBehaviour
{

    [Header("gameManager")]
    [SerializeField] private GameManager m_gameManager;

    [Header("po")]
    [SerializeField] private PoseJudgeManager m_poseJudgeManager;

    [Header("UI")]
    [SerializeField] private UIManager m_uIManager;


    private InGameState m_inGameState = InGameState.Start;
    private float GameTimeSeconds;                  //現在のゲーム時間
    public float GetCurrentTIme() { return GameTimeSeconds; }

    public Action<InGameState> m_UIManagerAction;
    public Action<InGameState> m_PoseJudgeManagerAction;

    [Header("終了の時間")]
    [SerializeField] private float m_endtimer;

    /*
    [Header("UIの操作")]
    [SerializeField] private UIController m_uiController;

    [Header("ポーズの判定")]
    [SerializeField] private PoseJudgeController m_poseJudgeController;

    [Header("スコア判定")]
    [SerializeField] private ScoreController m_scoreController;

    [Header("UIの保存場所")]
    [SerializeField] private ExcelLoader m_excelLoader;

    [Header("エフェクトシステム")]
    [SerializeField] private EffectSystem m_effectSystem;

    [Header("カメラ")]
    [SerializeField] private PoseCameraDirector poseCameraDirector;

    [Header("イベントディレクター")]
    [SerializeField]
    private EventSceneVisualDirector m_eventSceneVisualDirector;

    [Header("熱量")]
    [SerializeField] private VenueVoltageSystem m_venueVoltageSystem;
    */


    //オブザーバー
    private void OnEnable()
    {
        m_poseJudgeManager.setState += SetState;
        m_uIManager.setState += SetState;


    }

    //オブザーバー
    private void OnDisable()
    {
        m_poseJudgeManager.setState -= SetState;
        m_uIManager.setState += SetState;

    }

    public void Start()
    {
        m_gameManager.StartGame();
    }

    private void Update()
    {
        //ゲームを終了する
        if (m_endtimer <= GameTimeSeconds) { m_gameManager.FinishGame(); }

        //現在のゲーム時間の更新
        UpdateTime();
        m_uIManager.UIManagerUpdate();
        m_poseJudgeManager.PoseJudgeManagerUpdate();

        Debug.Log("m_inGameState" + m_inGameState);
    }

    /// <summary>
    /// 現在のゲーム時間の更新
    /// </summary>
    private void UpdateTime()
    {
        GameTimeSeconds = m_gameManager.GetTimeManager().GameTimeSeconds;
    }

    public void SetState(InGameState _state)
    {
        m_inGameState = _state;
        m_UIManagerAction?.Invoke(_state);
        m_PoseJudgeManagerAction?.Invoke(_state);

    }
}
