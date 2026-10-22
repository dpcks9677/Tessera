namespace Tessera.Games.Yacht
{
    /// <summary>Large Straight를 2·3·4·5·6이면 40점인 족보로 바꿉니다.</summary>
    public sealed class Mountain : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.MountainId;

        public override string DisplayName => "마운틴";

        public override string Description => "Large Straight를 2·3·4·5·6이면 40점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.LargeStraight;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.IsExact(2, 3, 4, 5, 6) ? 40 : 0;
    }
}
