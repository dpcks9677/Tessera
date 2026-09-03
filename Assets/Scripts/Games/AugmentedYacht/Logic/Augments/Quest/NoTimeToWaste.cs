using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class NoTimeToWasteState : IAugmentState
    {
        public int RemainingTurns = 3;
        public bool Failed;
        public bool Rewarded;

        public IAugmentState Clone() => new NoTimeToWasteState
        {
            RemainingTurns = RemainingTurns,
            Failed = Failed,
            Rewarded = Rewarded
        };
    }

    /// <summary>연속 3턴 첫 굴림 직후 기입하면 +15점입니다.</summary>
    public sealed class NoTimeToWaste : QuestAugment, IOnAugmentSelected, IAfterScoreCommit
    {
        public const int RequiredStreak = 3;
        public const int RewardScore = 15;

        public override string Id => YachtAugmentRuntime.NoTimeToWasteId;

        public override string DisplayName => "낭비할 시간 없다";

        public override string Description => "연속 3턴 첫 굴림 직후 기입하면 +15점입니다.";

        private static NoTimeToWasteState GetOrSync(AugmentContext context)
        {
            var state = context.State<NoTimeToWasteState>();
            if (context.Player.NoTimeRemaining > 0 && state.RemainingTurns == 3)
            {
                state.RemainingTurns = context.Player.NoTimeRemaining;
                state.Failed = context.Player.NoTimeFailed;
                state.Rewarded = context.Player.NoTimeRewarded;
            }
            return state;
        }

        public void OnSelected(AugmentSelectionContext context)
        {
            var state = context.State<NoTimeToWasteState>();
            state.RemainingTurns = RequiredStreak;
            state.Failed = false;
            state.Rewarded = false;

            context.Player.NoTimeRemaining = RequiredStreak;
            context.Player.NoTimeFailed = false;
            context.Player.NoTimeRewarded = false;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (state.Rewarded || state.Failed) return;

            if (context.NormalRollCount != 1)
            {
                state.Failed = true;
                context.Emit("낭비할 시간 없다 실패");
            }
            else
            {
                state.RemainingTurns = Math.Max(0, state.RemainingTurns - 1);
                if (state.RemainingTurns > 0)
                {
                    context.Emit($"낭비할 시간 없다: {state.RemainingTurns}턴 남음", state.RemainingTurns);
                }
                else
                {
                    state.Rewarded = true;
                    context.AddBonus(RewardScore, "낭비할 시간 없다 완료: +15점");
                }
            }

            context.Player.NoTimeRemaining = state.RemainingTurns;
            context.Player.NoTimeFailed = state.Failed;
            context.Player.NoTimeRewarded = state.Rewarded;
        }
    }
}
