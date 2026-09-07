using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class StepByStepState : IAugmentState
    {
        public int CategoryIndex;
        public bool Failed;
        public bool Rewarded;

        public IAugmentState Clone() => new StepByStepState
        {
            CategoryIndex = CategoryIndex,
            Failed = Failed,
            Rewarded = Rewarded
        };
    }

    /// <summary>상단 항목을 에이스부터 식스까지 순서대로 기입하면 상단 기준이 58점이 되고 완료 보너스를 받습니다.</summary>
    public sealed class StepByStep : QuestAugment, IOnAugmentSelected, IAfterScoreCommit
    {
        public const int UpperBonusThreshold = 58;
        public const int RewardScore = 55;

        public override string Id => YachtAugmentRuntime.StepByStepId;

        public override string DisplayName => "차근차근";

        public override string Description => "상단 항목을 에이스부터 식스까지 순서대로 기입하면 상단 기준이 58점이 되고 완료 보너스를 받습니다.";

        public override bool PhaseOneOnly => true;

        private static StepByStepState GetOrSync(AugmentContext context)
        {
            var state = context.State<StepByStepState>();
            if (context.Player.StepCategoryIndex > 0 && state.CategoryIndex == 0)
            {
                state.CategoryIndex = context.Player.StepCategoryIndex;
                state.Failed = context.Player.StepFailed;
                state.Rewarded = context.Player.StepRewarded;
            }
            return state;
        }

        public void OnSelected(AugmentSelectionContext context)
        {
            var state = context.State<StepByStepState>();
            state.CategoryIndex = 0;
            state.Failed = false;
            state.Rewarded = false;

            context.Player.StepCategoryIndex = 0;
            context.Player.StepFailed = false;
            context.Player.StepRewarded = false;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (!state.Failed && !state.Rewarded)
            {
                int expected = state.CategoryIndex;
                if ((int)context.Category != expected)
                {
                    state.Failed = true;
                    context.Emit("차근차근 실패");
                }
                else
                {
                    state.CategoryIndex++;
                    if (state.CategoryIndex >= 6)
                    {
                        state.Rewarded = true;
                        context.Player.StepRewarded = true;
                        context.Score.upperBonusThreshold = Math.Min(context.Score.upperBonusThreshold, UpperBonusThreshold);
                        TryGrantStepBonus(context.Game, context.PlayerIndex);
                        context.Emit(
                            context.Score.stepBonusGranted
                                ? "차근차근 완료: 상단 보너스 +55점"
                                : "차근차근 완료: 상단 보너스 기준 58점",
                            context.Score.stepBonusGranted ? RewardScore : 0);
                    }
                    else
                    {
                        context.Emit($"차근차근: {state.CategoryIndex}/6", state.CategoryIndex);
                    }
                }
            }
            else if (state.Rewarded)
            {
                TryGrantStepBonus(context.Game, context.PlayerIndex);
            }

            context.Player.StepCategoryIndex = state.CategoryIndex;
            context.Player.StepFailed = state.Failed;
            context.Player.StepRewarded = state.Rewarded;
        }

        public static void TryGrantStepBonus(YachtGameState state, int playerIndex)
        {
            YachtAugmentPlayerState progress = state.AugmentPlayers[playerIndex];
            var stepState = progress.States.GetOrCreate<StepByStepState>(YachtAugmentRuntime.StepByStepId);
            bool isRewarded = progress.StepRewarded || stepState.Rewarded;
            PlayerScoreData scores = state.Players[playerIndex];
            if (isRewarded)
            {
                scores.upperBonusThreshold = Math.Min(scores.upperBonusThreshold, UpperBonusThreshold);
                if (!scores.stepBonusGranted && scores.CalculateUpperSum() >= UpperBonusThreshold)
                    scores.stepBonusGranted = true;
            }
            scores.RecalculateTotal();
        }
    }
}
