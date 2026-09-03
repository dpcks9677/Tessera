using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class NozdormuState : IAugmentState
    {
        public int TargetTurn;
        public bool Rewarded;

        public IAugmentState Clone() => new NozdormuState
        {
            TargetTurn = TargetTurn,
            Rewarded = Rewarded
        };
    }

    /// <summary>턴 제한 시간을 15초로 제한하고 목표 턴을 유지하면 +9점입니다.</summary>
    public sealed class Nozdormu : QuestAugment, IOnAugmentSelected, ITurnDurationModifier, IAfterScoreCommit
    {
        public const float FastTurnSeconds = 15f;
        public const int RewardScore = 9;

        public override string Id => YachtAugmentRuntime.NozdormuId;

        public override string DisplayName => "노즈도르무";

        public override string Description => "턴 제한 시간을 15초로 제한하고 목표 턴을 유지하면 +9점입니다.";

        private static NozdormuState GetOrSync(AugmentContext context)
        {
            var state = context.State<NozdormuState>();
            if (context.Player.NozdormuTargetTurn > 0 && state.TargetTurn == 0)
            {
                state.TargetTurn = context.Player.NozdormuTargetTurn;
                state.Rewarded = context.Player.NozdormuRewarded;
            }
            return state;
        }

        public void OnSelected(AugmentSelectionContext context)
        {
            var state = context.State<NozdormuState>();
            int targetTurn = context.Game.CurrentRound < 6 ? 5 : context.Game.CurrentRound < 9 ? 8 : 12;
            state.TargetTurn = targetTurn;
            state.Rewarded = false;

            context.Player.NozdormuTargetTurn = targetTurn;
            context.Player.NozdormuRewarded = false;
        }

        public float ModifyTurnDuration(AugmentQueryContext context, float seconds)
        {
            var state = GetOrSync(context);
            return !state.Rewarded && context.Player.TurnsTaken < state.TargetTurn ? FastTurnSeconds : seconds;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            int turnNumber = context.Player.TurnsTaken + 1;

            if (!state.Rewarded && turnNumber >= state.TargetTurn)
            {
                state.Rewarded = true;
                context.AddBonus(RewardScore, "노즈도르무 완료: +9점");
            }

            context.Player.NozdormuTargetTurn = state.TargetTurn;
            context.Player.NozdormuRewarded = state.Rewarded;
        }
    }
}
