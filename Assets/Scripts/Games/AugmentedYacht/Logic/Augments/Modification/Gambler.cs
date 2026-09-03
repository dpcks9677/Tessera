namespace Tessera.Games.Yacht
{
    /// <summary>Choice를 합 24 이상이면 합계+7점인 족보로 바꿉니다.</summary>
    public sealed class Gambler : ModificationAugment
    {
        public override string Id => YachtAugmentRuntime.GamblerId;

        public override string DisplayName => "갬블러";

        public override string Description => "Choice를 합 24 이상이면 합계+7점인 족보로 바꿉니다.";

        public override ScoreCategory Target => ScoreCategory.Choice;

        protected override int CalculateScore(YachtDiceFacts facts) => facts.Sum >= 24 ? facts.Sum + 7 : 0;
    }
}
