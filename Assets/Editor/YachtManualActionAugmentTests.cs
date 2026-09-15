using System;
using NUnit.Framework;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;
using Tessera.Tabletop;

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
        public void TableFlip_IsUsableAfterFirstRollAndIsRecordedOnUse()
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
        public void EquivalentExchange_AllowsUpToThreeUsesAfterRollsRunOutAndCostsFivePointsEach()
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
        public void EquivalentExchange_IsUnusableWithoutOwnershipAndThrowsNoException()
        {
            // 코스믹 큐브의 소진(회색) 상태가 바로 이 경로다. 화면이 갱신마다 밟으므로
            // false를 돌려주는 것보다 예외가 나지 않는 것이 더 중요한 단언이다.
            state.HasRolled = true;
            state.RollsRemaining = 0;

            Assert.That(runtime.CanUseEquivalentExchange(state, 0, out var code, out _), Is.False);
            Assert.That(code, Is.EqualTo(YachtCommandErrorCode.AugmentRequired));
        }

        [Test]
        public void RollBudgetStateIsDecidedByRemainingRollsAndExchangeAvailability()
        {
            // 굴림이 남아 있으면 등가교환 보유 여부와 무관하게 평소 상태다.
            Assert.That(YachtTurnFlowPresenter.ResolveRollBudgetState(3, false), Is.EqualTo(RollBudgetState.Normal));
            Assert.That(YachtTurnFlowPresenter.ResolveRollBudgetState(3, true), Is.EqualTo(RollBudgetState.Normal));

            // 소진 후에는 등가교환만이 보라(대기)와 회색(소진)을 가른다. 판 뒤집기는 게이트가 아니다.
            Assert.That(YachtTurnFlowPresenter.ResolveRollBudgetState(0, true), Is.EqualTo(RollBudgetState.AugmentReady));
            Assert.That(YachtTurnFlowPresenter.ResolveRollBudgetState(0, false), Is.EqualTo(RollBudgetState.Drained));
        }

        [Test]
        public void Gambit_IsUsableBeforeRollReducesDiceToFourThenNextTurnHasSix()
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
        public void DoubleDown_IsUsableBeforeRollFromTurnNineAndBoostsScoreByOnePointFive()
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
        public void DiceAlchemy_DecreasesUnkeptDiceValuesByOneAfterFirstRoll()
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

        [Test]
        public void CoinToss_ZeroHeads_AppliesFivePointPenalty()
        {
            AcquireAugment(YachtAugmentRuntime.CoinTossId);

            // 굴림 전에는 불가
            state.HasRolled = false;
            Assert.That(runtime.TryUseCoinToss(state, 0, new SequenceRandom(0, 0, 0), out var code1, out _), Is.False);
            Assert.That(code1, Is.EqualTo(YachtCommandErrorCode.RollRequired));
            state.HasRolled = true;

            int before = state.Players[0].augmentBonusScore;
            Assert.That(runtime.TryUseCoinToss(state, 0, new SequenceRandom(0, 0, 0), out _, out _), Is.True);
            Assert.That(state.Players[0].augmentBonusScore - before, Is.EqualTo(-5));

            var coinState = (CoinTossState)state.AugmentPlayers[0].States.Find(YachtAugmentRuntime.CoinTossId);
            Assert.That(coinState.Heads, Is.EqualTo(0));
            Assert.That(coinState.IsUsed, Is.True);

            // 재사용 불가
            Assert.That(runtime.TryUseCoinToss(state, 0, new SequenceRandom(0, 0, 0), out var code2, out _), Is.False);
            Assert.That(code2, Is.EqualTo(YachtCommandErrorCode.AugmentAlreadyUsed));
        }

        [Test]
        public void CoinToss_OneHead_SetsLowestDieToSixEvenIfKeptAndBreaksTiesByIndex()
        {
            AcquireAugment(YachtAugmentRuntime.CoinTossId);
            state.HasRolled = true;

            // 슬롯 1, 2가 최저값(1)으로 동점. 슬롯 1은 킵된 상태.
            state.Dice[0].Value = 3;
            state.Dice[1].Value = 1;
            state.Dice[1].IsKept = true;
            state.Dice[2].Value = 1;
            state.Dice[3].Value = 5;
            state.Dice[4].Value = 6;

            Assert.That(runtime.TryUseCoinToss(state, 0, new SequenceRandom(1, 0, 0), out _, out _), Is.True);

            var coinState = (CoinTossState)state.AugmentPlayers[0].States.Find(YachtAugmentRuntime.CoinTossId);
            Assert.That(coinState.Heads, Is.EqualTo(1));

            // 동점이면 인덱스가 가장 앞선 주사위(슬롯 1)가 6이 된다. 킵 여부와 무관.
            Assert.That(state.Dice[1].Value, Is.EqualTo(6));
            Assert.That(state.Dice[2].Value, Is.EqualTo(1));
        }

        [Test]
        public void CoinToss_TwoHeads_GrantsOneExtraRoll()
        {
            AcquireAugment(YachtAugmentRuntime.CoinTossId);
            state.HasRolled = true;
            state.RollsRemaining = 0;

            Assert.That(runtime.TryUseCoinToss(state, 0, new SequenceRandom(1, 1, 0), out _, out _), Is.True);

            var coinState = (CoinTossState)state.AugmentPlayers[0].States.Find(YachtAugmentRuntime.CoinTossId);
            Assert.That(coinState.Heads, Is.EqualTo(2));
            Assert.That(state.RollsRemaining, Is.EqualTo(1));
            Assert.That(state.BonusRolls, Is.EqualTo(1));
        }

        [Test]
        public void CoinToss_ThreeHeads_LowersUpperBonusThresholdButNeverRaisesIt()
        {
            AcquireAugment(YachtAugmentRuntime.CoinTossId);
            state.HasRolled = true;
            state.Players[0].upperBonusThreshold = 58;

            Assert.That(runtime.TryUseCoinToss(state, 0, new SequenceRandom(1, 1, 1), out _, out _), Is.True);

            var coinState = (CoinTossState)state.AugmentPlayers[0].States.Find(YachtAugmentRuntime.CoinTossId);
            Assert.That(coinState.Heads, Is.EqualTo(3));
            Assert.That(state.Players[0].upperBonusThreshold, Is.EqualTo(57));
        }

        [Test]
        public void CoinToss_ThreeHeads_KeepsAlreadyLowerThreshold()
        {
            AcquireAugment(YachtAugmentRuntime.CoinTossId);
            state.HasRolled = true;
            state.Players[0].upperBonusThreshold = 55;

            Assert.That(runtime.TryUseCoinToss(state, 0, new SequenceRandom(1, 1, 1), out _, out _), Is.True);

            Assert.That(state.Players[0].upperBonusThreshold, Is.EqualTo(55));
        }

        [Test]
        public void CoinToss_ExtraRoll_AllowsRollingAgainAndResetsBonusRollsOnNextTurn()
        {
            // StartGame이 드래프트 후보를 섞느라 같은 난수원의 NextInt를 여러 번 소비한다.
            // 코인 결과만 정확히 통제하려고 NextBool 전용 채널을 쓰는 더블을 쓴다.
            LocalGameAuthority authority = CreateAugmentedAuthority(new FixedBoolRandom(true, true, false));
            ExecuteAuthority(authority, YachtCommandType.StartGame, "start");
            YachtGameState authorityState = authority.CurrentState;
            authorityState.Draft.IsActive = false;
            authorityState.Phase = YachtGamePhase.ScoreSelection;
            authorityState.HasRolled = true;
            authorityState.RollsRemaining = 0;
            authorityState.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.CoinTossId };

            YachtGameCommandResult use = ExecuteAuthority(
                authority, YachtCommandType.UseAugmentAction, "coin-toss", augmentId: YachtAugmentRuntime.CoinTossId);

            Assert.That(use.Accepted, Is.True);
            Assert.That(use.State.RollsRemaining, Is.EqualTo(1));
            Assert.That(use.State.BonusRolls, Is.EqualTo(1));
            bool canRoll = use.State.Phase == YachtGamePhase.ScoreSelection && use.State.RollsRemaining > 0;
            Assert.That(canRoll, Is.True);
            Assert.That(use.Events, Has.Length.EqualTo(1));
            Assert.That(use.Events[0].Type, Is.EqualTo(YachtGameEventType.AugmentActionUsed));
            Assert.That(use.Events[0].CoinFaces, Is.EqualTo(0b011));
            Assert.That(use.Events[0].Message, Is.EqualTo("코인 토스: 앞면 2개 — 리롤 +1"));

            YachtGameCommandResult commit = ExecuteAuthority(
                authority, YachtCommandType.CommitScore, "commit", category: ScoreCategory.Aces);
            Assert.That(commit.Accepted, Is.True);
            Assert.That(ExecuteAuthority(authority, YachtCommandType.AdvanceTurn, "advance").Accepted, Is.True);
            Assert.That(authority.CurrentState.BonusRolls, Is.EqualTo(0));
        }

        [Test]
        public void CoinToss_ExtraRoll_CountsAsOneRollForNoTimeToWaste()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority(new FixedBoolRandom(true, true, false));
            ExecuteAuthority(authority, YachtCommandType.StartGame, "start");
            YachtGameState authorityState = authority.CurrentState;
            authorityState.Draft.IsActive = false;
            authorityState.Phase = YachtGamePhase.ScoreSelection;
            authorityState.HasRolled = true;
            authorityState.RollsRemaining = YachtGameSession.MaxRolls - 1; // 1회 굴림을 소진한 상태
            authorityState.AugmentPlayers[0].OwnedIds = new[]
            {
                YachtAugmentRuntime.CoinTossId,
                YachtAugmentRuntime.NoTimeToWasteId
            };
            authorityState.AugmentPlayers[0].NoTimeRemaining = 3;

            ExecuteAuthority(authority, YachtCommandType.UseAugmentAction, "coin-toss", augmentId: YachtAugmentRuntime.CoinTossId);
            YachtGameCommandResult commit = ExecuteAuthority(
                authority, YachtCommandType.CommitScore, "commit", category: ScoreCategory.Aces);

            Assert.That(commit.Accepted, Is.True);
            Assert.That(commit.State.AugmentPlayers[0].NoTimeFailed, Is.False);
            Assert.That(commit.State.AugmentPlayers[0].NoTimeRemaining, Is.EqualTo(2));
        }

        private static LocalGameAuthority CreateAugmentedAuthority(IRandomSource random)
        {
            return new LocalGameAuthority(new YachtGameOptions
            {
                Mode = YachtGameMode.Augmented,
                PresetClipCount = 20
            }, random);
        }

        private static YachtGameCommandResult ExecuteAuthority(
            LocalGameAuthority authority,
            YachtCommandType type,
            string commandId,
            ScoreCategory category = default,
            string augmentId = null)
        {
            int playerIndex = authority.CurrentState.CurrentPlayerIndex;
            return authority.Execute(new YachtGameCommand
            {
                CommandId = commandId,
                ExpectedRevision = authority.CurrentState.Revision,
                PlayerIndex = playerIndex,
                Type = type,
                Category = category,
                AugmentId = augmentId
            });
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

        /// <summary>
        /// NextInt와 NextBool을 별도 채널로 다루는 더블입니다. LocalGameAuthority를 거치는 테스트는
        /// StartGame의 드래프트 셔플이 NextInt를 얼마나 소비하는지 알 수 없어 코인 결과(NextBool)만
        /// 정확히 고정하려고 씁니다. NextInt는 굴림·셔플에 쓰이지만 이 더블을 쓰는 테스트들은
        /// 굴림 결과를 직접 상태에 대입하므로 값은 아무거나 상관없습니다.
        /// </summary>
        private sealed class FixedBoolRandom : IRandomSource
        {
            private readonly bool[] values;
            private int index;

            public FixedBoolRandom(params bool[] values)
            {
                this.values = values;
            }

            public int NextInt(int minInclusive, int maxExclusive) => minInclusive;

            public bool NextBool()
            {
                bool value = index < values.Length && values[index];
                index++;
                return value;
            }
        }
    }
}
