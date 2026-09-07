using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class MomentumAugmentState : IAugmentState
    {
        public int State; // 0: idle, 1: primed, 2: consumed

        public IAugmentState Clone() => new MomentumAugmentState { State = State };
    }

    /// <summary>0점을 기록하면 다음 득점 시 1.5배의 점수를 얻습니다.</summary>
    public sealed class Momentum : EnhanceAugment, IOnAugmentSelected, IScoreEnhancementModifier, IAfterScoreCommit
    {
        public override string Id => YachtAugmentRuntime.MomentumId;

        public override string DisplayName => "추진력";

        public override string Description => "0점을 기록하면 다음 득점 시 1.5배의 점수를 얻습니다.";

        public void OnSelected(AugmentSelectionContext context)
        {
            var state = context.State<MomentumAugmentState>();
            state.State = 0;
            context.Player.MomentumState = 0;
        }

        private static MomentumAugmentState GetOrSync(AugmentContext context)
        {
            var state = context.State<MomentumAugmentState>();
            if (state.State == 0 && context.Player.MomentumState != 0)
            {
                state.State = context.Player.MomentumState;
            }
            return state;
        }

        public bool TryGetEnhancement(AugmentQueryContext context, ScoreCategory category, int baseScore, out float multiplier, out string enhancementSource)
        {
            var state = GetOrSync(context);
            if (state.State == 1 && baseScore > 0)
            {
                multiplier = 1.5f;
                enhancementSource = "Momentum";
                return true;
            }
            multiplier = 1f;
            enhancementSource = null;
            return false;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (state.State == 0 && context.BaseScore == 0)
            {
                state.State = 1;
            }
            else if (state.State == 1 && context.BaseScore > 0)
            {
                state.State = 2;
            }
            context.Player.MomentumState = state.State;
        }
    }
}
