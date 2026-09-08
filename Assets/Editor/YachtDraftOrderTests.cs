using System.Collections.Generic;
using NUnit.Framework;
using Tessera.Games.Yacht;

namespace Tessera.Editor.Tests
{
    /// <summary>
    /// 드래프트 제시 구성과 선택 순서를 고정하는 테스트입니다.
    /// 제시 구성은 같은 대상 족보의 변형 증강이 겹치지 않는지, 순서는 첫 증강이 무작위이고
    /// 두 번째부터 총점이 낮은 쪽이 먼저인지를 확인합니다.
    /// </summary>
    [TestFixture]
    public sealed class YachtDraftOrderTests
    {
        [Test]
        public void 드래프트_제시에_같은_족보를_교체하는_변형증강이_둘_이상_나오지_않는다()
        {
            // 셔플 결과가 달라지도록 시드를 바꿔 가며 반복 확인한다.
            for (int seed = 0; seed < 50; seed++)
            {
                var runtime = new YachtAugmentRuntime();
                YachtGameState state = CreateState(runtime, round: 1);
                Assert.That(runtime.TryBeginDraft(state, new SequenceRandom(seed, seed + 1, seed + 2), out _), Is.True);

                var targets = new List<string>();
                foreach (string id in state.Draft.Options)
                {
                    YachtAugmentDefinition definition = runtime.FindDefinition(id);
                    if (definition?.Kind != YachtAugmentKind.Modification) continue;
                    Assert.That(targets, Does.Not.Contain(definition.Target),
                        $"시드 {seed}: 대상 족보 {definition.Target}를 교체하는 변형 증강이 한 제시에 둘 이상 나왔습니다.");
                    targets.Add(definition.Target);
                }
            }
        }

        [Test]
        public void 첫_증강_드래프트는_무작위로_선공을_정한다()
        {
            BeginDraft(round: 1, p1Total: 0, p2Total: 0, random: new SequenceRandom(0), out YachtGameState first);
            BeginDraft(round: 1, p1Total: 0, p2Total: 0, random: new SequenceRandom(1), out YachtGameState second);

            Assert.That(first.Draft.PlayerIndex, Is.Zero);
            Assert.That(second.Draft.PlayerIndex, Is.EqualTo(1), "무작위 값이 다르면 선공도 달라져야 합니다.");
        }

        [Test]
        public void 두번째_드래프트부터는_총점이_낮은_쪽이_먼저_고른다()
        {
            BeginDraft(round: 6, p1Total: 40, p2Total: 12, random: new SequenceRandom(0), out YachtGameState state);

            Assert.That(state.Draft.PlayerIndex, Is.EqualTo(1));
        }

        [Test]
        public void 세번째_드래프트도_총점이_낮은_쪽이_먼저_고른다()
        {
            BeginDraft(round: 9, p1Total: 15, p2Total: 90, random: new SequenceRandom(0), out YachtGameState state);

            Assert.That(state.Draft.PlayerIndex, Is.Zero);
        }

        [Test]
        public void 선공이_고른_뒤에는_남은_플레이어에게_차례가_넘어간다()
        {
            YachtAugmentRuntime runtime = BeginDraft(
                round: 6, p1Total: 40, p2Total: 12, random: new SequenceRandom(0), out YachtGameState state);

            Assert.That(state.Draft.PlayerIndex, Is.EqualTo(1));

            bool selected = runtime.TrySelectAugment(
                state, 1, state.Draft.Options[0], new SequenceRandom(0), out _, out _, out _);

            Assert.That(selected, Is.True);
            Assert.That(state.Draft.PlayerIndex, Is.Zero, "선공이 고른 뒤에는 후공 차례여야 합니다.");
        }

        private static YachtAugmentRuntime BeginDraft(
            int round, int p1Total, int p2Total, IRandomSource random, out YachtGameState state)
        {
            var runtime = new YachtAugmentRuntime();
            state = CreateState(runtime, round);
            state.Players[0].totalScore = p1Total;
            state.Players[1].totalScore = p2Total;

            // 라운드 6·9는 이전 드래프트가 끝나 있어야 한다.
            int alreadyTaken = round >= 9 ? 2 : round >= 6 ? 1 : 0;
            state.Draft.SelectionCounts = new[] { alreadyTaken, alreadyTaken };

            Assert.That(runtime.TryBeginDraft(state, random, out _), Is.True);
            return runtime;
        }

        private static YachtGameState CreateState(YachtAugmentRuntime runtime, int round)
        {
            var state = new YachtGameState
            {
                Mode = YachtGameMode.Augmented,
                CurrentRound = round,
                Players = new[] { new PlayerScoreData(), new PlayerScoreData() },
                Dice = Dice(1, 2, 3, 4, 5)
            };
            runtime.Initialize(state, 2);
            state.Draft.SelectionCounts = new[] { 0, 0 };
            return state;
        }

        private static YachtDieState[] Dice(params int[] values)
        {
            var dice = new YachtDieState[values.Length];
            for (int i = 0; i < values.Length; i++) dice[i] = new YachtDieState { Id = i, Value = values[i] };
            return dice;
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
                return minInclusive + (value % range + range) % range;
            }

            public bool NextBool() => NextInt(0, 2) == 1;
        }
    }
}
