using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 증강 요트의 가변 주사위와 눈 7을 반영한 기본 족보 점수를 계산하는 순수 규칙 계층입니다.
    /// 족보를 교체하는 변형 증강은 <see cref="IBeforeScorePreview"/> 처리기가 담당합니다.
    /// </summary>
    public static class YachtAugmentScoreEngine
    {
        /// <summary>보유 증강에 따라 눈 7 허용 여부를 정한 뒤 족보 판정 결과를 만듭니다.</summary>
        public static YachtDiceFacts CreateFacts(
            IReadOnlyList<YachtDieState> dice,
            IReadOnlyList<string> ownedIds) =>
            YachtDiceFacts.From(dice, Contains(ownedIds, YachtAugmentRuntime.SevensDiceId));

        public static Dictionary<ScoreCategory, int> CalculateBaseScores(YachtDiceFacts facts)
        {
            return new Dictionary<ScoreCategory, int>
            {
                [ScoreCategory.Aces] = facts.CountOf(1),
                [ScoreCategory.Deuces] = facts.CountOf(2) * 2,
                [ScoreCategory.Threes] = facts.CountOf(3) * 3,
                [ScoreCategory.Fours] = facts.CountOf(4) * 4,
                [ScoreCategory.Fives] = facts.CountOf(5) * 5,
                [ScoreCategory.Sixes] = facts.CountOf(6) * 6,
                [ScoreCategory.Choice] = facts.Sum,
                [ScoreCategory.FourOfAKind] = facts.HasCount(4) ? facts.Sum : 0,
                [ScoreCategory.FullHouse] = facts.HasFullHouse ? facts.Sum : 0,
                [ScoreCategory.SmallStraight] = facts.HasSmallStraight ? 15 : 0,
                [ScoreCategory.LargeStraight] = facts.HasLargeStraight ? 30 : 0,
                [ScoreCategory.Yacht] = facts.HasYacht ? 50 : 0
            };
        }

        public static int CalculateDiceBonus(IReadOnlyList<YachtDieState> dice)
        {
            int bonus = 0;
            int coupleValue = -1;
            int coupleCount = 0;
            bool coupleMatches = true;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
            {
                YachtDieState die = dice[i];
                if (die.Type == YachtDieType.Golden && die.Value >= 1 && die.Value <= 3) bonus += 2;
                if (die.Type != YachtDieType.Couple) continue;
                coupleCount++;
                if (coupleValue < 0) coupleValue = die.Value;
                else coupleMatches &= coupleValue == die.Value;
            }
            if (coupleCount == 2 && coupleMatches) bonus += 3;
            return bonus;
        }

        private static bool Contains(IReadOnlyList<string> values, string target)
        {
            for (int i = 0; i < (values?.Count ?? 0); i++)
                if (string.Equals(values[i], target, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
