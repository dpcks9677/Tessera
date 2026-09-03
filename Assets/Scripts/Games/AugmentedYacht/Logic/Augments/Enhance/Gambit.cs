using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class GambitState : IAugmentState
    {
        public int State;

        public IAugmentState Clone() => new GambitState { State = State };
    }

    /// <summary>첫 굴림 전에 한 번 사용할 수 있습니다. 이번 턴 주사위를 4개로 굴리고, 다음 턴 주사위를 6개로 굴립니다.</summary>
    public sealed class Gambit : EnhanceAugment, IManualActionAugment, IDiceCountModifier, IAfterScoreCommit
    {
        public override string Id => YachtAugmentRuntime.GambitId;

        public override string DisplayName => "갬빗";

        public override string Description => "첫 굴림 전에 한 번 사용할 수 있습니다. 이번 턴 주사위를 4개로 굴리고, 다음 턴 주사위를 6개로 굴립니다.";

        public YachtGamePhase RequiredPhase => YachtGamePhase.TurnReady;

        public bool RerollsDice => false;

        private static GambitState GetOrSync(AugmentContext context)
        {
            var state = context.State<GambitState>();
            if (context.Player.GambitState != 0 && state.State != context.Player.GambitState)
            {
                state.State = context.Player.GambitState;
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
                message = "해당 증강을 보유하지 않았습니다.";
                return false;
            }
            if (context.Game.HasRolled)
            {
                code = YachtCommandErrorCode.InvalidPhase;
                message = "첫 굴림 전에만 사용할 수 있습니다.";
                return false;
            }
            var state = GetOrSync(context);
            if (state.State != 0)
            {
                code = YachtCommandErrorCode.AugmentAlreadyUsed;
                message = "갬빗을 이미 사용했습니다.";
                return false;
            }
            return true;
        }

        public void Use(AugmentActionContext context)
        {
            var state = GetOrSync(context);
            state.State = 1;
            context.Player.GambitState = 1;
        }

        public int ModifyDiceCount(AugmentQueryContext context, int diceCount)
        {
            var state = GetOrSync(context);
            return state.State == 1 ? 4 : state.State == 2 ? 6 : diceCount;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (state.State == 1) state.State = 2;
            else if (state.State == 2) state.State = 3;
            context.Player.GambitState = state.State;
        }
    }
}
