using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class TableFlipState : IAugmentState
    {
        public bool IsUsed;

        public IAugmentState Clone() => new TableFlipState { IsUsed = IsUsed };
    }

    /// <summary>첫 굴림 후 한 번 사용할 수 있습니다. 주사위를 다시 굴립니다. 8면 주사위와 충돌합니다.</summary>
    public sealed class TableFlip : EnhanceAugment, IManualActionAugment
    {
        public override string Id => YachtAugmentRuntime.TableFlipId;

        public override string DisplayName => "판 뒤집기";

        public override string Description => "첫 굴림 후 한 번 사용할 수 있습니다. 주사위를 다시 굴립니다. 8면 주사위와 충돌합니다.";

        public override string[] Conflicts => new[] { YachtAugmentRuntime.OctahedronId };

        public YachtGamePhase RequiredPhase => YachtGamePhase.ScoreSelection;

        public bool RerollsDice => true;

        private static TableFlipState GetOrSync(AugmentContext context)
        {
            var state = context.State<TableFlipState>();
            if (!state.IsUsed && context.Player.TableFlipUsed)
            {
                state.IsUsed = context.Player.TableFlipUsed;
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
                message = "판 뒤집기 증강을 보유하지 않았습니다.";
                return false;
            }
            var state = GetOrSync(context);
            if (state.IsUsed)
            {
                code = YachtCommandErrorCode.AugmentAlreadyUsed;
                message = "판 뒤집기를 이미 사용했습니다.";
                return false;
            }
            if (!context.Game.HasRolled)
            {
                code = YachtCommandErrorCode.RollRequired;
                message = "첫 굴림 후 판 뒤집기를 사용할 수 있습니다.";
                return false;
            }
            return true;
        }

        public void Use(AugmentActionContext context)
        {
            var state = GetOrSync(context);
            state.IsUsed = true;
            context.Player.TableFlipUsed = true;
        }
    }
}
