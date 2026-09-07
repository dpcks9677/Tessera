using System;

namespace Tessera.Games.Yacht
{
    /// <summary>Small Straight를 Large Straight 조건 30점으로 바꾸고 상단 기준을 60으로 낮춥니다.</summary>
    public sealed class DoubleLargeStraight : ModificationAugment, IOnAugmentSelected
    {
        public const int UpperBonusThreshold = 60;

        public override string Id => YachtAugmentRuntime.DoubleLargeStraightId;

        public override string DisplayName => "더블 라지 스트레이트";

        public override string Description => "Small Straight를 Large Straight 조건 30점으로 바꾸고 상단 기준을 60으로 낮춥니다.";

        public override ScoreCategory Target => ScoreCategory.SmallStraight;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.HasLargeStraight ? 30 : 0;

        public void OnSelected(AugmentSelectionContext context)
        {
            PlayerScoreData score = context.Score;
            score.upperBonusThreshold = Math.Min(score.upperBonusThreshold, UpperBonusThreshold);
            score.RecalculateTotal();
        }
    }
}
