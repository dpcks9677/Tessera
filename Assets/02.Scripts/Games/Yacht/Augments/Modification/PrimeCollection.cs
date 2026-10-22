namespace Tessera.Games.Yacht
{
    /// <summary>Large Straight를 모든 눈이 2·3·5·7이고 2·3·5를 포함하면 35점인 족보로 바꿉니다.</summary>
    public sealed class PrimeCollection : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.PrimeCollectionId;

        public override string DisplayName => "프라임 컬렉션";

        public override string Description => "Large Straight를 모든 눈이 2·3·5·7이고 2·3·5를 포함하면 35점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.LargeStraight;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.AllInSet(2, 3, 5, 7) && facts.CountOf(2) > 0 && facts.CountOf(3) > 0 && facts.CountOf(5) > 0 ? 35 : 0;
    }
}
