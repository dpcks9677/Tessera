using NUnit.Framework;
using Tessera.EditorTools;
using Tessera.Games.Yacht.Presentation;
using Tessera.Rendering;
using UnityEditor;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 탁자·트레이 표면 텍스처의 텍셀이 필터 한 칸과 맞는지 검증합니다(PIXEL-T7, D-050).
    /// 디스크 PNG 크기 테스트는 Tools/Tessera 메뉴로 텍스처를 다시 굽기 전까지 실패하는 것이 정상입니다.
    /// </summary>
    [TestFixture]
    public sealed class TabletopTexelTests
    {
        private const float Tolerance = 1e-5f;
        private const string FeltPngPath =
            "Assets/04.Art/Generated/Tabletop/Textures/Yacht_Tray_Visual_Runtime_Burgundy_Corduroy_Texture.png";

        [Test]
        public void FeltTilingReproducesFloorTexelWorldSize()
        {
            Vector2 tiling = TabletopSurfaceBuilder.FeltTiling();
            float uvWorldLength = TabletopSurfaceBuilder.TrayUvSourceUnits * Tessera.Core.DiceBoardMetrics.TrayScale;
            Vector2 texel = PixelTexelDensity.TexelWorldSize(PixelSurfaceFacing.Floor);

            float texelX = uvWorldLength / tiling.x / TabletopSurfaceBuilder.FeltWidthTexels;
            float texelY = uvWorldLength / tiling.y / TabletopSurfaceBuilder.FeltHeightTexels;

            Assert.That(texelX, Is.EqualTo(texel.x).Within(Tolerance));
            Assert.That(texelY, Is.EqualTo(texel.y).Within(Tolerance));
        }

        [Test]
        public void FeltHeightIsMultipleOfThreeTexelRibPeriod()
        {
            Assert.That(TabletopSurfaceBuilder.FeltRibPeriodTexels, Is.EqualTo(3));
            Assert.That(TabletopSurfaceBuilder.FeltHeightTexels % TabletopSurfaceBuilder.FeltRibPeriodTexels, Is.EqualTo(0));
        }

        [Test]
        public void FeltTextureIsPointWithoutMipmapsAtDeclaredSize()
        {
            Texture2D tex = TabletopSurfaceBuilder.CreateBurgundyCorduroyTexture();
            try
            {
                Assert.That(tex.width, Is.EqualTo(128));
                Assert.That(tex.height, Is.EqualTo(96));
                Assert.That(tex.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(tex.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
                Assert.That(tex.mipmapCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void FeltRowAverageRepeatsEveryRibPeriod()
        {
            Texture2D tex = TabletopSurfaceBuilder.CreateBurgundyCorduroyTexture();
            try
            {
                int period = TabletopSurfaceBuilder.FeltRibPeriodTexels;
                float[] rowAverage = new float[tex.height];
                for (int y = 0; y < tex.height; y++)
                {
                    float sum = 0f;
                    for (int x = 0; x < tex.width; x++) sum += tex.GetPixel(x, y).r;
                    rowAverage[y] = sum / tex.width;
                }

                for (int y = 0; y + period < tex.height; y++)
                {
                    Assert.That(rowAverage[y + period], Is.EqualTo(rowAverage[y]).Within(0.01f), $"행 {y}");
                }

                // 경계: 한 주기 안에서는 골 명암이 실제로 달라야 합니다(밋밋한 텍스처 거부).
                Assert.That(rowAverage[0] - rowAverage[2], Is.GreaterThan(0.1f));
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void FeltTextureIsTileableAcrossRepeatSeam()
        {
            Texture2D tex = TabletopSurfaceBuilder.CreateBurgundyCorduroyTexture();
            try
            {
                // 가로 이음매: 마지막 열과 첫 열의 차이가 같은 행의 이웃 열 차이 수준이어야 합니다.
                float seam = 0f;
                float interior = 0f;
                for (int y = 0; y < tex.height; y++)
                {
                    seam += Mathf.Abs(tex.GetPixel(tex.width - 1, y).r - tex.GetPixel(0, y).r);
                    interior += Mathf.Abs(tex.GetPixel(63, y).r - tex.GetPixel(64, y).r);
                }
                Assert.That(seam / tex.height, Is.LessThan(interior / tex.height + 0.03f));
            }
            finally
            {
                Object.DestroyImmediate(tex);
            }
        }

        [Test]
        public void FeltPhaseOffsetPutsCellCenterSamplesOnTexelCenters()
        {
            Camera camera = CreatePhaseCamera(out GameObject cameraObject);
            GameObject tray = CreatePhaseTray();
            try
            {
                Vector2 offset = TabletopSurfaceBuilder.FeltPhaseOffset(
                    camera, PixelFilterSettings.StartResolution, tray.transform, Tessera.Core.DiceBoardMetrics.PlayFloorSourceY);

                AssertFeltFractionsCentered(camera, tray.transform, offset);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(tray);
            }
        }

        [Test]
        public void FeltPhaseOffsetStaysCenteredAfterCameraMoves()
        {
            Camera camera = CreatePhaseCamera(out GameObject cameraObject);
            GameObject tray = CreatePhaseTray();
            try
            {
                float shift = PixelFilterSettings.CellWorldSize * 0.37f;
                camera.transform.position += new Vector3(shift, 0f, shift);

                Vector2 offset = TabletopSurfaceBuilder.FeltPhaseOffset(
                    camera, PixelFilterSettings.StartResolution, tray.transform, Tessera.Core.DiceBoardMetrics.PlayFloorSourceY);

                AssertFeltFractionsCentered(camera, tray.transform, offset);
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(tray);
            }
        }

        [Test]
        public void FeltPhaseOffsetFallsBackToDefaultOnInvalidInput()
        {
            Camera camera = CreatePhaseCamera(out GameObject cameraObject);
            GameObject tray = CreatePhaseTray();
            try
            {
                Vector2 expected = TabletopSurfaceBuilder.FeltOffset();
                float floorY = Tessera.Core.DiceBoardMetrics.PlayFloorSourceY;

                Assert.That(TabletopSurfaceBuilder.FeltPhaseOffset(null, PixelFilterSettings.StartResolution, tray.transform, floorY), Is.EqualTo(expected));
                Assert.That(TabletopSurfaceBuilder.FeltPhaseOffset(camera, Vector2Int.zero, tray.transform, floorY), Is.EqualTo(expected));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(tray);
            }
        }

        private static Camera CreatePhaseCamera(out GameObject cameraObject)
        {
            cameraObject = new GameObject("FeltPhaseTestCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = PixelFilterSettings.WorldOrthographicSize;
            camera.aspect = 16f / 9f;
            cameraObject.transform.SetPositionAndRotation(
                new Vector3(-0.39f, 11.5f, -3.1f), Quaternion.Euler(PixelFilterSettings.WorldCameraPitch, 0f, 0f));
            return camera;
        }

        private static GameObject CreatePhaseTray()
        {
            GameObject tray = new("FeltPhaseTestTray");
            tray.transform.position = new Vector3(
                -0.39f + Tessera.Core.DiceBoardMetrics.TrayCenterX,
                Tessera.Core.DiceBoardMetrics.TrayVisualY,
                Tessera.Core.DiceBoardMetrics.TrayCenterZ);
            tray.transform.localScale = Vector3.one * Tessera.Core.DiceBoardMetrics.TrayScale;
            return tray;
        }

        // 트레이 중심 칸에서 ±40칸 범위의 5×5 칸 중심 샘플이 모두 텍셀 한가운데에 떨어지는지 확인합니다.
        private static void AssertFeltFractionsCentered(Camera camera, Transform tray, Vector2 offset)
        {
            Vector2Int res = PixelFilterSettings.StartResolution;
            Vector3 floorCenter = tray.TransformPoint(new Vector3(0f, Tessera.Core.DiceBoardMetrics.PlayFloorSourceY, 0f));
            Vector3 viewport = camera.WorldToViewportPoint(floorCenter);
            int baseX = Mathf.RoundToInt(viewport.x * res.x - 0.5f);
            int baseY = Mathf.RoundToInt(viewport.y * res.y - 0.5f);
            Vector2 tiling = TabletopSurfaceBuilder.FeltTiling();
            Plane floor = new(tray.up, floorCenter);

            for (int dy = -40; dy <= 40; dy += 20)
            {
                for (int dx = -40; dx <= 40; dx += 20)
                {
                    Ray ray = camera.ViewportPointToRay(new Vector3(
                        (baseX + dx + 0.5f) / res.x, (baseY + dy + 0.5f) / res.y, 0f));
                    Assert.That(floor.Raycast(ray, out float enter), Is.True, $"({dx},{dy}) 광선이 바닥과 만나지 않습니다.");

                    Vector3 local = tray.InverseTransformPoint(ray.GetPoint(enter));
                    float tx = (local.x / TabletopSurfaceBuilder.TrayUvSourceUnits * tiling.x + offset.x) * TabletopSurfaceBuilder.FeltWidthTexels;
                    float ty = (local.z / TabletopSurfaceBuilder.TrayUvSourceUnits * tiling.y + offset.y) * TabletopSurfaceBuilder.FeltHeightTexels;

                    Assert.That(tx - Mathf.Floor(tx), Is.EqualTo(0.5f).Within(0.02f), $"u 소수부 ({dx},{dy})");
                    Assert.That(ty - Mathf.Floor(ty), Is.EqualTo(0.5f).Within(0.02f), $"v 소수부 ({dx},{dy})");
                }
            }
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void WoodPlankPngMatchesTexelCount(int plank)
        {
            Vector2Int expected = PixelTexelDensity.TexelCountFor(
                new Vector2(TabletopSurfaceBuilder.PlankWidth, TabletopSurfaceBuilder.PlankDepth), PixelSurfaceFacing.Floor);

            AssertPngSize($"Assets/04.Art/Generated/Wood/wood_plank_{plank}.png", expected);
        }

        [Test]
        public void FeltPngMatchesDeclaredSize()
        {
            AssertPngSize(FeltPngPath, new Vector2Int(TabletopSurfaceBuilder.FeltWidthTexels, TabletopSurfaceBuilder.FeltHeightTexels));
        }

        [Test]
        public void ParchmentPngsMatchPrefabLayerTexelCounts()
        {
            Vector2Int[] expected = ParchmentTextureGenerator.ReadLayerResolutions();
            Assert.That(expected, Is.Not.Null, "프리팹 레이어를 읽지 못했습니다.");

            string[] paths = ParchmentTextureGenerator.LayerTexturePaths();
            Assert.That(paths.Length, Is.EqualTo(5));
            for (int i = 0; i < paths.Length; i++) AssertPngSize(paths[i], expected[i]);
        }

        private static void AssertPngSize(string path, Vector2Int expected)
        {
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(tex, Is.Not.Null, $"{path} 텍스처를 찾지 못했습니다.");
            Assert.That(new Vector2Int(tex.width, tex.height), Is.EqualTo(expected), $"{path} 크기. 메뉴로 다시 굽지 않았다면 정상 실패입니다.");
        }
    }
}
