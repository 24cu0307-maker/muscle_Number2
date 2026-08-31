/*============================================================
*@file LightingPatternPresetReferenceSetup.cs*
*@brief Lighting Pattern完成Presetの参照をUnity APIで安全に設定する*
*@author 24CU0312 久場洸太*
*@date 2026/08/07*
*@remarks Prefab内部IDを手書きせずAssetDatabaseから正しいRoot GameObjectを取得する*
*============================================================*/

using UnityEditor;
using UnityEngine;

/// <summary>
/// Lighting Patternの基底Prefabへ、Spotlight・Backlight・Laserの正しいAsset参照を設定します。
/// Unity起動・Script再読込時に参照切れだけを修復し、既存の調整値は変更しません。
/// </summary>
[InitializeOnLoad]
public static class LightingPatternPresetReferenceSetup
{
    private const string ERigPrefabPath =
        "Assets/EffectSystem/LightingPatterns/Core/LightingPatternRig.prefab";
    private const string ESpotlightPrefabPath =
        "Assets/EffectSystem/SpotlightCone/Variants/Spotlight_01_WarmGold_Wide.prefab";
    private const string EBacklightPrefabPath =
        "Assets/EffectSystem/SpotlightCone/Variants/Spotlight_21_WarmGold_Backlight.prefab";
    private const string ELaserPrefabPath =
        "Assets/EffectSystem/LaserBeam/Variants/Laser_05_Yellow_Thin.prefab";

    /// <summary>Domain Reload直後はAsset Import完了を待ってから参照を検証します。</summary>
    static LightingPatternPresetReferenceSetup()
    {
        EditorApplication.delayCall += RepairReferencesIfNeeded;
    }

    /// <summary>必要に応じて手動でも全参照を再設定できます。</summary>
    public static void RepairReferencesIfNeeded()
    {
        GameObject rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ERigPrefabPath);
        GameObject spotlightPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(ESpotlightPrefabPath);
        GameObject backlightPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(EBacklightPrefabPath);
        GameObject laserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ELaserPrefabPath);

        if (rigPrefab == null || spotlightPrefab == null ||
            backlightPrefab == null || laserPrefab == null)
        {
            Debug.LogWarning(
                "Lighting Patternの参照元Prefabが見つからないため、自動設定を延期しました。");
            return;
        }

        LiveLightingPatternRig rig = rigPrefab.GetComponent<LiveLightingPatternRig>();
        if (rig == null)
        {
            Debug.LogError("LightingPatternRig.prefabにLiveLightingPatternRigがありません。");
            return;
        }

        SerializedObject serializedRig = new SerializedObject(rig);
        bool changed = false;
        changed |= SetReference(
            serializedRig.FindProperty("m_spotlightPrefab"),
            spotlightPrefab);
        changed |= SetReference(
            serializedRig.FindProperty("m_backlightPrefab"),
            backlightPrefab);
        changed |= SetReference(
            serializedRig.FindProperty("m_laserPrefab"),
            laserPrefab);

        if (!changed)return;

        serializedRig.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(rigPrefab);
        AssetDatabase.SaveAssetIfDirty(rigPrefab);
        Debug.Log("Lighting Pattern完成PresetのPrefab参照を自動修復しました。", rigPrefab);
    }

    /// <summary>SerializedPropertyが異なる場合だけPrefab参照を更新します。</summary>
    private static bool SetReference(
        SerializedProperty _property,
        GameObject _reference)
    {
        if (_property == null || _property.objectReferenceValue == _reference)return false;
        _property.objectReferenceValue = _reference;
        return true;
    }
}
