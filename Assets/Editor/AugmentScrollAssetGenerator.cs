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
    private const string PreviewFolder = RootFolder + "/Previews";
    private const string TexturePath = RootFolder + "/parchment_scroll_albedo.png";
    private const int PreviewLayer = 30;

    [InitializeOnLoadMethod]
    private static void GenerateMissingAssetsAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{RootFolder}/AugmentScrollPreset_0.prefab") != null
                && AssetDatabase.LoadAssetAtPath<Sprite>($"{PreviewFolder}/AugmentScrollPreview_0.png") != null)
                return;
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
        EnsureFolder(RootFolder, "Previews");
        ConfigureTexture();

        Material front = CreateOrReplaceLitMaterial(
            MaterialFolder + "/AugmentScrollPaperFront.mat",
            new Color(.98f, .97f, .95f, 1f), .10f, true);
        Material underside = CreateOrReplaceLitMaterial(
            MaterialFolder + "/AugmentScrollPaperUnderside.mat",
            new Color(.97f, .96f, .93f, 1f), .08f, true);
        Material leather = CreateOrReplaceLitMaterial(
            MaterialFolder + "/AugmentScrollLeather.mat",
            new Color(.20f, .09f, .045f, 1f), .18f, false);
        Material wax = CreateOrReplaceLitMaterial(
            MaterialFolder + "/AugmentScrollWax.mat",
            new Color(.54f, .10f, .09f, 1f), .34f, false);
        Material cyan = CreateOrReplaceGlowMaterial(
            MaterialFolder + "/AugmentScrollCyanBorder.mat",
            new Color(.37f, .86f, 1f, .78f));
        Material[] materials = { front, underside, leather, wax, cyan };

        RemoveObsoleteAssets();
        foreach (AugmentParchmentPreset preset in Enum.GetValues(typeof(AugmentParchmentPreset)))
        {
            int key = (int)preset;
            string prefabPath = $"{RootFolder}/AugmentScrollPreset_{key}.prefab";
            AssetDatabase.DeleteAsset(prefabPath);
            GameObject container = new($"AugmentScrollPreset_{key}_Generator");
            try
            {
                AugmentScrollModel model = AugmentScrollModelFactory.Build(
                    container.transform,
                    preset,
                    AugmentScrollModelFactory.ReferenceWidth,
                    AugmentScrollModelFactory.ReferenceHeight,
                    materials,
                    false);
                model.transform.SetParent(null, true);
                UnityEngine.Object.DestroyImmediate(container);

                MeshFilter[] filters = model.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < filters.Length; i++)
                {
                    Mesh mesh = filters[i].sharedMesh;
                    string safeName = filters[i].gameObject.name.Replace(" ", string.Empty).Replace(".", "_");
                    string meshPath = $"{MeshFolder}/Preset_{key}_{safeName}.asset";
                    AssetDatabase.DeleteAsset(meshPath);
                    AssetDatabase.CreateAsset(mesh, meshPath);
                    filters[i].sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                }

                PrefabUtility.SaveAsPrefabAsset(model.gameObject, prefabPath);
                RenderPreview(model.gameObject, key);
                UnityEngine.Object.DestroyImmediate(model.gameObject);
            }
            finally
            {
                if (container != null) UnityEngine.Object.DestroyImmediate(container);
            }
        }

        AssetDatabase.Refresh();
        for (int key = 0; key < AugmentParchmentVisuals.PresetCount; key++)
            ConfigurePreview($"{PreviewFolder}/AugmentScrollPreview_{key}.png");
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("증강 카드 직사각형 스크롤 프리팹과 선택 창 프리뷰 4종을 생성했습니다.");
    }

    private static Material CreateOrReplaceLitMaterial(
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
            material.mainTextureScale = new Vector2(1.10f, .92f);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material CreateOrReplaceGlowMaterial(string path, Color color)
    {
        AssetDatabase.DeleteAsset(path);
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        Material material = new(shader) { name = Path.GetFileNameWithoutExtension(path), color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void RenderPreview(GameObject target, int key)
    {
        SetLayerRecursively(target, PreviewLayer);
        GameObject cameraObject = new($"Augment Preview Camera {key}", typeof(Camera));
        GameObject lightObject = new($"Augment Preview Light {key}", typeof(Light));
        RenderTexture renderTexture = new(512, 288, 24, RenderTextureFormat.ARGB32)
        {
            name = $"Augment Scroll Preview RT {key}",
            antiAliasing = 1
        };
        Texture2D texture = new(512, 288, TextureFormat.RGBA32, false);
        try
        {
            Camera camera = cameraObject.GetComponent<Camera>();
            camera.transform.position = new Vector3(0f, 7f, 0f);
            camera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = AugmentScrollModelFactory.ReferenceHeight * .59f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.cullingMask = 1 << PreviewLayer;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.targetTexture = renderTexture;

            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, .98f, .95f);
            light.intensity = 1.20f;
            light.cullingMask = 1 << PreviewLayer;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            camera.Render();
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            texture.Apply(false, false);
            RenderTexture.active = previous;

            string assetPath = $"{PreviewFolder}/AugmentScrollPreview_{key}.png";
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
            renderTexture.Release();
            UnityEngine.Object.DestroyImmediate(renderTexture);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(lightObject);
        }
    }

    private static void ConfigurePreview(string path)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
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

    private static void RemoveObsoleteAssets()
    {
        AssetDatabase.DeleteAsset($"{RootFolder}/AugmentScrollPreset_4.prefab");
        AssetDatabase.DeleteAsset($"{PreviewFolder}/AugmentScrollPreview_4.png");
        string[] obsolete = AssetDatabase.FindAssets("Preset_4_", new[] { MeshFolder });
        for (int i = 0; i < obsolete.Length; i++)
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(obsolete[i]));
        for (int key = 0; key < 4; key++)
        {
            AssetDatabase.DeleteAsset($"{MeshFolder}/Preset_{key}_RibbonTail.asset");
            AssetDatabase.DeleteAsset($"{MeshFolder}/Preset_{key}_PixelReadableRollLayers.asset");
        }
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        for (int i = 0; i < target.transform.childCount; i++)
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
    }
}
