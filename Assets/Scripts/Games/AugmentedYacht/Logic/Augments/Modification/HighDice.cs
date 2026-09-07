namespace Tessera.Games.Yacht
{
    /// <summary>Large Straight를 4~7만 사용하고 합 26 이상이면 35점인 족보로 바꿉니다.</summary>
    public sealed class HighDice : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.HighDiceId;

        public override string DisplayName => "하이 다이스";

        public override string Description => "Large Straight를 4~7만 사용하고 합 26 이상이면 35점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.LargeStraight;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.AllInSet(4, 5, 6, 7) && facts.Sum >= 26 ? 35 : 0;
    }
}
