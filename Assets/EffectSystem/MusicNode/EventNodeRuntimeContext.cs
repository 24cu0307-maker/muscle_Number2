/*━━━━━━━━━*
*@file EventNodeRuntimeContext.cs*
*@brief Event Sceneへ専用Node設定を引き継ぐ*
*@author 24cu0312 久場洸太*
*@date 2026/08/02*
*最終更新日 2026/08/02*
*@remarks Scene切替後のEvent専用処理から参照する*
*━━━━━━━━━*/

using System.Collections.Generic;

/// <summary>
/// 選択されたEvent設定をScene切替後まで保持します。
/// </summary>
public static class EventNodeRuntimeContext
{
    public static MusicEventSceneData CurrentEvent { get; private set; }

    public static IReadOnlyList<SMusicNodeEvent> EventNodesList
    {
        get
        {
            if (CurrentEvent == null)return null;

            return CurrentEvent.m_eventNodesList;
        }
    }

    /// <summary>
    /// Event Sceneへ渡す設定を保存します。
    /// </summary>
    public static void Begin(MusicEventSceneData _eventData)
    {
        CurrentEvent = _eventData;
    }

    /// <summary>
    /// Event終了後に保持設定を消去します。
    /// </summary>
    public static void Clear()
    {
        CurrentEvent = null;
    }
}
