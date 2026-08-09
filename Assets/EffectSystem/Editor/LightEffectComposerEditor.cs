/*============================================================
*@file LightEffectComposerEditor.cs*
*@brief LightのInspectorから生成Effectをリアルタイム調整するEditor
*@author 24CU0312 久場洸太
*@date 2026/08/07
*============================================================*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LightEffectComposer))]
public sealed class LightEffectComposerEditor : Editor
{
    private const string ESavedEffectFolder = "Assets/EffectSystem/ComposableLights/SavedEffects";
    private readonly List<Editor> m_effectEditors = new List<Editor>();
    private Editor m_sourceLightEditor;
    private string m_saveName = "New_ComposableLight";
    private bool b_m_addLightControllerOnSave;
    private bool b_m_showSourceLightSettings = true;
    private string m_overwriteTargetPath;

    private void OnEnable()
    {
        LightEffectComposer composer = target as LightEffectComposer;
        if (composer == null)return;

        string sourcePath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(
            composer.gameObject);
        if (!string.IsNullOrEmpty(sourcePath) &&
            sourcePath.StartsWith(ESavedEffectFolder))
        {
            m_overwriteTargetPath = sourcePath;
            m_saveName = Path.GetFileNameWithoutExtension(sourcePath);
            b_m_addLightControllerOnSave =
                composer.GetComponent<LightController>() != null;
            return;
        }

        m_saveName = composer.gameObject.name;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LightEffectComposer composer = (LightEffectComposer)target;
        LightEffectBase[] effects = composer.GetGeneratedEffects();

        DrawSourceLightSettings(composer);

        EditorGUILayout.Space(10.0f);
        EditorGUILayout.LabelField("Realtime Effect Tuning", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Haloなどの値をここで変更すると、Scene ViewとGame Viewへ即時反映されます。",
            MessageType.Info);

        if (effects.Length == 0)
        {
            if (GUILayout.Button("Rebuild Attached Effects"))
            {
                composer.RebuildEffects();
                SceneView.RepaintAll();
            }
            return;
        }

        EnsureEditorCount(effects.Length);
        for (int effectIndex = 0; effectIndex < effects.Length; effectIndex++)
        {
            LightEffectBase effect = effects[effectIndex];
            if (effect == null)continue;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            bool effectEnabled = EditorGUILayout.ToggleLeft(
                effect.name,
                effect.gameObject.activeSelf,
                EditorStyles.boldLabel);
            if (effectEnabled != effect.gameObject.activeSelf)
            {
                effect.gameObject.SetActive(effectEnabled);
                SceneView.RepaintAll();
            }

            Editor effectEditor = m_effectEditors[effectIndex];
            CreateCachedEditor(effect, null, ref effectEditor);
            m_effectEditors[effectIndex] = effectEditor;
            EditorGUI.BeginChangeCheck();
            effectEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(effect);
                SceneView.RepaintAll();
            }

            if (PrefabUtility.IsPartOfPrefabInstance(effect.gameObject) &&
                GUILayout.Button("Apply To Effect Prefab"))
            {
                PrefabUtility.ApplyPrefabInstance(
                    effect.gameObject,
                    InteractionMode.UserAction);
            }
            EditorGUILayout.EndVertical();
        }

        DrawSaveControls(composer, effects);
    }

    private void OnDisable()
    {
        if (m_sourceLightEditor != null)
        {
            DestroyImmediate(m_sourceLightEditor);
            m_sourceLightEditor = null;
        }
        foreach (Editor effectEditor in m_effectEditors)
        {
            if (effectEditor != null)DestroyImmediate(effectEditor);
        }
        m_effectEditors.Clear();
    }

    /// <summary>生成Effectの親になっている実LightをComposer内から直接編集できるようにします。</summary>
    private void DrawSourceLightSettings(LightEffectComposer _composer)
    {
        EditorGUILayout.Space(8.0f);
        b_m_showSourceLightSettings = EditorGUILayout.Foldout(
            b_m_showSourceLightSettings,
            "Source Light Settings",
            true);
        if (!b_m_showSourceLightSettings)return;

        Light sourceLight = _composer.GetComponent<Light>();
        if (sourceLight == null)
        {
            EditorGUILayout.HelpBox("同じObjectにLightがありません。", MessageType.Error);
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            CreateCachedEditor(sourceLight, null, ref m_sourceLightEditor);
            EditorGUI.BeginChangeCheck();
            m_sourceLightEditor.OnInspectorGUI();
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(sourceLight);
                SceneView.RepaintAll();
            }
        }
    }

    private void EnsureEditorCount(int _requiredCount)
    {
        while (m_effectEditors.Count < _requiredCount)
        {
            m_effectEditors.Add(null);
        }
    }

    /// <summary>調整済みのLightと各Effectを独立したPrefab Setとして保存します。</summary>
    private void DrawSaveControls(
        LightEffectComposer _composer,
        LightEffectBase[] _effects)
    {
        EditorGUILayout.Space(10.0f);
        EditorGUILayout.LabelField("Save Composable Effect", EditorStyles.boldLabel);
        m_saveName = EditorGUILayout.TextField("Effect Name", m_saveName);
        b_m_addLightControllerOnSave = EditorGUILayout.Toggle(
            "Add Light Controller",
            b_m_addLightControllerOnSave);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Overwrite Save"))
        {
            SaveComposableEffect(_composer, _effects, false);
        }
        if (GUILayout.Button("Save As New"))
        {
            SaveComposableEffect(_composer, _effects, true);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Save Folder", ESavedEffectFolder);
        if (!string.IsNullOrEmpty(m_overwriteTargetPath))
        {
            EditorGUILayout.LabelField("Overwrite Target", m_overwriteTargetPath);
        }
    }

    /// <summary>
    /// 調整中InstanceからEffect部品Prefabを作り、それらを参照する親Light Prefabを保存します。
    /// 共通の元Effect Assetは変更しないため、別のLightへ調整値が波及しません。
    /// </summary>
    private void SaveComposableEffect(
        LightEffectComposer _composer,
        LightEffectBase[] _effects,
        bool _saveAsNew)
    {
        string safeName = SanitizeFileName(m_saveName);
        if (string.IsNullOrWhiteSpace(safeName))
        {
            EditorUtility.DisplayDialog("Save Failed", "Effect Nameを入力してください。", "OK");
            return;
        }

        EnsureSaveFolder();
        string parentPath = $"{ESavedEffectFolder}/{safeName}.prefab";
        if (_saveAsNew)
        {
            parentPath = AssetDatabase.GenerateUniqueAssetPath(parentPath);
            safeName = Path.GetFileNameWithoutExtension(parentPath);
        }
        else if (!string.IsNullOrEmpty(m_overwriteTargetPath))
        {
            parentPath = m_overwriteTargetPath;
            safeName = Path.GetFileNameWithoutExtension(parentPath);
        }
        else if (!File.Exists(parentPath))
        {
            EditorUtility.DisplayDialog(
                "Overwrite Save",
                "同名Prefabがないため、新規保存します。",
                "OK");
        }

        string partsFolder = $"{ESavedEffectFolder}/{safeName}_Parts";
        EnsureAssetFolder(partsFolder);
        List<LightEffectBase> savedParts = SaveEffectParts(_effects, partsFolder);

        GameObject lightCopy = Instantiate(_composer.gameObject);
        lightCopy.name = safeName;
        RemoveGeneratedRoots(lightCopy.transform);
        LightEffectComposer copiedComposer = lightCopy.GetComponent<LightEffectComposer>();
        LightController copiedLightController = lightCopy.GetComponent<LightController>();
        if (b_m_addLightControllerOnSave && copiedLightController == null)
        {
            lightCopy.AddComponent<LightController>();
        }
        else if (!b_m_addLightControllerOnSave && copiedLightController != null)
        {
            DestroyImmediate(copiedLightController);
        }
        AssignSavedParts(copiedComposer, savedParts);

        PrefabUtility.SaveAsPrefabAsset(lightCopy, parentPath);
        DestroyImmediate(lightCopy);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (_saveAsNew)
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(parentPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        else
        {
            Selection.activeGameObject = _composer.gameObject;
            m_overwriteTargetPath = parentPath;
        }
        Debug.Log($"Composable Effectを保存しました: {parentPath}");
    }

    private static List<LightEffectBase> SaveEffectParts(
        LightEffectBase[] _effects,
        string _partsFolder)
    {
        List<LightEffectBase> savedParts = new List<LightEffectBase>();
        for (int effectIndex = 0; effectIndex < _effects.Length; effectIndex++)
        {
            LightEffectBase sourceEffect = _effects[effectIndex];
            if (sourceEffect == null)continue;

            string partName = SanitizeFileName(sourceEffect.name);
            string partPath = $"{_partsFolder}/{effectIndex:00}_{partName}.prefab";
            GameObject partCopy = Instantiate(sourceEffect.gameObject);
            partCopy.name = partName;
            LightEffectBase copiedEffect = partCopy.GetComponent<LightEffectBase>();
            copiedEffect.AttachToLight(null);
            GameObject savedPart = PrefabUtility.SaveAsPrefabAsset(partCopy, partPath);
            DestroyImmediate(partCopy);
            savedParts.Add(savedPart.GetComponent<LightEffectBase>());
        }
        return savedParts;
    }

    private static void AssignSavedParts(
        LightEffectComposer _composer,
        List<LightEffectBase> _savedParts)
    {
        SerializedObject serializedComposer = new SerializedObject(_composer);
        SerializedProperty effectsProperty = serializedComposer.FindProperty("m_effects");
        int savedPartIndex = 0;
        for (int effectIndex = 0; effectIndex < effectsProperty.arraySize; effectIndex++)
        {
            SerializedProperty effectEntry = effectsProperty.GetArrayElementAtIndex(effectIndex);
            bool enabled = effectEntry.FindPropertyRelative("b_m_enabled").boolValue;
            Object sourcePrefab = effectEntry.FindPropertyRelative("m_effectPrefab").objectReferenceValue;
            if (!enabled || sourcePrefab == null)continue;
            if (savedPartIndex >= _savedParts.Count)break;
            effectEntry.FindPropertyRelative("m_effectPrefab").objectReferenceValue =
                _savedParts[savedPartIndex];
            savedPartIndex++;
        }
        serializedComposer.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveGeneratedRoots(Transform _parent)
    {
        for (int childIndex = _parent.childCount - 1; childIndex >= 0; childIndex--)
        {
            Transform child = _parent.GetChild(childIndex);
            if (child.name == "Attached Light Effects")
            {
                DestroyImmediate(child.gameObject);
            }
        }
    }

    private static void EnsureSaveFolder()
    {
        EnsureAssetFolder(ESavedEffectFolder);
    }

    private static void EnsureAssetFolder(string _folderPath)
    {
        string[] folderParts = _folderPath.Split('/');
        string currentPath = folderParts[0];
        for (int partIndex = 1; partIndex < folderParts.Length; partIndex++)
        {
            string nextPath = $"{currentPath}/{folderParts[partIndex]}";
            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folderParts[partIndex]);
            }
            currentPath = nextPath;
        }
    }

    private static string SanitizeFileName(string _name)
    {
        string safeName = _name == null ? string.Empty : _name.Trim();
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidCharacter, '_');
        }
        return safeName;
    }
}
