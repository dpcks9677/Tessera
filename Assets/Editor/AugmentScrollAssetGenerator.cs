using System;
using System.IO;
using Tessera.Games.AugmentedYacht;
using UnityEditor;
using UnityEngine;

public static class AugmentScrollAssetGenerator
{
    private const string RootFolder = "Assets/Resources/AugmentScrolls";
    private const string MeshFolder = RootFolder + "/Meshes";
    private const string MaterialFolder = RootFolder + "/Materials";
    private const string TexturePath = RootFolder + "/parchment_scroll_albedo.png";

    [InitializeOnLoadMethod]
    private static void GenerateMissingAssetsAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{RootFolder}/AugmentScrollPreset_0.prefab") != null) return;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath) == null) return;
            Generate();
        };
    }

    [MenuItem("Tessera/Graphics/Generate Augment Scroll Assets")]
    public static void Generate()
    {
        EnsureFolder("Assets/Resources", "AugmentScrolls");
        EnsureFolder(RootFolder, "Meshes");
        EnsureFolder(RootFolder, "Materials");
        ConfigureTexture();

        Material front = CreateOrReplaceMaterial(
            MaterialFolder + "/AugmentScrollPaperFront.mat",
            new Color(.91f, .76f, .48f, 1f), .11f, true);
        Material underside = CreateOrReplaceMaterial(
            MaterialFolder + "/AugmentScrollPaperUnderside.mat",
            new Color(.39f, .20f, .10f, 1f), .08f, true);
        Material wax = CreateOrReplaceMaterial(
            MaterialFolder + "/AugmentScrollWax.mat",
            new Color(.54f, .10f, .09f, 1f), .34f, false);
        Material[] materials = { front, underside, wax };

        foreach (AugmentParchmentPreset preset in Enum.GetValues(typeof(AugmentParchmentPreset)))
        {
            int key = (int)preset;
            string prefabPath = $"{RootFolder}/AugmentScrollPreset_{key}.prefab";
            AssetDatabase.DeleteAsset(prefabPath);
            GameObject container = new($"AugmentScrollPreset_{key}_Generator");
            try
            {
                AugmentScrollModel model = AugmentScrollModelFactory.Build(
                    container.transform, preset,
                    AugmentScrollModelFactory.ReferenceWidth,
                    AugmentScrollModelFactory.ReferenceHeight,
                    materials, false);
                model.transform.SetParent(null, true);
                UnityEngine.Object.DestroyImmediate(container);

                MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < filters.Length; i++)
                {
                    Mesh mesh = filters[i].sharedMesh;
                    string safeName = filters[i].gameObject.name.Replace(" ", string.Empty);
                    string meshPath = $"{MeshFolder}/Preset_{key}_{safeName}.asset";
                    AssetDatabase.DeleteAsset(meshPath);
                    AssetDatabase.CreateAsset(mesh, meshPath);
                    filters[i].sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                }

                PrefabUtility.SaveAsPrefabAsset(model.gameObject, prefabPath);
                UnityEngine.Object.DestroyImmediate(model.gameObject);
            }
            finally
            {
                if (container != null) UnityEngine.Object.DestroyImmediate(container);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("M7-T4 3D 증강 스크롤 프리팹 5종을 생성했습니다.");
    }

    private static Material CreateOrReplaceMaterial(
        string path,
        Color color,
        float smoothness,
        bool usePaperTexture)
    {
        AssetDatabase.DeleteAsset(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new(shader) { name = Path.GetFileNameWithoutExtension(path), color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (usePaperTexture)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            material.mainTexture = texture;
            material.mainTextureScale = new Vector2(1.25f, .9f);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void ConfigureTexture()
    {
        if (AssetImporter.GetAtPath(TexturePath) is not TextureImporter importer) return;
        importer.textureType = TextureImporterType.Default;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Bilinear;
        importer.mipmapEnabled = true;
        importer.sRGBTexture = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.SaveAndReimport();
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
