using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class ProphetState : IAugmentState
    {
        public int TurnsRemaining = 3;
        public int[] Targets = Array.Empty<int>();

        public IAugmentState Clone() => new ProphetState
        {
            TurnsRemaining = TurnsRemaining,
            Targets = Targets != null ? (int[])Targets.Clone() : Array.Empty<int>()
        };
    }

    /// <summary>3턴 동안 매 턴 3개의 목표 숫자가 주어지며, 기본 점수와 일치하면 +7점입니다.</summary>
    public sealed class Prophet : QuestAugment, IOnAugmentSelected, IOnTurnStarted, IAfterScoreCommit
    {
        public const int RewardScore = 7;
        public const int InitialTurns = 3;

        public override string Id => YachtAugmentRuntime.ProphetId;

        public override string DisplayName => "예언자";

        public override string Description => "3턴 동안 매 턴 3개의 목표 숫자가 주어지며, 기본 점수와 일치하면 +7점입니다.";

        private static ProphetState GetOrSync(AugmentContext context)
        {
            var state = context.State<ProphetState>();
            if (context.Player.ProphetTurnsRemaining > 0 && state.TurnsRemaining == 3 && state.Targets.Length == 0)
            {
                state.TurnsRemaining = context.Player.ProphetTurnsRemaining;
                state.Targets = context.Player.ProphetTargets ?? Array.Empty<int>();
            }
            return state;
        }

        public void OnSelected(AugmentSelectionContext context)
        {
            var state = context.State<ProphetState>();
            state.TurnsRemaining = InitialTurns;
            state.Targets = Array.Empty<int>();

            context.Player.ProphetTurnsRemaining = InitialTurns;
            context.Player.ProphetTargets = Array.Empty<int>();
        }

        public void OnTurnStarted(AugmentTurnContext context)
        {
            var state = GetOrSync(context);
            if (state.TurnsRemaining > 0 && (state.Targets == null || state.Targets.Length == 0))
            {
                state.Targets = new[]
                {
                    context.Random.NextInt(1, 31),
                    context.Random.NextInt(1, 31),
                    context.Random.NextInt(1, 31)
                };
                context.Player.ProphetTargets = state.Targets;
            }
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (state.TurnsRemaining > 0)
            {
                if (Contains(state.Targets, context.BaseScore))
                {
                    context.AddBonus(RewardScore, "예언자 완료: +7점");
                }
                state.TurnsRemaining--;
                state.Targets = Array.Empty<int>();
            }

            context.Player.ProphetTurnsRemaining = state.TurnsRemaining;
            context.Player.ProphetTargets = state.Targets;
        }

        private static bool Contains(IReadOnlyList<int> values, int target)
        {
            for (int i = 0; i < (values?.Count ?? 0); i++)
                if (values[i] == target) return true;
            return false;
        }
    }
}
