using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class HoldoutState : IAugmentState, IAugmentProgressText
    {
        public bool Rewarded;

        public IAugmentState Clone() => new HoldoutState { Rewarded = Rewarded };

        public AugmentProgress DescribeProgress(in AugmentProgressQuery query)
        {
            bool failed = !Rewarded && query.Scores.IsFilled(ScoreCategory.FullHouse);
            AugmentProgressOutcome outcome = Rewarded
                ? AugmentProgressOutcome.Succeeded
                : failed ? AugmentProgressOutcome.Failed : AugmentProgressOutcome.InProgress;
            var lines = new[]
            {
                new AugmentProgressLine($"{Holdout.MinTurn}턴 이후에 {ScoreCategoryNames.Get(ScoreCategory.FullHouse)} 기입", Rewarded)
            };
            return new AugmentProgress(outcome, lines);
        }
    }

    /// <summary>9턴 이후 풀하우스에 득점하면 +7점입니다.</summary>
    public sealed class Holdout : QuestAugment, IOnAugmentSelected, IAfterScoreCommit
    {
        public const int MinTurn = 9;
        public const int RewardScore = 7;

        public override string Id => YachtAugmentRuntime.HoldoutId;

        public override string DisplayName => "알박기";

        public override string Description => "9턴 이후 풀하우스에 득점하면 +7점입니다.";

        // 획득 시점에 상태를 만들어 둔다. 그래야 첫 점수 기입 전에도 카드에 초기 진행도가 뜬다.
        // 기본값이 그대로 올바른 시작 진행도라 따로 설정할 값은 없다.
        public void OnSelected(AugmentSelectionContext context) => context.State<HoldoutState>();

        private static HoldoutState GetOrSync(AugmentContext context)
        {
            var state = context.State<HoldoutState>();
            if (!state.Rewarded && context.Player.HoldoutRewarded)
            {
                state.Rewarded = context.Player.HoldoutRewarded;
            }
            return state;
        }

        public void AfterScoreCommit(AugmentCommitContext context)
        {
            var state = GetOrSync(context);
            int turnNumber = context.Player.TurnsTaken + 1;

            if (!state.Rewarded && turnNumber >= MinTurn && context.Category == ScoreCategory.FullHouse && context.BaseScore > 0)
            {
                state.Rewarded = true;
                context.AddBonus(RewardScore, "뚝심 완료: +7점");
            }

            context.Player.HoldoutRewarded = state.Rewarded;
        }
    }
}
