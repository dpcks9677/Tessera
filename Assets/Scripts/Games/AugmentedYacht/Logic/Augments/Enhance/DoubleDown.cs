using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class DoubleDownState : IAugmentState
    {
        public bool IsUsed;
        public bool IsActive;

        public IAugmentState Clone() => new DoubleDownState
        {
            IsUsed = IsUsed,
            IsActive = IsActive
        };
    }

    /// <summary>9번째 내 턴부터 한 번 기본 점수를 1.5배, 추진력과 함께면 2배로 강화합니다.</summary>
    public sealed class DoubleDown : EnhanceAugment, IManualActionAugment, IScoreEnhancementModifier, IAfterScoreCommit
    {
        public const int MinTurnsTaken = 8;

        public override string Id => YachtAugmentRuntime.DoubleDownId;

        public override string DisplayName => "더블 다운";

        public override string Description => "9번째 내 턴부터 한 번 기본 점수를 1.5배, 추진력과 함께면 2배로 강화합니다.";

        public YachtGamePhase RequiredPhase => YachtGamePhase.TurnReady;

        public bool RerollsDice => false;

        private static DoubleDownState GetOrSync(AugmentContext context)
        {
            var state = context.State<DoubleDownState>();
            if (context.Player.DoubleDownActive != state.IsActive)
                state.IsActive = context.Player.DoubleDownActive;
            if (context.Player.DoubleDownUsed && !state.IsUsed)
                state.IsUsed = context.Player.DoubleDownUsed;
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
            if (state.IsUsed)
            {
                code = YachtCommandErrorCode.AugmentAlreadyUsed;
                message = "더블 다운을 이미 사용했습니다.";
                return false;
            }
            if (context.Player.TurnsTaken < MinTurnsTaken)
            {
                code = YachtCommandErrorCode.AugmentUnavailable;
                message = "더블 다운은 아홉 번째 내 턴부터 사용할 수 있습니다.";
                return false;
            }
            return true;
        }

        public void Use(AugmentActionContext context)
        {
            var state = GetOrSync(context);
            state.IsUsed = true;
            state.IsActive = true;
            context.Player.DoubleDownUsed = true;
            context.Player.DoubleDownActive = true;
        }

        public bool TryGetEnhancement(
            AugmentQueryContext context,
            ScoreCategory category,
            int baseScore,
            out float multiplier,
            out string enhancementSource)
        {
            multiplier = 1f;
            enhancementSource = null;
            var state = GetOrSync(context);
            if ((state.IsActive || context.Player.DoubleDownActive) && baseScore > 0)
            {
                multiplier = 1.5f;
                enhancementSource = "DoubleDown";
                return true;
            }
            return false;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            state.IsActive = false;
            context.Player.DoubleDownActive = false;
        }
    }
}
