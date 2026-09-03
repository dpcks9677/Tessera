using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 주사위 한 벌에서 족보 판정에 필요한 사실만 한 번 계산해 담아 둡니다.
    /// 기본 족보 계산과 변형 증강 처리기가 같은 판정을 공유합니다.
    /// </summary>
    public sealed class YachtDiceFacts
    {
        private const int MaxValue = 7;

        private readonly int[] counts;
        private readonly bool allowSevens;

        private YachtDiceFacts(int[] counts, int sum, bool allowSevens)
        {
            this.counts = counts;
            this.allowSevens = allowSevens;
            Sum = sum;
        }

        public static YachtDiceFacts From(IReadOnlyList<YachtDieState> dice, bool allowSevens)
        {
            var counts = new int[MaxValue + 1];
            int sum = 0;
            for (int i = 0; i < (dice?.Count ?? 0); i++)
            {
                int value = dice[i].Value;
                if (value < 1 || value > MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(dice), "증강 주사위 눈은 1~7이어야 합니다.");
                counts[value]++;
                sum += value;
            }
            return new YachtDiceFacts(counts, sum, allowSevens);
        }

        public int Sum { get; }

        public int CountOf(int value) => value >= 1 && value <= MaxValue ? counts[value] : 0;

        /// <summary>같은 눈이 <paramref name="minimum"/>개 이상인 눈이 있는지 여부입니다.</summary>
        public bool HasCount(int minimum)
        {
            for (int value = 1; value <= MaxValue; value++) if (counts[value] >= minimum) return true;
            return false;
        }

        /// <summary><paramref name="first"/>부터 <paramref name="last"/>까지 모든 눈이 하나 이상 있는지 여부입니다.</summary>
        public bool HasSequence(int first, int last)
        {
            for (int value = first; value <= last; value++) if (counts[value] == 0) return false;
            return true;
        }

        /// <summary>사용한 눈이 모두 <paramref name="allowed"/> 안에 들어가는지 여부입니다. 주사위가 없으면 false입니다.</summary>
        public bool AllInSet(params int[] allowed)
        {
            int total = 0;
            for (int value = 1; value <= MaxValue; value++)
            {
                if (counts[value] == 0) continue;
                total += counts[value];
                bool found = false;
                for (int i = 0; i < allowed.Length; i++) found |= allowed[i] == value;
                if (!found) return false;
            }
            return total > 0;
        }

        /// <summary>눈 구성이 <paramref name="values"/>와 정확히 같은지 여부입니다.</summary>
        public bool IsExact(params int[] values)
        {
            var expected = new int[MaxValue + 1];
            for (int i = 0; i < values.Length; i++) expected[values[i]]++;
            for (int value = 1; value <= MaxValue; value++) if (counts[value] != expected[value]) return false;
            return true;
        }

        /// <summary>두 개 이상인 눈의 종류 수입니다.</summary>
        public int PairCount
        {
            get
            {
                int result = 0;
                for (int value = 1; value <= MaxValue; value++) if (counts[value] >= 2) result++;
                return result;
            }
        }

        public bool HasYacht => HasCount(5);

        /// <summary>풀하우스 여부입니다. 공통 규칙에 따라 같은 눈 5개도 풀하우스로 봅니다.</summary>
        public bool HasFullHouse
        {
            get
            {
                if (HasYacht) return true;
                for (int three = 1; three <= MaxValue; three++)
                {
                    if (counts[three] < 3) continue;
                    for (int pair = 1; pair <= MaxValue; pair++)
                        if (pair != three && counts[pair] >= 2) return true;
                }
                return false;
            }
        }

        /// <summary>1 차이 나는 두 눈이 2+3으로 다섯 개를 채우는지 여부입니다.</summary>
        public bool HasDuplexHouse
        {
            get
            {
                for (int low = 1; low < MaxValue; low++)
                {
                    int total = counts[low] + counts[low + 1];
                    if (total == 5 && (counts[low] == 2 || counts[low] == 3)
                        && (counts[low + 1] == 2 || counts[low + 1] == 3)) return true;
                }
                return false;
            }
        }

        /// <summary>같은 눈 두 개를 뺀 나머지에서 연속한 세 눈이 나오는지 여부입니다.</summary>
        public bool HasHeadAndTail
        {
            get
            {
                var working = (int[])counts.Clone();
                for (int pair = 1; pair <= MaxValue; pair++)
                {
                    if (working[pair] < 2) continue;
                    working[pair] -= 2;
                    for (int first = 1; first <= MaxValue - 2; first++)
                    {
                        if (working[first] > 0 && working[first + 1] > 0 && working[first + 2] > 0)
                        {
                            working[pair] += 2;
                            return true;
                        }
                    }
                    working[pair] += 2;
                }
                return false;
            }
        }

        /// <summary>스몰 스트레이트 여부입니다. 세븐스 다이스를 보유하면 4~7도 인정합니다.</summary>
        public bool HasSmallStraight =>
            HasSequence(1, 4) || HasSequence(2, 5) || HasSequence(3, 6) || allowSevens && HasSequence(4, 7);

        /// <summary>라지 스트레이트 여부입니다. 세븐스 다이스를 보유하면 3~7도 인정합니다.</summary>
        public bool HasLargeStraight =>
            HasSequence(1, 5) || HasSequence(2, 6) || allowSevens && HasSequence(3, 7);
    }
}
