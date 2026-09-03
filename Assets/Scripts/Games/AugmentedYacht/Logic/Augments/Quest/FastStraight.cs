using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class FastStraightState : IAugmentState
    {
        public bool SmallScored;
        public bool LargeScored;
        public bool Rewarded;

        public IAugmentState Clone() => new FastStraightState
        {
            SmallScored = SmallScored,
            LargeScored = LargeScored,
            Rewarded = Rewarded
        };
    }

    /// <summary>8턴 이내에 스몰 스트레이트와 라지 스트레이트를 모두 득점하면 +15점입니다.</summary>
    public sealed class FastStraight : QuestAugment, IAfterScoreCommit
    {
        public const int DeadlineTurn = 8;
        public const int RewardScore = 15;

        public override string Id => YachtAugmentRuntime.FastStraightId;

        public override string DisplayName => "재빠른 스트레이트";

        public override string Description => "8턴 이내에 스몰 스트레이트와 라지 스트레이트를 모두 득점하면 +15점입니다.";

        public override bool PhaseOneOnly => true;

        private static FastStraightState GetOrSync(AugmentContext context)
        {
            var state = context.State<FastStraightState>();
            if (!state.Rewarded && (context.Player.FastSmallScored || context.Player.FastLargeScored || context.Player.FastRewarded))
            {
                state.SmallScored = context.Player.FastSmallScored;
                state.LargeScored = context.Player.FastLargeScored;
                state.Rewarded = context.Player.FastRewarded;
            }
            return state;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            int turnNumber = context.Player.TurnsTaken + 1;

            if (!state.Rewarded && turnNumber <= DeadlineTurn)
            {
                if (context.Category == ScoreCategory.SmallStraight && context.BaseScore > 0) state.SmallScored = true;
                if (context.Category == ScoreCategory.LargeStraight && context.BaseScore > 0) state.LargeScored = true;

                if (state.SmallScored && state.LargeScored)
                {
                    state.Rewarded = true;
                    context.AddBonus(RewardScore, "패스트 스트레이트 완료: +15점");
                }
            }

            context.Player.FastSmallScored = state.SmallScored;
            context.Player.FastLargeScored = state.LargeScored;
            context.Player.FastRewarded = state.Rewarded;
        }
    }
}
