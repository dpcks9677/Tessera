namespace Tessera.Games.Yacht
{
    /// <summary>기본 주사위 1개를 황금 주사위로 바꿉니다. 6으로 득점 시 +3점 보너스를 얻습니다.</summary>
    public sealed class GoldenDie : EnhanceAugment, IDiceLayoutProvider
    {
        public override string Id => YachtAugmentRuntime.GoldenDieId;

        public override string DisplayName => "황금 주사위";

        public override string Description => "기본 주사위 1개를 황금 주사위로 바꿉니다. 6으로 득점 시 +3점 보너스를 얻습니다.";

        public int RequiredDiceSlots => 1;

        public void ConfigureDice(AugmentDiceContext context) => context.Assign(YachtDieType.Golden, 1);
    }
}
