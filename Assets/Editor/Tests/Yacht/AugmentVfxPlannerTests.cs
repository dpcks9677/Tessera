using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 권위 이벤트를 변형 증강 스티커 연출로 옮기는 규칙을 고정합니다.
    /// 변형 증강은 <c>AugmentTriggered</c>를 내지 않으므로 획득과 점수 확정만 신호로 씁니다.
    /// 근거는 <c>docs/augmented_yacht_m17_vfx_spec.md</c> §3.1.1입니다.
    /// </summary>
    [TestFixture]
    public sealed class AugmentVfxPlannerTests
    {
        private const string LuckySevens = "lucky-sevens";
        private const string Mountain = "mountain";
        private const string GoldenDie = "golden-die";

        [Test]
        public void AcquiringModificationAugmentCreatesAttachRequestOnTargetCategory()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(),
                Event(YachtGameEventType.AugmentSelected, 0, augmentId: LuckySevens));

            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests[0].Cue, Is.EqualTo(AugmentVfxCue.StickerAttach));
            Assert.That(requests[0].Category, Is.EqualTo(ScoreCategory.Aces));
            Assert.That(requests[0].PlayerIndex, Is.Zero);
        }

        [Test]
        public void AcquiringNonModificationKindCreatesNoRequest()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(),
                Event(YachtGameEventType.AugmentSelected, 0, augmentId: GoldenDie),
                Event(YachtGameEventType.AugmentSelected, 1, augmentId: "fast-straight"));

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void RandomBoxGrantingModificationAugmentCreatesAttachRequest()
        {
            YachtGameEvent replaced = Event(YachtGameEventType.AugmentReplaced, 1, augmentId: "random-box");
            replaced.RelatedAugmentId = Mountain;

            List<AugmentVfxRequest> requests = Plan(State(), replaced);

            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests[0].Category, Is.EqualTo(ScoreCategory.LargeStraight));
            Assert.That(requests[0].PlayerIndex, Is.EqualTo(1));
        }

        [Test]
        public void CommittingScoreOnReplacedCategoryCreatesBrandRequest()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.ScoreCommitted, 0, category: ScoreCategory.Aces));

            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests[0].Cue, Is.EqualTo(AugmentVfxCue.StickerStamp));
            Assert.That(requests[0].AugmentId, Is.EqualTo(LuckySevens));
        }

        [Test]
        public void CommittingOnUnreplacedCategoryCreatesNoRequest()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.ScoreCommitted, 0, category: ScoreCategory.Deuces));

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void CommitByOpponentWithoutAugmentIsIgnored()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.ScoreCommitted, 1, category: ScoreCategory.Aces));

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void MixedEventsCarryOnlyRelevantOnesInOrder()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.DiceRolled, 0),
                Event(YachtGameEventType.ScoreCommitted, 0, category: ScoreCategory.Aces),
                Event(YachtGameEventType.AugmentTriggered, 0, augmentId: "holdout"),
                Event(YachtGameEventType.AugmentSelected, 0, augmentId: Mountain),
                Event(YachtGameEventType.TurnAdvanced, 0));

            Assert.That(requests.Count, Is.EqualTo(2));
            Assert.That(requests[0].Cue, Is.EqualTo(AugmentVfxCue.StickerStamp));
            Assert.That(requests[1].Cue, Is.EqualTo(AugmentVfxCue.StickerAttach));
        }

        [Test]
        public void AugmentTriggeredIsIgnoredForModificationKind()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.AugmentTriggered, 0, augmentId: LuckySevens));

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void TriggeringDiceAlchemyCreatesSmokeCoverRequest()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(),
                Event(YachtGameEventType.AugmentActionUsed, 0, augmentId: YachtAugmentRuntime.DiceAlchemyId));

            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests[0].Cue, Is.EqualTo(AugmentVfxCue.DiceSmokeSwap));
            Assert.That(requests[0].PlayerIndex, Is.Zero);
        }

        [Test]
        public void TriggeringOtherManualActionCreatesNoRequest()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(),
                Event(YachtGameEventType.AugmentActionUsed, 0, augmentId: "table-flip"));

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void DiceAlchemyMixedWithOtherEventsCarriesInOrder()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.DiceRolled, 0),
                Event(YachtGameEventType.ScoreCommitted, 0, category: ScoreCategory.Aces),
                Event(YachtGameEventType.AugmentActionUsed, 0, augmentId: YachtAugmentRuntime.DiceAlchemyId),
                Event(YachtGameEventType.TurnAdvanced, 0));

            Assert.That(requests.Count, Is.EqualTo(2));
            Assert.That(requests[0].Cue, Is.EqualTo(AugmentVfxCue.StickerStamp));
            Assert.That(requests[1].Cue, Is.EqualTo(AugmentVfxCue.DiceSmokeSwap));
        }

        [Test]
        public void InvalidInputIsIgnoredWithoutException()
        {
            YachtGameState state = State(LuckySevens);
            var requests = new List<AugmentVfxRequest>();

            AugmentVfxPlanner.Plan(null, state, requests);
            AugmentVfxPlanner.Plan(new YachtGameEvent[] { null }, state, requests);
            AugmentVfxPlanner.Plan(new[] { Event(YachtGameEventType.ScoreCommitted, 7) }, state, requests);
            AugmentVfxPlanner.Plan(new[] { Event(YachtGameEventType.AugmentSelected, 0, augmentId: "no-such-id") }, state, requests);

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void ResultsAreAppendedWithoutClearing()
        {
            YachtGameState state = State();
            var requests = new List<AugmentVfxRequest>();

            AugmentVfxPlanner.Plan(new[] { Event(YachtGameEventType.AugmentSelected, 0, augmentId: Mountain) }, state, requests);
            AugmentVfxPlanner.Plan(new[] { Event(YachtGameEventType.AugmentSelected, 0, augmentId: "evens") }, state, requests);

            Assert.That(requests.Count, Is.EqualTo(2), "여러 명령의 결과를 한 큐에 모을 수 있어야 합니다.");
        }

        private static List<AugmentVfxRequest> Plan(YachtGameState state, params YachtGameEvent[] events)
        {
            var requests = new List<AugmentVfxRequest>();
            AugmentVfxPlanner.Plan(events, state, requests);
            return requests;
        }

        private static YachtGameEvent Event(
            YachtGameEventType type,
            int playerIndex,
            string augmentId = null,
            ScoreCategory category = ScoreCategory.Aces) => new()
            {
                Type = type,
                PlayerIndex = playerIndex,
                AugmentId = augmentId,
                Category = category
            };

        private static YachtGameState State(params string[] playerOneOwned)
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = 1,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() }
            };
            runtime.Initialize(state, 2);
            state.AugmentPlayers[0].OwnedIds = playerOneOwned ?? System.Array.Empty<string>();
            return state;
        }
    }
}
