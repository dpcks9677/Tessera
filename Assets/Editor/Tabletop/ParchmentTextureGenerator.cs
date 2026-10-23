#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Tessera.Core;
using Tessera.Games.Yacht.Presentation;
using Tessera.Rendering;

namespace Tessera.EditorTools
{
    public static class ParchmentTextureGenerator
    {
        private static readonly Color InkColor = new Color32(35, 23, 15, 255);          // #23170f (Deep Sepia Ink)
        private static readonly Color MutedInkColor = new Color32(145, 125, 105, 255);  // #917d69 (Classic Grid Ink Line)
        private static readonly Color HeaderBarColor = new Color32(43, 31, 23, 255);    // #2b1f17 (Dark Ebony Sepia Bar)
        private static readonly Color BonusWashColor = new Color32(218, 207, 187, 255); // #dacbb3 (Bonus Tint Wash Box)
        private static readonly Color Transparent = new Color(0, 0, 0, 0);

        private const string PrefabPath = "Assets/03.Prefabs/Tabletop/3D Layered Parchment Score Sheet.prefab";
        private const string ParchmentDirectory = "Assets/04.Art/Generated/Parchment";
        private const string LibraryAssetPath = "Assets/Resources/RuntimeAssetLibrary.asset";

        private enum ParchmentKind { Base, BurntEdge, WarmSand }

        /// <summary>양피지 레이어 한 장의 텍스처 규격. 크기는 프리팹의 레이어 메시 월드 크기에서 구한다.</summary>
        private readonly struct LayerSpec
        {
            public readonly string TransformPath;
            public readonly string FileName;
            public readonly ParchmentKind Kind;
            public readonly string LibraryField;

            public LayerSpec(string transformPath, string fileName, ParchmentKind kind, string libraryField)
            {
                TransformPath = transformPath;
                FileName = fileName;
                Kind = kind;
                LibraryField = libraryField;
            }
        }

        // L1~L5 순서. L5의 메시는 "Layer 5" 오브젝트가 아니라 그 자식 Visual Mesh다.
        private static readonly LayerSpec[] Layers =
        {
            new("Layer 1 - Bottom Burnt Parchment", "parchment_burnt_edge.png", ParchmentKind.BurntEdge, "parchmentBurntEdge"),
            new("Layer 2 - Warm Sand Parchment", "parchment_warm_sand.png", ParchmentKind.WarmSand, "parchmentWarmSand"),
            new("Layer 3 - Aged Parchment", "parchment_base_layer3.png", ParchmentKind.Base, "parchmentBaseLayer3"),
            new("Layer 4 - Cream Parchment", "parchment_base_layer4.png", ParchmentKind.Base, "parchmentBaseLayer4"),
            new("Layer 5 - Top Game Score Sheet/Visual Mesh", "parchment_base.png", ParchmentKind.Base, "parchmentBase"),
        };

        /// <summary>레이어별 텍스처 파일 에셋 경로(L1~L5 순서).</summary>
        public static string[] LayerTexturePaths()
        {
            string[] paths = new string[Layers.Length];
            for (int i = 0; i < Layers.Length; i++) paths[i] = $"{ParchmentDirectory}/{Layers[i].FileName}";
            return paths;
        }

        /// <summary>
        /// 프리팹 레이어 메시의 lossyScale(x, z)로 바닥 기준 텍셀 수를 구한다(L1~L5 순서).
        /// 프리팹이나 레이어를 찾지 못하면 LogError를 남기고 null을 돌려준다.
        /// </summary>
        public static Vector2Int[] ReadLayerResolutions()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[ParchmentTextureGenerator] 프리팹을 찾지 못했습니다: {PrefabPath}");
                return null;
            }

            Vector2Int[] resolutions = new Vector2Int[Layers.Length];
            for (int i = 0; i < Layers.Length; i++)
            {
                Transform layer = prefab.transform.Find(Layers[i].TransformPath);
                if (layer == null)
                {
                    Debug.LogError($"[ParchmentTextureGenerator] 레이어를 찾지 못했습니다: {Layers[i].TransformPath}");
                    return null;
                }

                Vector3 lossy = layer.lossyScale;
                resolutions[i] = PixelTexelDensity.TexelCountFor(new Vector2(lossy.x, lossy.z), PixelSurfaceFacing.Floor);
            }
            return resolutions;
        }

        [MenuItem("Tools/Tessera/Generate Parchment Textures")]
        public static void GenerateParchmentOnly()
        {
            if (!GenerateParchmentTextures()) return;

            AssetDatabase.Refresh();
            ConfigureParchmentImporters();
            AssignNewLayerTexturesToLibrary();
            Debug.Log("[ParchmentTextureGenerator] 양피지 텍스처 5장 생성 완료.");
        }

        [MenuItem("Tools/Tessera/Generate Parchment & Score Sheet Assets")]
        public static void GenerateAllAssets()
        {
            string iconsDir = Path.Combine(Application.dataPath, "04.Art", "Generated", "Games", "Yacht", "ScoreIcons");
            if (!Directory.Exists(iconsDir)) Directory.CreateDirectory(iconsDir);

            // 1. svgicons.js 기반 원본 벡터 둥근 주사위 및 특수 족보 아이콘 12종 PNG 생성 (128x128)
            GenerateSvgIcons(iconsDir);

            // 2. 양피지 레이어별 텍스처 5장 생성 (프리팹 레이어 크기 기준)
            bool parchmentGenerated = GenerateParchmentTextures();

            AssetDatabase.Refresh();
            ConfigureIconImporters();
            if (parchmentGenerated)
            {
                ConfigureParchmentImporters();
                AssignNewLayerTexturesToLibrary();
            }
            Debug.Log("✨ svgicons.js 원본 벡터 아이콘 복원 및 양피지 텍스처 생성 완료!");
        }

        private static void GenerateSvgIcons(string iconsDir)
        {
            int size = 128;

            for (int num = 1; num <= 6; num++)
            {
                Texture2D tex = RenderDiceSvg(num, size);
                SavePng(tex, Path.Combine(iconsDir, $"dice_{num}.png"));
                UnityEngine.Object.DestroyImmediate(tex);
            }

            string[] specials = { "choice", "4oak", "fullhouse", "s_straight", "l_straight", "yacht" };
            foreach (string id in specials)
            {
                Texture2D tex = RenderSpecialSvg(id, size);
                SavePng(tex, Path.Combine(iconsDir, $"{id}.png"));
                UnityEngine.Object.DestroyImmediate(tex);
            }
        }

        /// <summary>
        /// svgicons.js의 getDiceSvg(1..6) 원본 벡터 둥근 주사위 렌더링 복원
        /// </summary>
        private static Texture2D RenderDiceSvg(int num, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Transparent;

            float scale = size / 24f;

            // 둥근 챔퍼 외곽 테두리 (rx: 4, stroke: 2.0)
            DrawRoundedRectStroke(pixels, size, 2f * scale, 2f * scale, 20f * scale, 20f * scale, 4f * scale, 2.0f * scale, InkColor);

            // 동그란 원형 핍 (radius: 2.0)
            float r = 2.0f * scale;
            float c = 12f * scale;
            float l = 7f * scale;
            float r_pos = 17f * scale;
            float t = 17f * scale;
            float b = 7f * scale;
            float m = 12f * scale;

            if (num == 1)
            {
                DrawCircle(pixels, size, c, c, r, InkColor);
            }
            else if (num == 2)
            {
                DrawCircle(pixels, size, r_pos, t, r, InkColor);
                DrawCircle(pixels, size, l, b, r, InkColor);
            }
            else if (num == 3)
            {
                DrawCircle(pixels, size, r_pos, t, r, InkColor);
                DrawCircle(pixels, size, c, c, r, InkColor);
                DrawCircle(pixels, size, l, b, r, InkColor);
            }
            else if (num == 4)
            {
                DrawCircle(pixels, size, l, t, r, InkColor);
                DrawCircle(pixels, size, r_pos, t, r, InkColor);
                DrawCircle(pixels, size, l, b, r, InkColor);
                DrawCircle(pixels, size, r_pos, b, r, InkColor);
            }
            else if (num == 5)
            {
                DrawCircle(pixels, size, l, t, r, InkColor);
                DrawCircle(pixels, size, r_pos, t, r, InkColor);
                DrawCircle(pixels, size, c, c, r, InkColor);
                DrawCircle(pixels, size, l, b, r, InkColor);
                DrawCircle(pixels, size, r_pos, b, r, InkColor);
            }
            else if (num == 6)
            {
                DrawCircle(pixels, size, l, t, r, InkColor);
                DrawCircle(pixels, size, r_pos, t, r, InkColor);
                DrawCircle(pixels, size, l, m, r, InkColor);
                DrawCircle(pixels, size, r_pos, m, r, InkColor);
                DrawCircle(pixels, size, l, b, r, InkColor);
                DrawCircle(pixels, size, r_pos, b, r, InkColor);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static Texture2D RenderSpecialSvg(string id, int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Transparent;

            float scale = size / 24f;

            void RectBlock(float x, float ySvg, float w, float h, bool strokeOnly = false)
            {
                float y = 24f - ySvg - h;
                if (strokeOnly)
                {
                    DrawRoundedRectStroke(pixels, size, x * scale, y * scale, w * scale, h * scale, 0.6f * scale, 1.8f * scale, InkColor);
                }
                else
                {
                    DrawRoundedRectFill(pixels, size, x * scale, y * scale, w * scale, h * scale, 0.6f * scale, InkColor);
                }
            }

            if (id == "choice")
            {
                RectBlock(10, 4, 4, 4);
                RectBlock(10, 16, 4, 4);
                RectBlock(4, 10, 4, 4);
                RectBlock(16, 10, 4, 4);
                RectBlock(10, 10, 4, 4);
            }
            else if (id == "4oak")
            {
                Vector2 Pos(int idx)
                {
                    int col = (idx - 1) % 4;
                    int row = (idx - 1) / 4;
                    return new Vector2(2.5f + col * 5f, 2.5f + row * 5f);
                }

                Vector2 p4 = Pos(4);
                RectBlock(p4.x, p4.y, 4, 4, true);

                int[] filledIdx = { 1, 6, 11, 16 };
                foreach (int idx in filledIdx)
                {
                    Vector2 p = Pos(idx);
                    RectBlock(p.x, p.y, 4, 4, false);
                }
            }
            else if (id == "fullhouse")
            {
                RectBlock(3, 5, 4, 4);
                RectBlock(10, 5, 4, 4);
                RectBlock(17, 5, 4, 4);
                RectBlock(6.5f, 14, 4, 4);
                RectBlock(13.5f, 14, 4, 4);
            }
            else if (id == "s_straight")
            {
                RectBlock(4, 16, 4, 4);
                RectBlock(8, 12, 4, 4);
                RectBlock(12, 8, 4, 4);
                RectBlock(16, 4, 4, 4);
            }
            else if (id == "l_straight")
            {
                RectBlock(2, 18, 4, 4);
                RectBlock(6, 14, 4, 4);
                RectBlock(10, 10, 4, 4);
                RectBlock(14, 6, 4, 4);
                RectBlock(18, 2, 4, 4);
            }
            else if (id == "yacht")
            {
                RectBlock(10, 2, 4, 4);
                RectBlock(18, 9, 4, 4);
                RectBlock(14, 17, 4, 4);
                RectBlock(6, 17, 4, 4);
                RectBlock(2, 9, 4, 4);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static void DrawCircle(Color[] pixels, int size, float cx, float cy, float r, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - r - 2));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(cx + r + 2));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - r - 2));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(cy + r + 2));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float alpha = Mathf.Clamp01(r - dist + 0.5f);
                    if (alpha > 0)
                    {
                        int idx = y * size + x;
                        Color existing = pixels[idx];
                        pixels[idx] = Color.Lerp(existing, color, alpha);
                    }
                }
            }
        }

        private static void DrawRoundedRectFill(Color[] pixels, int size, float rx, float ry, float w, float h, float radius, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(rx - 1));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(rx + w + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(ry - 1));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(ry + h + 1));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float dx = Mathf.Max(0f, Mathf.Abs(px - (rx + w * 0.5f)) - (w * 0.5f - radius));
                    float dy = Mathf.Max(0f, Mathf.Abs(py - (ry + h * 0.5f)) - (h * 0.5f - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                    if (alpha > 0)
                    {
                        int idx = y * size + x;
                        pixels[idx] = Color.Lerp(pixels[idx], color, alpha);
                    }
                }
            }
        }

        private static void DrawRoundedRectStroke(Color[] pixels, int size, float rx, float ry, float w, float h, float radius, float strokeWidth, Color color)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(rx - strokeWidth - 1));
            int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(rx + w + strokeWidth + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(ry - strokeWidth - 1));
            int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(ry + h + strokeWidth + 1));

            float halfStroke = strokeWidth * 0.5f;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f;
                    float py = y + 0.5f;

                    float dx = Mathf.Max(0f, Mathf.Abs(px - (rx + w * 0.5f)) - (w * 0.5f - radius));
                    float dy = Mathf.Max(0f, Mathf.Abs(py - (ry + h * 0.5f)) - (h * 0.5f - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    float dBox = dist - radius;
                    float dStroke = Mathf.Abs(dBox + halfStroke) - halfStroke;

                    float alpha = Mathf.Clamp01(0.5f - dStroke);
                    if (alpha > 0)
                    {
                        int idx = y * size + x;
                        pixels[idx] = Color.Lerp(pixels[idx], color, alpha);
                    }
                }
            }
        }

        private static bool GenerateParchmentTextures()
        {
            Vector2Int[] resolutions = ReadLayerResolutions();
            if (resolutions == null) return false;

            string baseDir = Path.Combine(Application.dataPath, "04.Art", "Generated", "Parchment");
            if (!Directory.Exists(baseDir)) Directory.CreateDirectory(baseDir);

            for (int i = 0; i < Layers.Length; i++)
            {
                Vector2Int size = resolutions[i];
                Texture2D tex = Layers[i].Kind switch
                {
                    ParchmentKind.BurntEdge => BuildBurntEdgeTexture(size.x, size.y),
                    ParchmentKind.WarmSand => BuildWarmSandTexture(size.x, size.y),
                    _ => BuildBaseTexture(size.x, size.y),
                };
                SavePng(tex, Path.Combine(baseDir, Layers[i].FileName));
                UnityEngine.Object.DestroyImmediate(tex);
            }
            return true;
        }

        // 아래 세 함수는 정규화 좌표(0~1)로 그려 해상도가 달라져도 특징의 상대 크기를 유지한다.
        private static Texture2D BuildBaseTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pix = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = (y + 0.5f) / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w;
                    float n1 = Mathf.PerlinNoise(nx * 4f, ny * 8f) * 0.08f;
                    float n2 = Mathf.PerlinNoise(nx * 16f, ny * 32f) * 0.03f;
                    float micro = (TexelHash(x, y) - 0.5f) * 0.03f;

                    float r = Mathf.Clamp01(0.94f + n1 + n2 + micro);
                    float g = Mathf.Clamp01(0.88f + n1 * 0.9f + n2 + micro);
                    float b = Mathf.Clamp01(0.77f + n1 * 0.8f + n2 + micro);
                    pix[y * w + x] = new Color(r, g, b, 1f);
                }
            }
            tex.SetPixels(pix);
            tex.Apply(false);
            return tex;
        }

        private static Texture2D BuildBurntEdgeTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pix = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = (y + 0.5f) / h;
                float edgeY = Mathf.Min(ny, 1f - ny) * 2f;
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w;
                    float edgeX = Mathf.Min(nx, 1f - nx) * 2f;
                    float edgeDist = Mathf.Clamp01(Mathf.Min(edgeX, edgeY));

                    float noise = Mathf.PerlinNoise(nx * 8f, ny * 16f) * 0.25f;
                    float burnFactor = Mathf.Clamp01((0.24f - edgeDist + noise) / 0.24f);

                    float r = Mathf.Lerp(0.92f, 0.50f, burnFactor);
                    float g = Mathf.Lerp(0.85f, 0.36f, burnFactor);
                    float b = Mathf.Lerp(0.73f, 0.22f, burnFactor);
                    pix[y * w + x] = new Color(r, g, b, 1f);
                }
            }
            tex.SetPixels(pix);
            tex.Apply(false);
            return tex;
        }

        private static Texture2D BuildWarmSandTexture(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pix = new Color[w * h];
            for (int y = 0; y < h; y++)
            {
                float ny = (y + 0.5f) / h;
                for (int x = 0; x < w; x++)
                {
                    float nx = (x + 0.5f) / w;
                    float n = Mathf.PerlinNoise(nx * 6f, ny * 12f) * 0.07f;
                    pix[y * w + x] = new Color(0.90f + n, 0.82f + n, 0.68f + n, 1f);
                }
            }
            tex.SetPixels(pix);
            tex.Apply(false);
            return tex;
        }

        /// <summary>텍셀 좌표마다 고정된 0~1 값. 같은 텍셀은 항상 같은 값이다.</summary>
        private static float TexelHash(int x, int y)
        {
            unchecked
            {
                uint hash = (uint)(x * 73856093) ^ (uint)(y * 19349663);
                hash ^= hash >> 13;
                hash *= 0x5bd1e995u;
                hash ^= hash >> 15;
                return (hash & 0xffffu) / 65535f;
            }
        }

        private static void SavePng(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        private static void ConfigureIconImporters()
        {
            string[] iconFiles = Directory.GetFiles("Assets/04.Art/Generated/Games/Yacht/ScoreIcons", "*.png");
            foreach (string file in iconFiles)
            {
                string unityPath = file.Replace("\\", "/");
                TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear; // 부드럽고 선명한 벡터 렌더링
                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>양피지 5장 임포터. 텍셀 하나가 필터 한 칸이어야 하므로 Point, 밉맵 끔, NPOT 보존(D-050).</summary>
        private static void ConfigureParchmentImporters()
        {
            foreach (string path in LayerTexturePaths())
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer) continue;

                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = false;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        /// <summary>새로 생긴 L3·L4 텍스처를 RuntimeAssetLibrary 에셋의 해당 필드에 넣는다.</summary>
        private static void AssignNewLayerTexturesToLibrary()
        {
            RuntimeAssetLibrary library = AssetDatabase.LoadAssetAtPath<RuntimeAssetLibrary>(LibraryAssetPath);
            if (library == null)
            {
                Debug.LogWarning($"[ParchmentTextureGenerator] '{LibraryAssetPath}'가 없어 L3·L4 텍스처를 연결하지 못했습니다. Tools/Tessera/Bake Runtime Asset Library를 먼저 실행하십시오.");
                return;
            }

            SerializedObject so = new(library);
            foreach (string field in new[] { "parchmentBaseLayer3", "parchmentBaseLayer4" })
            {
                int index = Array.FindIndex(Layers, layer => layer.LibraryField == field);
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{ParchmentDirectory}/{Layers[index].FileName}");
                so.FindProperty(field).objectReferenceValue = texture;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
