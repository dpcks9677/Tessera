#if UNITY_EDITOR
using System.IO;
using Tessera.Games.Yacht.Presentation;
using UnityEditor;
using UnityEngine;

namespace Tessera.EditorTools
{
    /// <summary>
    /// 트레이 바닥 펠트(버건디 코듀로이)를 픽셀 격자에 맞춰 다시 굽는다(PIXEL-T7).
    ///
    /// 텍스처 크기·골 주기·타일링은 <see cref="TabletopSurfaceBuilder"/>의 상수와 헬퍼가 원본이다.
    /// 여기서는 PNG를 기존 경로에 덮어써 GUID를 유지하고, 임포터와 머티리얼 타일링만 맞춘다.
    /// </summary>
    public static class FeltTextureGenerator
    {
        private const string TexturePath =
            "Assets/04.Art/Generated/Tabletop/Textures/Yacht_Tray_Visual_Runtime_Burgundy_Corduroy_Texture.png";

        private const string MaterialPath =
            "Assets/04.Art/Generated/Tabletop/Materials/Yacht_Tray_Visual_Runtime_Yacht_Tray_Felt_Material.mat";

        [MenuItem("Tools/Tessera/Generate Felt Texture")]
        public static void Generate()
        {
            Texture2D texture = TabletopSurfaceBuilder.CreateBurgundyCorduroyTexture();
            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), TexturePath);
            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);

            AssetDatabase.Refresh();
            ConfigureImporter();
            ApplyToMaterial();
            Debug.Log($"[FeltTextureGenerator] 펠트 {TabletopSurfaceBuilder.FeltWidthTexels}×{TabletopSurfaceBuilder.FeltHeightTexels} 생성 완료.");
        }

        private static void ConfigureImporter()
        {
            if (AssetImporter.GetAtPath(TexturePath) is not TextureImporter importer) return;

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.sRGBTexture = true;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
        }

        private static void ApplyToMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TexturePath);
            if (material == null || texture == null)
            {
                Debug.LogError($"[FeltTextureGenerator] 머티리얼 또는 텍스처를 찾지 못했습니다: {MaterialPath}");
                return;
            }

            Vector2 tiling = TabletopSurfaceBuilder.FeltTiling();
            Vector2 offset = TabletopSurfaceBuilder.FeltOffset();
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetTextureOffset("_BaseMap", offset);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
