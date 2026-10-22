using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>기본 주사위 2개를 커플 주사위로 바꿉니다. 두 주사위의 눈이 같으면 +3점 보너스를 얻습니다.</summary>
    public sealed class CoupleDice : EnhanceAugment, IDiceLayoutProvider, IDiceBonusProvider
    {
        public override string Id => YachtAugmentRuntime.CoupleDiceId;

        public override string DisplayName => "커플 주사위";

        public override string Description => "기본 주사위 2개를 커플 주사위로 바꿉니다. 두 주사위의 눈이 같으면 +3점 보너스를 얻습니다.";

        public int RequiredDiceSlots => 2;

        public void ConfigureDice(AugmentDiceContext context) => context.Assign(YachtDieType.Couple, 2);

        public int CalculateDiceBonus(AugmentQueryContext context, IReadOnlyList<YachtDieState> dice)
        {
            int coupleValue = -1;
            int coupleCount = 0;
            bool coupleMatches = true;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
            {
                YachtDieState die = dice[i];
                if (die.Type != YachtDieType.Couple) continue;
                coupleCount++;
                if (coupleValue < 0) coupleValue = die.Value;
                else coupleMatches &= coupleValue == die.Value;
            }
            return coupleCount == 2 && coupleMatches ? 3 : 0;
        }
    }
}
