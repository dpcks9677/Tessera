namespace Tessera.Games.Yacht
{
    /// <summary>기본 주사위 2개를 4·5 눈금이 2개씩 있는 8면 주사위로 바꿉니다.</summary>
    public sealed class Octahedron : EnhanceAugment, IDiceLayoutProvider
    {
        public override string Id => YachtAugmentRuntime.OctahedronId;

        public override string DisplayName => "8면 주사위";

        public override string Description => "기본 주사위 2개를 4·5 눈금이 2개씩 있는 8면 주사위로 바꿉니다.";

        public override string[] Conflicts => new[] { YachtAugmentRuntime.TableFlipId };

        public int RequiredDiceSlots => 2;

        public void ConfigureDice(AugmentDiceContext context) => context.Assign(YachtDieType.Octahedron, 2);
    }
}
