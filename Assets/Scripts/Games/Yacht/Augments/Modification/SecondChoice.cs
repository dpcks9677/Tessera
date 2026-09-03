namespace Tessera.Games.Yacht
{
    /// <summary>Yacht를 조건 없이 합계의 절반을 얻는 족보로 바꿉니다.</summary>
    public sealed class SecondChoice : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.SecondChoiceId;

        public override string DisplayName => "두 번째 초이스";

        public override string Description => "Yacht를 조건 없이 합계의 절반을 얻는 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.Yacht;

        public override bool PhaseOneOnly => true;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.Sum / 2;
    }
}
