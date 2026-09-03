using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class EveryLittleCountsState : IAugmentState
    {
        public int Count;
        public bool Rewarded;

        public IAugmentState Clone() => new EveryLittleCountsState
        {
            Count = Count,
            Rewarded = Rewarded
        };
    }

    /// <summary>족보 기입 시 사용된 1의 눈금을 누적하여 7개에 도달하면 +15점입니다.</summary>
    public sealed class EveryLittleCounts : QuestAugment, IAfterScoreCommit
    {
        public const int RequiredCount = 7;
        public const int RewardScore = 15;

        public override string Id => YachtAugmentRuntime.EveryLittleId;

        public override string DisplayName => "티끌 모아 태산";

        public override string Description => "족보 기입 시 사용된 1의 눈금을 누적하여 7개에 도달하면 +15점입니다.";

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
