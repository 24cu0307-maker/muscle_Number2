/*============================================================
*@file ComposableLightTestSceneBuilder.cs*
*@brief モジュール式Lightを確認する専用Test Sceneを生成する*
*@author 24CU0312 久場洸太*
*@date 2026/08/07*
*@remarks 作業中Sceneを変更せず、Additive Sceneとして生成・保存する*
*============================================================*/

using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spot Light、Cone、Halo、影の見え方を確認しやすいPrimitive配置のTest Sceneを作成します。
/// </summary>
[InitializeOnLoad]
public static class ComposableLightTestSceneBuilder
{
    private const string ESceneFolder =
        "Assets/EffectSystem/ComposableLights/TestScene";
    private const string EScenePath =
        ESceneFolder + "/ComposableLightTest.unity";
    private const string EPresetPath =
        "Assets/EffectSystem/ComposableLights/Presets/StageLight_Spotlight_Halo.prefab";
    private const string EDarkMaterialPath =
        ESceneFolder + "/TestDark.mat";
    private const string EWarmMaterialPath =
        ESceneFolder + "/TestWarm.mat";
    private const string ECoolMaterialPath =
        ESceneFolder + "/TestCool.mat";

    /// <summary>Sceneがまだ存在しない場合だけ自動生成します。</summary>
    static ComposableLightTestSceneBuilder()
    {
        EditorApplication.delayCall += CreateSceneIfMissing;
        EditorApplication.delayCall += FocusTestSceneIfOpen;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    /// <summary>Test Sceneを開いた直後に確認対象へScene Viewを移動します。</summary>
    private static void OnSceneOpened(
        Scene _scene,
        OpenSceneMode _mode)
    {
        if (_scene.path != EScenePath)return;
        EditorApplication.delayCall += FocusTestSceneIfOpen;
    }

    /// <summary>Tools MenuからTest対象へScene Viewを戻せます。</summary>
    [MenuItem("Tools/Effect System/Focus Composable Light Test Objects")]
    public static void FocusTestSceneIfOpen()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.IsValid() || activeScene.path != EScenePath)return;

        GameObject focusTarget = FindSceneObject(activeScene, "Main Performer");
        if (focusTarget == null)
        {
            focusTarget = FindSceneObject(activeScene, "Environment");
        }
        if (focusTarget == null)return;

        Selection.activeGameObject = focusTarget;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
            SceneView.lastActiveSceneView.Repaint();
        }
    }

    /// <summary>既存Sceneを上書きせず、未作成時だけ生成します。</summary>
    private static void CreateSceneIfMissing()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)return;
        if (File.Exists(EScenePath))return;
        CreateTestScene(false);
    }

    /// <summary>Tools Menuから確認後にTest Sceneを作り直せます。</summary>
    [MenuItem("Tools/Effect System/Rebuild Composable Light Test Scene")]
    public static void RebuildTestScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Play Modeを終了してからTest Sceneを再生成してください。");
            return;
        }

        bool approved = EditorUtility.DisplayDialog(
            "Composable Light Test Scene",
            "既存のTest Sceneを標準配置で上書きします。よろしいですか？",
            "再生成",
            "キャンセル");
        if (!approved)return;
        CreateTestScene(true);
    }

    /// <summary>Primitive、Camera、Light Presetを配置してScene Assetを保存します。</summary>
    private static void CreateTestScene(bool _overwrite)
    {
        EnsureFolder();
        if (!_overwrite && File.Exists(EScenePath))return;

        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene testScene = EditorSceneManager.NewScene(
            NewSceneSetup.EmptyScene,
            NewSceneMode.Additive);
        SceneManager.SetActiveScene(testScene);

        Material darkMaterial = GetOrCreateMaterial(
            EDarkMaterialPath,
            new Color(0.055f, 0.065f, 0.085f, 1.0f),
            0.15f,
            0.65f);
        Material warmMaterial = GetOrCreateMaterial(
            EWarmMaterialPath,
            new Color(0.42f, 0.16f, 0.06f, 1.0f),
            0.3f,
            0.5f);
        Material coolMaterial = GetOrCreateMaterial(
            ECoolMaterialPath,
            new Color(0.06f, 0.18f, 0.38f, 1.0f),
            0.7f,
            0.35f);

        GameObject environmentRoot = new GameObject("Environment");
        CreatePrimitive("Floor", PrimitiveType.Cube,
            new Vector3(0.0f, -0.25f, 3.5f),
            new Vector3(16.0f, 0.5f, 18.0f),
            darkMaterial,
            environmentRoot.transform);
        CreatePrimitive("Back Wall", PrimitiveType.Cube,
            new Vector3(0.0f, 4.0f, 10.0f),
            new Vector3(16.0f, 8.0f, 0.5f),
            darkMaterial,
            environmentRoot.transform);
        CreatePrimitive("Left Wall", PrimitiveType.Cube,
            new Vector3(-8.0f, 3.0f, 3.5f),
            new Vector3(0.35f, 6.0f, 18.0f),
            darkMaterial,
            environmentRoot.transform);
        CreatePrimitive("Right Wall", PrimitiveType.Cube,
            new Vector3(8.0f, 3.0f, 3.5f),
            new Vector3(0.35f, 6.0f, 18.0f),
            darkMaterial,
            environmentRoot.transform);

        GameObject subjectsRoot = new GameObject("Primitive Subjects");
        CreatePrimitive("Main Performer", PrimitiveType.Capsule,
            new Vector3(0.0f, 1.0f, 3.0f),
            new Vector3(1.0f, 1.0f, 1.0f),
            warmMaterial,
            subjectsRoot.transform);
        CreatePrimitive("Left Cube", PrimitiveType.Cube,
            new Vector3(-3.1f, 0.75f, 4.5f),
            new Vector3(1.5f, 1.5f, 1.5f),
            coolMaterial,
            subjectsRoot.transform);
        CreatePrimitive("Right Sphere", PrimitiveType.Sphere,
            new Vector3(3.0f, 1.0f, 4.2f),
            new Vector3(2.0f, 2.0f, 2.0f),
            warmMaterial,
            subjectsRoot.transform);
        CreatePrimitive("Rear Cylinder", PrimitiveType.Cylinder,
            new Vector3(-1.9f, 1.25f, 7.2f),
            new Vector3(1.2f, 1.25f, 1.2f),
            coolMaterial,
            subjectsRoot.transform);
        CreatePrimitive("Shadow Pillar", PrimitiveType.Cube,
            new Vector3(2.2f, 2.0f, 7.4f),
            new Vector3(1.0f, 4.0f, 1.0f),
            darkMaterial,
            subjectsRoot.transform);

        CreateCamera();
        CreateLightPreset();
        CreateGuideText();

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.005f, 0.005f, 0.008f, 1.0f);
        RenderSettings.skybox = null;
        RenderSettings.reflectionIntensity = 0.0f;

        EditorSceneManager.SaveScene(testScene, EScenePath);
        EditorSceneManager.CloseScene(testScene, true);
        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousActiveScene);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Composable Light Test Sceneを生成しました: {EScenePath}");
    }

    /// <summary>指定TransformとMaterialで確認用Primitiveを生成します。</summary>
    private static GameObject CreatePrimitive(
        string _name,
        PrimitiveType _type,
        Vector3 _position,
        Vector3 _scale,
        Material _material,
        Transform _parent)
    {
        GameObject primitive = GameObject.CreatePrimitive(_type);
        primitive.name = _name;
        primitive.transform.SetParent(_parent);
        primitive.transform.position = _position;
        primitive.transform.localScale = _scale;
        Renderer renderer = primitive.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = _material;
        }
        return primitive;
    }

    /// <summary>Test対象を正面から見渡すMain Cameraを配置します。</summary>
    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera", typeof(Camera));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0.0f, 3.0f, -9.5f);
        cameraObject.transform.rotation = Quaternion.LookRotation(
            new Vector3(0.0f, 1.4f, 3.5f) - cameraObject.transform.position,
            Vector3.up);
        Camera camera = cameraObject.GetComponent<Camera>();
        camera.fieldOfView = 50.0f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.004f, 0.005f, 0.009f, 1.0f);
    }

    /// <summary>Spotlight＋Halo Presetを上方前方へ配置して中央Capsuleへ向けます。</summary>
    private static void CreateLightPreset()
    {
        GameObject preset = AssetDatabase.LoadAssetAtPath<GameObject>(EPresetPath);
        if (preset == null)
        {
            Debug.LogError($"Test対象のLight Prefabが見つかりません: {EPresetPath}");
            return;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(preset) as GameObject;
        if (instance == null)return;
        instance.name = "TEST StageLight Spotlight + Halo";
        instance.transform.position = new Vector3(0.0f, 6.5f, -1.5f);
        instance.transform.rotation = Quaternion.LookRotation(
            new Vector3(0.0f, 1.2f, 3.0f) - instance.transform.position,
            Vector3.up);
    }

    /// <summary>Hierarchy上で配置の目的が分かる案内Objectを追加します。</summary>
    private static void CreateGuideText()
    {
        GameObject guide = new GameObject(
            "README - Select TEST StageLight and edit Light / Effects");
        guide.transform.position = Vector3.zero;
    }

    /// <summary>URP Lit Materialを取得し、未作成時だけAssetとして生成します。</summary>
    private static Material GetOrCreateMaterial(
        string _path,
        Color _color,
        float _metallic,
        float _smoothness)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(_path);
        if (material != null)return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        material = new Material(shader);
        material.name = Path.GetFileNameWithoutExtension(_path);
        material.color = _color;
        material.SetFloat("_Metallic", _metallic);
        material.SetFloat("_Smoothness", _smoothness);
        AssetDatabase.CreateAsset(material, _path);
        return material;
    }

    /// <summary>Test Scene保存先Folderが存在することを保証します。</summary>
    private static void EnsureFolder()
    {
        if (AssetDatabase.IsValidFolder(ESceneFolder))return;
        AssetDatabase.CreateFolder(
            "Assets/EffectSystem/ComposableLights",
            "TestScene");
    }

    /// <summary>指定SceneのHierarchyから名前が一致するObjectを検索します。</summary>
    private static GameObject FindSceneObject(Scene _scene, string _objectName)
    {
        foreach (GameObject rootObject in _scene.GetRootGameObjects())
        {
            if (rootObject.name == _objectName)return rootObject;

            Transform[] children = rootObject.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == _objectName)return child.gameObject;
            }
        }
        return null;
    }
}
