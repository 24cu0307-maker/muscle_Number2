/*━━━━━━━━━
@file GameState.cs
@brief ゲーム全体の状態定義
@author 24CU0000 Name
@date 2026/07/10
最終更新日 2026/07/10
@remarks GameManagerが現在状態を管理するために使用
━━━━━━━━━*/

namespace GameFlowTemplate
{
    /// <summary>
    /// ゲーム全体が今どの状態にいるかを表す列挙型です。
    /// </summary>
    public enum GameState
    {
        Title,          //タイトルや開始待ち状態
        Ready,          //ゲーム開始前の準備状態
        Playing,        //インゲーム中
        DirectionPause, //演出などでゲーム時間を止めている状態
        Result          //ゲーム終了後の結果表示状態
    }
}
