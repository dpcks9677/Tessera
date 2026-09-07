namespace Tessera.Games.Yacht
{
    /// <summary>Large Straight를 연속한 두 눈의 2+3 Full House면 35점인 족보로 바꿉니다.</summary>
    public sealed class DuplexHouse : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.DuplexHouseId;

        public override string DisplayName => "땅콩주택";

        public override string Description => "Large Straight를 연속한 두 눈의 2+3 Full House면 35점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.LargeStraight;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.HasDuplexHouse ? 35 : 0;
    }
}
