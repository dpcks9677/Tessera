using NUnit.Framework;
using Tessera.Games.Yacht.Presentation;
using Tessera.Rendering;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 픽셀 필터 1칸 = 월드 1/16 계약을 고정한다(PIXEL-T1).
    /// 월드 카메라 시야가 이 상수에서 계산되므로, 값이 어긋나면 격자가 칸 단위로 떨어지지 않는다.
    /// </summary>
    [TestFixture]
    public sealed class PixelGridUnitTests
    {
        [Test]
        public void CellWorldSizeIsExactlyOneSixteenth()
        {
            Assert.AreEqual(0.0625f, PixelFilterSettings.CellWorldSize);
        }

        [Test]
        public void WorldOrthographicSizeIsExact()
        {
            Assert.AreEqual(8.4375f, PixelFilterSettings.WorldOrthographicSize);
        }

        [Test]
        public void CoarsePresetVerticalCellEqualsCellWorldSize()
        {
            float cell = 2f * PixelFilterSettings.WorldOrthographicSize / PixelFilterSettings.ResolutionB.y;
            Assert.AreEqual(PixelFilterSettings.CellWorldSize, cell);
        }

        [Test]
        public void WorldCameraFramingUsesPixelGridSize()
        {
            GameObject go = new("PixelGridTestCamera");
            try
            {
                Camera camera = go.AddComponent<Camera>();
                YachtSceneAssembler.ApplyWorldCameraFraming(camera, 0f);

                Assert.That(camera.orthographic, Is.True);
                Assert.AreEqual(PixelFilterSettings.WorldOrthographicSize, camera.orthographicSize);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
