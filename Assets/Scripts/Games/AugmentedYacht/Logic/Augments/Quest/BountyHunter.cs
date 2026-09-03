using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class BountyHunterState : IAugmentState
    {
        public int TargetCategory = -1;
        public int Successes;
        public int Scratches;
        public bool Rewarded;

        public IAugmentState Clone() => new BountyHunterState
        {
            TargetCategory = TargetCategory,
            Successes = Successes,
            Scratches = Scratches,
            Rewarded = Rewarded
        };
    }

    /// <summary>지정된 카테고리를 3번 채우면 보너스를 받습니다. 0점 기입 시 보너스가 감소합니다.</summary>
    public sealed class BountyHunter : QuestAugment, IOnAugmentSelected, IOnTurnStarted, IAfterScoreCommit
    {
        public const int MaxReward = 15;
        public const int ScratchPenalty = 3;
        public const int RequiredSuccesses = 3;

        public override string Id => YachtAugmentRuntime.BountyHunterId;

        public override string DisplayName => "현상금 사냥꾼";

        public override string Description => "지정된 카테고리를 3번 채우면 보너스를 받습니다. 0점 기입 시 보너스가 감소합니다.";

        private static BountyHunterState GetOrSync(AugmentContext context)
        {
            var state = context.State<BountyHunterState>();
            if (context.Player.BountyTargetCategory != -1 && state.TargetCategory == -1 && !state.Rewarded)
            {
                state.TargetCategory = context.Player.BountyTargetCategory;
                state.Successes = context.Player.BountySuccesses;
                state.Scratches = context.Player.BountyScratches;
                state.Rewarded = context.Player.BountyRewarded;
            }
            return state;
        }

        public void OnSelected(AugmentSelectionContext context)
        {
            var state = context.State<BountyHunterState>();
            state.TargetCategory = -1;
            state.Successes = 0;
            state.Scratches = 0;
            state.Rewarded = false;

            context.Player.BountyTargetCategory = -1;
            context.Player.BountySuccesses = 0;
            context.Player.BountyScratches = 0;
            context.Player.BountyRewarded = false;
        }

        public void OnTurnStarted(AugmentTurnContext context)
        {
            var state = GetOrSync(context);
            if (!state.Rewarded && state.TargetCategory < 0)
            {
                state.TargetCategory = SelectEmptyCategory(context.Game, context.PlayerIndex, context.Random);
                context.Player.BountyTargetCategory = state.TargetCategory;
            }
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (!state.Rewarded && state.TargetCategory == (int)context.Category)
            {
                state.Successes++;
                if (context.BaseScore == 0) state.Scratches++;

                if (state.Successes >= RequiredSuccesses)
                {
                    state.Rewarded = true;
                    int reward = Math.Max(0, MaxReward - state.Scratches * ScratchPenalty);
                    context.AddBonus(reward, $"현상금 사냥꾼 완료: +{reward}점");
                }

                state.TargetCategory = -1;
            }

            context.Player.BountyTargetCategory = state.TargetCategory;
            context.Player.BountySuccesses = state.Successes;
            context.Player.BountyScratches = state.Scratches;
            context.Player.BountyRewarded = state.Rewarded;
        }

        public static int SelectEmptyCategory(YachtGameState state, int playerIndex, IRandomSource random)
        {
            var empty = new List<int>();
            for (int i = 0; i < YachtScoreCalculator.ScorableCategories.Length; i++)
            {
                ScoreCategory category = YachtScoreCalculator.ScorableCategories[i];
                if (!IsFilled(state.Players[playerIndex], category)) empty.Add((int)category);
            }
            return empty.Count == 0 ? -1 : empty[random.NextInt(0, empty.Count)];
        }

        private static bool IsFilled(PlayerScoreData scores, ScoreCategory category)
        {
            int index = (int)category;
            return index <= 5
                ? scores.upperFilled[index] || scores.upperScores[index] != -1
                : scores.lowerFilled[index - 7] || scores.lowerScores[index - 7] != -1;
        }
    }
}
