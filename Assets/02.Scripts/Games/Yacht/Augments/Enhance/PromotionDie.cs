using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class PromotionDieState : IAugmentState
    {
        public int Value = 1;
        public bool IsActive = true;
        public bool SkipNextGrowth = true;

        public IAugmentState Clone() => new PromotionDieState
        {
            Value = Value,
            IsActive = IsActive,
            SkipNextGrowth = SkipNextGrowth
        };
    }

    /// <summary>매 턴 눈금이 1씩 증가하는 프로모션 주사위를 1개 추가합니다. 6에 도달하면 소멸합니다.</summary>
    public sealed class PromotionDie : EnhanceAugment, IOnAugmentSelected, IOnTurnStarted, IDiceLayoutProvider, IAfterScoreCommit
    {
        public override string Id => YachtAugmentRuntime.PromotionDieId;

        public override string DisplayName => "프로모션 주사위";

        public override string Description => "매 턴 눈금이 1씩 증가하는 프로모션 주사위를 1개 추가합니다. 6에 도달하면 소멸합니다.";

        public int RequiredDiceSlots => 1;

        private static PromotionDieState GetOrSync(AugmentContext context)
        {
            PromotionDieState data = context.State<PromotionDieState>();
            if (!context.Player.PromotionActive)
            {
                data.IsActive = false;
            }
            else
            {
                data.IsActive = true;
                data.Value = Math.Max(data.Value, context.Player.PromotionValue);
                if (data.SkipNextGrowth != context.Player.PromotionSkipNextGrowth && context.Player.PromotionSkipNextGrowth)
                    data.SkipNextGrowth = context.Player.PromotionSkipNextGrowth;
            }
            return data;
        }

        public void OnSelected(AugmentSelectionContext context)
        {
            PromotionDieState data = context.State<PromotionDieState>();
            data.IsActive = true;
            data.Value = 1;
            data.SkipNextGrowth = true;

            context.Player.PromotionActive = true;
            context.Player.PromotionValue = 1;
            context.Player.PromotionSkipNextGrowth = true;
        }

        public void OnTurnStarted(AugmentTurnContext context)
        {
            PromotionDieState data = GetOrSync(context);
            if (!data.IsActive) return;

            if (data.SkipNextGrowth)
            {
                data.SkipNextGrowth = false;
            }
            else if (context.GrowPromotion)
            {
                data.Value = Math.Min(6, Math.Max(1, data.Value) + 1);
            }

            context.Player.PromotionActive = data.IsActive;
            context.Player.PromotionValue = data.Value;
            context.Player.PromotionSkipNextGrowth = data.SkipNextGrowth;
        }

        public void ConfigureDice(AugmentDiceContext context)
        {
            PromotionDieState data = GetOrSync(context);
            if (!data.IsActive) return;

            YachtDieState die = context.AssignOne(YachtDieType.Promotion);
            if (die != null)
            {
                die.PromotionLevel = Math.Max(1, data.Value);
                die.Value = die.PromotionLevel;
            }
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            PromotionDieState data = GetOrSync(context);
            if (data.IsActive && data.Value >= 6)
            {
                data.IsActive = false;
                context.Player.PromotionActive = false;
            }
        }
    }
}
