using System;

namespace Tessera.Games.Yacht
{
    [Serializable]
    public sealed class RandomBoxState : IAugmentState
    {
        public string AwardId;

        public IAugmentState Clone() => new RandomBoxState { AwardId = AwardId };
    }

    /// <summary>상단 기준을 58로 낮추고 양쪽 선택 후 퀘스트가 아닌 무작위 증강으로 교체됩니다.</summary>
    public sealed class RandomBox : EnhanceAugment, IOnAugmentSelected
    {
        public const int LoweredUpperBonusThreshold = 58;

        public override string Id => YachtAugmentRuntime.RandomBoxId;

        public override string DisplayName => "랜덤 박스";

        public override string Description => "상단 기준을 58로 낮추고 양쪽 선택 후 퀘스트가 아닌 무작위 증강으로 교체됩니다.";

        public void OnSelected(AugmentSelectionContext context)
        {
            PlayerScoreData score = context.Score;
            score.upperBonusThreshold = Math.Min(score.upperBonusThreshold, LoweredUpperBonusThreshold);
            score.RecalculateTotal();
            context.Player.RandomBoxAwardId = null;
        }
    }
}
