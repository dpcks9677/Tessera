namespace Tessera.Games.Yacht
{
    /// <summary>기본 주사위 1개를 4·5·6만 나오는 묵직한 주사위로 바꿉니다.</summary>
    public sealed class WeightedDice : EnhanceAugment, IDiceLayoutProvider
    {
        public override string Id => YachtAugmentRuntime.WeightedDiceId;

        public override string DisplayName => "묵직한 주사위";

        public override string Description => "기본 주사위 1개를 4·5·6만 나오는 묵직한 주사위로 바꿉니다.";

        public int RequiredDiceSlots => 1;

        public void ConfigureDice(AugmentDiceContext context) => context.Assign(YachtDieType.Heavy, 1);
    }
}
