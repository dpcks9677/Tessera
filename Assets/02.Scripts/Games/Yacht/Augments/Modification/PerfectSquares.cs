namespace Tessera.Games.Yacht
{
    /// <summary>Aces를 합 9·16·25이면 12점인 족보로 바꿉니다.</summary>
    public sealed class PerfectSquares : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.PerfectSquaresId;

        public override string DisplayName => "퍼펙트 스퀘어";

        public override string Description => "Aces를 합 9·16·25이면 12점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.Aces;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.Sum == 9 || facts.Sum == 16 || facts.Sum == 25 ? 12 : 0;
    }
}
