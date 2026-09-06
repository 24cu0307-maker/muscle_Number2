using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PoseJudgementFeedbackPlayer))]
public sealed class PoseJudgementFeedbackPlayerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Play Mode中に各ボタンを押すと、設定した音とParticleを確認できます。",
            MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            PoseJudgementFeedbackPlayer player =
                target as PoseJudgementFeedbackPlayer;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Perfect"))player?.Play(EPoseMatchGrade.Perfect);
            if (GUILayout.Button("Test Great"))player?.Play(EPoseMatchGrade.Great);
            if (GUILayout.Button("Test Miss"))player?.Play(EPoseMatchGrade.Miss);
            EditorGUILayout.EndHorizontal();
        }
    }
}

[CustomEditor(typeof(AudiencePreferenceSystem))]
public sealed class AudiencePreferenceSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "Positive/Disappointed Commentsへ文を追加します。Comment Displaysへ表示窓を登録し、Minimum/Maximum Comment Windowsで一度にランダム表示する窓数を指定します。",
            MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            AudiencePreferenceSystem preferenceSystem =
                target as AudiencePreferenceSystem;
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Positive Comments"))
            {
                preferenceSystem?.PreviewRandomComments(true);
            }
            if (GUILayout.Button("Test Disappointed Comments"))
            {
                preferenceSystem?.PreviewRandomComments(false);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
