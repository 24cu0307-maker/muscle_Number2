/*━━━━━━━━━*
*@file LightControllerEditor.cs*
*@brief LightControllerのInspectorを整理して表示する*
*@author 24cu0312 久場洸太*
*@date 2026/07/28*
*最終更新日 2026/07/28*
*@remarks Editor専用*
*━━━━━━━━━*/

using UnityEditor;
using UnityEngine;

/// <summary>
/// LightControllerの設定を用途別の折りたたみ項目で表示します。
/// </summary>
[CustomEditor(typeof(LightController))]
public sealed class LightControllerEditor : Editor
{
    private bool b_m_showRotation;       //回転設定を表示するか
    private bool b_m_showPosition;       //移動設定を表示するか
    private bool b_m_showColor;          //色設定を表示するか
    private bool b_m_showBlink;          //点滅設定を表示するか

    /// <summary>
    /// 整理されたInspectorを描画します。
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "基本設定だけで点灯できます。動きが必要な項目だけ展開してください。"
            + "\n複雑なタイミング調整にはTimelineのAnimation/Signal Trackを使用します。",
            MessageType.Info);

        DrawProperty("m_light");
        DrawProperty("m_stopTime");

        b_m_showRotation = DrawSection(
            "Rotation（必要な場合のみ）",
            b_m_showRotation,
            "m_rotationRange",
            "m_rotationTime",
            "m_rotationMode",
            "m_rotationPingPongCount",
            "m_bUseLocalRotation",
            "m_bReturnStartRotationOnStop");

        b_m_showPosition = DrawSection(
            "Position（必要な場合のみ）",
            b_m_showPosition,
            "m_positionRange",
            "m_positionTime",
            "m_positionMode",
            "m_positionPingPongCount",
            "m_bUseLocalPosition",
            "m_bReturnStartPositionOnStop");

        b_m_showColor = DrawSection(
            "Color（必要な場合のみ）",
            b_m_showColor,
            "m_colors",
            "m_bReturnStartColorOnStop");

        b_m_showBlink = DrawSection(
            "Blink（必要な場合のみ）",
            b_m_showBlink,
            "m_bUseBlink",
            "m_blinkCount",
            "m_blinkTime");

        serializedObject.ApplyModifiedProperties();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Test Illumination (Play Mode)"))
            {
                LightController lightController = (LightController)target; //テスト対象
                lightController.Illumination();
            }
        }
    }

    /// <summary>
    /// 折りたたみ可能な設定グループを描画します。
    /// </summary>
    private bool DrawSection(
        string _label,
        bool _expanded,
        params string[] _propertynames)
    {
        _expanded = EditorGUILayout.Foldout(_expanded, _label, true);
        if (!_expanded)return false;

        using (new EditorGUI.IndentLevelScope())
        {
            foreach (string propertyName in _propertynames)
            {
                DrawProperty(propertyName);
            }
        }

        return true;
    }

    /// <summary>
    /// 指定されたSerializedPropertyを描画します。
    /// </summary>
    private void DrawProperty(string _propertyname)
    {
        SerializedProperty property = serializedObject.FindProperty(_propertyname); //描画対象
        if (property == null)return;

        EditorGUILayout.PropertyField(property, true);
    }
}
