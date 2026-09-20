using NUnit.Framework;
using Tessera.Games.AugmentedYacht;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 변형 증강 러너 스티커의 절차 생성을 고정하는 테스트입니다.
    ///
    /// 이 스티커는 한 텍셀이 픽셀 필터 화면의 한 픽셀이라는 전제로 그려집니다. 그래서 금테가
    /// 몸통 가장자리에서 정확히 몇 텍셀 안쪽인지, 그림자가 정확히 몇 텍셀 밀렸는지가 곧 화면
    /// 결과입니다. 좌표를 세는 테스트가 여기서는 과하지 않습니다.
    /// 시각 사양은 <c>docs/augmented_yacht_m17_vfx_spec.md</c> §3.1.1입니다.
    /// </summary>
    [TestFixture]
    public sealed class AugmentStickerTextureTests
    {
        private const int Size = AugmentStickerTexture.DefaultSize;
        private const int Offset = AugmentStickerTexture.ShadowOffset;
        private const int Inset = AugmentStickerTexture.BorderInset;

        private static readonly Color32 Indigo = new(0x36, 0x4b, 0x6e, 0xff);

        [Test]
        public void SameArgumentsAlwaysProduceSamePixels()
        {
            Color32[] first = Pixels(AugmentStickerTexture.DefaultBase);
            Color32[] second = Pixels(AugmentStickerTexture.DefaultBase);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void BodyIsSolidRectangleWithoutJaggedEdges()
        {
            Color32[] pixels = Pixels(AugmentStickerTexture.DefaultBase);

            for (int y = Offset; y < Size; y++)
            {
                for (int x = Offset; x < Size; x++)
                {
                    Assert.That(pixels[(y * Size) + x].a, Is.Not.Zero, $"몸통에 구멍이 있습니다: ({x}, {y})");
                }
            }
        }

        [Test]
        public void ShadowOffsetsOneCellTowardSingleCorner()
        {
            Color32[] pixels = Pixels(AugmentStickerTexture.DefaultBase);
            Color32 shadow = AugmentStickerTexture.DefaultShadow;

            // 텍스처 왼쪽 끝 열과 아래 끝 행이 그림자다(화면에서는 오른쪽·아래).
            for (int y = 0; y <= Size - 1 - Offset; y++)
                Assert.That(IsSame(pixels[(y * Size)], shadow), Is.True, $"한쪽 그림자가 빠졌습니다: y={y}");
            for (int x = 0; x <= Size - 1 - Offset; x++)
                Assert.That(IsSame(pixels[x], shadow), Is.True, $"다른 쪽 그림자가 빠졌습니다: x={x}");

            // 반대쪽 두 변에는 그림자가 없다. 있으면 사방으로 번진 테두리지 그림자가 아니다.
            for (int y = Offset; y < Size; y++)
                Assert.That(IsSame(pixels[(y * Size) + Size - 1], shadow), Is.False, $"반대쪽에 그림자가 생겼습니다: y={y}");
            for (int x = Offset; x < Size; x++)
                Assert.That(IsSame(pixels[((Size - 1) * Size) + x], shadow), Is.False, $"반대쪽에 그림자가 생겼습니다: x={x}");
        }

        [Test]
        public void OnlyTwoCornersOutsideShadowAreEmpty()
        {
            Color32[] pixels = Pixels(AugmentStickerTexture.DefaultBase);

            int empty = 0;
            foreach (Color32 pixel in pixels) if (pixel.a == 0) empty++;

            Assert.That(empty, Is.EqualTo(2 * Offset * Offset),
                "몸통을 밀어 만든 그림자라 어긋난 두 귀퉁이 말고는 빌 곳이 없습니다.");
            Assert.That(pixels[0].a, Is.Not.Zero, "밀린 쪽 귀퉁이는 그림자가 채웁니다.");
            Assert.That(pixels[(Size * Size) - 1].a, Is.Not.Zero, "반대쪽 귀퉁이는 몸통이 채웁니다.");
        }

        [Test]
        public void GoldBorderSitsFixedCellsInsideBodyEdge()
        {
            Color32[] pixels = Pixels(AugmentStickerTexture.DefaultBase);
            Color32 gold = AugmentStickerTexture.DefaultBorder;

            int lineX0 = Offset + Inset;
            int lineX1 = Size - 1 - Inset;
            int lineY0 = Offset + Inset;
            int lineY1 = Size - 1 - Inset;

            for (int x = lineX0; x <= lineX1; x++)
            {
                Assert.That(IsSame(pixels[(lineY0 * Size) + x], gold), Is.True, $"아래 금테가 끊겼습니다: x={x}");
                Assert.That(IsSame(pixels[(lineY1 * Size) + x], gold), Is.True, $"위 금테가 끊겼습니다: x={x}");
            }
            for (int y = lineY0; y <= lineY1; y++)
            {
                Assert.That(IsSame(pixels[(y * Size) + lineX0], gold), Is.True, $"왼쪽 금테가 끊겼습니다: y={y}");
                Assert.That(IsSame(pixels[(y * Size) + lineX1], gold), Is.True, $"오른쪽 금테가 끊겼습니다: y={y}");
            }

            Assert.That(IsSame(pixels[((Size / 2) * Size) + (Size / 2)], gold), Is.False,
                "링 안쪽은 증강 문양 자리라 비어 있어야 합니다.");
        }

        [Test]
        public void ChangingColorCodeKeepsGoldBorderAndShadow()
        {
            Color32[] burgundy = Pixels(AugmentStickerTexture.DefaultBase);
            Color32[] indigo = Pixels(Indigo);
            Color32 gold = AugmentStickerTexture.DefaultBorder;
            Color32 shadow = AugmentStickerTexture.DefaultShadow;

            int goldCount = 0;
            int shadowCount = 0;
            int bodyDiff = 0;
            for (int i = 0; i < burgundy.Length; i++)
            {
                Assert.That(indigo[i].a == 0, Is.EqualTo(burgundy[i].a == 0), $"실루엣이 달라졌습니다: {i}");

                if (IsSame(burgundy[i], gold))
                {
                    goldCount++;
                    Assert.That(IsSame(indigo[i], gold), Is.True, $"금테 픽셀이 변했습니다: {i}");
                }
                else if (IsSame(burgundy[i], shadow))
                {
                    shadowCount++;
                    Assert.That(IsSame(indigo[i], shadow), Is.True, $"그림자 픽셀이 변했습니다: {i}");
                }
                else if (burgundy[i].a != 0 && !IsSame(burgundy[i], indigo[i]))
                {
                    bodyDiff++;
                }
            }

            Assert.That(goldCount, Is.GreaterThan(0));
            Assert.That(shadowCount, Is.GreaterThan(0));
            Assert.That(bodyDiff, Is.GreaterThan(0), "베이스 색을 바꿨는데 몸통이 그대로면 색상코드가 먹지 않은 것입니다.");
        }

        [Test]
        public void StructureHoldsAtWideRealCellSize()
        {
            const int width = 59;
            const int height = 15;
            Color32[] pixels = AugmentStickerTexture.CreatePixels(
                AugmentStickerTexture.DefaultBase, AugmentStickerTexture.DefaultBorder, width, height);
            Color32 gold = AugmentStickerTexture.DefaultBorder;

            Assert.That(pixels.Length, Is.EqualTo(width * height));

            // 금테 두께는 칸이 가로로 길어져도 한 줄이다. 늘어나면 픽셀 필터에서 뭉개진다.
            int column = width / 2;
            int goldRows = 0;
            for (int y = 0; y < height; y++) if (IsSame(pixels[(y * width) + column], gold)) goldRows++;
            Assert.That(goldRows, Is.EqualTo(2), "세로로 자르면 금테는 위아래 한 줄씩이어야 합니다.");
        }

        [Test]
        public void RequestBelowMinimumSizeClampsToMinimum()
        {
            // 8보다 작게 부르면 8로 올린다. 그래야 그림자 한 칸과 금테 한 줄이 들어간다.
            Color32[] pixels = AugmentStickerTexture.CreatePixels(
                AugmentStickerTexture.DefaultBase, AugmentStickerTexture.DefaultBorder, 1, 1);

            Assert.That(pixels.Length, Is.EqualTo(8 * 8));
        }

        private static Color32[] Pixels(Color32 baseColor) =>
            AugmentStickerTexture.CreatePixels(baseColor, AugmentStickerTexture.DefaultBorder, Size, Size);

        private static bool IsSame(Color32 left, Color32 right) =>
            left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a;
    }
}
