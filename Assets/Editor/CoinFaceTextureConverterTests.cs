using NUnit.Framework;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 동전 면 선화 변환(<see cref="CoinFaceTextureConverter"/>)이 가는 선을 지우지 않고,
    /// 도안을 원 안에 중앙 배치·방향 보존하고, 음각 음영을 올바른 쪽에 칠하는지 검증합니다.
    /// 합성 입력만 씁니다(파일 IO 없음).
    /// </summary>
    [TestFixture]
    public sealed class CoinFaceTextureConverterTests
    {
        private const int Size = CoinFaceTextureConverter.PixelPathSize;

        [Test]
        public void OutputContainsOnlyThreeKnownColors()
        {
            Color32[] source = CreateCanvas(200, 200);
            FillRect(source, 200, 40, 40, 160, 160);

            Color32[] output = CoinFaceTextureConverter.Convert(source, 200, 200, Size, new Vector2Int(1, -1));

            Assert.That(output.Length, Is.EqualTo(Size * Size));
            foreach (Color32 pixel in output)
                Assert.That(
                    IsSame(pixel, CoinFaceTextureConverter.InkColor)
                    || IsSame(pixel, CoinFaceTextureConverter.InkShadowColor)
                    || IsSame(pixel, CoinFaceTextureConverter.BackgroundColor),
                    "출력은 배경·잉크·그림자 세 색만 담아야 합니다.");
        }

        [Test]
        public void TransparentInputProducesAllBackground()
        {
            Color32[] source = CreateCanvas(100, 100);

            Color32[] output = CoinFaceTextureConverter.Convert(source, 100, 100, Size, Vector2Int.zero);

            foreach (Color32 pixel in output)
                Assert.That(IsSame(pixel, CoinFaceTextureConverter.BackgroundColor), Is.True, "잉크가 없으면 전부 배경이어야 합니다.");
        }

        [Test]
        public void SinglePixelVerticalLineSurvivesAtDefaultMinStroke()
        {
            Color32[] source = CreateCanvas(1000, 1000);
            FillRect(source, 1000, 500, 100, 500, 899); // 폭 1px, 길이 800px 세로선.

            Color32[] output = CoinFaceTextureConverter.Convert(source, 1000, 1000, Size, Vector2Int.zero);

            AssertEveryInkRowAtLeastTexelsWide(output, 1);
        }

        [Test]
        public void SinglePixelVerticalLineWidensToTwoTexelsWithExplicitMinStroke()
        {
            Color32[] source = CreateCanvas(1000, 1000);
            FillRect(source, 1000, 500, 100, 500, 899); // 폭 1px, 길이 800px 세로선.

            Color32[] output = CoinFaceTextureConverter.Convert(source, 1000, 1000, Size, Vector2Int.zero, minStrokeTexels: 2);

            AssertEveryInkRowAtLeastTexelsWide(output, 2);
        }

        [Test]
        public void SquareFillStaysInsideDesignCircle()
        {
            Color32[] source = CreateCanvas(200, 200);
            FillRect(source, 200, 0, 0, 199, 199);

            Color32[] output = CoinFaceTextureConverter.Convert(source, 200, 200, Size, Vector2Int.zero);

            AssertOutsideDesignCircleIsEmpty(output);
        }

        [Test]
        public void TallRectangleFillStaysInsideDesignCircle()
        {
            Color32[] source = CreateCanvas(80, 320);
            FillRect(source, 80, 0, 0, 79, 319);

            Color32[] output = CoinFaceTextureConverter.Convert(source, 80, 320, Size, Vector2Int.zero);

            AssertOutsideDesignCircleIsEmpty(output);
        }

        [Test]
        public void DesignInCornerOfLargeCanvasCentersInOutput()
        {
            Color32[] source = CreateCanvas(1000, 1000);
            FillRect(source, 1000, 700, 50, 900, 150); // 캔버스 오른쪽 위 구석의 좌우 대칭 도안.

            Color32[] output = CoinFaceTextureConverter.Convert(source, 1000, 1000, Size, Vector2Int.zero);

            (float cx, float cy) = NonBackgroundCentroid(output);
            float center = Size / 2f;
            Assert.That(cx, Is.InRange(center - 2f, center + 2f), "가로 중앙 배치가 어긋났습니다.");
            Assert.That(cy, Is.InRange(center - 2f, center + 2f), "세로 중앙 배치가 어긋났습니다.");
        }

        [Test]
        public void AsymmetricShapePreservesOrientationTowardBottomLeft()
        {
            Color32[] source = CreateCanvas(300, 300);
            FillRect(source, 300, 0, 0, 59, 299);   // 왼쪽 세로 막대.
            FillRect(source, 300, 0, 0, 299, 59);   // 아래쪽 가로 막대. 합쳐서 L자.

            Color32[] output = CoinFaceTextureConverter.Convert(source, 300, 300, Size, Vector2Int.zero);

            (float cx, float cy) = NonBackgroundCentroid(output);
            float center = Size / 2f;
            Assert.That(cx, Is.LessThan(center), "왼쪽으로 치우친 도안인데 출력 무게중심이 왼쪽에 있지 않습니다.");
            Assert.That(cy, Is.LessThan(center), "아래로 치우친 도안인데 출력 무게중심이 아래에 있지 않습니다.");
        }

        [Test]
        public void OpaqueWhiteAndSemiTransparentBlackAreNotInk()
        {
            Color32[] source = CreateCanvas(10, 10);
            source[5] = new Color32(255, 255, 255, 255); // 불투명 흰색.
            source[6] = new Color32(0, 0, 0, 100);        // 반투명 검은색(a<128).

            Color32[] output = CoinFaceTextureConverter.Convert(source, 10, 10, Size, Vector2Int.zero);

            foreach (Color32 pixel in output)
                Assert.That(IsSame(pixel, CoinFaceTextureConverter.BackgroundColor), Is.True,
                    "불투명 흰색과 반투명 검은색은 잉크로 잡히면 안 됩니다.");
        }

        [Test]
        public void CenterSquareShadesEdgesOnTheLightSide()
        {
            Color32[] source = CreateCanvas(200, 200);
            FillRect(source, 200, 0, 0, 199, 199);

            // 빛이 오른쪽 아래(+x, -y)에서 들어온다. 홈의 오른쪽·아래 안쪽 벽은 빛을 등져 그림자가 진다.
            Color32[] output = CoinFaceTextureConverter.Convert(source, 200, 200, Size, new Vector2Int(1, -1));

            for (int y = 0; y < Size; y++)
            {
                int rightmost = -1;
                for (int x = 0; x < Size; x++)
                    if (!IsSame(output[(y * Size) + x], CoinFaceTextureConverter.BackgroundColor)) rightmost = x;
                if (rightmost < 0) continue;

                Assert.That(IsSame(output[(y * Size) + rightmost], CoinFaceTextureConverter.InkShadowColor), Is.True,
                    $"y={y} 행의 오른쪽 끝이 그림자가 아닙니다.");
            }

            for (int x = 0; x < Size; x++)
            {
                int bottommost = -1;
                for (int y = 0; y < Size; y++)
                {
                    if (IsSame(output[(y * Size) + x], CoinFaceTextureConverter.BackgroundColor)) continue;
                    bottommost = y;
                    break;
                }
                if (bottommost < 0) continue;

                Assert.That(IsSame(output[(bottommost * Size) + x], CoinFaceTextureConverter.InkShadowColor), Is.True,
                    $"x={x} 열의 아래쪽 끝이 그림자가 아닙니다.");
            }

            // 가장자리에서 충분히 떨어진 안쪽 텍셀은 이웃도 전부 잉크라 빛을 등질 일이 없다.
            int inner = (Size / 2) - 5;
            Assert.That(IsSame(output[(inner * Size) + inner], CoinFaceTextureConverter.InkColor), Is.True,
                "왼쪽 위 안쪽 텍셀이 그림자로 잘못 칠해졌습니다.");
        }

        [Test]
        public void ZeroLightProducesNoShadow()
        {
            Color32[] source = CreateCanvas(200, 200);
            FillRect(source, 200, 0, 0, 199, 199);

            Color32[] output = CoinFaceTextureConverter.Convert(source, 200, 200, Size, Vector2Int.zero);

            int shadowCount = 0;
            foreach (Color32 pixel in output)
                if (IsSame(pixel, CoinFaceTextureConverter.InkShadowColor)) shadowCount++;

            Assert.That(shadowCount, Is.EqualTo(0), "lightFrom이 (0,0)이면 그림자가 없어야 합니다.");
        }

        private static void AssertEveryInkRowAtLeastTexelsWide(Color32[] output, int minWidth)
        {
            bool anyInk = false;
            for (int y = 0; y < Size; y++)
            {
                int inkInRow = 0;
                for (int x = 0; x < Size; x++)
                    if (!IsSame(output[(y * Size) + x], CoinFaceTextureConverter.BackgroundColor)) inkInRow++;

                if (inkInRow == 0) continue;
                anyInk = true;
                Assert.That(inkInRow, Is.GreaterThanOrEqualTo(minWidth), $"y={y} 행의 선 굵기가 기대보다 얇습니다.");
            }

            Assert.That(anyInk, Is.True, "가는 선이 축소 과정에서 완전히 사라졌습니다.");
        }

        // 이산화 여유(margin)는 정사각·직사각 입력의 모서리가 설계 원 경계에 정확히 맞닿아 생기는
        // 반올림 오차를 흡수한다. 경계를 훨씬 넘어서는 잉크만 실패로 잡는다.
        private static void AssertOutsideDesignCircleIsEmpty(Color32[] output)
        {
            const float marginTexels = 1.5f;
            float radius = (CoinFaceTextureConverter.DesignRadiusRatio * Size / 2f) + marginTexels;
            float center = Size / 2f;

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + 0.5f) - center;
                    float dy = (y + 0.5f) - center;
                    float distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    if (distance <= radius) continue;

                    Assert.That(IsSame(output[(y * Size) + x], CoinFaceTextureConverter.BackgroundColor), Is.True,
                        $"바깥 링에 잉크가 있습니다: ({x}, {y})");
                }
            }
        }

        private static (float x, float y) NonBackgroundCentroid(Color32[] output)
        {
            float sumX = 0f, sumY = 0f;
            int count = 0;
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (IsSame(output[(y * Size) + x], CoinFaceTextureConverter.BackgroundColor)) continue;
                    sumX += x + 0.5f;
                    sumY += y + 0.5f;
                    count++;
                }
            }

            Assert.That(count, Is.GreaterThan(0), "무게중심을 잴 잉크가 없습니다.");
            return (sumX / count, sumY / count);
        }

        private static Color32[] CreateCanvas(int width, int height)
        {
            Color32[] pixels = new Color32[width * height];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(0, 0, 0, 0);
            return pixels;
        }

        private static void FillRect(Color32[] pixels, int width, int x0, int y0, int x1, int y1)
        {
            Color32 ink = new(0, 0, 0, 255);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    pixels[(y * width) + x] = ink;
        }

        private static bool IsSame(Color32 left, Color32 right) =>
            left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a;
    }
}
