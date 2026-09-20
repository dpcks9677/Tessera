using NUnit.Framework;
using Tessera.Games.AugmentedYacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 점수표 여섯 열의 가로 경계를 고정합니다.
    ///
    /// 표는 [P1 아이콘][P1 Categories][P1 점수][P2 점수][P2 Categories][P2 아이콘]이고,
    /// 두 Categories 열이 예산 하나를 나눠 씁니다. 현재 턴인 쪽이 예산을 전부 가져가므로
    /// 반대쪽은 0으로 접히고, 그 사이 값은 전환 중인 상태입니다.
    /// </summary>
    [TestFixture]
    public sealed class ScoreSheetColumnLayoutTests
    {
        private const float Tolerance = 1e-5f;

        private static float[] Bounds(float expandT)
        {
            var bounds = new float[7];
            ParchmentScoreSheet.ResolveColumnBounds(expandT, bounds);
            return bounds;
        }

        [TestCase(0f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void BoundariesNeverMoveBackwardLeftToRight(float expandT)
        {
            float[] bounds = Bounds(expandT);
            for (int i = 1; i < bounds.Length; i++)
            {
                Assert.GreaterOrEqual(bounds[i], bounds[i - 1] - Tolerance, $"경계 {i}가 앞 경계보다 왼쪽입니다.");
            }
        }

        [TestCase(0f)]
        [TestCase(0.25f)]
        [TestCase(0.5f)]
        [TestCase(1f)]
        public void TotalTableWidthIsConstantRegardlessOfExpansion(float expandT)
        {
            float[] bounds = Bounds(expandT);
            Assert.AreEqual(Bounds(0f)[6] - Bounds(0f)[0], bounds[6] - bounds[0], Tolerance);
        }

        [Test]
        public void P1TurnCollapsesP2NameColumnToZero()
        {
            float[] bounds = Bounds(0f);
            Assert.AreEqual(0f, bounds[5] - bounds[4], Tolerance, "P2 이름 열이 남아 있습니다.");
            Assert.Greater(bounds[2] - bounds[1], 0f, "P1 이름 열이 펴지지 않았습니다.");
        }

        [Test]
        public void P2TurnCollapsesP1NameColumnToZero()
        {
            float[] bounds = Bounds(1f);
            Assert.AreEqual(0f, bounds[2] - bounds[1], Tolerance, "P1 이름 열이 남아 있습니다.");
            Assert.Greater(bounds[5] - bounds[4], 0f, "P2 이름 열이 펴지지 않았습니다.");
        }

        [Test]
        public void IconSectorAndScoreColumnsIgnoreExpansionFactor()
        {
            float[] open = Bounds(0f);
            float[] mid = Bounds(0.5f);
            float[] closed = Bounds(1f);

            // 0-1 P1 아이콘, 2-3 P1 점수, 3-4 P2 점수, 5-6 P2 아이콘
            (int, int)[] fixedColumns = { (0, 1), (2, 3), (3, 4), (5, 6) };
            foreach ((int from, int to) in fixedColumns)
            {
                float width = open[to] - open[from];
                Assert.AreEqual(width, mid[to] - mid[from], Tolerance);
                Assert.AreEqual(width, closed[to] - closed[from], Tolerance);
            }
        }

        [Test]
        public void NameColumnBudgetIsSharedByBothPlayers()
        {
            float budget = Bounds(0f)[2] - Bounds(0f)[1];
            foreach (float t in new[] { 0f, 0.3f, 0.5f, 0.8f, 1f })
            {
                float[] bounds = Bounds(t);
                float used = (bounds[2] - bounds[1]) + (bounds[5] - bounds[4]);
                Assert.AreEqual(budget, used, Tolerance, $"펴짐 계수 {t}에서 예산이 어긋납니다.");
            }
        }

        [TestCase(-3f)]
        [TestCase(4f)]
        public void OutOfRangeFactorIsClampedToBothEnds(float expandT)
        {
            float[] clamped = Bounds(expandT);
            float[] expected = Bounds(expandT < 0f ? 0f : 1f);
            for (int i = 0; i < clamped.Length; i++) Assert.AreEqual(expected[i], clamped[i], Tolerance);
        }

        [Test]
        public void ArrayShorterThanExpectedIsLeftUntouched()
        {
            var tooShort = new float[3];
            ParchmentScoreSheet.ResolveColumnBounds(0.5f, tooShort);
            Assert.AreEqual(new[] { 0f, 0f, 0f }, tooShort);

            Assert.DoesNotThrow(() => ParchmentScoreSheet.ResolveColumnBounds(0.5f, null));
        }
    }
}
