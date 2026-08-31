using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 現行Gameplayを基に、MediaPipeを起動しないEffect確認Sceneを生成します。
/// </summary>
public static class EffectDebugSceneGenerator
{
    private const string SourceScene =
        "Assets/Scenes/GameFlow/Gameplay.unity";
    private const string DebugScene =
        "Assets/Scenes/GameFlow/Gameplay_EffectDebug.unity";

    public static void Generate()
    {
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(SourceScene))
        {
            throw new System.IO.FileNotFoundException(
                "Source scene was not found.", SourceScene);
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(DebugScene))
        {
            FileUtil.ReplaceFile(SourceScene, DebugScene);
        }
        else if (!AssetDatabase.CopyAsset(SourceScene, DebugScene))
        {
            throw new System.InvalidOperationException(
                "Failed to copy the effect debug scene.");
        }

        AssetDatabase.ImportAsset(
            DebugScene,
            ImportAssetOptions.ForceSynchronousImport
            | ImportAssetOptions.ForceUpdate);

        EditorSceneManager.OpenScene(DebugScene, OpenSceneMode.Single);
        DisableMediaPipeLoaders();
        EnableEffectTesters();
        RegisterInBuildSettings();
        EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        AssetDatabase.SaveAssets();
        Debug.Log(
            "Generated Gameplay_EffectDebug. MediaPipe is disabled and "
            + "LiveEffectQuickTester is enabled.");
    }

    private static void DisableMediaPipeLoaders()
    {
        ScenesLoad[] loaders = Object.FindObjectsByType<ScenesLoad>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);
        foreach (ScenesLoad loader in loaders)
        {
            loader.enabled = false;
            loader.gameObject.SetActive(false);
        }
    }

    private static void EnableEffectTesters()
    {
        LiveEffectQuickTester[] testers =
            Object.FindObjectsByType<LiveEffectQuickTester>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
        foreach (LiveEffectQuickTester tester in testers)
        {
            tester.gameObject.SetActive(true);
            tester.enabled = true;
        }

        if (testers.Length == 0)
        {
            Debug.LogWarning(
                "LiveEffectQuickTester was not found in the source scene.");
        }
    }

    private static void RegisterInBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(
            EditorBuildSettings.scenes);
        int existingIndex = scenes.FindIndex(
            scene => scene.path == DebugScene);
        if (existingIndex >= 0)
        {
            return;
        }

        scenes.Add(new EditorBuildSettingsScene(DebugScene, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
