using NUnit.Framework;
using Tessera.Rendering;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 픽셀 가독성 지표를 합성 입력으로 검증한다(M10.8-T8).
    ///
    /// 지표 자체가 판단 근거이므로, 지표가 틀리면 M10.6의 "고유 색 수" 오판을 반복한다.
    /// </summary>
    [TestFixture]
    public sealed class PixelReadabilityMetricsTests
    {
        [Test]
        public void 계단화된_그림은_밴드가_몇_개로_잡힌다()
        {
            Color32[] banded = { Gray(60), Gray(60), Gray(140), Gray(140), Gray(230) };

            Assert.That(PixelReadabilityMetrics.CountLuminanceBands(banded), Is.EqualTo(3));
        }

        [Test]
        public void 연속_그라데이션은_밴드가_많이_잡힌다()
        {
            Color32[] gradient = new Color32[256];
            for (int index = 0; index < gradient.Length; index++) gradient[index] = Gray((byte)index);

            int bands = PixelReadabilityMetrics.CountLuminanceBands(gradient);

            // 셀 셰이딩 이전 화면이 여기 해당한다. 계단화된 그림과 확실히 갈려야 지표로 쓸 수 있다.
            Assert.That(bands, Is.GreaterThan(30));
        }

        [Test]
        public void 한_색으로_채운_그림은_최대_영역이_전체다()
        {
            Color32[] flat = new Color32[16];
            for (int index = 0; index < flat.Length; index++) flat[index] = Gray(120);

            Assert.That(PixelReadabilityMetrics.LargestUniformRegionRatio(flat, 4, 4), Is.EqualTo(1f).Within(0.001f));
        }

        [Test]
        public void 체커보드는_최대_영역이_한_칸이다()
        {
            // 디더 무늬가 여기 해당한다. 색 수는 둘뿐이어도 평면이 없으므로 픽셀아트로 읽히지 않는다.
            Color32[] checker = new Color32[16];
            for (int index = 0; index < checker.Length; index++)
            {
                int x = index % 4;
                int y = index / 4;
                checker[index] = (x + y) % 2 == 0 ? Gray(20) : Gray(220);
            }

            float ratio = PixelReadabilityMetrics.LargestUniformRegionRatio(checker, 4, 4);

            Assert.That(ratio, Is.EqualTo(1f / 16f).Within(0.001f));
        }

        [Test]
        public void 반쪽만_바뀐_프레임은_변화율이_절반이다()
        {
            Color32[] previous = new Color32[10];
            Color32[] current = new Color32[10];
            for (int index = 0; index < previous.Length; index++)
            {
                previous[index] = Gray(10);
                current[index] = index < 5 ? Gray(10) : Gray(200);
            }

            Assert.That(PixelReadabilityMetrics.ChangedCellRatio(previous, current), Is.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void 크기가_다른_프레임은_0을_준다()
        {
            Color32[] previous = new Color32[4];
            Color32[] current = new Color32[8];

            Assert.That(PixelReadabilityMetrics.ChangedCellRatio(previous, current), Is.EqualTo(0f));
        }

        private static Color32 Gray(byte level)
        {
            return new Color32(level, level, level, 255);
        }
    }
}
