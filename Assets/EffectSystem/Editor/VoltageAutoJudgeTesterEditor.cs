/*━━━━━━━━━*
*@file VoltageAutoJudgeTesterEditor.cs*
*@brief Voltage自動判定Testerの配置と操作UIを提供する*
*@author 24cu0312 久場洸太*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Editor専用Debug補助*
*━━━━━━━━━*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// VoltageAutoJudgeTesterをInspectorから手動確認できるようにします。
/// </summary>
[CustomEditor(typeof(VoltageAutoJudgeTester))]
public sealed class VoltageAutoJudgeTesterEditor : Editor
{
    private const int EMenuPriority = 154; //Menu表示順

    /// <summary>
    /// 通常設定とPlay Mode用の手動判定Buttonを表示します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUI.BeginDisabledGroup(!Application.isPlaying);
        VoltageAutoJudgeTester tester =
            target as VoltageAutoJudgeTester; //操作対象
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Success"))
        {
            tester.RunSuccess();
        }

        if (GUILayout.Button("Failure"))
        {
            tester.RunFailure();
        }

        if (GUILayout.Button("Random"))
        {
            tester.RunSingleJudge();
        }

        GUILayout.EndHorizontal();
        if (GUILayout.Button("Reset Results"))
        {
            tester.ResetResults();
        }

        EditorGUI.EndDisabledGroup();
    }

    /// <summary>
    /// 現在SceneのVenueVoltageSystemへTesterを追加します。
    /// </summary>
    private static void AddTester()
    {
        VenueVoltageSystem voltageSystem =
            Object.FindFirstObjectByType<VenueVoltageSystem>(); //追加先
        if (voltageSystem == null)
        {
            Debug.LogWarning("VenueVoltageSystemがSceneに見つかりません。");
            return;
        }

        VoltageAutoJudgeTester tester =
            voltageSystem.GetComponent<VoltageAutoJudgeTester>(); //既存Tester
        if (tester == null)
        {
            tester = Undo.AddComponent<VoltageAutoJudgeTester>(
                voltageSystem.gameObject);
            EditorSceneManager.MarkSceneDirty(voltageSystem.gameObject.scene);
        }

        Selection.activeGameObject = voltageSystem.gameObject;
        EditorGUIUtility.PingObject(tester);
    }
}
