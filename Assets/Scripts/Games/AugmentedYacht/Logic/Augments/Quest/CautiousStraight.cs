using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class CautiousStraightState : IAugmentState
    {
        public bool SmallScored;
        public bool Failed;
        public bool Rewarded;

        public IAugmentState Clone() => new CautiousStraightState
        {
            SmallScored = SmallScored,
            Failed = Failed,
            Rewarded = Rewarded
        };
    }

    /// <summary>스몰 스트레이트를 먼저 득점한 뒤 라지 스트레이트를 득점하면 +7점입니다. 순서가 바뀌면 실패합니다.</summary>
    public sealed class CautiousStraight : QuestAugment, IAfterScoreCommit
    {
        public const int RewardScore = 7;

        public override string Id => YachtAugmentRuntime.CautiousStraightId;

        public override string DisplayName => "신중한 스트레이트";

        public override string Description => "스몰 스트레이트를 먼저 득점한 뒤 라지 스트레이트를 득점하면 +7점입니다. 순서가 바뀌면 실패합니다.";

        private static CautiousStraightState GetOrSync(AugmentContext context)
        {
            var state = context.State<CautiousStraightState>();
            if (!state.Rewarded && !state.Failed && (context.Player.CautiousSmallScored || context.Player.CautiousFailed || context.Player.CautiousRewarded))
            {
                state.SmallScored = context.Player.CautiousSmallScored;
                state.Failed = context.Player.CautiousFailed;
                state.Rewarded = context.Player.CautiousRewarded;
            }
            return state;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (!state.Failed && !state.Rewarded)
            {
                if (context.Category == ScoreCategory.LargeStraight && context.BaseScore > 0 && !state.SmallScored)
                {
                    state.Failed = true;
                }
                else if (context.Category == ScoreCategory.SmallStraight && context.BaseScore > 0)
                {
                    state.SmallScored = true;
                }
                else if (context.Category == ScoreCategory.LargeStraight && context.BaseScore > 0 && state.SmallScored)
                {
                    state.Rewarded = true;
                    context.AddBonus(RewardScore, "신중한 스트레이트 완료: +7점");
                }
            }

            context.Player.CautiousSmallScored = state.SmallScored;
            context.Player.CautiousFailed = state.Failed;
            context.Player.CautiousRewarded = state.Rewarded;
        }
    }
}
