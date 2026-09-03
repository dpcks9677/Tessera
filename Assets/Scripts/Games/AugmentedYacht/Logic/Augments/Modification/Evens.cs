namespace Tessera.Games.Yacht
{
    /// <summary>Small Straight를 모든 눈이 2·4·6이면 20점인 족보로 바꿉니다.</summary>
    public sealed class Evens : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.EvensId;

        public override string DisplayName => "에번스";

        public override string Description => "Small Straight를 모든 눈이 2·4·6이면 20점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.SmallStraight;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.AllInSet(2, 4, 6) ? 20 : 0;
    }
}
