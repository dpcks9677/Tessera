using System.IO;
using NUnit.Framework;
using Tessera.Games.Yacht.Presentation;
using Tessera.Rendering;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 정지 평면 소품(동전 면, 증강 스크롤 양피지)의 텍셀이 필터 한 칸과 맞는지 검증합니다(PIXEL-T9, D-050).
    /// 동전 면은 자동 축소 대신 22칸용 손그림 도트를 씁니다(D-053).
    /// 디스크 PNG·머티리얼 테스트는 Tools/Tessera 메뉴로 다시 굽기 전까지 실패하는 것이 정상입니다.
    /// </summary>
    [TestFixture]
    public sealed class PropTexelTests
    {
        private const float Tolerance = 1e-5f;
        private const string CoinHeadPngPath = "Assets/04.Art/Generated/Coin/Textures/coin_head_albedo.png";
        private const string CoinTailPngPath = "Assets/04.Art/Generated/Coin/Textures/coin_tail_albedo.png";
        private const string CoinGlbPath = "Assets/04.Art/Source/Coin/coin.glb";
        private const string ScrollFolder = "Assets/04.Art/Generated/Games/Yacht/Resources/AugmentScrolls";
        private const string ScrollPngPath = ScrollFolder + "/parchment_scroll_albedo.png";

        [Test]
        public void CoinFaceResolutionMatchesFloorTexelCount()
        {
            float diameter = CoinFaceDiameter();
            Vector2Int expected = PixelTexelDensity.TexelCountFor(new Vector2(diameter, diameter), PixelSurfaceFacing.Floor);

            Assert.That(CoinMeshBaker.FaceTextureSize, Is.EqualTo(Mathf.Min(expected.x, expected.y)));
            Assert.That(CoinMeshBaker.FaceTextureSize, Is.EqualTo(22));
        }

        [Test]
        public void CoinFaceTexelIsNotSmallerThanFilterCellAndNotOneTexelCoarser()
        {
            float diameter = CoinFaceDiameter();
            Vector2 texel = PixelTexelDensity.TexelWorldSize(PixelSurfaceFacing.Floor);
            int size = CoinMeshBaker.FaceTextureSize;

            // 내림이라 텍셀은 필터 칸 이상이고, 한 칸 더 늘리면 칸보다 작아진다.
            Assert.That(diameter / size, Is.GreaterThanOrEqualTo(Mathf.Max(texel.x, texel.y) - Tolerance));
            Assert.That(diameter / (size + 1), Is.LessThan(Mathf.Max(texel.x, texel.y)));
        }

        [Test]
        public void CoinFacePngsMatchResolution()
        {
            var size = new Vector2Int(CoinMeshBaker.FaceTextureSize, CoinMeshBaker.FaceTextureSize);
            AssertPngSize(CoinHeadPngPath, size);
            AssertPngSize(CoinTailPngPath, size);
            AssertPngSize(CoinMeshBaker.HeadPixelPath, size);
            AssertPngSize(CoinMeshBaker.TailPixelPath, size);
        }

        [Test]
        public void CoinFacePixelArtUsesOnlyEngravingPalette()
        {
            var palette = new[]
            {
                CoinFaceTextureConverter.BackgroundColor, CoinFaceTextureConverter.InkColor, CoinFaceTextureConverter.InkShadowColor,
            };

            foreach (string path in new[] { CoinMeshBaker.HeadPixelPath, CoinMeshBaker.TailPixelPath })
            {
                var texture = new Texture2D(2, 2);
                try
                {
                    Assert.That(texture.LoadImage(File.ReadAllBytes(path)), Is.True, $"{path}를 읽지 못했습니다.");
                    foreach (Color32 c in texture.GetPixels32())
                        Assert.That(System.Array.Exists(palette, p => p.r == c.r && p.g == c.g && p.b == c.b), Is.True,
                            $"{path}에 음각 3색 밖의 색 ({c.r}, {c.g}, {c.b})이 있습니다.");
                }
                finally
                {
                    Object.DestroyImmediate(texture);
                }
            }
        }

        [Test]
        public void CoinFaceSizeValidationRejectsNonMatchingSize()
        {
            int size = CoinMeshBaker.FaceTextureSize;

            Assert.DoesNotThrow(() => CoinMeshBaker.ValidateFaceSize("ok", size, size));
            Assert.Throws<InvalidDataException>(() => CoinMeshBaker.ValidateFaceSize("old", 64, 64));
            Assert.Throws<InvalidDataException>(() => CoinMeshBaker.ValidateFaceSize("wide", size + 1, size));
        }

        /// <summary>coin.glb 캡 최대 반지름(CoinMeshBaker.Split과 같은 기준) × 2 × 모델 배율.</summary>
        private static float CoinFaceDiameter()
        {
            float radius = 0f;
            foreach (CoinGlbPrimitive primitive in CoinGlbReader.Read(File.ReadAllBytes(CoinGlbPath)))
            {
                if (primitive.MaterialName != "Coin") continue;
                foreach (Vector3 p in primitive.Positions)
                    radius = Mathf.Max(radius, Mathf.Sqrt(p.x * p.x + p.z * p.z));
            }

            Assert.That(radius, Is.GreaterThan(0f), "Coin 프리미티브를 찾지 못했습니다.");
            return 2f * radius * CoinMeshBaker.ModelScale;
        }

        [Test]
        public void ScrollHelperMapsKnownBodyWorldSizeToTextureSizeAndTiling()
        {
            var body = new Vector2(6.0502f, 3.8372f);

            Vector2Int size = AugmentScrollTexelMapping.TextureSize(body);
            Vector2 tiling = AugmentScrollTexelMapping.Tiling(body, size.x, size.y);

            Assert.That(size, Is.EqualTo(new Vector2Int(76, 64)));
            Assert.That(tiling.x, Is.EqualTo(1.1076f).Within(1e-3f));
            Assert.That(tiling.y, Is.EqualTo(0.9266f).Within(1e-3f));
        }

        [Test]
        public void ScrollHelperTilingReproducesFloorTexelWorldSize()
        {
            var body = new Vector2(6.0502f, 3.8372f);
            Vector2Int size = AugmentScrollTexelMapping.TextureSize(body);
            Vector2 tiling = AugmentScrollTexelMapping.Tiling(body, size.x, size.y);
            Vector2 texel = PixelTexelDensity.TexelWorldSize(PixelSurfaceFacing.Floor);

            float texelX = body.x / (AugmentScrollTexelMapping.BodyUvScaleU * tiling.x) / size.x;
            float texelY = body.y / (AugmentScrollTexelMapping.BodyUvScaleV * tiling.y) / size.y;

            Assert.That(texelX, Is.EqualTo(texel.x).Within(Tolerance));
            Assert.That(texelY, Is.EqualTo(texel.y).Within(Tolerance));
        }

        [Test]
        public void ScrollTextureSizeClampsToOneTexelForTinyBody()
        {
            Vector2Int size = AugmentScrollTexelMapping.TextureSize(new Vector2(0.001f, 0.001f));

            Assert.That(size, Is.EqualTo(Vector2Int.one));
        }

        [Test]
        public void ScrollBodyWorldSizeUsesXAndZAndIgnoresScaleSign()
        {
            Vector2 body = AugmentScrollTexelMapping.BodyWorldSize(new Vector3(2f, 9f, 3f), new Vector3(-2f, 5f, 0.5f));

            Assert.That(body, Is.EqualTo(new Vector2(4f, 1.5f)));
        }

        [Test]
        public void PresetLocalScaleMatchesCreatedPresetInstance()
        {
            var cardSize = new Vector2(4.58f, 2.5817f);
            Vector3 scale = AugmentScrollModelFactory.PresetLocalScale(cardSize);

            Assert.That(scale.x, Is.EqualTo(cardSize.x / AugmentScrollModelFactory.ReferenceWidth).Within(Tolerance));
            Assert.That(scale.z, Is.EqualTo(cardSize.y / AugmentScrollModelFactory.ReferenceHeight).Within(Tolerance));
            Assert.That(scale.y, Is.EqualTo(Mathf.Min(scale.x, scale.z)).Within(Tolerance));

            var parent = new GameObject("ScrollParent");
            try
            {
                AugmentScrollModel model = AugmentScrollModelFactory.Create(
                    parent.transform, AugmentParchmentPreset.GentleWave, cardSize);
                Assert.That(model.transform.localScale, Is.EqualTo(scale));
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        [Test]
        public void ScrollPngMatchesMeanBodyTextureSize()
        {
            Assert.That(AugmentScrollAssetGenerator.TryGetMeanBodyWorldSize(out Vector2 body), Is.True);
            AssertPngSize(ScrollPngPath, AugmentScrollTexelMapping.TextureSize(body));
        }

        [Test]
        public void ScrollBodyWorldSizeRejectsMissingPreset()
        {
            Assert.That(AugmentScrollAssetGenerator.TryGetPresetBodyWorldSize(-1, out Vector2 body), Is.False);
            Assert.That(body, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ScrollMaterialTilingKeepsEveryPresetWithinQuarterTexel()
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ScrollPngPath);
            Assert.That(texture, Is.Not.Null);
            Vector2 texel = PixelTexelDensity.TexelWorldSize(PixelSurfaceFacing.Floor);

            foreach (string name in new[] { "AugmentScrollPaperFront", "AugmentScrollPaperUnderside" })
            {
                string path = $"{ScrollFolder}/Materials/{name}.mat";
                Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
                Assert.That(material, Is.Not.Null, $"{path} 머티리얼을 찾지 못했습니다.");
                Vector2 tiling = material.GetTextureScale("_BaseMap");

                for (int key = 0; key < AugmentParchmentVisuals.PresetCount; key++)
                {
                    Assert.That(AugmentScrollAssetGenerator.TryGetPresetBodyWorldSize(key, out Vector2 body), Is.True,
                        $"프리셋 {key} 본문 크기를 구하지 못했습니다.");

                    // 본문 전체 폭·깊이가 덮는 텍셀 수와 이상적인 텍셀 수의 차이.
                    float texelsU = AugmentScrollTexelMapping.BodyUvScaleU * tiling.x * texture.width;
                    float texelsV = AugmentScrollTexelMapping.BodyUvScaleV * tiling.y * texture.height;
                    Assert.That(Mathf.Abs(texelsU - (body.x / texel.x)), Is.LessThanOrEqualTo(0.25f),
                        $"{name} 프리셋 {key} 폭 누적 오차. 메뉴로 다시 굽지 않았다면 정상 실패입니다.");
                    Assert.That(Mathf.Abs(texelsV - (body.y / texel.y)), Is.LessThanOrEqualTo(0.25f),
                        $"{name} 프리셋 {key} 깊이 누적 오차. 메뉴로 다시 굽지 않았다면 정상 실패입니다.");
                }
            }
        }

        private static void AssertPngSize(string path, Vector2Int expected)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(tex, Is.Not.Null, $"{path} 텍스처를 찾지 못했습니다.");
            Assert.That(new Vector2Int(tex.width, tex.height), Is.EqualTo(expected), $"{path} 크기. 메뉴로 다시 굽지 않았다면 정상 실패입니다.");
        }
    }
}
