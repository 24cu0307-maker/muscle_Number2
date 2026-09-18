using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class RealtimeCameraSceneInstaller
{
    private const string EGameplayPath = "Assets/Scenes/GameFlow/Gameplay.unity";
    private const string EWorkScenePath = "Assets/Scenes/GameFlow/GameplayEffect_kuba.unity";
    private const string EMaterialPath = "Assets/_Scripts/RealtimeCameraSurface.mat";
    private const string EShaderName = "MuscleBeat/RealtimeCameraSurface";
    private const string EScreenName = "RealtimeCameraPreview";
    private const float EScreenWidth = 2.0f;
    private const float EPreviewAspect = 16.0f / 9.0f;
    private const float EPanelThickness = 0.1f;
    private const float ESurfaceFrontOffset = -0.051f;
    private const float EFramePadding = 0.1f;

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(EGameplayPath);
        int originalRootCount = scene.rootCount;
        Shader shader = Shader.Find(EShaderName);
        if (shader == null)
        {
            throw new InvalidOperationException("実映像用Shaderが見つかりません。");
        }
        Material material = AssetDatabase.LoadAssetAtPath<Material>(EMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, EMaterialPath);
        }
        GameObject monitor = new GameObject(EScreenName);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "MonitorBody";
        body.transform.SetParent(monitor.transform, false);
        body.transform.localScale = new Vector3(
            EScreenWidth + EFramePadding,
            EScreenWidth / EPreviewAspect + EFramePadding,
            EPanelThickness);
        UnityEngine.Object.DestroyImmediate(body.GetComponent<Collider>());
        Material bodyMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Scripts/RealtimeCameraMonitorBody.mat");
        if (bodyMaterial == null)
        {
            bodyMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            bodyMaterial.SetColor("_BaseColor", Color.black);
            AssetDatabase.CreateAsset(bodyMaterial, "Assets/_Scripts/RealtimeCameraMonitorBody.mat");
        }
        body.GetComponent<MeshRenderer>().sharedMaterial = bodyMaterial;
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "CameraSurface";
        screen.transform.SetParent(monitor.transform, false);
        screen.transform.localPosition = new Vector3(
            0.0f,
            0.0f,
            ESurfaceFrontOffset);
        UnityEngine.Object.DestroyImmediate(screen.GetComponent<Collider>());
        MeshRenderer renderer = screen.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        screen.transform.localScale = new Vector3(
            EScreenWidth,
            EScreenWidth / EPreviewAspect,
            1.0f);
        screen.AddComponent<RealtimeCameraSurface>();
        Camera camera = Camera.main;
        if (camera != null)
        {
            monitor.transform.position = camera.transform.position
                + camera.transform.rotation * new Vector3(1.5f, 0.35f, 4.0f);
            monitor.transform.rotation = camera.transform.rotation;
        }
        else
        {
            monitor.transform.position = new Vector3(0.0f, 2.0f, -3.0f);
        }
        if (scene.rootCount != originalRootCount + 1)
        {
            throw new InvalidOperationException("GameplayのRoot構成を保持できませんでした。");
        }
        if (!EditorSceneManager.SaveScene(scene, EWorkScenePath, true))
        {
            throw new InvalidOperationException("作業シーンの保存に失敗しました。");
        }
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene(EWorkScenePath);
        Debug.Log("[RealtimeCamera] Gameplayをコピーし、実映像表示Quadを追加しました。OriginalRoots=" + originalRootCount);
    }
}
