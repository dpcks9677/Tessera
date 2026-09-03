using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// M7.5-R3 이관 대상인 강화 증강 11종(주사위 6종 + 상시/특수 5종)의 동작을 고정하는 특성화 테스트입니다.
    /// </summary>
    [TestFixture]
    public sealed class YachtEnhanceAugmentTests
    {
        private YachtAugmentRuntime runtime;
        private YachtGameState state;

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

        #region 주사위 6종

        [Test]
        public void 묵직한주사위_슬롯1개를_Heavy로_배정하고_4에서6_눈금만_나온다()
        {
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.WeightedDiceId };
            runtime.ConfigureDice(state, 0, state.Dice);

            Assert.That(state.Dice[0].Type, Is.EqualTo(YachtDieType.Heavy));
            Assert.That(state.Dice[1].Type, Is.EqualTo(YachtDieType.Normal));

            var random = new SequenceRandom(0, 1, 2, 3, 4, 5);
            for (int i = 0; i < 6; i++)
            {
                int val = runtime.RollValue(state.Dice[0], random, () => 1);
                Assert.That(val, Is.InRange(4, 6));
            }
        }

        [Test]
        public void 황금주사위_슬롯1개를_Golden으로_배정하고_1에서3일때_2점보너스를준다()
        {
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.GoldenDieId };
            runtime.ConfigureDice(state, 0, state.Dice);

            Assert.That(state.Dice[0].Type, Is.EqualTo(YachtDieType.Golden));

            state.Dice[0].Value = 2;
            int bonusWhen2 = YachtAugmentScoreEngine.CalculateDiceBonus(state.Dice);
            Assert.That(bonusWhen2, Is.EqualTo(2));

            state.Dice[0].Value = 4;
            int bonusWhen4 = YachtAugmentScoreEngine.CalculateDiceBonus(state.Dice);
            Assert.That(bonusWhen4, Is.Zero);
        }

        [Test]
        public void 팔면주사위_슬롯2개를_Octahedron으로_배정하고_판뒤집기와_충돌한다()
        {
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.OctahedronId };
            runtime.ConfigureDice(state, 0, state.Dice);

            Assert.That(state.Dice[0].Type, Is.EqualTo(YachtDieType.Octahedron));
            Assert.That(state.Dice[1].Type, Is.EqualTo(YachtDieType.Octahedron));
            Assert.That(state.Dice[2].Type, Is.EqualTo(YachtDieType.Normal));

            YachtAugmentDefinition def = runtime.FindDefinition(YachtAugmentRuntime.OctahedronId);
            Assert.That(def.Conflicts, Contains.Item(YachtAugmentRuntime.TableFlipId));
        }

        [Test]
        public void 커플주사위_슬롯2개를_Couple로_배정하고_눈이같으면_3점보너스를준다()
        {
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.CoupleDiceId };
            runtime.ConfigureDice(state, 0, state.Dice);

            Assert.That(state.Dice[0].Type, Is.EqualTo(YachtDieType.Couple));
            Assert.That(state.Dice[1].Type, Is.EqualTo(YachtDieType.Couple));

            state.Dice[0].Value = 4;
            state.Dice[1].Value = 4;
            Assert.That(YachtAugmentScoreEngine.CalculateDiceBonus(state.Dice), Is.EqualTo(3));

            state.Dice[1].Value = 3;
            Assert.That(YachtAugmentScoreEngine.CalculateDiceBonus(state.Dice), Is.Zero);
        }

        [Test]
        public void 세븐스다이스_슬롯2개를_Sevens로_배정하고_2에서7_눈금이_나온다()
        {
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.SevensDiceId };
            runtime.ConfigureDice(state, 0, state.Dice);

            Assert.That(state.Dice[0].Type, Is.EqualTo(YachtDieType.Sevens));
            Assert.That(state.Dice[1].Type, Is.EqualTo(YachtDieType.Sevens));

            var random = new SequenceRandom(0, 1, 2, 3, 4, 5);
            for (int i = 0; i < 6; i++)
            {
                int val = runtime.RollValue(state.Dice[0], random, () => 1);
                Assert.That(val, Is.InRange(2, 7));
            }
        }

        [Test]
        public void 프로모션주사위_1부터_6까지_성장하고_6달성후_비활성화된다()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.PromotionDieId);

            // 첫 턴 준비: SkipNextGrowth 적용으로 1 유지
            runtime.PrepareTurn(state, 0, random, true);
            runtime.ConfigureDice(state, 0, state.Dice);
            Assert.That(state.Dice[0].Type, Is.EqualTo(YachtDieType.Promotion));
            Assert.That(state.Dice[0].Value, Is.EqualTo(1));

            // 두 번째 턴 준비: 2로 성장
            runtime.PrepareTurn(state, 0, random, true);
            runtime.ConfigureDice(state, 0, state.Dice);
            Assert.That(state.Dice[0].Value, Is.EqualTo(2));

            // 6레벨 도달 시뮬레이션
            for (int i = 0; i < 4; i++) runtime.PrepareTurn(state, 0, random, true);
            runtime.ConfigureDice(state, 0, state.Dice);
            Assert.That(state.Dice[0].Value, Is.EqualTo(6));

            // 6레벨 사용 후 커밋하면 비활성화
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Aces, 6, 6, state.Dice, random);
            state.Dice[0].Type = YachtDieType.Normal;
            runtime.ConfigureDice(state, 0, state.Dice);
            Assert.That(state.Dice[0].Type, Is.EqualTo(YachtDieType.Normal));
        }

        #endregion

        #region 상시/특수 5종

        [Test]
        public void 요트뱅크_가장_왼쪽_킵주사위를_3턴간_제외하고_다음턴에_지급한다()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.YachtBankId);

            // 1턴째: 킵된 6짜리 주사위 제외 및 저축
            state.Dice[0].IsKept = true;
            state.Dice[0].KeepSlotIndex = 0;
            state.Dice[0].Value = 6;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Aces, 0, 0, state.Dice, random);

            // 2턴째: 킵된 5짜리 주사위 제외 및 누적
            runtime.PrepareTurn(state, 0, random, false);
            state.Dice[0].IsKept = true;
            state.Dice[0].KeepSlotIndex = 0;
            state.Dice[0].Value = 5;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Deuces, 0, 0, state.Dice, random);

            // 3턴째: 킵된 5짜리 추가 저축 (최대 15점 캡)
            runtime.PrepareTurn(state, 0, random, false);
            state.Dice[0].IsKept = true;
            state.Dice[0].KeepSlotIndex = 0;
            state.Dice[0].Value = 5;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Threes, 0, 0, state.Dice, random);

            // 4턴째 시작 시 저축액 15점 지급
            int beforeBonus = state.Players[0].augmentBonusScore;
            runtime.PrepareTurn(state, 0, random, false);
            Assert.That(state.Players[0].augmentBonusScore - beforeBonus, Is.EqualTo(15));
        }

        [Test]
        public void 추진력_0점기입시_장전되고_다음득점시_1_5배_부스트를_적용한다()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.MomentumId);

            // 0점 기입 -> 장전
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Aces, 0, 0, state.Dice, random);

            // 득점 계산 시 1.5배 확인
            YachtScoreCandidate[] candidates = runtime.CreateScoreCandidates(state, 0, state.Dice);
            // Dice: 1, 2, 3, 4, 5 -> Choice = 15 -> 15 * 1.5 = 22
            YachtScoreCandidate choice = FindCandidate(candidates, ScoreCategory.Choice);
            Assert.That(choice.Score, Is.EqualTo(22));
            Assert.That(choice.IsEnhanced, Is.True);
            Assert.That(choice.EnhancementSource, Is.EqualTo("Momentum"));

            // 득점 기입 후 장전 소모
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, 15, 22, state.Dice, random);
            YachtScoreCandidate[] nextCandidates = runtime.CreateScoreCandidates(state, 0, state.Dice);
            YachtScoreCandidate nextChoice = FindCandidate(nextCandidates, ScoreCategory.Choice);
            Assert.That(nextChoice.Score, Is.EqualTo(15));
            Assert.That(nextChoice.IsEnhanced, Is.False);
        }

        [Test]
        public void 결투_라운드_점수가_상대보다_높으면_10점_비기면_5점을_받는다()
        {
            var random = new SequenceRandom(0);
            state.CurrentRound = 3;
            AcquireAugment(YachtAugmentRuntime.DuelId);

            // P0가 20점 기록
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, 20, 20, state.Dice, random);
            int bonusBefore = state.Players[0].augmentBonusScore;

            // P1이 15점 기록 -> P0 승리 (+10점)
            runtime.AfterScoreCommit(state, 1, 1, ScoreCategory.Choice, 15, 15, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - bonusBefore, Is.EqualTo(10));
        }

        [Test]
        public void 저금통_남은굴림수당_3원을_모아_12원마다_12점_보너스를_받는다()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.PiggyBankId);

            // 남은 굴림 2회 -> 6원 적립 (아직 12원 미만)
            state.RollsRemaining = 2;
            int bonusBefore = state.Players[0].augmentBonusScore;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Aces, 5, 5, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore, Is.EqualTo(bonusBefore));

            // 다음 턴 남은 굴림 2회 -> 총 12원 도달 -> +12점 보너스!
            state.RollsRemaining = 2;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Deuces, 5, 5, state.Dice, random);
            Assert.That(state.Players[0].augmentBonusScore - bonusBefore, Is.EqualTo(12));
        }

        [Test]
        public void 랜덤박스_상단기준을_58로_낮춘다()
        {
            var random = new SequenceRandom(0);
            AcquireAugment(YachtAugmentRuntime.RandomBoxId);

            Assert.That(state.Players[0].upperBonusThreshold, Is.EqualTo(58));
        }

        #endregion

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

        private static YachtScoreCandidate FindCandidate(YachtScoreCandidate[] candidates, ScoreCategory category)
        {
            for (int i = 0; i < candidates.Length; i++)
                if (candidates[i].Category == category) return candidates[i];
            return default;
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
