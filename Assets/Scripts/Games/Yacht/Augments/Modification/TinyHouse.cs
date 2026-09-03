namespace Tessera.Games.Yacht
{
    /// <summary>Full House를 1~4만 사용해 완성하면 28점인 족보로 바꿉니다.</summary>
    public sealed class TinyHouse : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.TinyHouseId;

        public override string DisplayName => "타이니 하우스";

        public override string Description => "Full House를 1~4만 사용해 완성하면 28점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.FullHouse;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.HasFullHouse && facts.CountOf(5) == 0 && facts.CountOf(6) == 0 && facts.CountOf(7) == 0 ? 28 : 0;
    }
}
