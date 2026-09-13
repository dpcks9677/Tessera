using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class DoublingState : IAugmentState, IAugmentProgressText
    {
        public List<int> RecordedBaseScores = new List<int>();
        public bool Rewarded;

        public IAugmentState Clone() => new DoublingState
        {
            RecordedBaseScores = new List<int>(RecordedBaseScores),
            Rewarded = Rewarded
        };

        public AugmentProgress DescribeProgress(in AugmentProgressQuery query)
        {
            AugmentProgressOutcome outcome = Rewarded ? AugmentProgressOutcome.Succeeded : AugmentProgressOutcome.InProgress;
            var lines = new[]
            {
                new AugmentProgressLine($"동일한 점수로 족보를 두 번 등록 ({(Rewarded ? 1 : 0)}/1)", Rewarded)
            };
            return new AugmentProgress(outcome, lines);
        }
    }

    /// <summary>이미 기입한 기본 점수와 동일한 기본 점수를 한 번 더 기입하면 +10점입니다.</summary>
    public sealed class Doubling : QuestAugment, IOnAugmentSelected, IAfterScoreCommit
    {
        public const int RewardScore = 10;

        public override string Id => YachtAugmentRuntime.DoublingId;

        public override string DisplayName => "더블링";

        public override string Description => "이미 기입한 기본 점수와 동일한 기본 점수를 한 번 더 기입하면 +10점입니다.";

        // 획득 시점에 상태를 만들어 둔다. 그래야 첫 점수 기입 전에도 카드에 초기 진행도가 뜬다.
        // 기본값이 그대로 올바른 시작 진행도라 따로 설정할 값은 없다.
        public void OnSelected(AugmentSelectionContext context) => context.State<DoublingState>();

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
