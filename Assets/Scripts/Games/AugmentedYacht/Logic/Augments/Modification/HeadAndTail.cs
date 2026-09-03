namespace Tessera.Games.Yacht
{
    /// <summary>Full House를 연속 3개와 같은 눈 2개면 합계+10점인 족보로 바꿉니다.</summary>
    public sealed class HeadAndTail : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.HeadAndTailId;

        public override string DisplayName => "머리와 몸통";

        public override string Description => "Full House를 연속 3개와 같은 눈 2개면 합계+10점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.FullHouse;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.HasHeadAndTail ? facts.Sum + 10 : 0;
    }
}
