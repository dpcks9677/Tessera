#if UNITY_EDITOR
using System;
using System.IO;
using Tessera.Rendering;
using UnityEditor;
using UnityEngine;

namespace Tessera.EditorTools
{
    /// <summary>
    /// 판자마다 다른 픽셀 나뭇결 텍스처를 굽는다(M17-T16).
    ///
    /// 이전 상판은 <c>Assets/Textures/Wood/wood_grain_knots.png</c> 한 장을 UV 오프셋 네 종으로
    /// 잘라 썼다. 원본이 블러 처리된 사인파 그라디언트라 픽셀 격자에 걸려도 인접 칸의 색이 거의
    /// 같아 부드럽게 보였고, 결 말고는 아무 패턴도 없었다.
    ///
    /// 여기서는 모든 단계를 정수 인덱스로 끊고 고정 셰이드 배열에서 색을 뽑는다. 어디에서도
    /// 보간하지 않는 것이 하드 엣지를 만드는 유일한 규칙이다. 색은 아트 가이드 §4의 나무 토큰
    /// 램프에서만 뽑으므로 픽셀 필터의 팔레트 양자화 모드를 켜도 색이 튀지 않는다.
    /// </summary>
    public static class WoodPlankTextureGenerator
    {
        private const string OutputDirectory = "Assets/Textures/Wood";
        private const int PlankCount = 4;
        private const int SeedBase = 20260908;

        /// <summary>
        /// 텍셀 해상도. 판자는 월드 38 × 4.9이고 저해상도 격자는 480×270이라, 이 크기에서
        /// 텍셀 하나가 월드 0.148 × 0.153 — 거의 정방형에 화면 격자 두세 칸이다.
        /// 여전히 곱게 보이면 128 × 16으로 낮춰 다시 굽는다.
        /// </summary>
        private const int Width = 256;

        /// <inheritdoc cref="Width"/>
        private const int Height = 32;

        private const float Tau = Mathf.PI * 2f;

        /// <summary>판자별 옹이 개수. 무작위로 뽑으면 한 장도 없는 판자가 생길 수 있어 고정한다.</summary>
        private static readonly int[] KnotCounts = { 2, 1, 1, 2 };

        [MenuItem("Tools/Tessera/Generate Wood Plank Textures")]
        public static void GenerateAll()
        {
            Color32[] ladder = BuildShadeLadder();

            string absoluteDirectory = Path.Combine(Directory.GetCurrentDirectory(), OutputDirectory);
            if (!Directory.Exists(absoluteDirectory)) Directory.CreateDirectory(absoluteDirectory);

            for (int plank = 0; plank < PlankCount; plank++)
            {
                Texture2D texture = BuildPlankTexture(plank, ladder);
                File.WriteAllBytes(Path.Combine(absoluteDirectory, FileName(plank)), texture.EncodeToPNG());
                UnityEngine.Object.DestroyImmediate(texture);
            }

            AssetDatabase.Refresh();
            ConfigureImporters();
            Debug.Log($"[WoodPlankTextureGenerator] 픽셀 나뭇결 {PlankCount}장 생성 완료 ({Width}×{Height}).");
        }

        private static string FileName(int plankIndex) => $"wood_plank_{plankIndex + 1}.png";

        /// <summary>
        /// 아트 가이드 §4 나무 토큰 두 개의 명암 램프를 밝기 순으로 늘어놓는다.
        ///
        /// <see cref="TesseraPixelPalette.Build"/>는 토큰마다 램프 네 단을 순서대로 펼치므로,
        /// 나무 토큰이 세 번째·네 번째면 결과의 8~15번이 나무 램프다. 이 인덱스 결합이 깨지면
        /// 엉뚱한 토큰 색으로 나뭇결을 칠하게 되므로, 첫 색을 직접 계산한 값과 대조해 검사한다.
        /// </summary>
        private static Color32[] BuildShadeLadder()
        {
            const int firstWoodIndex = 8;
            const int woodShadeCount = 8;

            Color[] palette = TesseraPixelPalette.Build();
            if (palette.Length < firstWoodIndex + woodShadeCount)
            {
                throw new InvalidOperationException(
                    $"팔레트가 {palette.Length}색뿐이라 나무 램프({firstWoodIndex}~{firstWoodIndex + woodShadeCount - 1})를 뽑을 수 없다. " +
                    "TesseraPixelPalette의 토큰 순서를 확인할 것.");
            }

            Color32 first = palette[firstWoodIndex];
            Color32 expected = DarkestWoodMainShade();
            if (Mathf.Abs(first.r - expected.r) > 2 || Mathf.Abs(first.g - expected.g) > 2 || Mathf.Abs(first.b - expected.b) > 2)
            {
                throw new InvalidOperationException(
                    $"팔레트 {firstWoodIndex}번이 나무 램프의 최하단이 아니다(기대 {expected}, 실제 {first}). " +
                    "TesseraPixelPalette.Tokens 순서가 바뀌었으면 firstWoodIndex를 함께 고칠 것.");
            }

            Color32[] ladder = new Color32[woodShadeCount];
            for (int i = 0; i < woodShadeCount; i++) ladder[i] = palette[firstWoodIndex + i];

            Array.Sort(ladder, (a, b) => Luminance(a).CompareTo(Luminance(b)));
            return ladder;
        }

        /// <summary>art_style_guide.md §4 <c>color-table-wood-main</c>에 명암 램프 최하단을 적용한 색.</summary>
        private static Color32 DarkestWoodMainShade()
        {
            Color.RGBToHSV(new Color32(0x6e, 0x43, 0x2a, 0xff), out float hue, out float saturation, out float value);
            return Color.HSVToRGB(hue, saturation, value * TesseraPixelPalette.RampVector.x);
        }

        private static float Luminance(Color32 color) => 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;

        private struct Knot
        {
            public float CenterX;
            public float CenterY;
            public float RadiusX;
            public float RadiusY;
            public int RingCount;
            public float PushAmount;
        }

        private static Texture2D BuildPlankTexture(int plankIndex, Color32[] ladder)
        {
            System.Random random = new(SeedBase + plankIndex);

            // 결이 흐르는 방향은 판자 길이 방향(U축)으로 고정하고, 휘는 정도와 위상만 흔든다.
            float frequencyA = 1.0f + (float)random.NextDouble() * 1.5f;
            float frequencyB = 3.0f + (float)random.NextDouble() * 3.0f;
            float phaseA = (float)random.NextDouble() * Tau;
            float phaseB = (float)random.NextDouble() * Tau;
            float warpAmplitude = 0.10f + (float)random.NextDouble() * 0.10f;
            int bandCount = 5 + random.Next(3);

            int[] bandShades = BuildBandPattern(plankIndex, ladder.Length, random);
            Knot[] knots = BuildKnots(plankIndex, random);

            int[] shadeIndices = new int[Width * Height];

            for (int y = 0; y < Height; y++)
            {
                float v = (y + 0.5f) / Height;
                for (int x = 0; x < Width; x++)
                {
                    float u = (x + 0.5f) / Width;

                    float warp = warpAmplitude *
                        (Mathf.Sin(u * frequencyA * Tau + phaseA) + 0.5f * Mathf.Sin(u * frequencyB * Tau + phaseB));
                    float grainCoordinate = v + warp;

                    int shade = -1;
                    foreach (Knot knot in knots)
                    {
                        float dx = (x - knot.CenterX) / knot.RadiusX;
                        float dy = (y - knot.CenterY) / knot.RadiusY;
                        float radius = Mathf.Sqrt(dx * dx + dy * dy);

                        // 결이 옹이를 피해 흐르게 민다. 이 왜곡이 없으면 옹이가 결 위에 얹힌 도장처럼 보인다.
                        const float pushRange = 2.5f;
                        if (radius < pushRange && radius > 0.0001f)
                        {
                            float falloff = 1f - radius / pushRange;
                            grainCoordinate += knot.PushAmount * falloff * falloff * Mathf.Sign(y - knot.CenterY);
                        }

                        if (radius >= 1f) continue;

                        // 동심 링. 중심 타원은 가장 어두운 색으로 채운다.
                        shade = radius < 0.35f ? 0 : (Mathf.FloorToInt(radius * knot.RingCount) % 2 == 0 ? 1 : 2);
                        break;
                    }

                    if (shade < 0)
                    {
                        int band = Mathf.FloorToInt(grainCoordinate * bandCount);
                        shade = bandShades[Modulo(band, bandShades.Length)];
                    }

                    shadeIndices[y * Width + x] = shade;
                }
            }

            DrawCrack(shadeIndices, random);
            DrawPinKnots(shadeIndices, random);
            DarkenEdges(shadeIndices);

            return BuildTexture(plankIndex, shadeIndices, ladder);
        }

        /// <summary>
        /// 밴드마다 어떤 셰이드를 쓸지 정하는 짧은 순환 패턴. 밴드 폭이 균등해도 색이 불규칙하면
        /// 결이 기계적으로 보이지 않는다. 이웃한 밴드가 같은 색이면 밴드가 뭉치므로 연속을 막는다.
        /// </summary>
        private static int[] BuildBandPattern(int plankIndex, int ladderLength, System.Random random)
        {
            // 같은 값을 여러 번 넣어 가중치를 준다. 램프 양 끝은 아트 가이드 §2의 나무 톤
            // (#633e26~#7d4d2e) 밖이라, 균등하게 뽑으면 밝은 판자에서 최상단이 결의 3분의 1을 덮는다.
            int[] grainShades = { 2, 3, 3, 4, 4, 5 };
            int toneShift = plankIndex % 2 == 0 ? 0 : 1; // 판자마다 전체 밝기를 한 단 어긋나게 둔다.

            int[] pattern = new int[7];
            int previous = -1;
            for (int i = 0; i < pattern.Length; i++)
            {
                int pick;
                do { pick = grainShades[random.Next(grainShades.Length)]; } while (pick == previous);
                previous = pick;
                pattern[i] = Mathf.Clamp(pick + toneShift, 0, ladderLength - 1);
            }
            return pattern;
        }

        private static Knot[] BuildKnots(int plankIndex, System.Random random)
        {
            int count = KnotCounts[plankIndex % KnotCounts.Length];
            Knot[] knots = new Knot[count];

            for (int i = 0; i < count; i++)
            {
                float radiusY = 4f + (float)random.NextDouble() * 3f;
                knots[i] = new Knot
                {
                    // 링이 위아래로 잘리지 않도록 세로는 가운데로 몰고, 가로만 판자 전체에 흩는다.
                    CenterX = Width * (0.15f + (float)random.NextDouble() * 0.70f),
                    CenterY = Height * (0.30f + (float)random.NextDouble() * 0.40f),
                    RadiusY = radiusY,
                    RadiusX = radiusY * (1.6f + (float)random.NextDouble() * 1.0f),
                    RingCount = 3 + random.Next(2),
                    PushAmount = 0.10f + (float)random.NextDouble() * 0.06f
                };
            }
            return knots;
        }

        /// <summary>판자 길이 방향으로 흐르는 짧은 균열선 하나. 굵기는 1텍셀이다.</summary>
        private static void DrawCrack(int[] shadeIndices, System.Random random)
        {
            int x = random.Next(Width);
            int y = 2 + random.Next(Height - 4);
            int length = Width / 6 + random.Next(Width / 6);

            for (int i = 0; i < length && x < Width; i++, x++)
            {
                shadeIndices[y * Width + x] = 0;
                if (random.Next(6) != 0) continue;

                y = Mathf.Clamp(y + (random.Next(2) == 0 ? -1 : 1), 1, Height - 2);
            }
        }

        /// <summary>
        /// 작은 핀 노트 몇 개. 결 밴드만으로는 만들 수 없는 잔 얼룩을 준다.
        ///
        /// 가로로만 한두 텍셀 눕힌다. 세로로 키우면 화면에서 십자 글리프로 읽혀 나무 얼룩이 아니라
        /// 렌더링 결함처럼 보인다.
        /// </summary>
        private static void DrawPinKnots(int[] shadeIndices, System.Random random)
        {
            int count = 2 + random.Next(3);
            for (int i = 0; i < count; i++)
            {
                int startX = random.Next(Width);
                int y = 2 + random.Next(Height - 4);
                int length = 1 + random.Next(2);

                for (int dx = 0; dx < length; dx++)
                {
                    int x = startX + dx;
                    if (x >= Width) break;

                    shadeIndices[y * Width + x] = 1;
                }
            }
        }

        /// <summary>위아래 두 줄을 단계적으로 어둡게 해 판자끼리 분리돼 보이게 한다.</summary>
        private static void DarkenEdges(int[] shadeIndices)
        {
            for (int x = 0; x < Width; x++)
            {
                Darken(shadeIndices, x, 0, 2);
                Darken(shadeIndices, x, Height - 1, 2);
                Darken(shadeIndices, x, 1, 1);
                Darken(shadeIndices, x, Height - 2, 1);
            }
            return;

            static void Darken(int[] indices, int x, int y, int steps)
            {
                int offset = y * Width + x;
                indices[offset] = Mathf.Max(0, indices[offset] - steps);
            }
        }

        private static Texture2D BuildTexture(int plankIndex, int[] shadeIndices, Color32[] ladder)
        {
            Color32[] pixels = new Color32[shadeIndices.Length];
            for (int i = 0; i < shadeIndices.Length; i++)
            {
                pixels[i] = ladder[Mathf.Clamp(shadeIndices[i], 0, ladder.Length - 1)];
            }

            Texture2D texture = new(Width, Height, TextureFormat.RGBA32, false, false)
            {
                name = $"Wood Plank {plankIndex + 1}"
            };
            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static int Modulo(int value, int length)
        {
            int remainder = value % length;
            return remainder < 0 ? remainder + length : remainder;
        }

        /// <summary>
        /// 픽셀아트로 읽히게 하는 임포터 설정.
        ///
        /// Baseline 연출은 월드를 1920×1080으로 렌더한 뒤 업스케일 셰이더가 480×270 격자로
        /// 스냅해 칸마다 한 점만 뽑는다. 확대 샘플링이므로 Bilinear나 밉맵이 켜져 있으면 텍셀
        /// 경계가 열 화면픽셀에 걸쳐 번지고, 격자 스냅이 그 중간값을 집어 블록마다 중간색 줄이
        /// 생긴다. Point + 밉맵 off가 선택이 아니라 필수인 이유다.
        /// </summary>
        private static void ConfigureImporters()
        {
            for (int plank = 0; plank < PlankCount; plank++)
            {
                string assetPath = $"{OutputDirectory}/{FileName(plank)}";
                if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer) continue;

                importer.textureType = TextureImporterType.Default;
                importer.alphaSource = TextureImporterAlphaSource.None;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.sRGBTexture = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 512;
                importer.SaveAndReimport();
            }
        }
    }
}
#endif
