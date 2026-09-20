using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>수동 행동 증강 5종의 <see cref="IAugmentProgressText.DescribeProgress"/> 출력 문구를 검증한다.</summary>
    [TestFixture]
    public sealed class AugmentManualActionProgressTextTests
    {
        private YachtAugmentPlayerState player;
        private PlayerScoreData scores;

        [SetUp]
        public void SetUp()
        {
            player = new YachtAugmentPlayerState();
            scores = new PlayerScoreData();
        }

        private AugmentProgressQuery Query() => new(player, scores);

        [Test]
        public void TableFlip_ReportsAvailableThenUsed()
        {
            var state = new TableFlipState();
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용 가능"));

            state.IsUsed = true;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용함"));
        }

        [Test]
        public void DiceAlchemy_ReportsAvailableThenUsed()
        {
            var state = new DiceAlchemyState();
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용 가능"));

            state.IsUsed = true;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용함"));
        }

        [Test]
        public void DoubleDown_ReportsTurnGateThenAvailableThenUsedWithActiveMultiplier()
        {
            var state = new DoubleDownState();
            player.TurnsTaken = 0;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("9턴부터 사용 가능"));

            player.TurnsTaken = DoubleDown.MinTurnsTaken;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용 가능"));

            state.IsUsed = true;
            state.IsActive = true;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용함 · 배율 적용 중"));

            state.IsActive = false;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용함"));
        }

        [Test]
        public void EquivalentExchange_ReportsRemainingUsesAtZeroOneAndThree()
        {
            var state = new EquivalentExchangeState { Uses = 0 };
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("남은 사용 3/3"));

            state.Uses = 1;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("남은 사용 2/3"));

            state.Uses = 3;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("모두 사용함"));
        }

        [Test]
        public void Gambit_ReportsEachStateStepText()
        {
            var state = new GambitState { State = 0 };
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용 가능"));

            state.State = 1;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용함 · 이번 턴 주사위 4개"));

            state.State = 2;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용함 · 다음 턴 주사위 6개"));

            state.State = 3;
            Assert.That(state.DescribeProgress(Query()).Lines[0].Text, Is.EqualTo("사용함"));
        }
    }
}
