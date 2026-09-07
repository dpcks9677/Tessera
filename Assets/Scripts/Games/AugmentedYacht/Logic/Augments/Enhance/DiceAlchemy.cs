using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class DiceAlchemyState : IAugmentState
    {
        public bool IsUsed;

        public IAugmentState Clone() => new DiceAlchemyState { IsUsed = IsUsed };
    }

    /// <summary>첫 굴림 후 한 번 킵되지 않은 주사위의 눈금을 1씩 감소시킵니다. (최소 1)</summary>
    public sealed class DiceAlchemy : EnhanceAugment, IManualActionAugment
    {
        public override string Id => YachtAugmentRuntime.DiceAlchemyId;

        public override string DisplayName => "주사위 연금술";

        public override string Description => "첫 굴림 후 한 번 킵되지 않은 주사위의 눈금을 1씩 감소시킵니다. (최소 1)";

        public YachtGamePhase RequiredPhase => YachtGamePhase.ScoreSelection;

        public bool RerollsDice => false;

        private static DiceAlchemyState GetOrSync(AugmentContext context)
        {
            var state = context.State<DiceAlchemyState>();
            if (!state.IsUsed && context.Player.DiceAlchemyUsed)
            {
                state.IsUsed = context.Player.DiceAlchemyUsed;
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
                message = "주사위 연금술 증강을 보유하지 않았습니다.";
                return false;
            }
            var state = GetOrSync(context);
            if (state.IsUsed)
            {
                code = YachtCommandErrorCode.AugmentAlreadyUsed;
                message = "주사위 연금술을 이미 사용했습니다.";
                return false;
            }
            if (!context.Game.HasRolled)
            {
                code = YachtCommandErrorCode.RollRequired;
                message = "첫 굴림 후 사용할 수 있습니다.";
                return false;
            }
            return true;
        }

        public void Use(AugmentActionContext context)
        {
            var state = GetOrSync(context);
            for (int i = 0; i < context.Game.Dice.Length; i++)
            {
                if (!context.Game.Dice[i].IsKept)
                    context.Game.Dice[i].Value = Math.Max(1, context.Game.Dice[i].Value - 1);
            }
            state.IsUsed = true;
            context.Player.DiceAlchemyUsed = true;
        }
    }
}
