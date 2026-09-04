using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Tessera.EditorTools
{
    /// <summary>
    /// 씬에 코드로 생성돼 있는 테이블 프롭을 프리팹과 에셋으로 굽는다(M9-T1).
    ///
    /// 굽기 대상은 절차적 생성 코드를 다시 실행한 결과가 아니라 <b>현재 씬의 계층</b>이다.
    /// 재실행 방식은 테이블/러너처럼 독립 Create()가 없는 프롭을 다룰 수 없고,
    /// 현재 화면에 보이는 결과와 미세하게 달라질 위험이 있다.
    ///
    /// 런타임 생성 메시·머티리얼·텍스처는 에셋이 아니므로 그대로 프리팹에 저장하면 참조가 끊긴다.
    /// 따라서 에셋으로 먼저 저장하고 참조를 갈아 끼운 뒤 프리팹을 만든다.
    /// </summary>
    public static class TabletopPrefabBaker
    {
        private const string PrefabFolder = "Assets/Prefabs/Tabletop";
        private const string GeneratedFolder = "Assets/Art/Generated/Tabletop";
        private const string MeshFolder = GeneratedFolder + "/Meshes";
        private const string MaterialFolder = GeneratedFolder + "/Materials";
        private const string TextureFolder = GeneratedFolder + "/Textures";
        private const string LayoutRootName = "Graphics Layout";

        /// <summary>레이아웃 루트 아래에서 찾을 프롭의 직계 자식 이름.</summary>
        private static readonly string[] PropNames =
        {
            "3D Wood Planks Table",
            "3D Fabric Runner",
            "Yacht Tray Visual",
            "3D Stone Augment Card Tray",
            "3D Layered Parchment Score Sheet",
            "3D Inkwell and Quill Decoration",
            "3D Parchment Paperweight",
            "3D Roll Cosmic Cube",
            "3D Roll Orb",
            "3D Reroll Counter Bar",
            "3D Hourglass Timer",
            "3D Cozy Beeswax Candle Decoration",
            "3D Runic Slate & Crystal Matrix",
            "3D Trinket Cluster (Ring, Brooch, Crystal)",
            "3D Turn Balance Indicator"
        };

        [MenuItem("Tessera/Tabletop/Bake Tabletop Prefabs")]
        public static void Bake()
        {
            Transform layoutRoot = FindLayoutRoot();
            if (layoutRoot == null)
            {
                EditorUtility.DisplayDialog(
                    "테이블 프롭 굽기",
                    "씬에서 '" + LayoutRootName + "' 오브젝트를 찾지 못했습니다. 메인 씬을 열고 다시 실행하십시오.",
                    "확인");
                return;
            }

            EnsureFolders();

            List<string> baked = new();
            List<string> skipped = new();
            try
            {
                for (int i = 0; i < PropNames.Length; i++)
                {
                    string propName = PropNames[i];
                    EditorUtility.DisplayProgressBar("테이블 프롭 굽기", propName, (float)i / PropNames.Length);

                    Transform source = layoutRoot.Find(propName);
                    if (source == null)
                    {
                        skipped.Add(propName);
                        continue;
                    }

                    baked.Add(BakeProp(source));
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[TabletopPrefabBaker] 프리팹 {baked.Count}종을 생성했습니다.\n{string.Join("\n", baked)}");
            if (skipped.Count > 0)
            {
                Debug.LogWarning($"[TabletopPrefabBaker] 씬에서 찾지 못해 건너뛴 프롭: {string.Join(", ", skipped)}");
            }
        }

        private static string BakeProp(Transform source)
        {
            // 프리팹 파일명은 씬 오브젝트 이름을 그대로 쓴다.
            // SaveAsPrefabAsset이 루트 GameObject 이름을 파일명으로 덮어쓰기 때문에,
            // 파일명을 치환하면 프리팹 루트 이름이 씬 이름과 어긋나 이름 기반 조회가 전부 깨진다.
            string safeName = ToSafeName(source.name);
            string prefabPath = $"{PrefabFolder}/{ToFileName(source.name)}.prefab";

            // 씬 원본을 건드리지 않도록 사본을 만들어 굽는다.
            GameObject clone = UnityEngine.Object.Instantiate(source.gameObject);
            try
            {
                clone.name = source.name;
                clone.transform.SetParent(null, false);
                // 프리팹의 기준 포즈는 원점이다. 실제 배치는 씬 인스턴스가 소유한다.
                clone.transform.localPosition = Vector3.zero;
                clone.transform.localRotation = Quaternion.identity;
                clone.transform.localScale = source.localScale;

                Dictionary<Mesh, Mesh> meshCache = new();
                Dictionary<Material, Material> materialCache = new();
                Dictionary<Texture2D, Texture2D> textureCache = new();

                ExtractMeshes(clone, safeName, meshCache);
                ExtractMaterials(clone, safeName, materialCache, textureCache);

                AssetDatabase.DeleteAsset(prefabPath);
                PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(clone);
            }

            return prefabPath;
        }

        private static void ExtractMeshes(GameObject root, string propName, Dictionary<Mesh, Mesh> cache)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            foreach (MeshFilter filter in filters)
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null || AssetDatabase.Contains(mesh)) continue;

                if (!cache.TryGetValue(mesh, out Mesh asset))
                {
                    string path = UniquePath($"{MeshFolder}/{propName}_{ToSafeName(filter.gameObject.name)}", ".asset");
                    asset = UnityEngine.Object.Instantiate(mesh);
                    asset.name = Path.GetFileNameWithoutExtension(path);
                    AssetDatabase.CreateAsset(asset, path);
                    cache[mesh] = asset;
                }

                filter.sharedMesh = asset;
            }
        }

        private static void ExtractMaterials(
            GameObject root,
            string propName,
            Dictionary<Material, Material> materialCache,
            Dictionary<Texture2D, Texture2D> textureCache)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                if (materials == null) continue;

                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material material = materials[i];
                    if (material == null || AssetDatabase.Contains(material)) continue;

                    if (!materialCache.TryGetValue(material, out Material asset))
                    {
                        asset = new Material(material);
                        ExtractTextures(asset, propName, textureCache);
                        string path = UniquePath($"{MaterialFolder}/{propName}_{ToSafeName(material.name)}", ".mat");
                        asset.name = Path.GetFileNameWithoutExtension(path);
                        AssetDatabase.CreateAsset(asset, path);
                        materialCache[material] = asset;
                    }

                    materials[i] = asset;
                    changed = true;
                }

                if (changed) renderer.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// 머티리얼이 참조하는 런타임 생성 텍스처를 PNG 에셋으로 저장하고 참조를 갈아 끼운다.
        /// 읽기 불가 텍스처는 인코딩할 수 없으므로 참조를 비우고 경고를 남긴다.
        /// </summary>
        private static void ExtractTextures(Material material, string propName, Dictionary<Texture2D, Texture2D> cache)
        {
            string[] propertyNames = material.GetTexturePropertyNames();
            foreach (string property in propertyNames)
            {
                if (material.GetTexture(property) is not Texture2D texture) continue;
                if (AssetDatabase.Contains(texture)) continue;

                if (!cache.TryGetValue(texture, out Texture2D asset))
                {
                    string path = UniquePath($"{TextureFolder}/{propName}_{ToSafeName(texture.name)}", ".png");
                    if (!TryWritePng(texture, path))
                    {
                        Debug.LogWarning(
                            $"[TabletopPrefabBaker] {propName}: 텍스처 '{texture.name}'를 저장하지 못해 참조를 비웁니다. " +
                            "생성 코드에서 Texture2D를 읽기 가능하게 만들어야 합니다.");
                        material.SetTexture(property, null);
                        continue;
                    }

                    ConfigureTextureImporter(path);
                    asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    cache[texture] = asset;
                }

                material.SetTexture(property, asset);
            }
        }

        /// <summary>
        /// 텍스처를 PNG로 저장한다.
        ///
        /// EncodeToPNG를 직접 부르지 않는다. 일부 생성 코드가 Apply(false, true)로 텍스처를
        /// 읽기 불가 상태로 만들어 두어(InkwellAndQuill 등) 직접 인코딩이 실패한다.
        /// RenderTexture로 Blit한 뒤 읽어오면 읽기 가능 여부와 압축 포맷에 상관없이 동작한다.
        /// </summary>
        private static bool TryWritePng(Texture2D texture, string assetPath)
        {
            RenderTexture temporary = RenderTexture.GetTemporary(
                texture.width, texture.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            Texture2D readable = null;
            try
            {
                Graphics.Blit(texture, temporary);
                RenderTexture.active = temporary;

                readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0f, 0f, temporary.width, temporary.height), 0, 0);
                readable.Apply(false, false);

                byte[] png = readable.EncodeToPNG();
                if (png == null || png.Length == 0) return false;

                File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), assetPath), png);
                AssetDatabase.ImportAsset(assetPath);
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[TabletopPrefabBaker] PNG 인코딩 실패: {exception.Message}");
                return false;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(temporary);
                if (readable != null) UnityEngine.Object.DestroyImmediate(readable);
            }
        }

        private static void ConfigureTextureImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer) return;

            importer.textureType = TextureImporterType.Default;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = true;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static Transform FindLayoutRoot()
        {
            GameObject root = GameObject.Find(LayoutRootName);
            return root != null ? root.transform : null;
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "Prefabs");
            EnsureFolder("Assets/Prefabs", "Tabletop");
            EnsureFolder("Assets/Art", "Generated");
            EnsureFolder("Assets/Art/Generated", "Tabletop");
            EnsureFolder(GeneratedFolder, "Meshes");
            EnsureFolder(GeneratedFolder, "Materials");
            EnsureFolder(GeneratedFolder, "Textures");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = $"{parent}/{child}";
            if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(parent, child);
        }

        private static string UniquePath(string pathWithoutExtension, string extension)
        {
            string candidate = pathWithoutExtension + extension;
            int suffix = 1;
            while (AssetDatabase.LoadMainAssetAtPath(candidate) != null)
            {
                candidate = $"{pathWithoutExtension}_{suffix:00}{extension}";
                suffix++;
            }

            return candidate;
        }

        /// <summary>
        /// 파일 경로에서 실제로 금지된 문자만 치환한다. 공백·&amp;·괄호·쉼표는 유지한다.
        /// 프리팹 파일명이 곧 루트 GameObject 이름이 되므로 씬 이름을 최대한 보존해야 한다.
        /// </summary>
        private static string ToFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            char[] buffer = name.ToCharArray();
            for (int i = 0; i < buffer.Length; i++)
            {
                if (Array.IndexOf(invalid, buffer[i]) >= 0) buffer[i] = '_';
            }

            string result = new string(buffer).Trim();
            return string.IsNullOrEmpty(result) ? "Unnamed" : result;
        }

        /// <summary>메시·머티리얼·텍스처 에셋 이름용. 내부 식별자이므로 보수적으로 치환한다.</summary>
        private static string ToSafeName(string name)
        {
            string trimmed = name.Replace("(Instance)", string.Empty).Replace("(Clone)", string.Empty).Trim();
            char[] buffer = new char[trimmed.Length];
            int length = 0;
            foreach (char character in trimmed)
            {
                bool allowed = char.IsLetterOrDigit(character) || character == '_' || character == '-';
                buffer[length++] = allowed ? character : '_';
            }

            string safe = new string(buffer, 0, length).Trim('_');
            while (safe.Contains("__")) safe = safe.Replace("__", "_");
            return string.IsNullOrEmpty(safe) ? "Unnamed" : safe;
        }
    }
}
