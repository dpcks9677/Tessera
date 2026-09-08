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
        public void 변형증강을_획득하면_대상_칸에_부착_요청이_생긴다()
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
        public void 변형이_아닌_계열을_획득하면_요청이_생기지_않는다()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(),
                Event(YachtGameEventType.AugmentSelected, 0, augmentId: GoldenDie),
                Event(YachtGameEventType.AugmentSelected, 1, augmentId: "fast-straight"));

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void 랜덤박스가_변형증강을_주면_부착_요청이_생긴다()
        {
            YachtGameEvent replaced = Event(YachtGameEventType.AugmentReplaced, 1, augmentId: "random-box");
            replaced.RelatedAugmentId = Mountain;

            List<AugmentVfxRequest> requests = Plan(State(), replaced);

            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests[0].Category, Is.EqualTo(ScoreCategory.LargeStraight));
            Assert.That(requests[0].PlayerIndex, Is.EqualTo(1));
        }

        [Test]
        public void 교체된_칸으로_점수를_확정하면_낙인_요청이_생긴다()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.ScoreCommitted, 0, category: ScoreCategory.Aces));

            Assert.That(requests.Count, Is.EqualTo(1));
            Assert.That(requests[0].Cue, Is.EqualTo(AugmentVfxCue.StickerStamp));
            Assert.That(requests[0].AugmentId, Is.EqualTo(LuckySevens));
        }

        [Test]
        public void 교체되지_않은_칸으로_확정하면_요청이_생기지_않는다()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.ScoreCommitted, 0, category: ScoreCategory.Deuces));

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void 증강을_가지지_않은_상대의_확정에는_반응하지_않는다()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.ScoreCommitted, 1, category: ScoreCategory.Aces));

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void 여러_이벤트가_섞여_와도_관련된_것만_순서대로_옮긴다()
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
        public void AugmentTriggered는_변형계열에서_쓰지_않으므로_무시한다()
        {
            List<AugmentVfxRequest> requests = Plan(
                State(LuckySevens),
                Event(YachtGameEventType.AugmentTriggered, 0, augmentId: LuckySevens));

            Assert.That(requests, Is.Empty);
        }

        [Test]
        public void 잘못된_입력에도_예외_없이_무시한다()
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
        public void 결과를_비우지_않고_이어_붙인다()
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
