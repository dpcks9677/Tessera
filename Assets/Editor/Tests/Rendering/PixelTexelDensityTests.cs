using NUnit.Framework;
using Tessera.Rendering;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 면 방향별 텍셀 밀도 규칙(<see cref="PixelTexelDensity"/>)을 고정하는 테스트입니다.
    /// 텍셀 하나는 필터 한 칸 이상이고, 텍셀을 하나 더 늘리면 한 칸 미만이 되어야 합니다.
    /// </summary>
    [TestFixture]
    public sealed class PixelTexelDensityTests
    {
        private const float Tolerance = 1e-6f;

        [Test]
        public void TexelIsAtLeastOneCellAndOneMoreWouldBeSmaller(
            [Values(PixelSurfaceFacing.FacingCamera, PixelSurfaceFacing.Floor, PixelSurfaceFacing.Wall)] PixelSurfaceFacing facing,
            [Values(0.6f, 0.9f, 1.0f, 2.0f, 3.54f, 5.2f)] float size)
        {
            Vector2 texel = PixelTexelDensity.TexelWorldSize(facing);
            Vector2Int count = PixelTexelDensity.TexelCountFor(new Vector2(size, size), facing);

            Assert.That(size / count.x, Is.GreaterThanOrEqualTo(texel.x - Tolerance), $"{facing} x: 텍셀이 한 칸 미만입니다.");
            Assert.That(size / count.y, Is.GreaterThanOrEqualTo(texel.y - Tolerance), $"{facing} y: 텍셀이 한 칸 미만입니다.");
            Assert.That(size / (count.x + 1), Is.LessThan(texel.x), $"{facing} x: 텍셀 수를 더 늘릴 수 있습니다.");
            Assert.That(size / (count.y + 1), Is.LessThan(texel.y), $"{facing} y: 텍셀 수를 더 늘릴 수 있습니다.");
        }

        [Test]
        public void FacingCameraTexelIsOneCell()
        {
            Vector2 texel = PixelTexelDensity.TexelWorldSize(PixelSurfaceFacing.FacingCamera);

            Assert.That(texel.x, Is.EqualTo(PixelFilterSettings.CellWorldSize).Within(Tolerance));
            Assert.That(texel.y, Is.EqualTo(PixelFilterSettings.CellWorldSize).Within(Tolerance));
        }

        [Test]
        public void WallTexelIsTallerThanFloorTexelAndBothTallerThanOneCell()
        {
            float floorY = PixelTexelDensity.TexelWorldSize(PixelSurfaceFacing.Floor).y;
            float wallY = PixelTexelDensity.TexelWorldSize(PixelSurfaceFacing.Wall).y;

            Assert.That(floorY, Is.GreaterThan(PixelFilterSettings.CellWorldSize), "바닥은 세로로 줄어 보여 텍셀이 더 길어야 합니다.");
            Assert.That(wallY, Is.GreaterThan(floorY), "벽은 바닥보다 더 많이 줄어 보여 텍셀이 더 길어야 합니다.");
        }

        [Test]
        public void TinySizeStillGetsOneTexel(
            [Values(PixelSurfaceFacing.FacingCamera, PixelSurfaceFacing.Floor, PixelSurfaceFacing.Wall)] PixelSurfaceFacing facing)
        {
            Vector2Int count = PixelTexelDensity.TexelCountFor(new Vector2(0.01f, 0.01f), facing);

            Assert.That(count, Is.EqualTo(new Vector2Int(1, 1)), $"{facing}");
        }
    }
}
