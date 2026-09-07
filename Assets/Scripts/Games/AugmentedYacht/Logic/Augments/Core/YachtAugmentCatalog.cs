using System;
using System.Collections.Generic;

namespace Tessera.Games.Yacht
{
    /// <summary>
    /// 활성 증강 처리기의 등록 목록입니다. 리플렉션으로 찾지 않고 명시적으로 나열해
    /// IL2CPP/AOT 빌드에서도 동작을 보장합니다. 증강을 추가할 때는 처리기 파일 하나를
    /// 만들고 아래 배열에 한 줄을 더합니다.
    /// </summary>
    public static class YachtAugmentCatalog
    {
        private static readonly IAugmentHandler[] Handlers =
        {
            // 변형 18종. 등록 순서가 드래프트 후보와 정의 목록의 노출 순서가 됩니다.
            new LuckySevens(),
            new PerfectSquares(),
            new Gambler(),
            new ThreeOfAKind(),
            new TinyHouse(),
            new TwoPair(),
            new HeadAndTail(),
            new Evens(),
            new Odds(),
            new DoubleLargeStraight(),
            new PrimeCollection(),
            new DuplexHouse(),
            new Mountain(),
            new HighDice(),
            new SecondChoice(),
            new FibonacciNumbers(),
            new ReverseChoice(),
            new Blackjack21(),

            // 강화 11종 (주사위 6 + 상시/특수 5)
            new WeightedDice(),
            new GoldenDie(),
            new Octahedron(),
            new PromotionDie(),
            new CoupleDice(),
            new SevensDice(),
            new YachtBank(),
            new Momentum(),
            new Duel(),
            new PiggyBank(),
            new RandomBox(),

            // 퀘스트 11종
            new NoTimeToWaste(),
            new BountyHunter(),
            new StepByStep(),
            new FastStraight(),
            new Holdout(),
            new CautiousStraight(),
            new EveryLittleCounts(),
            new Copycat(),
            new Prophet(),
            new Doubling(),
            new Nozdormu(),

            // 수동 행동 5종
            new TableFlip(),
            new EquivalentExchange(),
            new Gambit(),
            new DoubleDown(),
            new DiceAlchemy()
        };

        public static IReadOnlyList<IAugmentHandler> All => Handlers;

        public static IAugmentHandler Find(string augmentId)
        {
            for (int i = 0; i < Handlers.Length; i++)
                if (string.Equals(Handlers[i].Id, augmentId, StringComparison.Ordinal)) return Handlers[i];
            return null;
        }

        /// <summary>등록 목록의 순서를 반환합니다. 같은 <c>Order</c>일 때의 정렬 기준입니다.</summary>
        internal static int IndexOf(IAugmentHandler handler)
        {
            for (int i = 0; i < Handlers.Length; i++)
                if (ReferenceEquals(Handlers[i], handler)) return i;
            return int.MaxValue;
        }
    }
}
