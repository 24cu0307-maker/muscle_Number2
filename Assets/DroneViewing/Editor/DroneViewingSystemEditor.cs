#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// DroneのSpline経路点をSceneビュー上で編集します。
/// </summary>
[CustomEditor(typeof(DroneViewingSystem))]
public sealed class DroneViewingSystemEditor : Editor
{
    private SerializedProperty m_splinePointsProperty;

    private void OnEnable()
    {
        m_splinePointsProperty = serializedObject.FindProperty("m_splinePoints");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox(
            "Sceneビューの水色ハンドルでSpline経路点を移動できます。"
            + " Spline Speedが移動速度、Loop Splineが周回設定です。",
            MessageType.Info);
    }

    private void OnSceneGUI()
    {
        if (m_splinePointsProperty == null)return;

        serializedObject.Update();
        DroneViewingSystem droneSystem = (DroneViewingSystem)target;
        Transform systemTransform = droneSystem.transform;
        for (int i = 0; i < m_splinePointsProperty.arraySize; ++i)
        {
            SerializedProperty pointProperty =
                m_splinePointsProperty.GetArrayElementAtIndex(i);
            Vector3 worldPosition = systemTransform.TransformPoint(
                pointProperty.vector3Value);
            Handles.color = Color.cyan;
            Handles.Label(worldPosition + Vector3.up * 0.4f, $"Point {i + 1}");
            EditorGUI.BeginChangeCheck();
            Vector3 updatedPosition = Handles.PositionHandle(
                worldPosition,
                Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(droneSystem, "Move Drone Spline Point");
                pointProperty.vector3Value =
                    systemTransform.InverseTransformPoint(updatedPosition);
            }
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
