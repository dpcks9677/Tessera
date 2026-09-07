using System;
using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    [TestFixture]
    public sealed class YachtManualActionAugmentTests
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
                    new YachtDieState { Id = 1, Value = 3 },
                    new YachtDieState { Id = 2, Value = 4 },
                    new YachtDieState { Id = 3, Value = 5 },
                    new YachtDieState { Id = 4, Value = 6 },
                    new YachtDieState { Id = 5, Value = 6 }
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
        public void 판뒤집기_첫굴림후_사용가능하고_사용시_기록된다()
        {
            AcquireAugment(YachtAugmentRuntime.TableFlipId);

            // 굴림 전에는 불가
            state.HasRolled = false;
            Assert.That(runtime.CanUseTableFlip(state, 0, out var code1, out _), Is.False);
            Assert.That(code1, Is.EqualTo(YachtCommandErrorCode.RollRequired));

            // 굴림 후 사용 가능
            state.HasRolled = true;
            Assert.That(runtime.CanUseTableFlip(state, 0, out _, out _), Is.True);

            // 사용 처리 후 재사용 불가
            runtime.MarkTableFlipUsed(state, 0);
            Assert.That(runtime.CanUseTableFlip(state, 0, out var code2, out _), Is.False);
            Assert.That(code2, Is.EqualTo(YachtCommandErrorCode.AugmentAlreadyUsed));
        }

        [Test]
        public void 등가교환_기본굴림소진후_최대3회_사용가능하고_5점씩_차감된다()
        {
            AcquireAugment(YachtAugmentRuntime.EquivalentExchangeId);

            // 굴림이 남아있으면 불가
            state.HasRolled = true;
            state.RollsRemaining = 1;
            Assert.That(runtime.CanUseEquivalentExchange(state, 0, out var code1, out _), Is.False);
            Assert.That(code1, Is.EqualTo(YachtCommandErrorCode.NoRollsRemaining));

            // 굴림 0회일 때 사용 가능
            state.RollsRemaining = 0;
            Assert.That(runtime.CanUseEquivalentExchange(state, 0, out _, out _), Is.True);

            // 3회 사용
            int before = state.Players[0].augmentBonusScore;
            runtime.MarkEquivalentExchangeUsed(state, 0);
            runtime.MarkEquivalentExchangeUsed(state, 0);
            runtime.MarkEquivalentExchangeUsed(state, 0);

            // 3회 사용 시 -15점
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(-15));
            Assert.That(state.AugmentPlayers[0].EquivalentExchangeUses, Is.EqualTo(3));

            // 4회차 시도 시 이미 사용 완료
            Assert.That(runtime.CanUseEquivalentExchange(state, 0, out var code2, out _), Is.False);
            Assert.That(code2, Is.EqualTo(YachtCommandErrorCode.AugmentAlreadyUsed));
        }

        [Test]
        public void 갬빗_굴림전_사용가능하고_주사위수를_4개로_줄인후_다음턴은_6개가된다()
        {
            AcquireAugment(YachtAugmentRuntime.GambitId);

            // 굴림 후에는 불가
            state.HasRolled = true;
            Assert.That(runtime.TryActivateBeforeRoll(state, 0, YachtAugmentRuntime.GambitId, out var code1, out _), Is.False);
            Assert.That(code1, Is.EqualTo(YachtCommandErrorCode.InvalidPhase));

            // 굴림 전 사용 성공
            state.HasRolled = false;
            Assert.That(runtime.TryActivateBeforeRoll(state, 0, YachtAugmentRuntime.GambitId, out _, out _), Is.True);
            Assert.That(state.AugmentPlayers[0].GambitState, Is.EqualTo(1));

            // 이번 턴 주사위 수: 4개
            Assert.That(runtime.GetDiceCount(state, 0, 5), Is.EqualTo(4));

            // 턴 종료 후 커밋 시 GambitState -> 2로 승격
            var random = new SequenceRandom(0);
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Aces, 0, 0, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].GambitState, Is.EqualTo(2));

            // 다음 턴 주사위 수: 6개
            Assert.That(runtime.GetDiceCount(state, 0, 5), Is.EqualTo(6));

            // 그 다음 턴 종료 후 커밋 시 GambitState -> 3 (종료)
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Deuces, 0, 0, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].GambitState, Is.EqualTo(3));
            Assert.That(runtime.GetDiceCount(state, 0, 5), Is.EqualTo(5));
        }

        [Test]
        public void 더블다운_9턴이후_굴림전_사용가능하고_점수를_1_5배로_부스트한다()
        {
            AcquireAugment(YachtAugmentRuntime.DoubleDownId);

            // 8턴 이전에는 사용 불가
            state.HasRolled = false;
            state.AugmentPlayers[0].TurnsTaken = 7;
            Assert.That(runtime.TryActivateBeforeRoll(state, 0, YachtAugmentRuntime.DoubleDownId, out var code1, out _), Is.False);
            Assert.That(code1, Is.EqualTo(YachtCommandErrorCode.AugmentUnavailable));

            // 9턴째 (TurnsTaken >= 8) 사용 가능
            state.AugmentPlayers[0].TurnsTaken = 8;
            Assert.That(runtime.TryActivateBeforeRoll(state, 0, YachtAugmentRuntime.DoubleDownId, out _, out _), Is.True);
            Assert.That(state.AugmentPlayers[0].DoubleDownActive, Is.True);
            Assert.That(state.AugmentPlayers[0].DoubleDownUsed, Is.True);

            // 점수 후보 확인 (Choice 24점 -> 36점, 1.5배)
            state.Dice[0].Value = 4;
            state.Dice[1].Value = 5;
            state.Dice[2].Value = 5;
            state.Dice[3].Value = 5;
            state.Dice[4].Value = 5; // Choice = 24
            var candidates = runtime.CreateScoreCandidates(state, 0, state.Dice);
            Assert.That(candidates[(int)ScoreCategory.Choice].Score, Is.EqualTo(36));
            Assert.That(candidates[(int)ScoreCategory.Choice].IsEnhanced, Is.True);

            // 커밋 후 DoubleDownActive 리셋
            var random = new SequenceRandom(0);
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, 24, 36, state.Dice, random);
            Assert.That(state.AugmentPlayers[0].DoubleDownActive, Is.False);
        }

        [Test]
        public void 주사위연금술_첫굴림후_킵되지않은_주사위_눈금을_1씩_감소시킨다()
        {
            AcquireAugment(YachtAugmentRuntime.DiceAlchemyId);

            // 굴림 전에는 불가
            state.HasRolled = false;
            Assert.That(runtime.TryUseDiceAlchemy(state, 0, out var code1, out _), Is.False);
            Assert.That(code1, Is.EqualTo(YachtCommandErrorCode.RollRequired));

            // 굴림 후: 슬롯 0은 킵, 슬롯 1~4는 미킵 (값: 1번=4, 2번=5, 3번=6, 4번=1)
            state.HasRolled = true;
            state.Dice[0].IsKept = true;
            state.Dice[0].Value = 3;
            state.Dice[1].Value = 4;
            state.Dice[2].Value = 5;
            state.Dice[3].Value = 6;
            state.Dice[4].Value = 1;

            Assert.That(runtime.TryUseDiceAlchemy(state, 0, out _, out _), Is.True);
            Assert.That(state.AugmentPlayers[0].DiceAlchemyUsed, Is.True);

            // 킵된 0번 주사위는 그대로 3
            Assert.That(state.Dice[0].Value, Is.EqualTo(3));
            // 미킵 주사위들은 1씩 감소 (최소 1 유지)
            Assert.That(state.Dice[1].Value, Is.EqualTo(3));
            Assert.That(state.Dice[2].Value, Is.EqualTo(4));
            Assert.That(state.Dice[3].Value, Is.EqualTo(5));
            Assert.That(state.Dice[4].Value, Is.EqualTo(1)); // 1 미만으로는 내려가지 않음

            // 재사용 불가
            Assert.That(runtime.TryUseDiceAlchemy(state, 0, out var code2, out _), Is.False);
            Assert.That(code2, Is.EqualTo(YachtCommandErrorCode.AugmentAlreadyUsed));
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
