using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class RealtimeCameraUIInstaller
{
    private const string EWorkScenePath = "Assets/Scenes/GameFlow/GameplayEffect_kuba.unity";
    private const string EMaterialPath = "Assets/_Scripts/RealtimeCameraUI.mat";
    private const string EPrefabPath = "Assets/EffectSystem/RealtimeCameraUIPreview.prefab";
    private const string ECanvasName = "RealtimeCameraUICanvas";
    private const int ECanvasOrder = 10;
    private static readonly Vector2 EReferenceResolution = new Vector2(1920.0f, 1080.0f);
    private static readonly Vector2 EWindowSize = new Vector2(384.0f, 216.0f);
    private static readonly Vector2 EWindowOffset = new Vector2(-32.0f, 32.0f);
    private const float EScaleMatch = 0.5f;

    public static void Run()
    {
        var scene = EditorSceneManager.OpenScene(EWorkScenePath);
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == ECanvasName)
            {
                Debug.Log("[RealtimeCameraUI] 配置済みのため既存UIを維持します。");
                return;
            }
        }
        Shader shader = Shader.Find("MuscleBeat/RealtimeCameraUI");
        if (shader == null)
        {
            throw new InvalidOperationException("UI用Shaderが見つかりません。");
        }
        Material material = AssetDatabase.LoadAssetAtPath<Material>(EMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, EMaterialPath);
        }
        GameObject canvasObject = new GameObject(
            ECanvasName,
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = ECanvasOrder;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = EReferenceResolution;
        scaler.matchWidthOrHeight = EScaleMatch;
        GameObject imageObject = new GameObject(
            "RealtimeCameraUI",
            typeof(RectTransform),
            typeof(RawImage));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RawImage image = imageObject.GetComponent<RawImage>();
        image.raycastTarget = false;
        image.material = material;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(1.0f, 0.0f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = rect.anchorMin;
        rect.sizeDelta = EWindowSize;
        rect.anchoredPosition = EWindowOffset;
        imageObject.AddComponent<RealtimeCameraUI>();
        PrefabUtility.SaveAsPrefabAsset(canvasObject, EPrefabPath);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("[RealtimeCameraUI] 作業シーンの右下へUIを追加し、Prefabを保存しました。既存3Dモニターは維持。");
    }
}
