/*━━━━━━━━━*
*@file VoltageBgmSetup.cs*
*@brief 追加BGMをVoltage連携とMusic Node編集へ設定する*
*@author 24CU0000 Name*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Gameplay_EffectWorkへ一度だけ設定*
*━━━━━━━━━*/

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// BGM Import設定、Voltage BGM、Music Node Sequenceを準備します。
/// </summary>
public static class VoltageBgmSetup
{
    private const string ETargetSceneName = "Gameplay_EffectWork"; //設定対象Scene
    private const string EBgmPath = "Assets/sound/Test/Numb.wav"; //設定対象BGM
    private const string ESequenceFolder =
        "Assets/EffectSystem/MusicNode/Sequences"; //Sequence保存先
    private const string ESequencePath =
        ESequenceFolder + "/NumbMusicNodeSequence.asset"; //Sequence保存先
    private const string EBgmObjectName = "VoltageBGM"; //BGM Object名
    private const float ELayerStartVoltage = 0.0f; //Layer開始Voltage
    private const float ELayerFadeWidth = 0.05f; //Layer Fade幅
    private const float ELayerMaximumVolume = 1.0f; //Layer最大音量

    /// <summary>
    /// BGM Import、Sequence、Scene Objectを設定します。
    /// </summary>
    private static void Setup()
    {
        AudioClip bgmClip =
            AssetDatabase.LoadAssetAtPath<AudioClip>(EBgmPath); //対象BGM
        if (bgmClip == null)return;

        ConfigureAudioImporter();
        CreateOrUpdateSequence(bgmClip);

        Scene scene = SceneManager.GetActiveScene(); //現在Scene
        if (!scene.IsValid() || scene.name != ETargetSceneName)return;
        GameObject existingBgmObject = GameObject.Find(EBgmObjectName); //既存BGM
        if (existingBgmObject != null)
        {
            return;
        }

        GameObject bgmObject = new GameObject(EBgmObjectName); //Voltage BGM Object
        VoltageBgmSystem bgmSystem =
            bgmObject.AddComponent<VoltageBgmSystem>(); //BGM制御
        SerializedObject serializedSystem =
            new SerializedObject(bgmSystem); //BGM設定
        SerializedProperty layers =
            serializedSystem.FindProperty("m_layers"); //BGM Layer一覧
        layers.arraySize = 1;
        SerializedProperty layer = layers.GetArrayElementAtIndex(0); //基本Layer
        layer.FindPropertyRelative("m_clip").objectReferenceValue = bgmClip;
        layer.FindPropertyRelative("m_startVoltage").floatValue =
            ELayerStartVoltage;
        layer.FindPropertyRelative("m_fadeWidth").floatValue =
            ELayerFadeWidth;
        layer.FindPropertyRelative("m_maximumVolume").floatValue =
            ELayerMaximumVolume;
        serializedSystem.FindProperty("b_m_playOnStart").boolValue = true;
        serializedSystem.FindProperty(
            "b_m_playAfterOpeningCamera").boolValue = true;
        serializedSystem.ApplyModifiedPropertiesWithoutUndo();
        Undo.RegisterCreatedObjectUndo(bgmObject, "Setup Voltage BGM");
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = bgmObject;
        Debug.Log(
            "VoltageBGMを配置しました。Sceneを保存してください。");
    }

    /// <summary>
    /// 大容量BGMをStreaming読込へ設定します。
    /// </summary>
    private static void ConfigureAudioImporter()
    {
        AudioImporter importer =
            AssetImporter.GetAtPath(EBgmPath) as AudioImporter; //BGM Importer
        if (importer == null)return;

        AudioImporterSampleSettings settings =
            importer.defaultSampleSettings; //現在のSample設定
        settings.loadType = AudioClipLoadType.Streaming;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = 0.7f;
        importer.defaultSampleSettings = settings;
        importer.loadInBackground = true;
        importer.SaveAndReimport();
    }

    /// <summary>
    /// Music Node Editor用Sequenceを作成または更新します。
    /// </summary>
    private static void CreateOrUpdateSequence(AudioClip _bgmclip)
    {
        if (!AssetDatabase.IsValidFolder(ESequenceFolder))
        {
            AssetDatabase.CreateFolder(
                "Assets/EffectSystem/MusicNode",
                "Sequences");
        }

        MusicNodeSequence sequence =
            AssetDatabase.LoadAssetAtPath<MusicNodeSequence>(
                ESequencePath); //対象Sequence
        if (sequence == null)
        {
            sequence = ScriptableObject.CreateInstance<MusicNodeSequence>();
            AssetDatabase.CreateAsset(sequence, ESequencePath);
        }

        sequence.BgmClip = _bgmclip;
        EditorUtility.SetDirty(sequence);
        AssetDatabase.SaveAssets();
    }
}
