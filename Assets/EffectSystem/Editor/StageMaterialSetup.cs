/*━━━━━━━━━*
*@file StageMaterialSetup.cs*
*@brief ステージFBXの内蔵MaterialをURP用外部Materialへ変換する*
*@author 24CU0000 Name*
*@date 2026/07/29*
*最終更新日 2026/07/29*
*@remarks Editor専用*
*━━━━━━━━━*/

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// ステージFBXのMaterialを外部化し、青みを抑えたURP Materialへ置換します。
/// </summary>
public static class StageMaterialSetup
{
    private const string EStageModelPath = "Assets/FBX/stage_0728_2.fbx"; //対象FBX
    private const string EStageFolderPath = "Assets/Stage"; //ステージAsset格納先
    private const string EMaterialFolderPath = "Assets/Stage/Materials"; //外部Material格納先
    private const string EUrpLitShaderName = "Universal Render Pipeline/Lit"; //URP用Shader
    private const string EStandardShaderName = "Standard"; //Shader取得失敗時の予備
    private const string EMenuPath = "Tools/Effect System/Fix Stage Materials"; //手動実行Menu
    private const float EDefaultMetallic = 0.08f; //青い環境反射を抑える金属度
    private const float EDefaultSmoothness = 0.28f; //青い環境反射を抑える光沢
    private const float EMinimumNeutralBrightness = 0.45f; //無Texture Materialの最低明度
    private const int EOpaqueSurfaceType = 0; //URP LitのOpaque設定
    private const int EEmptyMaterialCount = 0; //Materialが存在しない状態

    /// <summary>
    /// Inspector MenuからステージMaterialを再作成します。
    /// </summary>
    private static void ApplyFromMenu()
    {
        Apply(true);
    }

    /// <summary>
    /// 内蔵Materialごとに外部URP Materialを作成してRemapします。
    /// </summary>
    private static void Apply(bool _bforce)
    {
        ModelImporter importer =
            AssetImporter.GetAtPath(EStageModelPath) as ModelImporter; //対象Importer
        if (importer == null)return;

        Material[] sourceMaterials = LoadSourceMaterials(); //FBX内蔵Material一覧
        if (sourceMaterials.Length == EEmptyMaterialCount)return;

        Dictionary<AssetImporter.SourceAssetIdentifier, Object> remaps =
            importer.GetExternalObjectMap(); //現在のMaterial Remap
        if (!_bforce && remaps.Count >= sourceMaterials.Length)return;

        EnsureFolders();
        Shader shader = Shader.Find(EUrpLitShaderName); //外部Material用Shader
        if (shader == null)
        {
            shader = Shader.Find(EStandardShaderName);
        }

        if (shader == null)
        {
            Debug.LogError("ステージMaterialに使用できるShaderが見つかりません。");
            return;
        }

        for (int i = 0; i < sourceMaterials.Length; ++i)
        {
            Material sourceMaterial = sourceMaterials[i]; //FBX内蔵Material
            Material externalMaterial =
                CreateOrUpdateMaterial(sourceMaterial, shader); //外部Material
            AssetImporter.SourceAssetIdentifier sourceIdentifier =
                new AssetImporter.SourceAssetIdentifier(sourceMaterial); //Remap元
            importer.AddRemap(sourceIdentifier, externalMaterial);
        }

        importer.importAnimation = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importBlendShapes = false;
        importer.SaveAndReimport();
        AssetDatabase.SaveAssets();
        Debug.Log(
            $"ステージMaterialを{sourceMaterials.Length}件URP用に変換しました。"
            + $" 保存先: {EMaterialFolderPath}");
    }

    /// <summary>
    /// FBX内に含まれるMaterialだけを取得します。
    /// </summary>
    private static Material[] LoadSourceMaterials()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(EStageModelPath); //FBX SubAsset
        List<Material> materials = new List<Material>(); //取得したMaterial一覧
        for (int i = 0; i < assets.Length; ++i)
        {
            Material material = assets[i] as Material; //現在のSubAsset
            if (material == null)continue;

            materials.Add(material);
        }

        return materials.ToArray();
    }

    /// <summary>
    /// 外部Materialを作成または更新し、中立色と控えめな反射へ設定します。
    /// </summary>
    private static Material CreateOrUpdateMaterial(
        Material _sourcematerial,
        Shader _shader)
    {
        string safeName = GetSafeFileName(_sourcematerial.name); //安全なファイル名
        string materialPath =
            $"{EMaterialFolderPath}/{safeName}.mat"; //外部Material Path
        Material material =
            AssetDatabase.LoadAssetAtPath<Material>(materialPath); //既存Material
        if (material == null)
        {
            material = new Material(_shader);
            material.name = safeName;
            AssetDatabase.CreateAsset(material, materialPath);
        }
        else
        {
            material.shader = _shader;
        }

        Texture baseTexture = GetBaseTexture(_sourcematerial); //内蔵MaterialのTexture
        Color sourceColor = GetBaseColor(_sourcematerial); //内蔵Materialの基本色
        float neutralBrightness = Mathf.Max(
            EMinimumNeutralBrightness,
            sourceColor.grayscale); //青みを除いた明度
        Color neutralColor = baseTexture != null
            ? Color.white
            : new Color(
                neutralBrightness,
                neutralBrightness,
                neutralBrightness,
                1.0f); //青みのない基本色

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", baseTexture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", baseTexture);
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", neutralColor);
        }

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", neutralColor);
        }

        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", EDefaultMetallic);
        }

        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", EDefaultSmoothness);
        }

        if (material.HasProperty("_Surface"))
        {
            material.SetFloat("_Surface", EOpaqueSurfaceType);
        }

        material.renderQueue = -1;
        EditorUtility.SetDirty(material);
        return material;
    }

    /// <summary>
    /// 内蔵Materialから基本Textureを取得します。
    /// </summary>
    private static Texture GetBaseTexture(Material _material)
    {
        if (_material.HasProperty("_BaseMap"))
        {
            return _material.GetTexture("_BaseMap");
        }

        if (_material.HasProperty("_MainTex"))
        {
            return _material.GetTexture("_MainTex");
        }

        return null;
    }

    /// <summary>
    /// 内蔵Materialから基本色を取得します。
    /// </summary>
    private static Color GetBaseColor(Material _material)
    {
        if (_material.HasProperty("_BaseColor"))
        {
            return _material.GetColor("_BaseColor");
        }

        if (_material.HasProperty("_Color"))
        {
            return _material.GetColor("_Color");
        }

        return Color.white;
    }

    /// <summary>
    /// Material Assetへ使用できるファイル名へ変換します。
    /// </summary>
    private static string GetSafeFileName(string _name)
    {
        string safeName = _name; //変換中のMaterial名
        char[] invalidCharacters = Path.GetInvalidFileNameChars(); //使用不可文字一覧
        for (int i = 0; i < invalidCharacters.Length; ++i)
        {
            safeName = safeName.Replace(invalidCharacters[i], '_');
        }

        return string.IsNullOrWhiteSpace(safeName)
            ? "StageMaterial"
            : safeName;
    }

    /// <summary>
    /// 外部Material保存用フォルダを準備します。
    /// </summary>
    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder(EStageFolderPath))
        {
            AssetDatabase.CreateFolder("Assets", "Stage");
        }

        if (!AssetDatabase.IsValidFolder(EMaterialFolderPath))
        {
            AssetDatabase.CreateFolder(EStageFolderPath, "Materials");
        }
    }
}
