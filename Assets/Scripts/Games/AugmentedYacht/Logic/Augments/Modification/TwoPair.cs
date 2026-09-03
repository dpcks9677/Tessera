namespace Tessera.Games.Yacht
{
    /// <summary>Full House를 서로 다른 두 쌍 또는 포카드면 15점인 족보로 바꿉니다.</summary>
    public sealed class TwoPair : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.TwoPairId;

        public override string DisplayName => "투 페어";

        public override string Description => "Full House를 서로 다른 두 쌍 또는 포카드면 15점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.FullHouse;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.PairCount >= 2 || facts.HasCount(4) ? 15 : 0;
    }
}
