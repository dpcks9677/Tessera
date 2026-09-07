namespace Tessera.Games.Yacht
{
    /// <summary>Yacht를 1·1·2·3·5이면 25점인 족보로 바꿉니다.</summary>
    public sealed class FibonacciNumbers : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.FibonacciId;

        public override string DisplayName => "피보나치 넘버즈";

        public override string Description => "Yacht를 1·1·2·3·5이면 25점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.Yacht;

        public override bool PhaseOneOnly => true;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.IsExact(1, 1, 2, 3, 5) ? 25 : 0;
    }
}
