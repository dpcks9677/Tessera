namespace Tessera.Games.Yacht
{
    /// <summary>기본 주사위 2개를 2부터 7까지 나오는 세븐스 다이스로 바꿉니다.</summary>
    public sealed class SevensDice : EnhanceAugment, IDiceLayoutProvider
    {
        public override string Id => YachtAugmentRuntime.SevensDiceId;

        public override string DisplayName => "세븐스 다이스";

        public override string Description => "기본 주사위 2개를 2부터 7까지 나오는 세븐스 다이스로 바꿉니다.";

        public int RequiredDiceSlots => 2;

        public void ConfigureDice(AugmentDiceContext context) => context.Assign(YachtDieType.Sevens, 2);
    }
}
