using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class HoldoutState : IAugmentState
    {
        public bool Rewarded;

        public IAugmentState Clone() => new HoldoutState { Rewarded = Rewarded };
    }

    /// <summary>9턴 이후 풀하우스에 득점하면 +7점입니다.</summary>
    public sealed class Holdout : QuestAugment, IAfterScoreCommit
    {
        public const int MinTurn = 9;
        public const int RewardScore = 7;

        public override string Id => YachtAugmentRuntime.HoldoutId;

        public override string DisplayName => "알박기";

        public override string Description => "9턴 이후 풀하우스에 득점하면 +7점입니다.";

        private static HoldoutState GetOrSync(AugmentContext context)
        {
            var state = context.State<HoldoutState>();
            if (!state.Rewarded && context.Player.HoldoutRewarded)
            {
                state.Rewarded = context.Player.HoldoutRewarded;
            }
            return state;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            int turnNumber = context.Player.TurnsTaken + 1;

            if (!state.Rewarded && turnNumber >= MinTurn && context.Category == ScoreCategory.FullHouse && context.BaseScore > 0)
            {
                state.Rewarded = true;
                context.AddBonus(RewardScore, "뚝심 완료: +7점");
            }

            context.Player.HoldoutRewarded = state.Rewarded;
        }
    }
}
