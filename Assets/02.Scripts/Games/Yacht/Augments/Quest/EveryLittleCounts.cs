using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class EveryLittleCountsState : IAugmentState, IAugmentProgressText
    {
        public int Count;
        public bool Rewarded;

        public IAugmentState Clone() => new EveryLittleCountsState
        {
            Count = Count,
            Rewarded = Rewarded
        };

        public AugmentProgress DescribeProgress(in AugmentProgressQuery query)
        {
            AugmentProgressOutcome outcome = Rewarded ? AugmentProgressOutcome.Succeeded : AugmentProgressOutcome.InProgress;
            var lines = new[]
            {
                new AugmentProgressLine($"족보 기입에 사용한 1의 눈 모으기 ({Count}/{EveryLittleCounts.RequiredCount})", Count >= EveryLittleCounts.RequiredCount)
            };
            return new AugmentProgress(outcome, lines);
        }
    }

    /// <summary>족보 기입 시 사용된 1의 눈금을 누적하여 7개에 도달하면 +15점입니다.</summary>
    public sealed class EveryLittleCounts : QuestAugment, IOnAugmentSelected, IAfterScoreCommit
    {
        public const int RequiredCount = 7;
        public const int RewardScore = 15;

        public override string Id => YachtAugmentRuntime.EveryLittleId;

        public override string DisplayName => "티끌 모아 태산";

        public override string Description => "족보 기입 시 사용된 1의 눈금을 누적하여 7개에 도달하면 +15점입니다.";

        // 획득 시점에 상태를 만들어 둔다. 그래야 첫 점수 기입 전에도 카드에 초기 진행도가 뜬다.
        // 기본값이 그대로 올바른 시작 진행도라 따로 설정할 값은 없다.
        public void OnSelected(AugmentSelectionContext context) => context.State<EveryLittleCountsState>();

        private static EveryLittleCountsState GetOrSync(AugmentContext context)
        {
            var state = context.State<EveryLittleCountsState>();
            if (!state.Rewarded && (context.Player.EveryLittleCount > 0 || context.Player.EveryLittleRewarded))
            {
                state.Count = context.Player.EveryLittleCount;
                state.Rewarded = context.Player.EveryLittleRewarded;
            }
            return state;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (!state.Rewarded)
            {
                state.Count += CountUsedOnes(context.Category, context.BaseScore, context.Dice);
                if (state.Count >= RequiredCount)
                {
                    state.Rewarded = true;
                    context.AddBonus(RewardScore, "티끌 모아 태산 완료: +15점");
                }
            }

            context.Player.EveryLittleCount = state.Count;
            context.Player.EveryLittleRewarded = state.Rewarded;
        }

        public static int CountUsedOnes(ScoreCategory category, int baseScore, IReadOnlyList<YachtDieState> dice)
        {
            if (baseScore == 0) return 0;
            int count = 0;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
                if (dice[i].Value == 1) count++;
            return count;
        }
    }
}
