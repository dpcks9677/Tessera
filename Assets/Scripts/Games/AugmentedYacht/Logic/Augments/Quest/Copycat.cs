using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class CopycatState : IAugmentState, IAugmentProgressText
    {
        public int Count;
        public bool Rewarded;

        public IAugmentState Clone() => new CopycatState
        {
            Count = Count,
            Rewarded = Rewarded
        };

        public AugmentProgress DescribeProgress(in AugmentProgressQuery query)
        {
            AugmentProgressOutcome outcome = Rewarded ? AugmentProgressOutcome.Succeeded : AugmentProgressOutcome.InProgress;
            string text = $"상대방이 이미 기입한 족보와 동일한 족보 기입 ({Count}/{Copycat.RequiredCount})";
            if (Rewarded && Count < Copycat.RequiredCount) text += " (조건 달성!)";
            var lines = new[] { new AugmentProgressLine(text, Rewarded) };
            return new AugmentProgress(outcome, lines);
        }
    }

    /// <summary>상대가 이미 기입한 족보를 따라 기입합니다. 초이스 이상에서 동점 기입 시 즉시, 또는 3회 누적 시 +10점입니다.</summary>
    public sealed class Copycat : QuestAugment, IOnAugmentSelected, IAfterScoreCommit
    {
        public const int RequiredCount = 3;
        public const int RewardScore = 10;

        public override string Id => YachtAugmentRuntime.CopycatId;

        public override string DisplayName => "카피캣";

        public override string Description => "상대가 이미 기입한 족보를 따라 기입합니다. 초이스 이상에서 동점 기입 시 즉시, 또는 3회 누적 시 +10점입니다.";

        // 획득 시점에 상태를 만들어 둔다. 그래야 첫 점수 기입 전에도 카드에 초기 진행도가 뜬다.
        // 기본값이 그대로 올바른 시작 진행도라 따로 설정할 값은 없다.
        public void OnSelected(AugmentSelectionContext context) => context.State<CopycatState>();

        private static CopycatState GetOrSync(AugmentContext context)
        {
            var state = context.State<CopycatState>();
            if (!state.Rewarded && (context.Player.CopycatCount > 0 || context.Player.CopycatRewarded))
            {
                state.Count = context.Player.CopycatCount;
                state.Rewarded = context.Player.CopycatRewarded;
            }
            return state;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (!state.Rewarded)
            {
                int opponent = context.PlayerIndex == 0 ? 1 : 0;
                PlayerScoreData opponentScores = context.Game.Players[opponent];

                if (IsFilled(opponentScores, context.Category))
                {
                    int opponentBase = GetBaseScore(opponentScores, context.Category);
                    bool immediate = (int)context.Category >= (int)ScoreCategory.Choice && opponentBase == context.BaseScore;
                    state.Count++;
                    if (immediate || state.Count >= RequiredCount)
                    {
                        state.Rewarded = true;
                        context.AddBonus(RewardScore, "따라쟁이 완료: +10점");
                    }
                }
            }

            context.Player.CopycatCount = state.Count;
            context.Player.CopycatRewarded = state.Rewarded;
        }

        private static bool IsFilled(PlayerScoreData scores, ScoreCategory category)
        {
            int index = (int)category;
            return index <= 5
                ? scores.upperFilled[index] || scores.upperScores[index] != -1
                : scores.lowerFilled[index - 7] || scores.lowerScores[index - 7] != -1;
        }

        private static int GetBaseScore(PlayerScoreData scores, ScoreCategory category)
        {
            int index = (int)category;
            if (index <= 5)
                return scores.upperBaseScores[index] != -1 ? scores.upperBaseScores[index] : scores.upperScores[index];
            int lower = index - 7;
            return scores.lowerBaseScores[lower] != -1 ? scores.lowerBaseScores[lower] : scores.lowerScores[lower];
        }
    }
}
