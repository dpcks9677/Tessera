using System;
using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    [TestFixture]
    public sealed class YachtQuestAugmentTests
    {
        private YachtGameState state;
        private YachtAugmentRuntime runtime;

        [SetUp]
        public void SetUp()
        {
            runtime = new YachtAugmentRuntime();
            state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = 1,
                CurrentPlayerIndex = 0,
                Players = new[]
                {
                    new PlayerScoreData(),
                    new PlayerScoreData()
                },
                Dice = new[]
                {
                    new YachtDieState { Id = 1, Value = 1 },
                    new YachtDieState { Id = 2, Value = 2 },
                    new YachtDieState { Id = 3, Value = 3 },
                    new YachtDieState { Id = 4, Value = 4 },
                    new YachtDieState { Id = 5, Value = 5 }
                }
            };
            runtime.Initialize(state, 2);
        }

        private void AcquireAugment(string augmentId, int playerIndex = 0)
        {
            state.Mode = YachtGameMode.Augmented;
            state.Phase = YachtGamePhase.Draft;
            state.Draft.IsActive = true;
            state.Draft.PlayerIndex = playerIndex;
            state.Draft.Options = new[] { augmentId };
            state.Draft.SelectionCounts = new int[state.AugmentPlayers.Length];
            var random = new SequenceRandom(0);
            runtime.TrySelectAugment(state, playerIndex, augmentId, random, out _, out _, out _);
            state.Phase = YachtGamePhase.TurnReady;
            state.Draft.IsActive = false;
        }

        [Test]
        public void NoTimeToWaste_GivesFifteenPointsForThreeSingleRollTurnsAndFailsOnReroll()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.NoTimeToWasteId);

            // 1턴째: 1회 굴림으로 성공 (2턴 남음)
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Aces, 1, 1, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].NoTimeRemaining, Is.EqualTo(2));

            // 2턴째: 1회 굴림으로 성공 (1턴 남음)
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Deuces, 2, 2, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].NoTimeRemaining, Is.EqualTo(1));

            // 3턴째: 1회 굴림으로 3연속 성공 -> 15점 지급
            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Threes, 3, 3, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(15));
            Assert.That(state.AugmentPlayers[0].NoTimeRewarded, Is.True);
        }

        [Test]
        public void BountyHunter_RewardsThreeTargetsWithScratchPenaltyApplied()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.BountyHunterId);

            // 첫 턴 준비 시 목표가 설정됨
            runtime.PrepareTurn(state, 0, random, false);
            int target1 = state.AugmentPlayers[0].BountyTargetCategory;
            Assert.That(target1, Is.GreaterThanOrEqualTo(0));

            // 1회차 성공 (득점 있음)
            runtime.AfterScoreCommit(state, 0, 1, (ScoreCategory)target1, 10, 10, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].BountySuccesses, Is.EqualTo(1));

            // 다음 턴 준비 -> 새 목표 배정
            runtime.PrepareTurn(state, 0, random, false);
            int target2 = state.AugmentPlayers[0].BountyTargetCategory;
            // 2회차 성공 (0점 스크래치)
            runtime.AfterScoreCommit(state, 0, 1, (ScoreCategory)target2, 0, 0, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].BountyScratches, Is.EqualTo(1));

            // 다음 턴 준비 -> 3회차 배정 및 성공
            runtime.PrepareTurn(state, 0, random, false);
            int target3 = state.AugmentPlayers[0].BountyTargetCategory;
            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, (ScoreCategory)target3, 10, 10, state.Dice, random);

            // 3회 달성: 15 - (스크래치 1개 * 3) = 12점
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(12));
            Assert.That(state.AugmentPlayers[0].BountyRewarded, Is.True);
        }

        [Test]
        public void StepByStep_LowersUpperThresholdToFiftyEightWhenAcesToSixesFilledInOrder()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.StepByStepId);

            for (int i = 0; i < 6; i++)
            {
                runtime.AfterScoreCommit(state, 0, 1, (ScoreCategory)i, 3 * (i + 1), 3 * (i + 1), state.Dice, random);
            }

            Assert.That(state.AugmentPlayers[0].StepRewarded, Is.True);
            Assert.That(state.Players[0].upperBonusThreshold, Is.EqualTo(58));
        }

        [Test]
        public void FastStraight_GivesFifteenPointsForSmallAndLargeWithinEightTurns()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.FastStraightId);

            state.AugmentPlayers[0].TurnsTaken = 4;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.SmallStraight, 15, 15, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].FastSmallScored, Is.True);
            Assert.That(state.AugmentPlayers[0].FastRewarded, Is.False);

            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.LargeStraight, 30, 30, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(15));
            Assert.That(state.AugmentPlayers[0].FastRewarded, Is.True);
        }

        [Test]
        public void Grit_GivesSevenPointsForFullHouseFromTurnNine()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.HoldoutId);

            // 8턴 이전에는 풀하우스를 쳐도 미지급
            state.AugmentPlayers[0].TurnsTaken = 7;
            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.FullHouse, 20, 20, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore, Is.EqualTo(before));

            // 9턴째(TurnsTaken = 8 -> turnNumber = 9)에 득점 시 지급
            state.AugmentPlayers[0].TurnsTaken = 8;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.FullHouse, 20, 20, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(7));
            Assert.That(state.AugmentPlayers[0].HoldoutRewarded, Is.True);
        }

        [Test]
        public void CautiousStraight_GivesSevenPointsForSmallThenLargeAndFailsIfLargeFirst()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.CautiousStraightId);

            // 스몰 먼저 성공
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.SmallStraight, 15, 15, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].CautiousSmallScored, Is.True);

            // 라지 성공 시 7점 지급
            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.LargeStraight, 30, 30, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(7));
            Assert.That(state.AugmentPlayers[0].CautiousRewarded, Is.True);
        }

        [Test]
        public void EveryLittleCounts_GivesFifteenPointsOnSevenAccumulatedOnes()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.EveryLittleId);

            // 1이 4개인 주사위로 Aces 기입 (4개 인정)
            state.Dice[0].Value = 1;
            state.Dice[1].Value = 1;
            state.Dice[2].Value = 1;
            state.Dice[3].Value = 1;
            state.Dice[4].Value = 2;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Aces, 4, 4, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].EveryLittleCount, Is.EqualTo(4));

            // 다음 턴 1이 3개인 주사위로 FullHouse 기입 (3개 추가 -> 총 7개 도달)
            state.Dice[0].Value = 1;
            state.Dice[1].Value = 1;
            state.Dice[2].Value = 1;
            state.Dice[3].Value = 2;
            state.Dice[4].Value = 2;
            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.FullHouse, 7, 7, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(15));
            Assert.That(state.AugmentPlayers[0].EveryLittleRewarded, Is.True);
        }

        [Test]
        public void EveryLittleCounts_StopsCountingAfterRewardIsGranted()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.EveryLittleId);

            // 1이 다섯 개면 한 번에 7개 문턱을 넘지는 못하므로 두 번에 나눠 도달시킨다.
            for (int i = 0; i < 4; i++) state.Dice[i].Value = 1;
            state.Dice[4].Value = 2;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Aces, 4, 4, state.Dice, random);

            for (int i = 0; i < 3; i++) state.Dice[i].Value = 1;
            state.Dice[3].Value = 2;
            state.Dice[4].Value = 2;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.FullHouse, 7, 7, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].EveryLittleRewarded, Is.True);

            // 보상 이후의 기입은 눈금도 보상도 늘리지 않는다.
            int countAfterReward = state.AugmentPlayers[0].EveryLittleCount;
            int bonusAfterReward = state.Players[0].augmentBonusScore;
            for (int i = 0; i < 5; i++) state.Dice[i].Value = 1;
            runtime.AfterScoreCommit(state, 0, 2, ScoreCategory.Choice, 5, 5, state.Dice, random);

            Assert.That(state.Players[0].augmentBonusScore, Is.EqualTo(bonusAfterReward));
            Assert.That(state.AugmentPlayers[0].EveryLittleCount, Is.EqualTo(countAfterReward));
        }

        [Test]
        public void EveryLittleCounts_DoesNotCountOnesFromScratchedTurn()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.EveryLittleId);

            // 1이 다섯 개여도 기본 점수가 0이면(스크래치) 눈금을 세지 않는다.
            for (int i = 0; i < 5; i++) state.Dice[i].Value = 1;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.LargeStraight, 0, 0, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].EveryLittleCount, Is.EqualTo(0));

            // 같은 주사위라도 점수가 들어간 기입은 정상적으로 센다.
            runtime.AfterScoreCommit(state, 0, 2, ScoreCategory.Aces, 5, 5, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].EveryLittleCount, Is.EqualTo(5));
        }

        [Test]
        public void Copycat_RewardsFillingSameCategoryAsOpponent()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.CopycatId);

            // 상대(P1)가 Choice에 20점을 먼저 기록
            state.Players[1].lowerFilled[(int)ScoreCategory.Choice - 7] = true;
            state.Players[1].lowerBaseScores[(int)ScoreCategory.Choice - 7] = 20;

            // P0가 Choice에 동일하게 20점 기입 -> 즉시 10점 획득!
            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, 20, 20, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(10));
            Assert.That(state.AugmentPlayers[0].CopycatRewarded, Is.True);
        }

        [Test]
        public void Prophet_GivesSevenPointsWhenTargetNumberMatchesForThreeTurns()
        {
            var random = new SequenceRandom(10);
            AcquireAugment(YachtAugmentRuntime.ProphetId);

            // 첫 턴 시작 시 3개 목표 설정
            runtime.PrepareTurn(state, 0, random, false);
            Assert.That(state.AugmentPlayers[0].ProphetTargets.Length, Is.EqualTo(3));
            int hitTarget = state.AugmentPlayers[0].ProphetTargets[0];

            // 목표 숫자와 일치하는 점수 기입 시 7점 지급
            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, hitTarget, hitTarget, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(7));
            Assert.That(state.AugmentPlayers[0].ProphetTurnsRemaining, Is.EqualTo(2));
        }

        [Test]
        public void LastStand_GivesTenPointsForMatchingPreviouslyFilledBaseScore()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.DoublingId);

            // 첫 기입: 15점 (기록됨)
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, 15, 15, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].DoublingRewarded, Is.False);

            // 두 번째 기입: 동일한 기본점수 15점 기입 -> 10점 획득!
            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.FourOfAKind, 15, 15, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(10));
            Assert.That(state.AugmentPlayers[0].DoublingRewarded, Is.True);
        }

        [Test]
        public void Nozdormu_LimitsTurnToFifteenSecondsAndGivesNinePointsAfterTenTurns()
        {
            var random = new SequenceRandom(0);
            state.AugmentPlayers[0].TurnsTaken = 2;
            AcquireAugment(YachtAugmentRuntime.NozdormuId);

            // 목표 턴: 1라운드이므로 5턴
            Assert.That(state.AugmentPlayers[0].NozdormuTargetTurn, Is.EqualTo(5));

            // 턴 제한 시간 15초 적용 확인
            float duration = runtime.GetTurnDuration(state, 0, 60f);
            Assert.That(duration, Is.EqualTo(15f));

            // 5턴 도달 시 커밋 -> 9점 획득
            state.AugmentPlayers[0].TurnsTaken = 4; // turnNumber = 5
            int before = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, 10, 10, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(9));
            Assert.That(state.AugmentPlayers[0].NozdormuRewarded, Is.True);

            // 보상 획득 후 기본 시간(60초)으로 복귀
            Assert.That(runtime.GetTurnDuration(state, 0, 60f), Is.EqualTo(60f));
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
                int range = maxExclusive - minInclusive;
                return minInclusive + (Math.Abs(value) % range);
            }

            public bool NextBool() => NextInt(0, 2) == 1;
        }
    }
}
