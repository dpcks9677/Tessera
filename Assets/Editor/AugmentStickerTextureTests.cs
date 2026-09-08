using System;
using NUnit.Framework;
using Tessera.Games.AugmentedYacht;
using UnityEngine;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 변형 증강 우표 스티커의 절차 생성을 고정하는 테스트입니다.
    /// 좌표를 박아 두면 톱니 간격을 조정할 때마다 깨지므로 기하 구조만 검사합니다.
    /// 시각 사양은 <c>docs/augmented_yacht_m17_vfx_spec.md</c> §3.1.1입니다.
    /// </summary>
    [TestFixture]
    public sealed class AugmentStickerTextureTests
    {
        private const int Size = AugmentStickerTexture.DefaultSize;

        private static readonly Color32 Indigo = new(0x36, 0x4b, 0x6e, 0xff);

        [Test]
        public void 같은_인자면_항상_같은_픽셀이_나온다()
        {
            Color32[] first = Pixels(AugmentStickerTexture.DefaultBase);
            Color32[] second = Pixels(AugmentStickerTexture.DefaultBase);

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void 네_변에_톱니가_같은_개수로_생긴다()
        {
            Color32[] pixels = Pixels(AugmentStickerTexture.DefaultBase);

            int bottom = TransparentRuns(pixels, i => i);
            int top = TransparentRuns(pixels, i => ((Size - 1) * Size) + i);
            int left = TransparentRuns(pixels, i => i * Size);
            int right = TransparentRuns(pixels, i => (i * Size) + Size - 1);

            Assert.That(bottom, Is.GreaterThan(1), "톱니가 하나도 없으면 우표로 읽히지 않습니다.");
            Assert.That(new[] { top, left, right }, Is.All.EqualTo(bottom));
        }

        [Test]
        public void 톱니_사이에_몸통이_남는다()
        {
            Color32[] pixels = Pixels(AugmentStickerTexture.DefaultBase);

            int body = 0;
            for (int x = 0; x < Size; x++) if (pixels[x].a != 0) body++;

            Assert.That(body, Is.GreaterThanOrEqualTo(Size / 3),
                "톱니가 너무 깊으면 우표가 아니라 톱니바퀴로 보입니다.");
        }

        [Test]
        public void 네_귀퉁이는_뚫리지_않는다()
        {
            Color32[] pixels = Pixels(AugmentStickerTexture.DefaultBase);

            Assert.That(pixels[0].a, Is.Not.Zero);
            Assert.That(pixels[Size - 1].a, Is.Not.Zero);
            Assert.That(pixels[(Size - 1) * Size].a, Is.Not.Zero);
            Assert.That(pixels[(Size * Size) - 1].a, Is.Not.Zero);
        }

        [Test]
        public void 좌우와_상하로_대칭이다()
        {
            Color32[] pixels = Pixels(AugmentStickerTexture.DefaultBase);

            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    Assert.That(pixels[(y * Size) + x], Is.EqualTo(pixels[(y * Size) + Size - 1 - x]),
                        $"좌우 대칭이 깨졌습니다: ({x}, {y})");
                    Assert.That(pixels[(y * Size) + x], Is.EqualTo(pixels[((Size - 1 - y) * Size) + x]),
                        $"상하 대칭이 깨졌습니다: ({x}, {y})");
                }
            }
        }

        [Test]
        public void 내부_금테는_끊김_없는_사각_링이고_안쪽은_비어_있다()
        {
            Color32[] pixels = Pixels(AugmentStickerTexture.DefaultBase);
            Color32 gold = AugmentStickerTexture.DefaultBorder;

            int inset = -1;
            for (int i = 0; i < Size && inset < 0; i++)
                if (IsSame(pixels[(i * Size) + (Size / 2)], gold)) inset = i;
            Assert.That(inset, Is.GreaterThan(0), "금테를 찾지 못했습니다.");

            int far = Size - 1 - inset;
            for (int i = inset; i <= far; i++)
            {
                Assert.That(IsSame(pixels[(inset * Size) + i], gold), Is.True, $"아래 링이 끊겼습니다: x={i}");
                Assert.That(IsSame(pixels[(far * Size) + i], gold), Is.True, $"위 링이 끊겼습니다: x={i}");
                Assert.That(IsSame(pixels[(i * Size) + inset], gold), Is.True, $"왼쪽 링이 끊겼습니다: y={i}");
                Assert.That(IsSame(pixels[(i * Size) + far], gold), Is.True, $"오른쪽 링이 끊겼습니다: y={i}");
            }

            Assert.That(IsSame(pixels[((Size / 2) * Size) + (Size / 2)], gold), Is.False,
                "링 안쪽은 증강 문양 자리라 비어 있어야 합니다.");
        }

        [Test]
        public void 색상코드를_바꿔도_실루엣과_금테는_그대로다()
        {
            Color32[] burgundy = Pixels(AugmentStickerTexture.DefaultBase);
            Color32[] indigo = Pixels(Indigo);
            Color32 gold = AugmentStickerTexture.DefaultBorder;

            int goldCount = 0;
            int bodyDiff = 0;
            for (int i = 0; i < burgundy.Length; i++)
            {
                Assert.That(indigo[i].a == 0, Is.EqualTo(burgundy[i].a == 0), $"톱니 실루엣이 달라졌습니다: {i}");

                if (IsSame(burgundy[i], gold))
                {
                    goldCount++;
                    Assert.That(IsSame(indigo[i], gold), Is.True, $"금테 픽셀이 변했습니다: {i}");
                }
                else if (burgundy[i].a != 0 && !IsSame(burgundy[i], indigo[i]))
                {
                    bodyDiff++;
                }
            }

            Assert.That(goldCount, Is.GreaterThan(0));
            Assert.That(bodyDiff, Is.GreaterThan(0), "베이스 색을 바꿨는데 몸통이 그대로면 색상코드가 먹지 않은 것입니다.");
        }

        [Test]
        public void 가로로_긴_칸_크기에서도_구조가_유지된다()
        {
            Color32[] pixels = AugmentStickerTexture.CreatePixels(
                AugmentStickerTexture.DefaultBase, AugmentStickerTexture.DefaultBorder, 240, 52);

            Assert.That(pixels.Length, Is.EqualTo(240 * 52));
            Assert.That(pixels[0].a, Is.Not.Zero, "귀퉁이는 크기와 무관하게 유지돼야 합니다.");
            Assert.That(pixels[239].a, Is.Not.Zero);
        }

        private static Color32[] Pixels(Color32 baseColor) =>
            AugmentStickerTexture.CreatePixels(baseColor, AugmentStickerTexture.DefaultBorder, Size, Size);

        private static bool IsSame(Color32 left, Color32 right) =>
            left.r == right.r && left.g == right.g && left.b == right.b && left.a == right.a;

        private static int TransparentRuns(Color32[] pixels, Func<int, int> index)
        {
            int runs = 0;
            bool inRun = false;
            for (int i = 0; i < Size; i++)
            {
                bool hole = pixels[index(i)].a == 0;
                if (hole && !inRun) runs++;
                inRun = hole;
            }
            return runs;
        }
    }
}
