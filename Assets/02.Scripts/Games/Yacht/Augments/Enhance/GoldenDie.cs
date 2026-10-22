using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>기본 주사위 1개를 황금 주사위로 바꿉니다. 눈이 1~3이면 +2점 보너스를 얻습니다.</summary>
    public sealed class GoldenDie : EnhanceAugment, IDiceLayoutProvider, IDiceBonusProvider
    {
        public override string Id => YachtAugmentRuntime.GoldenDieId;

        public override string DisplayName => "황금 주사위";

        public override string Description => "기본 주사위 1개를 황금 주사위로 바꿉니다. 눈이 1~3이면 +2점 보너스를 얻습니다.";

        public int RequiredDiceSlots => 1;

        public void ConfigureDice(AugmentDiceContext context) => context.Assign(YachtDieType.Golden, 1);

        public int CalculateDiceBonus(AugmentQueryContext context, IReadOnlyList<YachtDieState> dice)
        {
            int bonus = 0;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
            {
                YachtDieState die = dice[i];
                if (die.Type == YachtDieType.Golden && die.Value >= 1 && die.Value <= 3) bonus += 2;
            }
            return bonus;
        }
    }
}
