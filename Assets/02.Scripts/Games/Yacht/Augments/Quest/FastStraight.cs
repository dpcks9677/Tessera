using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class FastStraightState : IAugmentState, IAugmentProgressText
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

        public AugmentProgress DescribeProgress(in AugmentProgressQuery query)
        {
            bool failed = !Rewarded && query.Player.TurnsTaken >= FastStraight.DeadlineTurn;
            AugmentProgressOutcome outcome = Rewarded
                ? AugmentProgressOutcome.Succeeded
                : failed ? AugmentProgressOutcome.Failed : AugmentProgressOutcome.InProgress;
            var lines = new[]
            {
                new AugmentProgressLine($"{FastStraight.DeadlineTurn}턴 안에 {ScoreCategoryNames.Get(ScoreCategory.SmallStraight)} 기입", SmallScored),
                new AugmentProgressLine($"{FastStraight.DeadlineTurn}턴 안에 {ScoreCategoryNames.Get(ScoreCategory.LargeStraight)} 기입", LargeScored)
            };
            return new AugmentProgress(outcome, lines);
        }
    }

    /// <summary>8턴 이내에 스몰 스트레이트와 라지 스트레이트를 모두 득점하면 +15점입니다.</summary>
    public sealed class FastStraight : QuestAugment, IOnAugmentSelected, IAfterScoreCommit
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

        public void OnSelected(AugmentSelectionContext context)
        {
            var state = context.State<FastStraightState>();
            state.SmallScored = false;
            state.LargeScored = false;
            state.Rewarded = false;

            context.Player.FastSmallScored = false;
            context.Player.FastLargeScored = false;
            context.Player.FastRewarded = false;
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
