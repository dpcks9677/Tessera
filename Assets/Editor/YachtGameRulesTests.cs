using NUnit.Framework;
using Tessera.Games.AugmentedYacht;

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

            var fullHouse = YachtScoreCalculator.Calculate(new[] { 2, 2, 3, 3, 3 });
            Assert.That(fullHouse[ScoreCategory.FullHouse], Is.EqualTo(13));
        }

        [Test]
        public void PlayerScoreData_상단합계63점부터_35점보너스를_적용한다()
        {
            var data = new PlayerScoreData
            {
                upperScores = new[] { 3, 6, 9, 12, 15, 18 }
            };

            data.RecalculateTotal();

            Assert.That(data.CalculateUpperSum(), Is.EqualTo(63));
            Assert.That(data.hasBonus, Is.True);
            Assert.That(data.bonusScore, Is.EqualTo(35));
            Assert.That(data.totalScore, Is.EqualTo(98));
        }

        [Test]
        public void Session_턴당_굴림을_세번으로_제한한다()
        {
            YachtGameSession session = CreateStartedSession();
            int[] dice = { 1, 2, 3, 4, 5 };

            for (int expectedRemaining = 2; expectedRemaining >= 0; expectedRemaining--)
            {
                Assert.That(session.TryBeginRoll(), Is.True);
                Assert.That(session.RollsRemaining, Is.EqualTo(expectedRemaining));
                Assert.That(session.CompleteRoll(dice), Is.True);
            }

            Assert.That(session.TryBeginRoll(), Is.False);
        }

        [Test]
        public void Session_점수확정후_P1_P2_라운드를_순서대로_전환한다()
        {
            YachtGameSession session = CreateStartedSession();
            Assert.That(session.TryBeginRoll(), Is.True);
            Assert.That(session.CompleteRoll(new[] { 1, 1, 1, 2, 3 }), Is.True);
            Assert.That(session.TryCommitScore(ScoreCategory.Aces, out YachtTurnResult p1), Is.True);
            Assert.That(p1.ScoredPlayerIndex, Is.Zero);
            Assert.That(session.Phase, Is.EqualTo(YachtGamePhase.TurnTransition));
            Assert.That(session.CurrentPlayerIndex, Is.Zero);
            Assert.That(session.AdvanceTurnAfterAnimation(), Is.True);
            Assert.That(session.CurrentPlayerIndex, Is.EqualTo(1));
            Assert.That(session.CurrentRound, Is.EqualTo(1));

            Assert.That(session.TryBeginRoll(), Is.True);
            Assert.That(session.CompleteRoll(new[] { 2, 2, 2, 3, 4 }), Is.True);
            Assert.That(session.TryCommitScore(ScoreCategory.Deuces, out YachtTurnResult p2), Is.True);
            Assert.That(p2.ScoredPlayerIndex, Is.EqualTo(1));
            Assert.That(session.Phase, Is.EqualTo(YachtGamePhase.TurnTransition));
            Assert.That(session.AdvanceTurnAfterAnimation(), Is.True);
            Assert.That(session.CurrentPlayerIndex, Is.Zero);
            Assert.That(session.CurrentRound, Is.EqualTo(2));
        }

        [Test]
        public void Session_미굴림_시간초과는_족보순서의_빈칸을_0점처리한다()
        {
            YachtGameSession session = CreateStartedSession();

            Assert.That(session.ResolveTimeout(out YachtTurnResult result), Is.True);

            Assert.That(result.Category, Is.EqualTo(ScoreCategory.Aces));
            Assert.That(result.Score, Is.Zero);
            Assert.That(session.GetPlayer(0).upperScores[0], Is.Zero);
            Assert.That(session.AdvanceTurnAfterAnimation(), Is.True);
            Assert.That(session.CurrentPlayerIndex, Is.EqualTo(1));
        }

        [Test]
        public void Session_24개_개인턴후_종료하고_재시작할수있다()
        {
            YachtGameSession session = CreateStartedSession();
            YachtTurnResult result = default;

            for (int turn = 0; turn < 24; turn++)
            {
                Assert.That(session.ResolveTimeout(out result), Is.True, $"turn {turn + 1}");
                if (!result.GameEnded) Assert.That(session.AdvanceTurnAfterAnimation(), Is.True, $"turn {turn + 1} transition");
            }

            Assert.That(result.GameEnded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(YachtGamePhase.GameOver));
            Assert.That(session.RollsRemaining, Is.Zero);

            session.StartNewGame();
            Assert.That(session.Phase, Is.EqualTo(YachtGamePhase.TurnReady));
            Assert.That(session.CurrentPlayerIndex, Is.Zero);
            Assert.That(session.CurrentRound, Is.EqualTo(1));
            Assert.That(session.RollsRemaining, Is.EqualTo(3));
            Assert.That(session.GetPlayer(0).upperScores[0], Is.EqualTo(-1));
        }

        private static YachtGameSession CreateStartedSession()
        {
            var session = new YachtGameSession(new PlayerScoreData(), new PlayerScoreData());
            session.StartNewGame();
            return session;
        }
    }
}
