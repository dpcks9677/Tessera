using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class DoublingState : IAugmentState
    {
        public List<int> RecordedBaseScores = new List<int>();
        public bool Rewarded;

        public IAugmentState Clone() => new DoublingState
        {
            RecordedBaseScores = new List<int>(RecordedBaseScores),
            Rewarded = Rewarded
        };
    }

    /// <summary>이미 기입한 기본 점수와 동일한 기본 점수를 한 번 더 기입하면 +10점입니다.</summary>
    public sealed class Doubling : QuestAugment, IAfterScoreCommit
    {
        public const int RewardScore = 10;

        public override string Id => YachtAugmentRuntime.DoublingId;

        public override string DisplayName => "더블링";

        public override string Description => "이미 기입한 기본 점수와 동일한 기본 점수를 한 번 더 기입하면 +10점입니다.";

        private static DoublingState GetOrSync(AugmentContext context)
        {
            var state = context.State<DoublingState>();
            if (!state.Rewarded && context.Player.RecordedBaseScores != null && context.Player.RecordedBaseScores.Length > 0 && state.RecordedBaseScores.Count == 0)
            {
                state.RecordedBaseScores.AddRange(context.Player.RecordedBaseScores);
                state.Rewarded = context.Player.DoublingRewarded;
            }
            return state;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            if (!state.Rewarded && context.BaseScore != 0)
            {
                if (state.RecordedBaseScores.Contains(context.BaseScore))
                {
                    state.Rewarded = true;
                    context.AddBonus(RewardScore, "배수진 완료: +10점");
                }
                else
                {
                    state.RecordedBaseScores.Add(context.BaseScore);
                }
            }

            context.Player.RecordedBaseScores = state.RecordedBaseScores.ToArray();
            context.Player.DoublingRewarded = state.Rewarded;
        }
    }
}
