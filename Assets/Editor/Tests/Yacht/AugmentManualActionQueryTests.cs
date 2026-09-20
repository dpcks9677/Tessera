using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// <see cref="LocalGameAuthority.CanUseAugmentAction"/>이 상태를 바꾸지 않고
    /// <see cref="LocalGameAuthority"/> 내부의 실제 사용 판정(UseAugmentAction)과 같은 결과를 내는지 검증한다.
    /// </summary>
    [TestFixture]
    public sealed class AugmentManualActionQueryTests
    {
        [Test]
        public void CanUseAugmentAction_AfterTableFlipUsed_ReturnsFalseAndDoesNotMutateState()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority();
            ExecuteAuthority(authority, YachtCommandType.StartGame, "start");
            authority.DebugGrantAugment(0, YachtAugmentRuntime.TableFlipId, out _, out _);

            YachtGameState state = authority.CurrentState;
            state.Draft.IsActive = false;
            state.Phase = YachtGamePhase.ScoreSelection;
            state.HasRolled = true;

            YachtGameCommandResult use = ExecuteAuthority(
                authority, YachtCommandType.UseAugmentAction, "use-table-flip", augmentId: YachtAugmentRuntime.TableFlipId);
            Assert.That(use.Accepted, Is.True);

            bool tableFlipUsedBefore = state.AugmentPlayers[0].TableFlipUsed;
            long revisionBefore = state.Revision;

            bool canUse = authority.CanUseAugmentAction(0, YachtAugmentRuntime.TableFlipId, out YachtCommandErrorCode code, out string message);

            Assert.That(canUse, Is.False);
            Assert.That(code, Is.EqualTo(YachtCommandErrorCode.AugmentAlreadyUsed));
            Assert.That(message, Is.Not.Null.And.Not.Empty);
            Assert.That(state.AugmentPlayers[0].TableFlipUsed, Is.EqualTo(tableFlipUsedBefore));
            Assert.That(state.Revision, Is.EqualTo(revisionBefore));
        }

        [Test]
        public void CanUseAugmentAction_AfterEquivalentExchangeExhausted_ReturnsFalseAndDoesNotMutateState()
        {
            LocalGameAuthority authority = CreateAugmentedAuthority();
            ExecuteAuthority(authority, YachtCommandType.StartGame, "start");
            authority.DebugGrantAugment(0, YachtAugmentRuntime.EquivalentExchangeId, out _, out _);

            YachtGameState state = authority.CurrentState;
            state.Draft.IsActive = false;
            state.Phase = YachtGamePhase.ScoreSelection;
            state.HasRolled = true;
            state.RollsRemaining = 0;

            for (int i = 0; i < 3; i++)
            {
                YachtGameCommandResult use = ExecuteAuthority(
                    authority, YachtCommandType.UseAugmentAction, $"use-exchange-{i}", augmentId: YachtAugmentRuntime.EquivalentExchangeId);
                Assert.That(use.Accepted, Is.True);
                state.HasRolled = true;
                state.RollsRemaining = 0;
            }

            int usesBefore = state.AugmentPlayers[0].EquivalentExchangeUses;
            int bonusScoreBefore = state.Players[0].augmentBonusScore;
            long revisionBefore = state.Revision;

            bool canUse = authority.CanUseAugmentAction(0, YachtAugmentRuntime.EquivalentExchangeId, out YachtCommandErrorCode code, out string message);

            Assert.That(canUse, Is.False);
            Assert.That(code, Is.EqualTo(YachtCommandErrorCode.AugmentAlreadyUsed));
            Assert.That(message, Is.Not.Null.And.Not.Empty);
            Assert.That(state.AugmentPlayers[0].EquivalentExchangeUses, Is.EqualTo(usesBefore));
            Assert.That(state.Players[0].augmentBonusScore, Is.EqualTo(bonusScoreBefore));
            Assert.That(state.Revision, Is.EqualTo(revisionBefore));
        }

        private static LocalGameAuthority CreateAugmentedAuthority()
        {
            return new LocalGameAuthority(new YachtGameOptions
            {
                Mode = YachtGameMode.Augmented,
                PresetClipCount = 20
            });
        }

        private static YachtGameCommandResult ExecuteAuthority(
            LocalGameAuthority authority,
            YachtCommandType type,
            string commandId,
            string augmentId = null)
        {
            int playerIndex = authority.CurrentState.CurrentPlayerIndex;
            return authority.Execute(new YachtGameCommand
            {
                CommandId = commandId,
                ExpectedRevision = authority.CurrentState.Revision,
                PlayerIndex = playerIndex,
                Type = type,
                AugmentId = augmentId
            });
        }
    }
}
