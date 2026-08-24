using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    [TestFixture]
    public sealed class YachtGameRulesTests
    {
        [Test]
        public void Calculate_기본족보를_웹규칙과_동일하게_계산한다()
        {
            var yacht = YachtScoreCalculator.Calculate(new[] { 6, 6, 6, 6, 6 });
            Assert.That(yacht[ScoreCategory.Sixes], Is.EqualTo(30));
            Assert.That(yacht[ScoreCategory.Choice], Is.EqualTo(30));
            Assert.That(yacht[ScoreCategory.FourOfAKind], Is.EqualTo(30));
            Assert.That(yacht[ScoreCategory.FullHouse], Is.EqualTo(30));
            Assert.That(yacht[ScoreCategory.Yacht], Is.EqualTo(50));

            var smallStraight = YachtScoreCalculator.Calculate(new[] { 1, 2, 3, 4, 4 });
            Assert.That(smallStraight[ScoreCategory.SmallStraight], Is.EqualTo(15));
            Assert.That(smallStraight[ScoreCategory.LargeStraight], Is.Zero);

            var largeStraight = YachtScoreCalculator.Calculate(new[] { 2, 3, 4, 5, 6 });
            Assert.That(largeStraight[ScoreCategory.SmallStraight], Is.EqualTo(15));
            Assert.That(largeStraight[ScoreCategory.LargeStraight], Is.EqualTo(30));
        }

        [Test]
        public void PlayerScoreData_상단합계63점부터_35점보너스를_적용한다()
        {
            var data = new PlayerScoreData { upperScores = new[] { 3, 6, 9, 12, 15, 18 } };
            data.RecalculateTotal();
            Assert.That(data.CalculateUpperSum(), Is.EqualTo(63));
            Assert.That(data.hasBonus, Is.True);
            Assert.That(data.bonusScore, Is.EqualTo(35));
            Assert.That(data.totalScore, Is.EqualTo(98));
        }

        [Test]
        public void Options_일반턴_제한시간은_60초다()
        {
            var options = new YachtGameOptions();

            Assert.That(options.TurnDurationSeconds, Is.EqualTo(60f));
            Assert.That(YachtGameOptions.DefaultTurnDurationSeconds, Is.EqualTo(60f));
        }

        [Test]
        public void Authority_고정난수에서_주사위값과_프리셋을_하나의_결과로_확정한다()
        {
            var authority = CreateAuthority(new SequenceRandomSource(0, 1, 2, 3, 4, 7, 1));
            Execute(authority, YachtCommandType.StartGame, "start");

            YachtGameCommandResult result = Execute(authority, YachtCommandType.RollDice, "roll");

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.RollPresentation.FinalValues, Has.Length.EqualTo(5));
            Assert.That(result.RollPresentation.FinalValues[0].Value, Is.EqualTo(1));
            Assert.That(result.RollPresentation.FinalValues[4].Value, Is.EqualTo(5));
            Assert.That(result.RollPresentation.PresetFile, Is.EqualTo("dice_presets_normal_5.json"));
            Assert.That(result.RollPresentation.PresetIndex, Is.EqualTo(7));
            Assert.That(result.RollPresentation.IsMirrored, Is.True);
            Assert.That(result.State.Dice[4].Value, Is.EqualTo(5));
        }

        [Test]
        public void Authority_킵한_주사위는_재굴림에서도_값을_보존한다()
        {
            var authority = CreateAuthority(new SequenceRandomSource(0, 1, 2, 3, 4, 0, 0, 5, 5, 5, 5, 0, 0));
            Execute(authority, YachtCommandType.StartGame, "start");
            Execute(authority, YachtCommandType.RollDice, "roll-1");
            int keptValue = authority.CurrentState.Dice[0].Value;
            Execute(authority, YachtCommandType.SetDieKept, "keep", dieId: 1, isKept: true);

            YachtGameCommandResult result = Execute(authority, YachtCommandType.RollDice, "roll-2");

            Assert.That(result.State.Dice[0].IsKept, Is.True);
            Assert.That(result.State.Dice[0].Value, Is.EqualTo(keptValue));
            Assert.That(result.State.RollsRemaining, Is.EqualTo(1));
        }

        [Test]
        public void Authority_중복명령과_오래된Revision을_거부한다()
        {
            var authority = CreateAuthority(new SequenceRandomSource(0));
            YachtGameCommandResult start = Execute(authority, YachtCommandType.StartGame, "same");
            Assert.That(start.Accepted, Is.True);

            YachtGameCommandResult duplicate = authority.Execute(new YachtGameCommand
            {
                CommandId = "same",
                ExpectedRevision = authority.CurrentState.Revision,
                PlayerIndex = 0,
                Type = YachtCommandType.RollDice
            });
            Assert.That(duplicate.ErrorCode, Is.EqualTo(YachtCommandErrorCode.DuplicateCommand));

            YachtGameCommandResult stale = authority.Execute(new YachtGameCommand
            {
                CommandId = "stale",
                ExpectedRevision = 0,
                PlayerIndex = 0,
                Type = YachtCommandType.RollDice
            });
            Assert.That(stale.ErrorCode, Is.EqualTo(YachtCommandErrorCode.RevisionMismatch));
        }

        [Test]
        public void Session_점수확정후_P1_P2_라운드를_순서대로_전환한다()
        {
            YachtGameSession session = CreateSession();
            Assert.That(session.TryRoll(out _), Is.True);
            Assert.That(session.TryCommitScore(ScoreCategory.Aces, out YachtTurnResult p1), Is.True);
            Assert.That(p1.ScoredPlayerIndex, Is.Zero);
            Assert.That(session.AdvanceTurnAfterAnimation(), Is.True);
            Assert.That(session.CurrentPlayerIndex, Is.EqualTo(1));
            Assert.That(session.CurrentRound, Is.EqualTo(1));

            Assert.That(session.TryRoll(out _), Is.True);
            Assert.That(session.TryCommitScore(ScoreCategory.Deuces, out YachtTurnResult p2), Is.True);
            Assert.That(p2.ScoredPlayerIndex, Is.EqualTo(1));
            Assert.That(session.AdvanceTurnAfterAnimation(), Is.True);
            Assert.That(session.CurrentPlayerIndex, Is.Zero);
            Assert.That(session.CurrentRound, Is.EqualTo(2));
        }

        [Test]
        public void Session_24개_개인턴후_종료하고_재시작할수있다()
        {
            YachtGameSession session = CreateSession();
            YachtTurnResult result = default;
            for (int turn = 0; turn < 24; turn++)
            {
                Assert.That(session.ResolveTimeout(out result), Is.True, $"turn {turn + 1}");
                if (!result.GameEnded) Assert.That(session.AdvanceTurnAfterAnimation(), Is.True, $"turn {turn + 1} transition");
            }

            Assert.That(result.GameEnded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(YachtGamePhase.GameOver));
            Assert.That(session.GetPlayer(0).upperScores[0], Is.Zero);

            session.StartNewGame();
            Assert.That(session.Phase, Is.EqualTo(YachtGamePhase.TurnReady));
            Assert.That(session.CurrentRound, Is.EqualTo(1));
            Assert.That(session.GetPlayer(0).upperScores[0], Is.EqualTo(-1));
        }

        [Test]
        public void Mode_증강규칙은_기본규칙을_합성하고_현재기본결과는_같다()
        {
            IYachtRuleSet normal = YachtRuleSetFactory.Create(YachtGameMode.Normal);
            IYachtRuleSet augmented = YachtRuleSetFactory.Create(YachtGameMode.Augmented);
            Assert.That(augmented, Is.TypeOf<AugmentedYachtRuleSet>());
            Assert.That(((AugmentedYachtRuleSet)augmented).BaseRules, Is.TypeOf<NormalYachtRuleSet>());

            YachtDieState[] dice = normal.CreateInitialDice(5);
            int[] values = { 2, 3, 4, 5, 6 };
            for (int i = 0; i < dice.Length; i++) dice[i].Value = values[i];
            Assert.That(augmented.CalculateScores(dice)[ScoreCategory.LargeStraight],
                Is.EqualTo(normal.CalculateScores(dice)[ScoreCategory.LargeStraight]));
        }

        [Test]
        public void AugmentRuntime_대표증강의_정적정의와_플레이어상태를_분리한다()
        {
            var runtime = new YachtAugmentRuntime();
            IReadOnlyList<YachtAugmentDefinition> definitions = runtime.GetDefinitions();
            var state = new YachtGameState { Mode = YachtGameMode.Augmented };

            runtime.Initialize(state, 2);
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.NoTimeToWasteId };

            Assert.That(definitions.Count, Is.EqualTo(45));
            Assert.That(definitions[0].Id, Is.EqualTo(YachtAugmentRuntime.LuckySevensId));
            Assert.That(state.AugmentPlayers[1].OwnedIds, Is.Empty);
            Assert.That(state.GlobalAugmentIds, Is.Empty);
            Assert.That(definitions[0].IsGlobal, Is.False);
            Assert.That(runtime.FindDefinition(YachtAugmentRuntime.StepByStepId).PhaseOneOnly, Is.True);
            Assert.That(runtime.FindDefinition(YachtAugmentRuntime.LuckySevensId).DisplayName, Is.EqualTo("럭키 세븐"));
        }

        [Test]
        public void AugmentDraft_두플레이어가_하나씩_선택하면_첫턴을_시작한다()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority(new SequenceRandomSource(4, 3, 2, 1));
            Execute(authority, YachtCommandType.StartGame, "start");

            Assert.That(authority.CurrentState.Phase, Is.EqualTo(YachtGamePhase.Draft));
            Assert.That(authority.CurrentState.Draft.PlayerIndex, Is.Zero);
            string first = authority.CurrentState.Draft.Options[0];
            YachtGameCommandResult firstResult = Execute(authority, YachtCommandType.SelectAugment, "draft-p1", augmentId: first);
            Assert.That(firstResult.Accepted, Is.True, $"첫 선택 실패: {first}, {firstResult.ErrorCode}, {firstResult.ErrorMessage}");

            Assert.That(authority.CurrentState.Draft.PlayerIndex, Is.EqualTo(1));
            string second = null;
            for (int i = 0; i < authority.CurrentState.Draft.Options.Length; i++)
            {
                string candidate = authority.CurrentState.Draft.Options[i];
                bool conflicts = (first == YachtAugmentRuntime.OctahedronId && candidate == YachtAugmentRuntime.TableFlipId)
                    || (first == YachtAugmentRuntime.TableFlipId && candidate == YachtAugmentRuntime.OctahedronId);
                if (candidate != first && !conflicts)
                {
                    second = candidate;
                    break;
                }
            }
            Assert.That(second, Is.Not.Null);
            YachtGameCommandResult secondResult = Execute(authority, YachtCommandType.SelectAugment, "draft-p2", augmentId: second);
            Assert.That(secondResult.Accepted, Is.True, $"두 번째 선택 실패: {second}, {secondResult.ErrorCode}, {secondResult.ErrorMessage}");

            Assert.That(authority.CurrentState.Phase, Is.EqualTo(YachtGamePhase.TurnReady));
            Assert.That(authority.CurrentState.Draft.IsActive, Is.False);
            bool firstResolved = first == YachtAugmentRuntime.RandomBoxId
                ? !string.IsNullOrEmpty(authority.CurrentState.AugmentPlayers[0].RandomBoxAwardId)
                : IsOwnedByOrGlobal(authority.CurrentState, 0, first);
            bool secondResolved = second == YachtAugmentRuntime.RandomBoxId
                ? !string.IsNullOrEmpty(authority.CurrentState.AugmentPlayers[1].RandomBoxAwardId)
                : IsOwnedByOrGlobal(authority.CurrentState, 1, second);
            Assert.That(firstResolved, Is.True);
            Assert.That(secondResolved, Is.True);
        }

        [Test]
        public void LuckySevens_합계7을_에이스15점으로_교체한다()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority(new SequenceRandomSource(1));
            Execute(authority, YachtCommandType.StartGame, "start");
            authority.CurrentState.Draft.Options = new[] { YachtAugmentRuntime.LuckySevensId };
            Execute(authority, YachtCommandType.SelectAugment, "draft-p1", augmentId: YachtAugmentRuntime.LuckySevensId);
            authority.CurrentState.Draft.Options = new[] { YachtAugmentRuntime.LuckySevensId };
            Execute(authority, YachtCommandType.SelectAugment, "draft-p2", augmentId: YachtAugmentRuntime.LuckySevensId);
            int[] keptValues = { 1, 1, 1, 2 };
            for (int i = 0; i < keptValues.Length; i++)
            {
                authority.CurrentState.Dice[i].Value = keptValues[i];
                authority.CurrentState.Dice[i].IsKept = true;
            }

            YachtGameCommandResult roll = Execute(authority, YachtCommandType.RollDice, "roll");

            Assert.That(roll.Accepted, Is.True);
            Assert.That(GetCandidate(authority.CurrentState, ScoreCategory.Aces), Is.EqualTo(15));
            Assert.That(authority.CurrentState.AugmentPlayers[0].OwnedIds, Does.Contain(YachtAugmentRuntime.LuckySevensId));
            Assert.That(authority.CurrentState.AugmentPlayers[1].OwnedIds, Does.Contain(YachtAugmentRuntime.LuckySevensId));
            Assert.That(authority.CurrentState.GlobalAugmentIds, Is.Empty);
        }

        [Test]
        public void LuckySevens_중간획득시_보유자에이스만_초기화하고_추가턴을_준다()
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = 6,
                CurrentPlayerIndex = 0,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() }
            };
            state.Players[0].upperScores[0] = 4;
            state.Players[1].upperScores[0] = 2;
            state.Players[0].RecalculateTotal();
            state.Players[1].RecalculateTotal();
            runtime.Initialize(state, 2);
            state.Draft.SelectionCounts = new[] { 1, 1 };
            var random = new SequenceRandomSource(4, 3, 2, 1);

            Assert.That(runtime.TryBeginDraft(state, random, out _), Is.True);
            state.Draft.Options = new[] { YachtAugmentRuntime.LuckySevensId };
            Assert.That(runtime.TrySelectAugment(state, 0, YachtAugmentRuntime.LuckySevensId, random,
                out _, out _, out _), Is.True);

            Assert.That(state.Players[0].upperScores[0], Is.EqualTo(-1));
            Assert.That(state.Players[1].upperScores[0], Is.EqualTo(2));
            Assert.That(state.AugmentPlayers[0].ExtraTurns, Is.EqualTo(1));
            Assert.That(state.AugmentPlayers[1].ExtraTurns, Is.Zero);
            Assert.That(state.AugmentPlayers[0].OwnedIds, Does.Contain(YachtAugmentRuntime.LuckySevensId));
            Assert.That(state.GlobalAugmentIds, Is.Empty);

            var ownerScores = new Dictionary<ScoreCategory, int> { [ScoreCategory.Aces] = 5 };
            var opponentScores = new Dictionary<ScoreCategory, int> { [ScoreCategory.Aces] = 5 };
            YachtDieState[] dice =
            {
                new() { Value = 1 }, new() { Value = 1 }, new() { Value = 1 },
                new() { Value = 2 }, new() { Value = 2 }
            };
            runtime.ModifyScorePreview(state, 0, dice, ownerScores);
            runtime.ModifyScorePreview(state, 1, dice, opponentScores);

            Assert.That(ownerScores[ScoreCategory.Aces], Is.EqualTo(15));
            Assert.That(opponentScores[ScoreCategory.Aces], Is.EqualTo(3));
        }

        [Test]
        public void Authority_표준12라운드후_예약된_추가턴을_소비하고_종료한다()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority(new SequenceRandomSource(0));
            Execute(authority, YachtCommandType.StartGame, "start");
            YachtGameState state = authority.CurrentState;
            state.Draft.IsActive = false;
            state.CurrentRound = YachtGameSession.LastRound;
            state.CurrentPlayerIndex = 1;
            state.Phase = YachtGamePhase.ScoreSelection;
            state.HasRolled = true;
            state.RollsRemaining = 2;
            for (int player = 0; player < 2; player++)
            {
                state.Players[player].upperScores = new[] { 0, 0, 0, 0, 0, 0 };
                state.Players[player].lowerScores = new[] { 0, 0, 0, 0, 0, 0 };
            }
            state.Players[0].upperScores[0] = -1;
            state.Players[1].lowerScores[5] = -1;
            state.AugmentPlayers[0].ExtraTurns = 1;
            state.Candidates = new[] { new YachtScoreCandidate { Category = ScoreCategory.Yacht, Score = 0 } };

            YachtGameCommandResult finalStandard = Execute(
                authority,
                YachtCommandType.CommitScore,
                "final-standard",
                category: ScoreCategory.Yacht);
            Assert.That(finalStandard.State.Phase, Is.EqualTo(YachtGamePhase.TurnTransition));
            Assert.That(Execute(authority, YachtCommandType.AdvanceTurn, "extra-turn").Accepted, Is.True);
            Assert.That(authority.CurrentState.IsExtraTurnPhase, Is.True);
            Assert.That(authority.CurrentState.CurrentPlayerIndex, Is.Zero);

            YachtGameCommandResult finalExtra = Execute(authority, YachtCommandType.ResolveTimeout, "final-extra");
            Assert.That(finalExtra.State.Phase, Is.EqualTo(YachtGamePhase.GameOver));
            Assert.That(finalExtra.State.AugmentPlayers[0].ExtraTurns, Is.Zero);
        }

        [Test]
        public void EightSided_두주사위와_혼합프리셋을_권위결과에_포함한다()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority(new SequenceRandomSource(0));
            Execute(authority, YachtCommandType.StartGame, "start");
            authority.CurrentState.Draft.Options = new[] { YachtAugmentRuntime.OctahedronId };
            Execute(authority, YachtCommandType.SelectAugment, "draft-p1", augmentId: YachtAugmentRuntime.OctahedronId);
            authority.CurrentState.Draft.Options = new[] { YachtAugmentRuntime.NoTimeToWasteId };
            Execute(authority, YachtCommandType.SelectAugment, "draft-p2", augmentId: YachtAugmentRuntime.NoTimeToWasteId);

            YachtGameCommandResult roll = Execute(authority, YachtCommandType.RollDice, "roll");

            Assert.That(authority.CurrentState.Dice[0].Type, Is.EqualTo(YachtDieType.Octahedron));
            Assert.That(authority.CurrentState.Dice[1].Type, Is.EqualTo(YachtDieType.Octahedron));
            Assert.That(roll.RollPresentation.PresetFile, Is.EqualTo("dice_presets_mixed_3normal_2octa.json"));
        }

        [Test]
        public void TableFlip_굴림횟수를_소모하지않고_NoTime퀘스트도_유지한다()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority(new SequenceRandomSource(0, 1, 2, 3, 4));
            Execute(authority, YachtCommandType.StartGame, "start");
            YachtGameState state = authority.CurrentState;
            state.Draft.IsActive = false;
            state.Draft.PlayerIndex = -1;
            state.Phase = YachtGamePhase.TurnReady;
            state.AugmentPlayers[0].OwnedIds = new[]
            {
                YachtAugmentRuntime.TableFlipId,
                YachtAugmentRuntime.NoTimeToWasteId
            };
            state.AugmentPlayers[0].NoTimeRemaining = 3;

            Execute(authority, YachtCommandType.RollDice, "roll");
            YachtGameCommandResult flip = Execute(
                authority,
                YachtCommandType.UseAugmentAction,
                "table-flip",
                augmentId: YachtAugmentRuntime.TableFlipId);

            Assert.That(flip.Accepted, Is.True);
            Assert.That(flip.State.RollsRemaining, Is.EqualTo(2));
            Assert.That(flip.RollPresentation.PresetFile, Is.EqualTo("dice_presets_flip_5.json"));
            Assert.That(Execute(authority, YachtCommandType.UseAugmentAction, "table-flip-again",
                augmentId: YachtAugmentRuntime.TableFlipId).ErrorCode, Is.EqualTo(YachtCommandErrorCode.AugmentAlreadyUsed));

            YachtGameCommandResult commit = Execute(authority, YachtCommandType.CommitScore, "score", category: ScoreCategory.Aces);
            Assert.That(commit.Accepted, Is.True);
            Assert.That(commit.State.AugmentPlayers[0].NoTimeFailed, Is.False);
            Assert.That(commit.State.AugmentPlayers[0].NoTimeRemaining, Is.EqualTo(2));
        }

        [Test]
        public void NoTimeToWaste_세턴성공시_15점을_지급하고_리롤시_실패한다()
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() }
            };
            runtime.Initialize(state, 2);
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.NoTimeToWasteId };
            state.AugmentPlayers[0].NoTimeRemaining = 3;

            runtime.AfterScoreCommit(state, 0, 1);
            runtime.AfterScoreCommit(state, 0, 1);
            YachtGameEvent[] completed = runtime.AfterScoreCommit(state, 0, 1);

            Assert.That(state.AugmentPlayers[0].NoTimeRewarded, Is.True);
            Assert.That(state.Players[0].augmentBonusScore, Is.EqualTo(15));
            Assert.That(completed[0].Score, Is.EqualTo(15));

            state.AugmentPlayers[1].OwnedIds = new[] { YachtAugmentRuntime.NoTimeToWasteId };
            state.AugmentPlayers[1].NoTimeRemaining = 3;
            runtime.AfterScoreCommit(state, 1, 2);
            Assert.That(state.AugmentPlayers[1].NoTimeFailed, Is.True);
        }

        [Test]
        public void StepByStep_순서완료후_상단합계58점에서_55점보너스를_확정한다()
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() }
            };
            runtime.Initialize(state, 2);
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.StepByStepId };
            state.AugmentPlayers[0].StepCategoryIndex = 5;
            state.Players[0].upperScores = new[] { 8, 8, 9, 10, 11, 12 };
            state.Players[0].RecalculateTotal();

            YachtGameEvent[] events = runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Sixes);

            Assert.That(state.AugmentPlayers[0].StepRewarded, Is.True);
            Assert.That(state.AugmentPlayers[0].StepFailed, Is.False);
            Assert.That(state.Players[0].upperBonusThreshold, Is.EqualTo(58));
            Assert.That(state.Players[0].stepBonusGranted, Is.True);
            Assert.That(state.Players[0].bonusScore, Is.EqualTo(55));
            Assert.That(state.Players[0].totalScore, Is.EqualTo(113));
            Assert.That(events.Length, Is.EqualTo(1));
            Assert.That(events[0].Score, Is.EqualTo(55));
        }

        [Test]
        public void StepByStep_순서완료시_상단합계가58미만이면_보너스를_유보한다()
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() }
            };
            runtime.Initialize(state, 2);
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.StepByStepId };
            state.AugmentPlayers[0].StepCategoryIndex = 5;
            state.Players[0].upperScores = new[] { 8, 8, 9, 10, 10, 12 };
            state.Players[0].RecalculateTotal();

            YachtGameEvent[] events = runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Sixes);

            Assert.That(state.AugmentPlayers[0].StepRewarded, Is.True);
            Assert.That(state.Players[0].upperBonusThreshold, Is.EqualTo(58));
            Assert.That(state.Players[0].stepBonusGranted, Is.False);
            Assert.That(state.Players[0].bonusScore, Is.Zero);
            Assert.That(events.Length, Is.EqualTo(1));
            Assert.That(events[0].Score, Is.Zero);

            state.Players[0].upperScores[4] = 11;
            runtime.RecalculateStepBonus(state, 0);
            Assert.That(state.Players[0].stepBonusGranted, Is.True);
            Assert.That(state.Players[0].bonusScore, Is.EqualTo(55));
        }

        [Test]
        public void StepByStep_순서를_어기면_실패하고_보상하지_않는다()
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() }
            };
            runtime.Initialize(state, 2);
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.StepByStepId };

            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Deuces);

            Assert.That(state.AugmentPlayers[0].StepFailed, Is.True);
            Assert.That(state.AugmentPlayers[0].StepRewarded, Is.False);
            Assert.That(state.Players[0].upperBonusThreshold, Is.EqualTo(63));
            Assert.That(state.Players[0].stepBonusGranted, Is.False);
        }

        [Test]
        public void RandomBox_퀘스트와_충돌후보를_제외해_결정적으로_교체한다()
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = 1,
                CurrentPlayerIndex = 0,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() }
            };
            runtime.Initialize(state, 2);
            var random = new SequenceRandomSource(
                0, 3, 2, 1,
                0, 3, 2, 1,
                2, 1,
                1);
            Assert.That(runtime.TryBeginDraft(state, random, out _), Is.True);
            state.Draft.Options = new[] { YachtAugmentRuntime.RandomBoxId };
            Assert.That(runtime.TrySelectAugment(state, 0, YachtAugmentRuntime.RandomBoxId, random,
                out _, out _, out _), Is.True);
            state.Draft.Options = new[] { YachtAugmentRuntime.RandomBoxId };
            Assert.That(runtime.TrySelectAugment(state, 1, YachtAugmentRuntime.RandomBoxId, random,
                out YachtGameEvent[] events, out _, out _), Is.True);

            Assert.That(state.Players[0].upperBonusThreshold, Is.EqualTo(58));
            Assert.That(state.Players[1].upperBonusThreshold, Is.EqualTo(58));
            Assert.That(state.AugmentPlayers[0].OwnedIds, Does.Not.Contain(YachtAugmentRuntime.RandomBoxId));
            Assert.That(state.AugmentPlayers[1].OwnedIds, Does.Not.Contain(YachtAugmentRuntime.RandomBoxId));
            Assert.That(state.AugmentPlayers[0].RandomBoxAwardId, Is.Not.Null);
            Assert.That(state.AugmentPlayers[1].RandomBoxAwardId, Is.Not.Null);
            Assert.That(state.AugmentPlayers[0].RandomBoxAwardId, Does.Not.EqualTo(YachtAugmentRuntime.StepByStepId));
            Assert.That(state.AugmentPlayers[1].RandomBoxAwardId, Does.Not.EqualTo(YachtAugmentRuntime.StepByStepId));
            Assert.That(events, Has.Some.Matches<YachtGameEvent>(item => item.Type == YachtGameEventType.AugmentReplaced));

            state.CurrentRound = 6;
            state.Draft.SelectionCounts = new[] { 1, 1 };
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.OctahedronId };
            Assert.That(runtime.TryBeginDraft(state, new SequenceRandomSource(0), out _), Is.True);
            Assert.That(state.Draft.Options, Does.Not.Contain(YachtAugmentRuntime.TableFlipId));
            Assert.That(state.Draft.Options, Does.Not.Contain(YachtAugmentRuntime.StepByStepId));
        }

        [Test]
        public void RandomBox_상대결과를_중복으로보지않고_같은증강을_각자획득할수있다()
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = 1,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() },
                Phase = YachtGamePhase.Draft
            };
            runtime.Initialize(state, 2);
            state.Draft.IsActive = true;
            state.Draft.PlayerIndex = 0;
            state.Draft.Options = new[] { YachtAugmentRuntime.RandomBoxId };
            var random = new SequenceRandomSource(0);

            Assert.That(runtime.TrySelectAugment(state, 0, YachtAugmentRuntime.RandomBoxId, random,
                out _, out _, out _), Is.True);
            state.Draft.Options = new[] { YachtAugmentRuntime.RandomBoxId };
            Assert.That(runtime.TrySelectAugment(state, 1, YachtAugmentRuntime.RandomBoxId, random,
                out _, out _, out _), Is.True);

            Assert.That(state.AugmentPlayers[0].RandomBoxAwardId, Is.Not.Null);
            Assert.That(state.AugmentPlayers[1].RandomBoxAwardId,
                Is.EqualTo(state.AugmentPlayers[0].RandomBoxAwardId));
            Assert.That(state.AugmentPlayers[0].OwnedIds,
                Does.Contain(state.AugmentPlayers[0].RandomBoxAwardId));
            Assert.That(state.AugmentPlayers[1].OwnedIds,
                Does.Contain(state.AugmentPlayers[1].RandomBoxAwardId));
        }

        [Test]
        public void M6_점수교체형18종의_핵심경계를_계산한다()
        {
            Assert.That(AugmentScore(YachtAugmentRuntime.LuckySevensId, ScoreCategory.Aces, 1, 1, 1, 2, 2), Is.EqualTo(15));
            Assert.That(AugmentScore(YachtAugmentRuntime.PerfectSquaresId, ScoreCategory.Aces, 1, 1, 1, 3, 3), Is.EqualTo(12));
            Assert.That(AugmentScore(YachtAugmentRuntime.GamblerId, ScoreCategory.Choice, 4, 5, 5, 5, 5), Is.EqualTo(31));
            Assert.That(AugmentScore(YachtAugmentRuntime.ThreeOfAKindId, ScoreCategory.FourOfAKind, 2, 2, 2, 4, 5), Is.EqualTo(15));
            Assert.That(AugmentScore(YachtAugmentRuntime.TinyHouseId, ScoreCategory.FullHouse, 1, 1, 2, 2, 2), Is.EqualTo(28));
            Assert.That(AugmentScore(YachtAugmentRuntime.TwoPairId, ScoreCategory.FullHouse, 4, 4, 4, 4, 1), Is.EqualTo(15));
            Assert.That(AugmentScore(YachtAugmentRuntime.HeadAndTailId, ScoreCategory.FullHouse, 1, 1, 2, 3, 4), Is.EqualTo(21));
            Assert.That(AugmentScore(YachtAugmentRuntime.EvensId, ScoreCategory.SmallStraight, 2, 2, 4, 4, 6), Is.EqualTo(20));
            Assert.That(AugmentScore(YachtAugmentRuntime.OddsId, ScoreCategory.SmallStraight, 1, 3, 5, 7, 7), Is.EqualTo(20));
            Assert.That(AugmentScore(YachtAugmentRuntime.DoubleLargeStraightId, ScoreCategory.SmallStraight, 1, 2, 3, 4, 5), Is.EqualTo(30));
            Assert.That(AugmentScore(YachtAugmentRuntime.PrimeCollectionId, ScoreCategory.LargeStraight, 2, 3, 5, 7, 7), Is.EqualTo(35));
            Assert.That(AugmentScore(YachtAugmentRuntime.DuplexHouseId, ScoreCategory.LargeStraight, 2, 2, 3, 3, 3), Is.EqualTo(35));
            Assert.That(AugmentScore(YachtAugmentRuntime.MountainId, ScoreCategory.LargeStraight, 2, 3, 4, 5, 6), Is.EqualTo(40));
            Assert.That(AugmentScore(YachtAugmentRuntime.HighDiceId, ScoreCategory.LargeStraight, 4, 5, 5, 6, 6), Is.EqualTo(35));
            Assert.That(AugmentScore(YachtAugmentRuntime.SecondChoiceId, ScoreCategory.Yacht, 1, 2, 3, 4, 5), Is.EqualTo(7));
            Assert.That(AugmentScore(YachtAugmentRuntime.FibonacciId, ScoreCategory.Yacht, 1, 1, 2, 3, 5), Is.EqualTo(25));
            Assert.That(AugmentScore(YachtAugmentRuntime.ReverseChoiceId, ScoreCategory.Yacht, 7, 7, 7, 7, 7), Is.EqualTo(-5));
            Assert.That(AugmentScore(YachtAugmentRuntime.BlackjackId, ScoreCategory.Yacht, 3, 4, 4, 5, 5), Is.EqualTo(21));

            Assert.That(AugmentScore(YachtAugmentRuntime.OddsId, ScoreCategory.SmallStraight, 1, 2, 3, 5, 7), Is.Zero);
            Assert.That(AugmentScore(YachtAugmentRuntime.BlackjackId, ScoreCategory.Yacht, 1, 2, 3, 4, 5), Is.Zero);
        }

        [Test]
        public void M6_기본점수에_강화배율을_적용한뒤_주사위보너스를_더하고_스크래치는0이다()
        {
            var runtime = new YachtAugmentRuntime();
            YachtGameState state = CreateRuntimeState(runtime);
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.MomentumId, YachtAugmentRuntime.GoldenDieId };
            state.AugmentPlayers[0].MomentumState = 1;
            state.AugmentPlayers[0].DoubleDownActive = true;
            YachtDieState[] dice = CreateDice(1, 2, 3, 4, 6);
            dice[0].Type = YachtDieType.Golden;

            YachtScoreCandidate choice = FindCandidate(runtime.CreateScoreCandidates(state, 0, dice), ScoreCategory.Choice);
            YachtScoreCandidate yacht = FindCandidate(runtime.CreateScoreCandidates(state, 0, dice), ScoreCategory.Yacht);

            Assert.That(choice.BaseScore, Is.EqualTo(16));
            Assert.That(choice.DiceBonusScore, Is.EqualTo(2));
            Assert.That(choice.Score, Is.EqualTo(34));
            Assert.That(choice.IsEnhanced, Is.True);
            Assert.That(choice.EnhancementSource, Is.EqualTo("Momentum+DoubleDown"));
            Assert.That(yacht.BaseScore, Is.Zero);
            Assert.That(yacht.Score, Is.Zero);
        }

        [Test]
        public void M6_음수점수를_기입상태와_분리해_저장하고_총점에서_차감한다()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority(new SequenceRandomSource(0));
            Execute(authority, YachtCommandType.StartGame, "start");
            YachtGameState state = authority.CurrentState;
            state.Draft.IsActive = false;
            state.Phase = YachtGamePhase.ScoreSelection;
            state.HasRolled = true;
            state.Dice = CreateDice(7, 7, 7, 7, 7);
            state.Candidates = new[]
            {
                new YachtScoreCandidate { Category = ScoreCategory.Yacht, BaseScore = -5, Score = -5 }
            };

            YachtGameCommandResult result = Execute(authority, YachtCommandType.CommitScore, "negative", category: ScoreCategory.Yacht);

            Assert.That(result.Accepted, Is.True);
            Assert.That(result.State.Players[0].lowerScores[5], Is.EqualTo(-5));
            Assert.That(result.State.Players[0].lowerBaseScores[5], Is.EqualTo(-5));
            Assert.That(result.State.Players[0].lowerFilled[5], Is.True);
            Assert.That(result.State.Players[0].totalScore, Is.EqualTo(-5));
            Assert.That(authority.IsCategoryFilled(0, ScoreCategory.Yacht), Is.True);
        }

        [Test]
        public void M6_요트뱅크는_가장왼쪽킵을_제외해3턴저축하고_다음턴에_지급한다()
        {
            var runtime = new YachtAugmentRuntime();
            YachtGameState state = CreateRuntimeState(runtime);
            state.AugmentPlayers[0].OwnedIds = new[] { YachtAugmentRuntime.YachtBankId };
            state.AugmentPlayers[0].YachtBankRemainingTurns = 3;
            YachtDieState[] dice = CreateDice(6, 2, 3, 4, 5);
            dice[0].IsKept = true;
            dice[0].KeepSlotIndex = 1;
            dice[1].IsKept = true;
            dice[1].KeepSlotIndex = 0;

            YachtScoreCandidate choice = FindCandidate(runtime.CreateScoreCandidates(state, 0, dice), ScoreCategory.Choice);
            Assert.That(choice.BaseScore, Is.EqualTo(18));

            for (int i = 0; i < 3; i++)
                runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, 18, 18, dice, new SequenceRandomSource(0));
            Assert.That(state.AugmentPlayers[0].YachtBankBalance, Is.EqualTo(6));
            Assert.That(state.Players[0].augmentBonusScore, Is.Zero);

            runtime.PrepareTurn(state, 0, new SequenceRandomSource(0), true);
            Assert.That(state.Players[0].augmentBonusScore, Is.EqualTo(6));
        }

        [Test]
        public void M6_프로모션주사위는_획득턴을_건너뛰고_추가턴포함_성장후6에서_소모된다()
        {
            var runtime = new YachtAugmentRuntime();
            YachtGameState state = CreateRuntimeState(runtime);
            YachtAugmentPlayerState player = state.AugmentPlayers[0];
            player.OwnedIds = new[] { YachtAugmentRuntime.PromotionDieId };
            player.PromotionValue = 1;
            player.PromotionActive = true;
            player.PromotionSkipNextGrowth = true;

            runtime.PrepareTurn(state, 0, new SequenceRandomSource(0), true);
            Assert.That(player.PromotionValue, Is.EqualTo(1));
            runtime.PrepareTurn(state, 0, new SequenceRandomSource(0), true);
            Assert.That(player.PromotionValue, Is.EqualTo(2));

            player.PromotionValue = 6;
            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, 6, 6, CreateDice(6), new SequenceRandomSource(0));
            Assert.That(player.PromotionActive, Is.False);
        }

        [Test]
        public void M6_갬빗_등가교환_더블다운_연금술의_수동제약을_권위에서_검증한다()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority(new SequenceRandomSource(2, 3, 4, 5, 0));
            Execute(authority, YachtCommandType.StartGame, "start");
            YachtGameState state = authority.CurrentState;
            state.Draft.IsActive = false;
            state.Phase = YachtGamePhase.TurnReady;
            state.AugmentPlayers[0].OwnedIds = new[]
            {
                YachtAugmentRuntime.GambitId,
                YachtAugmentRuntime.DoubleDownId,
                YachtAugmentRuntime.EquivalentExchangeId,
                YachtAugmentRuntime.DiceAlchemyId
            };
            state.AugmentPlayers[0].TurnsTaken = 8;

            Assert.That(Execute(authority, YachtCommandType.UseAugmentAction, "gambit",
                augmentId: YachtAugmentRuntime.GambitId).Accepted, Is.True);
            Assert.That(state.Dice, Has.Length.EqualTo(4));
            Assert.That(Execute(authority, YachtCommandType.UseAugmentAction, "double",
                augmentId: YachtAugmentRuntime.DoubleDownId).Accepted, Is.True);

            state.Phase = YachtGamePhase.ScoreSelection;
            state.HasRolled = true;
            state.RollsRemaining = 0;
            state.Dice = CreateDice(3, 4, 5, 6);
            state.Dice[1].IsKept = true;
            Assert.That(Execute(authority, YachtCommandType.UseAugmentAction, "alchemy",
                augmentId: YachtAugmentRuntime.DiceAlchemyId).Accepted, Is.True);
            Assert.That(state.Dice[0].Value, Is.EqualTo(2));
            Assert.That(state.Dice[1].Value, Is.EqualTo(4));

            for (int i = 0; i < 3; i++)
                Assert.That(Execute(authority, YachtCommandType.UseAugmentAction, $"exchange-{i}",
                    augmentId: YachtAugmentRuntime.EquivalentExchangeId).Accepted, Is.True);
            Assert.That(state.Players[0].augmentBonusScore, Is.EqualTo(-15));
            Assert.That(Execute(authority, YachtCommandType.UseAugmentAction, "exchange-4",
                augmentId: YachtAugmentRuntime.EquivalentExchangeId).ErrorCode,
                Is.EqualTo(YachtCommandErrorCode.AugmentAlreadyUsed));
        }

        [Test]
        public void M6_퀘스트와_라운드형보상은_기본점수와_개인진행으로_처리한다()
        {
            var runtime = new YachtAugmentRuntime();
            YachtGameState state = CreateRuntimeState(runtime);
            state.CurrentRound = 3;
            state.AugmentPlayers[0].OwnedIds = new[]
            {
                YachtAugmentRuntime.HoldoutId,
                YachtAugmentRuntime.DoublingId,
                YachtAugmentRuntime.ProphetId,
                YachtAugmentRuntime.PiggyBankId,
                YachtAugmentRuntime.DuelId
            };
            state.AugmentPlayers[0].TurnsTaken = 8;
            state.AugmentPlayers[0].ProphetTurnsRemaining = 3;
            state.AugmentPlayers[0].ProphetTargets = new[] { 10, 20, 30 };
            state.AugmentPlayers[0].DuelRound = 3;
            state.RollsRemaining = 2;

            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.FullHouse, 10, 10, CreateDice(1, 1, 2, 2, 2), new SequenceRandomSource(0));
            Assert.That(state.AugmentPlayers[0].HoldoutRewarded, Is.True);
            Assert.That(state.Players[0].augmentBonusScore, Is.EqualTo(14));

            runtime.AfterScoreCommit(state, 0, 1, ScoreCategory.Choice, 10, 10, CreateDice(1, 2, 2, 2, 3), new SequenceRandomSource(0));
            Assert.That(state.AugmentPlayers[0].DoublingRewarded, Is.True);
            Assert.That(state.AugmentPlayers[0].PiggyBankBalance, Is.Zero);
            runtime.AfterScoreCommit(state, 1, 1, ScoreCategory.Choice, 10, 10, CreateDice(2, 2, 2, 2, 2), new SequenceRandomSource(0));
            Assert.That(state.AugmentPlayers[0].DuelResolved, Is.True);
            Assert.That(state.Players[0].augmentBonusScore, Is.EqualTo(41));
        }

        [Test]
        public void M6_삭제및미구현증강은_드래프트정의에서_제외한다()
        {
            var runtime = new YachtAugmentRuntime();
            string[] excluded =
            {
                "anti-ace-deuces", "anti-four-threes", "prime-numbers", "anti-six-fours", "anti-six-fives", "anti-five-sixes",
                "four-by-four", "two-households", "strange-die", "coin-toss"
            };
            for (int i = 0; i < excluded.Length; i++)
                Assert.That(runtime.FindDefinition(excluded[i]), Is.Null, excluded[i]);
        }

        private static LocalGameAuthority CreateAuthority(IRandomSource random)
        {
            return new LocalGameAuthority(new YachtGameOptions
            {
                Mode = YachtGameMode.Normal,
                PresetClipCount = 20
            }, random);
        }

        private static LocalGameAuthority CreateAugmentedAuthority(IRandomSource random)
        {
            return new LocalGameAuthority(new YachtGameOptions
            {
                Mode = YachtGameMode.Augmented,
                PresetClipCount = 20
            }, random);
        }

        private static YachtGameSession CreateSession()
        {
            var session = new YachtGameSession(
                new PlayerScoreData(),
                new PlayerScoreData(),
                new YachtGameOptions { Mode = YachtGameMode.Normal },
                new SequenceRandomSource(0, 1, 2, 3, 4, 0, 0));
            session.StartNewGame();
            return session;
        }

        private static YachtGameCommandResult Execute(
            LocalGameAuthority authority,
            YachtCommandType type,
            string commandId,
            int dieId = 0,
            bool isKept = false,
            ScoreCategory category = default,
            string augmentId = null)
        {
            int playerIndex = type == YachtCommandType.SelectAugment
                ? authority.CurrentState.Draft.PlayerIndex
                : authority.CurrentState.CurrentPlayerIndex;
            return authority.Execute(new YachtGameCommand
            {
                CommandId = commandId,
                ExpectedRevision = authority.CurrentState.Revision,
                PlayerIndex = playerIndex,
                Type = type,
                DieId = dieId,
                IsKept = isKept,
                Category = category,
                AugmentId = augmentId
            });
        }

        private static int GetCandidate(YachtGameState state, ScoreCategory category)
        {
            for (int i = 0; i < state.Candidates.Length; i++)
                if (state.Candidates[i].Category == category) return state.Candidates[i].Score;
            Assert.Fail($"{category} 후보가 없습니다.");
            return 0;
        }

        private static bool IsOwnedByOrGlobal(YachtGameState state, int playerIndex, string augmentId)
        {
            if (System.Array.IndexOf(state.GlobalAugmentIds, augmentId) >= 0) return true;
            return System.Array.IndexOf(state.AugmentPlayers[playerIndex].OwnedIds, augmentId) >= 0;
        }

        private static YachtGameState CreateRuntimeState(YachtAugmentRuntime runtime)
        {
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = 1,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() },
                Dice = CreateDice(1, 2, 3, 4, 5)
            };
            runtime.Initialize(state, 2);
            return state;
        }

        private static YachtDieState[] CreateDice(params int[] values)
        {
            var dice = new YachtDieState[values.Length];
            for (int i = 0; i < values.Length; i++) dice[i] = new YachtDieState { Id = i + 1, Value = values[i] };
            return dice;
        }

        private static int AugmentScore(string augmentId, ScoreCategory category, params int[] values)
        {
            return YachtAugmentScoreEngine.CalculateBaseScores(CreateDice(values), new[] { augmentId })[category];
        }

        private static YachtScoreCandidate FindCandidate(IReadOnlyList<YachtScoreCandidate> candidates, ScoreCategory category)
        {
            for (int i = 0; i < candidates.Count; i++) if (candidates[i].Category == category) return candidates[i];
            Assert.Fail($"{category} 후보가 없습니다.");
            return null;
        }

        private sealed class SequenceRandomSource : IRandomSource
        {
            private readonly IReadOnlyList<int> values;
            private int index;

            public SequenceRandomSource(params int[] values)
            {
                this.values = values.Length > 0 ? values : new[] { 0 };
            }

            public int NextInt(int minInclusive, int maxExclusive)
            {
                int range = maxExclusive - minInclusive;
                int value = values[index++ % values.Count];
                return minInclusive + ((value % range) + range) % range;
            }

            public bool NextBool()
            {
                return NextInt(0, 2) == 1;
            }
        }
    }
}
