namespace Tessera.Games.Yacht
{
    /// <summary>Yacht를 조건 없이 30-합계 점수로 바꾸며 음수도 허용합니다.</summary>
    public sealed class ReverseChoice : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.ReverseChoiceId;

        public override string DisplayName => "리버스 초이스";

        public override string Description => "Yacht를 조건 없이 30-합계 점수로 바꾸며 음수도 허용합니다.";

        public override ScoreCategory Target => ScoreCategory.Yacht;

        public override bool PhaseOneOnly => true;

        protected override int CalculateScore(YachtDiceFacts facts) => 30 - facts.Sum;
    }
}
