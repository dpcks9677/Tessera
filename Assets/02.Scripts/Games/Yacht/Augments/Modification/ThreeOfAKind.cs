namespace Tessera.Games.Yacht
{
    /// <summary>Four of a Kind를 같은 눈 3개 이상이면 합계 점수인 족보로 바꿉니다.</summary>
    public sealed class ThreeOfAKind : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.ThreeOfAKindId;

        public override string DisplayName => "쓰리 오브 어 카인드";

        public override string Description => "Four of a Kind를 같은 눈 3개 이상이면 합계 점수인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.FourOfAKind;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.HasCount(3) ? facts.Sum : 0;
    }
}
