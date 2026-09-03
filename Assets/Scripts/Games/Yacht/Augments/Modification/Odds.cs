namespace Tessera.Games.Yacht
{
    /// <summary>Small Straight를 모든 눈이 1·3·5·7이면 20점인 족보로 바꿉니다.</summary>
    public sealed class Odds : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.OddsId;

        public override string DisplayName => "오즈";

        public override string Description => "Small Straight를 모든 눈이 1·3·5·7이면 20점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.SmallStraight;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.AllInSet(1, 3, 5, 7) ? 20 : 0;
    }
}
