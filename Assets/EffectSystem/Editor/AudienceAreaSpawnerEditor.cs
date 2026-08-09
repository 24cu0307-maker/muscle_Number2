using UnityEditor;
using UnityEngine;

/// <summary>観客の密集地点をSceneビューから簡単に作成・選択します。</summary>
[CustomEditor(typeof(AudienceAreaSpawner))]
public sealed class AudienceAreaSpawnerEditor : Editor
{
    private SerializedProperty m_densityFocusPoint;

    private void OnEnable()
    {
        m_densityFocusPoint = serializedObject.FindProperty("m_densityFocusPoint");
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Density Focus Pointをステージ前へ移動し、黄色いBoxで密集範囲を調整します。",
            MessageType.Info);

        if (m_densityFocusPoint.objectReferenceValue == null)
        {
            if (GUILayout.Button("Create Density Focus Point"))
            {
                CreateFocusPoint();
            }
        }
        else if (GUILayout.Button("Select Density Focus Point"))
        {
            Selection.activeObject = m_densityFocusPoint.objectReferenceValue;
        }
    }

    private void CreateFocusPoint()
    {
        AudienceAreaSpawner spawner = (AudienceAreaSpawner)target;
        GameObject focusObject = new GameObject("AudienceDensityFocus");
        Undo.RegisterCreatedObjectUndo(focusObject, "Create Audience Density Focus");
        focusObject.transform.SetParent(spawner.transform, false);

        serializedObject.Update();
        m_densityFocusPoint.objectReferenceValue = focusObject.transform;
        serializedObject.ApplyModifiedProperties();
        Selection.activeGameObject = focusObject;
    }
}
