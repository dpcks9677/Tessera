namespace Tessera.Games.Yacht
{
    /// <summary>Yacht를 합계가 21이면 21점인 족보로 바꿉니다.</summary>
    public sealed class Blackjack21 : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.BlackjackId;

        public override string DisplayName => "블랙잭 21";

        public override string Description => "Yacht를 합계가 21이면 21점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.Yacht;

        public override bool PhaseOneOnly => true;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.Sum == 21 ? 21 : 0;
    }
}
