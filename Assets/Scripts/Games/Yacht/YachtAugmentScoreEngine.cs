using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>증강 요트의 가변 주사위·눈 7·개인 변형 점수를 계산하는 순수 규칙 계층입니다.</summary>
    public static class YachtAugmentScoreEngine
    {
        public static Dictionary<ScoreCategory, int> CalculateBaseScores(
            IReadOnlyList<YachtDieState> dice,
            IReadOnlyList<string> ownedIds)
        {
            int[] counts = new int[8];
            int sum = 0;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
            {
                int value = dice[i].Value;
                if (value < 1 || value > 7) throw new ArgumentOutOfRangeException(nameof(dice), "증강 주사위 눈은 1~7이어야 합니다.");
                counts[value]++;
                sum += value;
            }

            bool allowSeven = Contains(ownedIds, YachtAugmentRuntime.SevensDiceId);
            bool four = HasCount(counts, 4);
            bool yacht = HasCount(counts, 5);
            bool fullHouse = yacht || HasFullHouse(counts);
            bool small = HasSequence(counts, 1, 4) || HasSequence(counts, 2, 5)
                || HasSequence(counts, 3, 6) || allowSeven && HasSequence(counts, 4, 7);
            bool large = HasSequence(counts, 1, 5) || HasSequence(counts, 2, 6)
                || allowSeven && HasSequence(counts, 3, 7);

            var scores = new Dictionary<ScoreCategory, int>
            {
                [ScoreCategory.Aces] = counts[1],
                [ScoreCategory.Deuces] = counts[2] * 2,
                [ScoreCategory.Threes] = counts[3] * 3,
                [ScoreCategory.Fours] = counts[4] * 4,
                [ScoreCategory.Fives] = counts[5] * 5,
                [ScoreCategory.Sixes] = counts[6] * 6,
                [ScoreCategory.Choice] = sum,
                [ScoreCategory.FourOfAKind] = four ? sum : 0,
                [ScoreCategory.FullHouse] = fullHouse ? sum : 0,
                [ScoreCategory.SmallStraight] = small ? 15 : 0,
                [ScoreCategory.LargeStraight] = large ? 30 : 0,
                [ScoreCategory.Yacht] = yacht ? 50 : 0
            };

            ApplyReplacementScores(scores, ownedIds, counts, sum, fullHouse, large);
            return scores;
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

        private static void ApplyReplacementScores(
            IDictionary<ScoreCategory, int> scores,
            IReadOnlyList<string> owned,
            IReadOnlyList<int> counts,
            int sum,
            bool fullHouse,
            bool largeStraight)
        {
            if (Contains(owned, YachtAugmentRuntime.LuckySevensId))
                scores[ScoreCategory.Aces] = sum == 7 || sum == 17 || sum == 27 ? 15 : 0;
            else if (Contains(owned, YachtAugmentRuntime.PerfectSquaresId))
                scores[ScoreCategory.Aces] = sum == 9 || sum == 16 || sum == 25 ? 12 : 0;

            if (Contains(owned, YachtAugmentRuntime.GamblerId))
                scores[ScoreCategory.Choice] = sum >= 24 ? sum + 7 : 0;

            if (Contains(owned, YachtAugmentRuntime.ThreeOfAKindId))
                scores[ScoreCategory.FourOfAKind] = HasCount(counts, 3) ? sum : 0;

            if (Contains(owned, YachtAugmentRuntime.TinyHouseId))
                scores[ScoreCategory.FullHouse] = fullHouse && counts[5] == 0 && counts[6] == 0 && counts[7] == 0 ? 28 : 0;
            else if (Contains(owned, YachtAugmentRuntime.TwoPairId))
                scores[ScoreCategory.FullHouse] = CountPairs(counts) >= 2 || HasCount(counts, 4) ? 15 : 0;
            else if (Contains(owned, YachtAugmentRuntime.HeadAndTailId))
                scores[ScoreCategory.FullHouse] = HasHeadAndTail(counts) ? sum + 10 : 0;

            if (Contains(owned, YachtAugmentRuntime.EvensId))
                scores[ScoreCategory.SmallStraight] = AllInSet(counts, 2, 4, 6) ? 20 : 0;
            else if (Contains(owned, YachtAugmentRuntime.OddsId))
                scores[ScoreCategory.SmallStraight] = AllInSet(counts, 1, 3, 5, 7) ? 20 : 0;
            else if (Contains(owned, YachtAugmentRuntime.DoubleLargeStraightId))
                scores[ScoreCategory.SmallStraight] = largeStraight ? 30 : 0;

            if (Contains(owned, YachtAugmentRuntime.PrimeCollectionId))
                scores[ScoreCategory.LargeStraight] = AllInSet(counts, 2, 3, 5, 7)
                    && counts[2] > 0 && counts[3] > 0 && counts[5] > 0 ? 35 : 0;
            else if (Contains(owned, YachtAugmentRuntime.DuplexHouseId))
                scores[ScoreCategory.LargeStraight] = HasDuplexHouse(counts) ? 35 : 0;
            else if (Contains(owned, YachtAugmentRuntime.MountainId))
                scores[ScoreCategory.LargeStraight] = IsExact(counts, 2, 3, 4, 5, 6) ? 40 : 0;
            else if (Contains(owned, YachtAugmentRuntime.HighDiceId))
                scores[ScoreCategory.LargeStraight] = AllInSet(counts, 4, 5, 6, 7) && sum >= 26 ? 35 : 0;

            if (Contains(owned, YachtAugmentRuntime.SecondChoiceId))
                scores[ScoreCategory.Yacht] = sum / 2;
            else if (Contains(owned, YachtAugmentRuntime.FibonacciId))
                scores[ScoreCategory.Yacht] = IsExact(counts, 1, 1, 2, 3, 5) ? 25 : 0;
            else if (Contains(owned, YachtAugmentRuntime.ReverseChoiceId))
                scores[ScoreCategory.Yacht] = 30 - sum;
            else if (Contains(owned, YachtAugmentRuntime.BlackjackId))
                scores[ScoreCategory.Yacht] = sum == 21 ? 21 : 0;
        }

        private static bool HasFullHouse(IReadOnlyList<int> counts)
        {
            for (int three = 1; three < counts.Count; three++)
            {
                if (counts[three] < 3) continue;
                for (int pair = 1; pair < counts.Count; pair++)
                    if (pair != three && counts[pair] >= 2) return true;
            }
            return false;
        }

        private static bool HasDuplexHouse(IReadOnlyList<int> counts)
        {
            for (int low = 1; low <= 6; low++)
            {
                int total = counts[low] + counts[low + 1];
                if (total == 5 && (counts[low] == 2 || counts[low] == 3)
                    && (counts[low + 1] == 2 || counts[low + 1] == 3)) return true;
            }
            return false;
        }

        private static bool HasHeadAndTail(IReadOnlyList<int> source)
        {
            var counts = new int[source.Count];
            for (int i = 0; i < source.Count; i++) counts[i] = source[i];
            for (int pair = 1; pair < counts.Length; pair++)
            {
                if (counts[pair] < 2) continue;
                counts[pair] -= 2;
                for (int first = 1; first <= 5; first++)
                {
                    if (counts[first] > 0 && counts[first + 1] > 0 && counts[first + 2] > 0)
                    {
                        counts[pair] += 2;
                        return true;
                    }
                }
                counts[pair] += 2;
            }
            return false;
        }

        private static int CountPairs(IReadOnlyList<int> counts)
        {
            int result = 0;
            for (int i = 1; i < counts.Count; i++) if (counts[i] >= 2) result++;
            return result;
        }

        private static bool HasCount(IReadOnlyList<int> counts, int minimum)
        {
            for (int i = 1; i < counts.Count; i++) if (counts[i] >= minimum) return true;
            return false;
        }

        private static bool HasSequence(IReadOnlyList<int> counts, int first, int last)
        {
            for (int value = first; value <= last; value++) if (counts[value] == 0) return false;
            return true;
        }

        private static bool AllInSet(IReadOnlyList<int> counts, params int[] allowed)
        {
            int total = 0;
            for (int value = 1; value < counts.Count; value++)
            {
                if (counts[value] == 0) continue;
                total += counts[value];
                bool found = false;
                for (int i = 0; i < allowed.Length; i++) found |= allowed[i] == value;
                if (!found) return false;
            }
            return total > 0;
        }

        private static bool IsExact(IReadOnlyList<int> counts, params int[] values)
        {
            var expected = new int[counts.Count];
            for (int i = 0; i < values.Length; i++) expected[values[i]]++;
            for (int i = 1; i < counts.Count; i++) if (counts[i] != expected[i]) return false;
            return true;
        }

        private static bool Contains(IReadOnlyList<string> values, string target)
        {
            for (int i = 0; i < (values?.Count ?? 0); i++)
                if (string.Equals(values[i], target, StringComparison.Ordinal)) return true;
            return false;
        }
    }
}
