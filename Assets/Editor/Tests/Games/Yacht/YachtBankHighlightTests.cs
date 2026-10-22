using NUnit.Framework;
using Tessera.Games.Yacht;
using Tessera.Games.Yacht.Presentation;

namespace Tessera.Editor.Tests
{
    /// <summary>요트 뱅크 킵 칸 하이라이트의 표시 판정(M17-T25)을 고정하는 테스트입니다.</summary>
    [TestFixture]
    public sealed class YachtBankHighlightTests
    {
        private YachtGameState state;

        [SetUp]
        public void SetUp()
        {
            var runtime = new YachtAugmentRuntime();
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

        private void Own(int playerIndex, int remainingTurns)
        {
            YachtAugmentPlayerState player = state.AugmentPlayers[playerIndex];
            player.OwnedIds = new[] { YachtAugmentRuntime.YachtBankId };
            player.YachtBankRemainingTurns = remainingTurns;
            player.YachtBankPayoutPending = false;
            player.YachtBankPaid = false;
        }

        [Test]
        public void NotOwned_ReturnsFalse()
        {
            Assert.That(YachtTurnFlowPresenter.ShouldShowYachtBankHighlight(state, false), Is.False);
        }

        [Test]
        public void OwnedWithThreeTurnsLeft_ReturnsTrue()
        {
            Own(0, 3);

            Assert.That(YachtTurnFlowPresenter.ShouldShowYachtBankHighlight(state, false), Is.True);
        }

        [Test]
        public void OwnedWithOneTurnLeft_ReturnsTrue()
        {
            Own(0, 1);

            Assert.That(YachtTurnFlowPresenter.ShouldShowYachtBankHighlight(state, false), Is.True);
        }

        [Test]
        public void OnlyOpponentOwns_ReturnsFalse()
        {
            Own(1, 3);

            Assert.That(YachtTurnFlowPresenter.ShouldShowYachtBankHighlight(state, false), Is.False);
        }

        [Test]
        public void OwnedWithPayoutPending_ReturnsFalse()
        {
            Own(0, 0);
            state.AugmentPlayers[0].YachtBankPayoutPending = true;

            Assert.That(YachtTurnFlowPresenter.ShouldShowYachtBankHighlight(state, false), Is.False);
        }

        [Test]
        public void OwnedAndPaid_ReturnsFalse()
        {
            Own(0, 3);
            state.AugmentPlayers[0].YachtBankPaid = true;

            Assert.That(YachtTurnFlowPresenter.ShouldShowYachtBankHighlight(state, false), Is.False);
        }

        [Test]
        public void OwnedButDrafting_ReturnsFalse()
        {
            Own(0, 3);

            Assert.That(YachtTurnFlowPresenter.ShouldShowYachtBankHighlight(state, true), Is.False);
        }
    }
}
