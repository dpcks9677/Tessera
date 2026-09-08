using System;
using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Games.AugmentedYacht;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 변형 증강 스티커의 색상코드와 붙을 자리를 고정합니다.
    /// 점수표는 플레이어마다 Categories 열을 하나씩 가지므로 수집도 한 사람 단위입니다.
    /// 근거는 <c>docs/augmented_yacht_m17_vfx_spec.md</c> §3.1.1입니다.
    /// </summary>
    [TestFixture]
    public sealed class AugmentStickerCatalogTests
    {
        [Test]
        public void 활성_변형증강_18개가_모두_색상코드와_대상칸을_가진다()
        {
            var runtime = new YachtAugmentRuntime();
            var modification = new List<YachtAugmentDefinition>();
            foreach (YachtAugmentDefinition definition in runtime.GetDefinitions())
                if (definition.Kind == YachtAugmentKind.Modification) modification.Add(definition);

            Assert.That(modification.Count, Is.EqualTo(18));
            foreach (YachtAugmentDefinition definition in modification)
            {
                Assert.That(AugmentStickerCatalog.HasSticker(definition), Is.True, definition.Id);
                Assert.That(AugmentStickerCatalog.TryGetTargetCategory(definition, out ScoreCategory category), Is.True,
                    $"{definition.Id}의 대상 칸 '{definition.Target}'을 해석하지 못했습니다.");
                Assert.That((int)category, Is.InRange(0, 13), $"{definition.Id}의 대상 칸이 점수표 밖입니다.");
                Assert.That(AugmentStickerCatalog.BaseColor(definition.Id).a, Is.Not.Zero, definition.Id);
            }
        }

        [Test]
        public void 활성_변형증강_18개가_모두_영문_축약표기를_가진다()
        {
            var runtime = new YachtAugmentRuntime();
            const string fallback = "<축약어 없음>";

            foreach (YachtAugmentDefinition definition in runtime.GetDefinitions())
            {
                if (definition.Kind != YachtAugmentKind.Modification) continue;

                string mark = AugmentStickerCatalog.MarkLabel(definition.Id, fallback);
                Assert.That(mark, Is.Not.EqualTo(fallback), $"{definition.Id}의 축약 표기가 없습니다.");
                Assert.That(mark, Is.Not.Empty, definition.Id);

                foreach (char c in mark)
                {
                    Assert.That(c, Is.LessThan((char)128),
                        $"{definition.Id}의 축약 표기 '{mark}'에 비ASCII 문자가 있습니다. 스티커 표기는 영문입니다.");
                }
            }
        }

        [Test]
        public void 축약어가_없는_증강은_증강_이름을_그대로_쓴다()
        {
            Assert.That(AugmentStickerCatalog.MarkLabel("존재하지-않는-증강", "폴백"), Is.EqualTo("폴백"));
            Assert.That(AugmentStickerCatalog.MarkLabel(null, "폴백"), Is.EqualTo("폴백"));
        }

        [Test]
        public void 변형이_아닌_계열은_스티커를_붙이지_않는다()
        {
            var runtime = new YachtAugmentRuntime();

            foreach (YachtAugmentDefinition definition in runtime.GetDefinitions())
            {
                if (definition.Kind == YachtAugmentKind.Modification) continue;
                Assert.That(AugmentStickerCatalog.HasSticker(definition), Is.False, definition.Id);
            }
        }

        [Test]
        public void 색상코드를_지정하지_않은_증강은_기본_버건디를_쓴다()
        {
            Assert.That(AugmentStickerCatalog.BaseColor("lucky-sevens"),
                Is.EqualTo(AugmentStickerCatalog.DefaultBase));
            Assert.That(AugmentStickerCatalog.BaseColor(null),
                Is.EqualTo(AugmentStickerCatalog.DefaultBase));
        }

        [Test]
        public void 리버스초이스는_역방향이라_다른_색을_쓴다()
        {
            Assert.That(AugmentStickerCatalog.BaseColor(YachtAugmentRuntime.ReverseChoiceId),
                Is.Not.EqualTo(AugmentStickerCatalog.DefaultBase));
        }

        [Test]
        public void 현재_플레이어의_변형증강만_수집한다()
        {
            YachtGameState state = State(new[] { "lucky-sevens", "golden-die" }, new[] { "mountain" });

            List<AugmentStickerPlacement> first = Collect(state, 0);
            List<AugmentStickerPlacement> second = Collect(state, 1);

            Assert.That(first.Count, Is.EqualTo(1), "강화 계열은 스티커를 만들지 않습니다.");
            Assert.That(first[0].Category, Is.EqualTo(ScoreCategory.Aces));
            Assert.That(second.Count, Is.EqualTo(1));
            Assert.That(second[0].Category, Is.EqualTo(ScoreCategory.LargeStraight));
        }

        [Test]
        public void 스티커는_대상_족보_칸에만_한_장_붙는다()
        {
            YachtGameState state = State(new[] { YachtAugmentRuntime.DoubleLargeStraightId }, null);

            List<AugmentStickerPlacement> placements = Collect(state, 0);

            // 보너스 행에는 붙이지 않는다. 그 칸의 Categories 자리는 "Bonus (0/63)" 진행 표시가
            // 차지하고 있어 스티커로 덮으면 정보가 사라진다. 상단 보너스 기준 변경 표시는 M17-T10 범위다.
            Assert.That(placements.Count, Is.EqualTo(1));
            Assert.That(placements[0].Category, Is.EqualTo(ScoreCategory.SmallStraight));
        }

        [Test]
        public void 결과_목록은_비우고_채운다()
        {
            YachtGameState state = State(new[] { "lucky-sevens", "mountain" }, new[] { "evens" });
            var reused = new List<AugmentStickerPlacement>();

            AugmentStickerCatalog.CollectFor(state, 0, reused);
            AugmentStickerCatalog.CollectFor(state, 1, reused);

            Assert.That(reused.Count, Is.EqualTo(1), "턴이 바뀌면 이전 플레이어 스티커가 남으면 안 됩니다.");
            Assert.That(reused[0].AugmentId, Is.EqualTo("evens"));
        }

        [Test]
        public void 잘못된_입력에도_예외_없이_빈_결과를_준다()
        {
            YachtGameState state = State(new[] { "lucky-sevens" }, null);
            var placements = new List<AugmentStickerPlacement>();

            AugmentStickerCatalog.CollectFor(null, 0, placements);
            Assert.That(placements, Is.Empty);

            AugmentStickerCatalog.CollectFor(state, 9, placements);
            Assert.That(placements, Is.Empty);

            Assert.DoesNotThrow(() => AugmentStickerCatalog.CollectFor(state, 0, null));
        }

        private static List<ScoreCategory> Categories(List<AugmentStickerPlacement> placements)
        {
            var result = new List<ScoreCategory>();
            foreach (AugmentStickerPlacement placement in placements) result.Add(placement.Category);
            return result;
        }

        private static List<AugmentStickerPlacement> Collect(YachtGameState state, int playerIndex)
        {
            var placements = new List<AugmentStickerPlacement>();
            AugmentStickerCatalog.CollectFor(state, playerIndex, placements);
            return placements;
        }

        private static YachtGameState State(string[] playerOne, string[] playerTwo)
        {
            var runtime = new YachtAugmentRuntime();
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = 1,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() }
            };
            runtime.Initialize(state, 2);
            state.AugmentPlayers[0].OwnedIds = playerOne ?? Array.Empty<string>();
            state.AugmentPlayers[1].OwnedIds = playerTwo ?? Array.Empty<string>();
            return state;
        }
    }
}
