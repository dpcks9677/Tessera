namespace Tessera.Games.Yacht
{
    /// <summary>Aces를 합 7·17·27이면 15점인 족보로 바꿉니다.</summary>
    public sealed class LuckySevens : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.LuckySevensId;

        public override string DisplayName => "럭키 세븐";

        public override string Description => "Aces를 합 7·17·27이면 15점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.Aces;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.Sum == 7 || facts.Sum == 17 || facts.Sum == 27 ? 15 : 0;
    }
}
