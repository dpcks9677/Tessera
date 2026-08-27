using System.Collections.Generic;
using UnityEngine;

namespace Tessera.Games.AugmentedYacht
{
    public enum AugmentParchmentPreset
    {
        GentleWave,
        Notched,
        Scalloped,
        AsymmetricTear,
        WornCorners
    }

    /// <summary>드래프트 UI와 트레이 3D 카드가 공유하는 다섯 양피지 외곽을 생성합니다.</summary>
    public static class AugmentParchmentVisuals
    {
        public const int PresetCount = 5;
        private const int TextureWidth = 512;
        private const int TextureHeight = 288;
        private const int MeshColumns = 25;
        private const int MeshRows = 17;
        private const float LeftCurlUv = .22f;
        private const float BottomCurlUv = .20f;
        private static readonly Dictionary<int, Sprite> FullSprites = new();
        private static readonly Dictionary<int, Sprite> BorderSprites = new();

        private static readonly float[][] TopProfiles =
        {
            new[] { .03f, .00f, .02f, .01f, .04f, .00f, .02f, .01f, .03f },
            new[] { .02f, .06f, .01f, .03f, .00f, .05f, .01f, .04f, .02f },
            new[] { .04f, .01f, .05f, .01f, .05f, .01f, .05f, .01f, .04f },
            new[] { .01f, .05f, .02f, .00f, .04f, .02f, .06f, .01f, .03f },
            new[] { .07f, .02f, .01f, .03f, .00f, .02f, .01f, .03f, .07f }
        };

        private static readonly float[][] BottomProfiles =
        {
            new[] { .02f, .04f, .01f, .03f, .00f, .02f, .04f, .01f, .02f },
            new[] { .03f, .01f, .05f, .00f, .04f, .01f, .06f, .02f, .03f },
            new[] { .03f, .06f, .02f, .06f, .02f, .06f, .02f, .06f, .03f },
            new[] { .05f, .01f, .03f, .06f, .01f, .04f, .00f, .05f, .02f },
            new[] { .08f, .03f, .01f, .02f, .04f, .01f, .03f, .02f, .08f }
        };

        public static AugmentParchmentPreset Normalize(int value) =>
            (AugmentParchmentPreset)(value >= 0 && value < PresetCount ? value : 0);

        public static string GetOutlineSignature(AugmentParchmentPreset preset) =>
            string.Join(",", TopProfiles[(int)Normalize((int)preset)]);

        public static Sprite GetSprite(AugmentParchmentPreset preset, bool borderOnly)
        {
            int key = (int)Normalize((int)preset);
            Dictionary<int, Sprite> cache = borderOnly ? BorderSprites : FullSprites;
            if (cache.TryGetValue(key, out Sprite cached) && cached != null) return cached;

            Color32[] pixels = new Color32[TextureWidth * TextureHeight];
            float[] top = TopProfiles[key];
            float[] bottom = BottomProfiles[key];
            Color32 paper = new(255, 246, 216, 255);
            Color32 gold = new(229, 169, 60, 255);
            Color32 curlLight = new(241, 205, 139, 255);
            Color32 curlDark = new(103, 58, 31, 255);

            for (int y = 0; y < TextureHeight; y++)
            {
                float ny = (float)y / (TextureHeight - 1);
                for (int x = 0; x < TextureWidth; x++)
                {
                    float nx = (float)x / (TextureWidth - 1);
                    float topInset = Sample(top, nx);
                    float bottomInset = Sample(bottom, nx);
                    float leftInset = 0.018f + 0.010f * Mathf.Sin(ny * Mathf.PI * (key + 2));
                    float rightInset = key == (int)AugmentParchmentPreset.WornCorners
                        ? 0.018f + Mathf.Abs(ny - .5f) * .08f
                        : 0.018f + .006f * Mathf.Sin(ny * Mathf.PI * 3f + key);
                    bool inside = nx >= leftInset && nx <= 1f - rightInset
                        && ny >= bottomInset && ny <= 1f - topInset;
                    if (!inside) continue;

                    float edgeDistance = Mathf.Min(
                        Mathf.Min(nx - leftInset, 1f - rightInset - nx),
                        Mathf.Min(ny - bottomInset, 1f - topInset - ny));
                    bool border = edgeDistance < .015f;
                    if (borderOnly && !border) continue;
                    Color32 color = border ? gold : paper;
                    if (!borderOnly && nx < .13f)
                    {
                        float curl = Mathf.Clamp01(nx / .13f);
                        float rolledLight = .32f
                            + .45f * Mathf.SmoothStep(0f, 1f, curl)
                            + .20f * Mathf.Sin(curl * Mathf.PI * 2.7f + .35f);
                        color = Color32.Lerp(curlDark, curlLight, Mathf.Clamp01(rolledLight));
                    }
                    if (!borderOnly && ny < .16f)
                    {
                        float leftCurl = Mathf.Exp(-Mathf.Pow((nx - .15f) / .22f, 2f));
                        float rightCurl = .48f * Mathf.Exp(-Mathf.Pow((nx - .88f) / .15f, 2f));
                        float bottomRoll = Mathf.Clamp01((.16f - ny) / .16f) * Mathf.Clamp01(leftCurl + rightCurl);
                        color = Color32.Lerp(color, curlDark, bottomRoll * .34f);
                    }
                    pixels[y * TextureWidth + x] = color;
                }
            }

            Texture2D texture = new(TextureWidth, TextureHeight, TextureFormat.RGBA32, false)
            {
                name = $"Augment_Parchment_{key}_{(borderOnly ? "Border" : "Full")}",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, TextureWidth, TextureHeight), new Vector2(.5f, .5f), 100f);
            sprite.name = texture.name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            cache[key] = sprite;
            return sprite;
        }

        public static Mesh CreateMesh(AugmentParchmentPreset preset, float width, float height)
        {
            int key = (int)Normalize((int)preset);
            const float thickness = .018f;
            float[] top = TopProfiles[key];
            float[] bottom = BottomProfiles[key];
            var vertices = new List<Vector3>(MeshColumns * MeshRows * 2);
            var uvs = new List<Vector2>(MeshColumns * MeshRows * 2);
            var frontTriangles = new List<int>();
            var undersideTriangles = new List<int>();

            for (int layer = 0; layer < 2; layer++)
            {
                float layerOffset = layer == 0 ? -thickness : 0f;
                for (int zIndex = 0; zIndex < MeshRows; zIndex++)
                {
                    float v = (float)zIndex / (MeshRows - 1);
                    for (int xIndex = 0; xIndex < MeshColumns; xIndex++)
                    {
                        float u = (float)xIndex / (MeshColumns - 1);
                        Vector3 surface = EvaluateSurface(key, top, bottom, width, height, u, v);
                        surface.y += layerOffset;
                        vertices.Add(surface);
                        uvs.Add(new Vector2(u, v));
                    }
                }
            }

            int layerVertexCount = MeshColumns * MeshRows;
            for (int layer = 0; layer < 2; layer++)
            {
                bool topLayer = layer == 1;
                int offset = layer * layerVertexCount;
                List<int> target = topLayer ? frontTriangles : undersideTriangles;
                for (int z = 0; z < MeshRows - 1; z++)
                for (int x = 0; x < MeshColumns - 1; x++)
                {
                    int a = offset + z * MeshColumns + x;
                    int b = a + 1;
                    int c = a + MeshColumns;
                    int d = c + 1;
                    if (topLayer)
                    {
                        target.Add(a); target.Add(c); target.Add(b);
                        target.Add(b); target.Add(c); target.Add(d);
                    }
                    else
                    {
                        target.Add(a); target.Add(b); target.Add(c);
                        target.Add(b); target.Add(d); target.Add(c);
                    }
                }
            }

            AddSide(undersideTriangles, MeshColumns, MeshRows, layerVertexCount, true);
            AddSide(undersideTriangles, MeshColumns, MeshRows, layerVertexCount, false);
            var mesh = new Mesh { name = $"Augment_Parchment_Mesh_{key}" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(frontTriangles, 0);
            mesh.SetTriangles(undersideTriangles, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 EvaluateSurface(
            int key,
            float[] top,
            float[] bottom,
            float width,
            float height,
            float u,
            float v)
        {
            float minZ = (-.5f + Sample(bottom, u)) * height;
            float maxZ = (.5f - Sample(top, u)) * height;
            float outerRadius = width * (.052f + key * .0025f);
            float innerRadius = outerRadius * (.32f + key * .015f);
            float joinX = -.5f * width + outerRadius * .42f;
            float x;
            float y = 0f;

            if (u < LeftCurlUv)
            {
                float q = 1f - u / LeftCurlUv;
                float easedQ = Mathf.SmoothStep(0f, 1f, q);
                float turns = 1.28f + key * .035f;
                float angle = easedQ * turns * Mathf.PI * 2f;
                float radius = Mathf.Lerp(outerRadius, innerRadius, easedQ);
                x = joinX - radius * Mathf.Sin(angle);
                y += (outerRadius - radius * Mathf.Cos(angle)) * 1.58f;
            }
            else
            {
                float flatU = (u - LeftCurlUv) / (1f - LeftCurlUv);
                x = Mathf.Lerp(joinX, .5f * width, flatU);
            }

            float z = Mathf.Lerp(minZ, maxZ, v);
            if (v < BottomCurlUv)
            {
                float leftCorner = Mathf.Exp(-Mathf.Pow((u - .13f) / .22f, 2f));
                float rightCorner = .48f * Mathf.Exp(-Mathf.Pow((u - .88f) / .15f, 2f));
                float presetVariation = .90f + .10f * Mathf.Sin((key + 1) * 1.37f + u * Mathf.PI);
                float strength = Mathf.Clamp01((.12f + leftCorner + rightCorner) * presetVariation);
                float q = 1f - v / BottomCurlUv;
                float easedQ = Mathf.SmoothStep(0f, 1f, q);
                float radius = height * .065f * strength;
                float angle = easedQ * Mathf.PI * (1.22f + key * .045f);
                float joinZ = Mathf.Lerp(minZ, maxZ, BottomCurlUv);
                z = joinZ - radius * Mathf.Sin(angle);
                y += (radius - radius * Mathf.Cos(angle)) * 1.52f;
            }

            return new Vector3(x, y, z);
        }

        private static void AddSide(List<int> triangles, int columns, int rows, int layerVertexCount, bool horizontal)
        {
            int count = horizontal ? columns : rows;
            for (int edge = 0; edge < 2; edge++)
            for (int i = 0; i < count - 1; i++)
            {
                int a = horizontal ? edge * (rows - 1) * columns + i : i * columns + edge * (columns - 1);
                int b = horizontal ? a + 1 : a + columns;
                int c = a + layerVertexCount;
                int d = b + layerVertexCount;
                triangles.Add(a); triangles.Add(b); triangles.Add(c);
                triangles.Add(b); triangles.Add(d); triangles.Add(c);
            }
        }

        private static float Sample(float[] values, float normalized)
        {
            float scaled = Mathf.Clamp01(normalized) * (values.Length - 1);
            int index = Mathf.Min(Mathf.FloorToInt(scaled), values.Length - 2);
            return Mathf.Lerp(values[index], values[index + 1], scaled - index);
        }
    }
}
