using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// M7.5-R2 이관 전에 변형 증강 18종의 현재 동작을 고정하는 특성화 테스트입니다.
    /// 기존 <see cref="YachtGameRulesTests"/>는 성공 사례만 한 줄씩 검증하므로
    /// 여기서는 실패·경계 분기와 대상 칸 밖으로 영향이 새지 않는지를 확인합니다.
    /// </summary>
    [TestFixture]
    public sealed class YachtModificationAugmentTests
    {
        [Test]
        public void LuckySevens_GivesFifteenPointsOnlyAtSumSevenBoundary()
        {
            Assert.That(Score(YachtAugmentRuntime.LuckySevensId, ScoreCategory.Aces, 1, 1, 1, 2, 2), Is.EqualTo(15));
            Assert.That(Score(YachtAugmentRuntime.LuckySevensId, ScoreCategory.Aces, 2, 3, 4, 4, 4), Is.EqualTo(15));
            Assert.That(Score(YachtAugmentRuntime.LuckySevensId, ScoreCategory.Aces, 6, 6, 6, 6, 3), Is.EqualTo(15));
            Assert.That(Score(YachtAugmentRuntime.LuckySevensId, ScoreCategory.Aces, 1, 1, 2, 2, 2), Is.Zero);
        }

        [Test]
        public void LuckySevens_ReplacesTargetCategoryEvenWithoutAnyOne()
        {
            Assert.That(Score(YachtAugmentRuntime.LuckySevensId, ScoreCategory.Aces, 2, 3, 4, 4, 4), Is.EqualTo(15));
        }

        [Test]
        public void PerfectSquare_GivesTwelvePointsOnlyWhenSumIsPerfectSquare()
        {
            Assert.That(Score(YachtAugmentRuntime.PerfectSquaresId, ScoreCategory.Aces, 1, 1, 1, 3, 3), Is.EqualTo(12));
            Assert.That(Score(YachtAugmentRuntime.PerfectSquaresId, ScoreCategory.Aces, 2, 2, 4, 4, 4), Is.EqualTo(12));
            Assert.That(Score(YachtAugmentRuntime.PerfectSquaresId, ScoreCategory.Aces, 6, 6, 6, 6, 1), Is.EqualTo(12));
            Assert.That(Score(YachtAugmentRuntime.PerfectSquaresId, ScoreCategory.Aces, 1, 1, 2, 3, 3), Is.Zero);
        }

        [Test]
        public void Gambler_AddsSevenToSumFromTwentyFour()
        {
            Assert.That(Score(YachtAugmentRuntime.GamblerId, ScoreCategory.Choice, 4, 4, 4, 6, 6), Is.EqualTo(31));
            Assert.That(Score(YachtAugmentRuntime.GamblerId, ScoreCategory.Choice, 3, 4, 4, 6, 6), Is.Zero);
        }

        [Test]
        public void ThreeOfAKind_ScoresZeroBelowThreeMatchingDice()
        {
            Assert.That(Score(YachtAugmentRuntime.ThreeOfAKindId, ScoreCategory.FourOfAKind, 2, 2, 2, 4, 5), Is.EqualTo(15));
            Assert.That(Score(YachtAugmentRuntime.ThreeOfAKindId, ScoreCategory.FourOfAKind, 2, 2, 2, 2, 5), Is.EqualTo(13));
            Assert.That(Score(YachtAugmentRuntime.ThreeOfAKindId, ScoreCategory.FourOfAKind, 2, 2, 4, 5, 6), Is.Zero);
        }

        [Test]
        public void TinyHouse_ScoresZeroWhenAnyDieIsFiveOrMore()
        {
            Assert.That(Score(YachtAugmentRuntime.TinyHouseId, ScoreCategory.FullHouse, 1, 1, 2, 2, 2), Is.EqualTo(28));
            Assert.That(Score(YachtAugmentRuntime.TinyHouseId, ScoreCategory.FullHouse, 1, 1, 5, 5, 5), Is.Zero);
        }

        [Test]
        public void TinyHouse_AcceptsFiveMatchingDiceOneToFourAsFullHouse()
        {
            // 공통 계산에서 풀하우스는 야추를 포함하므로 4가 다섯 개여도 28점이 된다.
            Assert.That(Score(YachtAugmentRuntime.TinyHouseId, ScoreCategory.FullHouse, 4, 4, 4, 4, 4), Is.EqualTo(28));
        }

        [Test]
        public void TwoPair_ScoresZeroBelowTwoDistinctPairs()
        {
            Assert.That(Score(YachtAugmentRuntime.TwoPairId, ScoreCategory.FullHouse, 4, 4, 4, 4, 1), Is.EqualTo(15));
            Assert.That(Score(YachtAugmentRuntime.TwoPairId, ScoreCategory.FullHouse, 2, 2, 2, 3, 3), Is.EqualTo(15));
            Assert.That(Score(YachtAugmentRuntime.TwoPairId, ScoreCategory.FullHouse, 2, 2, 3, 4, 5), Is.Zero);
        }

        [Test]
        public void HeadAndBody_ScoresZeroWithoutThreeConsecutiveAfterRemovingPair()
        {
            Assert.That(Score(YachtAugmentRuntime.HeadAndTailId, ScoreCategory.FullHouse, 1, 1, 2, 3, 4), Is.EqualTo(21));
            Assert.That(Score(YachtAugmentRuntime.HeadAndTailId, ScoreCategory.FullHouse, 1, 1, 2, 2, 5), Is.Zero);
        }

        [Test]
        public void Evens_ScoresZeroIfAnyOddDieExists()
        {
            Assert.That(Score(YachtAugmentRuntime.EvensId, ScoreCategory.SmallStraight, 2, 2, 4, 4, 6), Is.EqualTo(20));
            Assert.That(Score(YachtAugmentRuntime.EvensId, ScoreCategory.SmallStraight, 2, 2, 4, 4, 5), Is.Zero);
        }

        [Test]
        public void Odds_ScoresZeroIfAnyEvenDieExists()
        {
            Assert.That(Score(YachtAugmentRuntime.OddsId, ScoreCategory.SmallStraight, 1, 3, 5, 7, 7), Is.EqualTo(20));
            Assert.That(Score(YachtAugmentRuntime.OddsId, ScoreCategory.SmallStraight, 1, 2, 3, 5, 7), Is.Zero);
        }

        [Test]
        public void DoubleLargeStraight_ScoresZeroForSmallStraightAlone()
        {
            Assert.That(Score(YachtAugmentRuntime.DoubleLargeStraightId, ScoreCategory.SmallStraight, 1, 2, 3, 4, 5), Is.EqualTo(30));
            Assert.That(Score(YachtAugmentRuntime.DoubleLargeStraightId, ScoreCategory.SmallStraight, 1, 2, 3, 4, 4), Is.Zero);
        }

        [Test]
        public void PrimeCollection_NeedsTwoThreeAndFiveForThirtyFivePoints()
        {
            Assert.That(Score(YachtAugmentRuntime.PrimeCollectionId, ScoreCategory.LargeStraight, 2, 3, 5, 7, 7), Is.EqualTo(35));
            Assert.That(Score(YachtAugmentRuntime.PrimeCollectionId, ScoreCategory.LargeStraight, 2, 2, 3, 3, 7), Is.Zero);
            Assert.That(Score(YachtAugmentRuntime.PrimeCollectionId, ScoreCategory.LargeStraight, 2, 3, 4, 5, 7), Is.Zero);
        }

        [Test]
        public void PeanutHouse_ScoresZeroWhenTwoValuesAreNotAdjacent()
        {
            Assert.That(Score(YachtAugmentRuntime.DuplexHouseId, ScoreCategory.LargeStraight, 2, 2, 3, 3, 3), Is.EqualTo(35));
            Assert.That(Score(YachtAugmentRuntime.DuplexHouseId, ScoreCategory.LargeStraight, 2, 2, 2, 4, 4), Is.Zero);
        }

        [Test]
        public void Mountain_NeedsExactlyTwoThroughSixForFortyPoints()
        {
            Assert.That(Score(YachtAugmentRuntime.MountainId, ScoreCategory.LargeStraight, 2, 3, 4, 5, 6), Is.EqualTo(40));
            Assert.That(Score(YachtAugmentRuntime.MountainId, ScoreCategory.LargeStraight, 1, 2, 3, 4, 5), Is.Zero);
        }

        [Test]
        public void HighDice_ScoresZeroBelowSumTwentySix()
        {
            Assert.That(Score(YachtAugmentRuntime.HighDiceId, ScoreCategory.LargeStraight, 4, 5, 5, 6, 6), Is.EqualTo(35));
            Assert.That(Score(YachtAugmentRuntime.HighDiceId, ScoreCategory.LargeStraight, 4, 4, 5, 6, 6), Is.Zero);
            Assert.That(Score(YachtAugmentRuntime.HighDiceId, ScoreCategory.LargeStraight, 3, 6, 6, 6, 6), Is.Zero);
        }

        [Test]
        public void SecondChoice_FloorsHalfOfSum()
        {
            Assert.That(Score(YachtAugmentRuntime.SecondChoiceId, ScoreCategory.Yacht, 1, 2, 3, 4, 5), Is.EqualTo(7));
            Assert.That(Score(YachtAugmentRuntime.SecondChoiceId, ScoreCategory.Yacht, 1, 2, 3, 4, 6), Is.EqualTo(8));
        }

        [Test]
        public void Fibonacci_NeedsExactCompositionForTwentyFivePoints()
        {
            Assert.That(Score(YachtAugmentRuntime.FibonacciId, ScoreCategory.Yacht, 1, 1, 2, 3, 5), Is.EqualTo(25));
            Assert.That(Score(YachtAugmentRuntime.FibonacciId, ScoreCategory.Yacht, 1, 2, 3, 5, 5), Is.Zero);
        }

        [Test]
        public void ReverseChoice_TurnsNegativeWhenSumExceedsThirty()
        {
            Assert.That(Score(YachtAugmentRuntime.ReverseChoiceId, ScoreCategory.Yacht, 7, 7, 7, 7, 7), Is.EqualTo(-5));
            Assert.That(Score(YachtAugmentRuntime.ReverseChoiceId, ScoreCategory.Yacht, 6, 6, 6, 6, 6), Is.Zero);
            Assert.That(Score(YachtAugmentRuntime.ReverseChoiceId, ScoreCategory.Yacht, 1, 1, 1, 1, 1), Is.EqualTo(25));
        }

        [Test]
        public void Blackjack21_NeedsSumExactlyTwentyOneForTwentyOnePoints()
        {
            Assert.That(Score(YachtAugmentRuntime.BlackjackId, ScoreCategory.Yacht, 3, 4, 4, 5, 5), Is.EqualTo(21));
            Assert.That(Score(YachtAugmentRuntime.BlackjackId, ScoreCategory.Yacht, 3, 4, 4, 5, 6), Is.Zero);
            Assert.That(Score(YachtAugmentRuntime.BlackjackId, ScoreCategory.Yacht, 1, 2, 3, 4, 5), Is.Zero);
        }

        [Test]
        public void ModificationAugmentDoesNotChangeCategoriesOtherThanTarget()
        {
            Dictionary<ScoreCategory, int> withAugment = ScoresWith(YachtAugmentRuntime.LuckySevensId, 1, 1, 1, 2, 2);
            Dictionary<ScoreCategory, int> withoutAugment = ScoresWith(null, 1, 1, 1, 2, 2);

            Assert.That(withAugment[ScoreCategory.Aces], Is.EqualTo(15));
            Assert.That(withoutAugment[ScoreCategory.Aces], Is.EqualTo(3));
            foreach (ScoreCategory category in YachtScoreCalculator.ScorableCategories)
            {
                if (category == ScoreCategory.Aces) continue;
                Assert.That(withAugment[category], Is.EqualTo(withoutAugment[category]), $"{category}가 변형 증강에 영향을 받았습니다.");
            }
        }

        [Test]
        public void WithoutModificationAugmentBaseCategoryIsComputed()
        {
            Dictionary<ScoreCategory, int> scores = ScoresWith(null, 1, 1, 1, 2, 2);

            Assert.That(scores[ScoreCategory.Aces], Is.EqualTo(3));
            Assert.That(scores[ScoreCategory.Deuces], Is.EqualTo(4));
            Assert.That(scores[ScoreCategory.FullHouse], Is.EqualTo(7));
            Assert.That(scores[ScoreCategory.Yacht], Is.Zero);
        }

        [Test]
        public void DoubleLargeStraight_LowersOwnerUpperBonusThresholdToSixty()
        {
            var runtime = new YachtAugmentRuntime();
            YachtGameState state = CreateDraftState(runtime);
            state.Draft.Options = new[] { YachtAugmentRuntime.DoubleLargeStraightId };

            bool selected = runtime.TrySelectAugment(
                state, 0, YachtAugmentRuntime.DoubleLargeStraightId, new SequenceRandom(0),
                out _, out _, out _);

            Assert.That(selected, Is.True);
            Assert.That(state.Players[0].upperBonusThreshold, Is.EqualTo(60));
            Assert.That(state.Players[1].upperBonusThreshold, Is.EqualTo(63), "상대의 기준은 바뀌지 않아야 합니다.");
        }

        private static YachtGameState CreateDraftState(YachtAugmentRuntime runtime)
        {
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = 1,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() },
                Dice = Dice(1, 2, 3, 4, 5)
            };
            runtime.Initialize(state, 2);
            state.Draft.SelectionCounts = new[] { 0, 0 };
            Assert.That(runtime.TryBeginDraft(state, new SequenceRandom(0), out _), Is.True);
            return state;
        }

        private static int Score(string augmentId, ScoreCategory category, params int[] values) =>
            ScoresWith(augmentId, values)[category];

        /// <summary>증강을 보유한 상태에서 실제 점수 계산 경로를 그대로 타 족보 점수를 얻습니다.</summary>
        private static Dictionary<ScoreCategory, int> ScoresWith(string augmentId, params int[] values)
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = 1,
                Players = new[] { new PlayerScoreData() },
                Dice = Dice(values)
            };
            runtime.Initialize(state, 1);
            if (augmentId != null) state.AugmentPlayers[0].OwnedIds = new[] { augmentId };
            return runtime.CalculateScores(state, 0, state.Dice);
        }

        private static YachtDieState[] Dice(params int[] values)
        {
            var dice = new YachtDieState[values.Length];
            for (int i = 0; i < values.Length; i++) dice[i] = new YachtDieState { Id = i + 1, Value = values[i] };
            return dice;
        }

        private sealed class SequenceRandom : IRandomSource
        {
            private readonly int[] values;
            private int index;

            public SequenceRandom(params int[] values)
            {
                this.values = values.Length > 0 ? values : new[] { 0 };
            }

            public int NextInt(int minInclusive, int maxExclusive)
            {
                if (maxExclusive <= minInclusive) return minInclusive;
                int value = values[index % values.Length];
                index++;
                return minInclusive + (value % (maxExclusive - minInclusive) + (maxExclusive - minInclusive)) % (maxExclusive - minInclusive);
            }

            public bool NextBool() => NextInt(0, 2) == 1;
        }
    }
}
