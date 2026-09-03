using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class EquivalentExchangeState : IAugmentState
    {
        public int Uses;

        public IAugmentState Clone() => new EquivalentExchangeState { Uses = Uses };
    }

    /// <summary>기본 굴림을 모두 사용한 뒤 5점을 지불하고 주사위를 다시 굴립니다. 최대 3번 사용할 수 있습니다.</summary>
    public sealed class EquivalentExchange : EnhanceAugment, IManualActionAugment
    {
        public const int Cost = 5;
        public const int MaxUses = 3;

        public override string Id => YachtAugmentRuntime.EquivalentExchangeId;

        public override string DisplayName => "등가교환";

        public override string Description => "기본 굴림을 모두 사용한 뒤 5점을 지불하고 주사위를 다시 굴립니다. 최대 3번 사용할 수 있습니다.";

        public YachtGamePhase RequiredPhase => YachtGamePhase.ScoreSelection;

        public bool RerollsDice => true;

        private static EquivalentExchangeState GetOrSync(AugmentContext context)
        {
            var state = context.State<EquivalentExchangeState>();
            if (state.Uses == 0 && context.Player.EquivalentExchangeUses > 0)
            {
                state.Uses = context.Player.EquivalentExchangeUses;
            }
            return state;
        }

        public bool CanUse(AugmentActionContext context, out YachtCommandErrorCode code, out string message)
        {
            code = YachtCommandErrorCode.None;
            message = null;
            if (!context.Owns(Id))
            {
                code = YachtCommandErrorCode.AugmentRequired;
                message = "등가교환 증강을 보유하지 않았습니다.";
                return false;
            }
            var state = GetOrSync(context);
            if (state.Uses >= MaxUses)
            {
                code = YachtCommandErrorCode.AugmentAlreadyUsed;
                message = "등가교환을 모두 사용했습니다.";
                return false;
            }
            if (!context.Game.HasRolled || context.Game.RollsRemaining > 0)
            {
                code = YachtCommandErrorCode.NoRollsRemaining;
                message = "기본 굴림을 모두 사용한 뒤 등가교환을 사용할 수 있습니다.";
                return false;
            }
            return true;
        }

        public void Use(AugmentActionContext context)
        {
            var state = GetOrSync(context);
            state.Uses++;
            context.Player.EquivalentExchangeUses = state.Uses;
            context.Score.augmentBonusScore -= Cost;
            context.Score.RecalculateTotal();
        }
    }
}
