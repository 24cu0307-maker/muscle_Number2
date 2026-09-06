#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DroneSplineRoute))]
public sealed class DroneSplineRouteEditor : Editor
{
    private SerializedProperty m_pointsProperty;

    private void OnEnable()
    {
        m_pointsProperty = serializedObject.FindProperty("m_points");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox(
            "このDrone専用の経路です。水色のPointをSceneビューで移動できます。",
            MessageType.Info);
    }

    private void OnSceneGUI()
    {
        DroneSplineRoute route = (DroneSplineRoute)target;
        if (m_pointsProperty == null || route.transform.parent == null)return;

        serializedObject.Update();
        Transform routeSpace = route.transform.parent;
        for (int i = 0; i < m_pointsProperty.arraySize; ++i)
        {
            SerializedProperty point = m_pointsProperty.GetArrayElementAtIndex(i);
            Vector3 worldPosition = routeSpace.TransformPoint(point.vector3Value);
            Handles.color = Color.cyan;
            Handles.Label(worldPosition + Vector3.up * 0.35f, $"Point {i + 1}");
            EditorGUI.BeginChangeCheck();
            Vector3 updated = Handles.PositionHandle(worldPosition, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(route, "Move Drone Route Point");
                point.vector3Value = routeSpace.InverseTransformPoint(updated);
            }
        }
        serializedObject.ApplyModifiedProperties();

        if (!route.ShowSpline)return;
        Handles.color = Color.cyan;
        int segmentCount = route.GetSegmentCount();
        for (int i = 0; i < segmentCount; ++i)
        {
            Vector3 previous = routeSpace.TransformPoint(route.EvaluateSegment(i, 0.0f));
            for (int j = 1; j <= 24; ++j)
            {
                Vector3 current = routeSpace.TransformPoint(
                    route.EvaluateSegment(i, j / 24.0f));
                Handles.DrawLine(previous, current, 2.0f);
                previous = current;
            }
        }
    }
}
#endif
