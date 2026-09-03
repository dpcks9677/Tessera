namespace Tessera.Games.Yacht
{
    /// <summary>기본 주사위 2개를 커플 주사위로 바꿉니다. 두 주사위의 눈이 같으면 +5점 보너스를 얻습니다.</summary>
    public sealed class CoupleDice : EnhanceAugment, IDiceLayoutProvider
    {
        public override string Id => YachtAugmentRuntime.CoupleDiceId;

        public override string DisplayName => "커플 주사위";

        public override string Description => "기본 주사위 2개를 커플 주사위로 바꿉니다. 두 주사위의 눈이 같으면 +5점 보너스를 얻습니다.";

        public int RequiredDiceSlots => 2;

        public void ConfigureDice(AugmentDiceContext context) => context.Assign(YachtDieType.Couple, 2);
    }
}
